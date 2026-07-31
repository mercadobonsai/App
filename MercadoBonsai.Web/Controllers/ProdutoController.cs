using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MercadoBonsai.Domain.Entities;
using MercadoBonsai.Domain.Enums;
using MercadoBonsai.Domain.Interfaces;
using MercadoBonsai.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace MercadoBonsai.Web.Controllers;

public class ProdutoController : Controller
{
    private readonly IProdutoRepository _produtoRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public ProdutoController(
        IProdutoRepository produtoRepository, 
        IUsuarioRepository usuarioRepository,
        IWebHostEnvironment webHostEnvironment)
    {
        _produtoRepository = produtoRepository;
        _usuarioRepository = usuarioRepository;
        _webHostEnvironment = webHostEnvironment;
    }

    // GET: /Produto (Vitrine de Plantas: apenas Pré-bonsai e Bonsai)
    [HttpGet]
    public async Task<IActionResult> Index(string? busca, string? ordem)
    {
        var produtos = await _produtoRepository.ListarPorCategoriasAsync("pré-bonsai", "bonsai");

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim().ToLower();
            produtos = produtos.Where(p => 
                p.Nome.ToLower().Contains(termo) || 
                (p.Descricao != null && p.Descricao.ToLower().Contains(termo)));
        }

        produtos = ordem switch
        {
            "preco_asc" => produtos.OrderBy(p => p.Preco),
            "preco_desc" => produtos.OrderByDescending(p => p.Preco),
            "nome" => produtos.OrderBy(p => p.Nome),
            _ => produtos.OrderByDescending(p => p.DataCriacao)
        };

        ViewData["Busca"] = busca;
        ViewData["Ordem"] = ordem;

        return View(produtos);
    }

    // GET: /Produto/Detalhes/{id}
    [HttpGet]
    public async Task<IActionResult> Detalhes(int id)
    {
        var produto = await _produtoRepository.ObterPorIdAsync(id);
        if (produto == null)
        {
            return NotFound();
        }

        // Regra de Negócio: Bloqueio de acesso para produtos Indisponíveis/Vendidos se não for o proprietário ou Admin
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        int.TryParse(userIdClaim, out int currentUserId);
        bool isOwnerOrAdmin = User.IsInRole("Administrador") || (currentUserId > 0 && produto.VendedorId == currentUserId);

        if ((produto.Status == StatusProduto.Indisponivel || produto.Status == StatusProduto.Vendido) && !isOwnerOrAdmin)
        {
            TempData["Erro"] = "Este produto está indisponível para visualização pública.";
            return RedirectToAction("Index");
        }

        // Obtém o vendedor responsável para validações prévias do frete Melhor Envio
        var vendedor = await _usuarioRepository.ObterPorIdAsync(produto.VendedorId);
        bool vendedorValido = vendedor != null 
            && !string.IsNullOrWhiteSpace(vendedor.Telefone) 
            && !string.IsNullOrWhiteSpace(vendedor.CpfCnpj);

        bool especificacoesCompletas = produto.Altura > 0 
            && produto.Largura > 0 
            && produto.Comprimento > 0 
            && produto.Peso > 0;

        ViewData["VendedorValido"] = vendedorValido;
        ViewData["EspecificacoesCompletas"] = especificacoesCompletas;
        ViewData["VendedorNome"] = vendedor?.Nome ?? "Vendedor";

        return View(produto);
    }

    // GET: /Produto/Criar
    [HttpGet]
    [Authorize(Roles = "Vendedor, Administrador")]
    public IActionResult Criar()
    {
        return View(new CriarProdutoViewModel { FormaEnvio = "Frete por conta comprador" });
    }

    // POST: /Produto/Criar
    [HttpPost]
    [Authorize(Roles = "Vendedor, Administrador")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(CriarProdutoViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        string? imagemUrl = null;

        if (model.Imagem != null && model.Imagem.Length > 0)
        {
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "produtos");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var extension = Path.GetExtension(model.Imagem.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await model.Imagem.CopyToAsync(stream);
            }

            imagemUrl = $"/uploads/produtos/{uniqueFileName}";
        }

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int vendedorId))
        {
            return Unauthorized();
        }

        var produto = new Produto
        {
            VendedorId = vendedorId,
            Nome = model.Nome,
            Descricao = model.Descricao ?? string.Empty,
            Preco = model.Preco,
            QuantidadeEstoque = model.QuantidadeEstoque,
            Status = model.Status,
            Altura = model.Altura,
            Largura = model.Largura,
            Comprimento = model.Comprimento,
            Peso = model.Peso,
            FormaEnvio = string.IsNullOrWhiteSpace(model.FormaEnvio) ? "Frete por conta comprador" : model.FormaEnvio,
            Categoria = model.Categoria,
            ImagemUrl = imagemUrl ?? string.Empty,
            DataCriacao = DateTime.UtcNow
        };

        await _produtoRepository.InserirAsync(produto);

        TempData["Sucesso"] = "Produto cadastrado com sucesso!";
        return RedirectToAction("MeusProdutos");
    }

    // GET: /Produto/Editar/{id}
    [HttpGet]
    [Authorize(Roles = "Vendedor, Administrador")]
    public async Task<IActionResult> Editar(int id)
    {
        var produto = await _produtoRepository.ObterPorIdAsync(id);
        if (produto == null)
        {
            return NotFound();
        }

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!User.IsInRole("Administrador") && (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int vendedorId) || produto.VendedorId != vendedorId))
        {
            return Forbid();
        }

        var model = new EditarProdutoViewModel
        {
            Id = produto.Id,
            Nome = produto.Nome,
            Descricao = produto.Descricao,
            Preco = produto.Preco,
            QuantidadeEstoque = produto.QuantidadeEstoque,
            Status = produto.Status,
            Altura = produto.Altura,
            Largura = produto.Largura,
            Comprimento = produto.Comprimento,
            Peso = produto.Peso,
            FormaEnvio = string.IsNullOrWhiteSpace(produto.FormaEnvio) ? "Frete por conta comprador" : produto.FormaEnvio,
            Categoria = produto.Categoria,
            ImagemUrlAtual = produto.ImagemUrl
        };

        return View(model);
    }

    // POST: /Produto/Editar/{id}
    [HttpPost]
    [Authorize(Roles = "Vendedor, Administrador")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, EditarProdutoViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var produto = await _produtoRepository.ObterPorIdAsync(id);
        if (produto == null)
        {
            return NotFound();
        }

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!User.IsInRole("Administrador") && (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int vendedorId) || produto.VendedorId != vendedorId))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (model.NovaImagem != null && model.NovaImagem.Length > 0)
        {
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "produtos");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var extension = Path.GetExtension(model.NovaImagem.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await model.NovaImagem.CopyToAsync(stream);
            }

            produto.ImagemUrl = $"/uploads/produtos/{uniqueFileName}";
        }

        produto.Nome = model.Nome;
        produto.Descricao = model.Descricao ?? string.Empty;
        produto.Preco = model.Preco;
        produto.QuantidadeEstoque = model.QuantidadeEstoque;
        produto.Status = model.Status;
        produto.Altura = model.Altura;
        produto.Largura = model.Largura;
        produto.Comprimento = model.Comprimento;
        produto.Peso = model.Peso;
        produto.FormaEnvio = string.IsNullOrWhiteSpace(model.FormaEnvio) ? "Frete por conta comprador" : model.FormaEnvio;
        produto.Categoria = model.Categoria;

        await _produtoRepository.AtualizarAsync(produto);

        TempData["Sucesso"] = $"Produto '{produto.Nome}' alterado com sucesso!";

        if (User.IsInRole("Administrador"))
        {
            return RedirectToAction("Index");
        }
        return RedirectToAction("MeusProdutos");
    }

    // GET: /Produto/MeusProdutos
    [HttpGet]
    [Authorize(Roles = "Vendedor, Administrador")]
    public async Task<IActionResult> MeusProdutos()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int vendedorId))
        {
            return Unauthorized();
        }

        var meusProdutos = await _produtoRepository.ListarPorVendedorAsync(vendedorId);
        return View(meusProdutos);
    }
}
