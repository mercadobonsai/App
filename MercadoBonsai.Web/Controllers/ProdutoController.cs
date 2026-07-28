using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MercadoBonsai.Domain.Entities;
using MercadoBonsai.Domain.Interfaces;
using MercadoBonsai.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace MercadoBonsai.Web.Controllers;

public class ProdutoController : Controller
{
    private readonly IProdutoRepository _produtoRepository;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public ProdutoController(IProdutoRepository produtoRepository, IWebHostEnvironment webHostEnvironment)
    {
        _produtoRepository = produtoRepository;
        _webHostEnvironment = webHostEnvironment;
    }

    // GET: /Produto (Catálogo Completo / Vitrine de Busca com Filtros)
    [HttpGet]
    public async Task<IActionResult> Index(string? busca, string? ordem)
    {
        var produtos = await _produtoRepository.ListarTodosAsync();

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
        return View(produto);
    }

    // GET: /Produto/Criar
    [HttpGet]
    [Authorize(Roles = "Vendedor, Administrador")]
    public IActionResult Criar()
    {
        return View();
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
            ImagemUrl = imagemUrl ?? string.Empty,
            DataCriacao = DateTime.UtcNow
        };

        await _produtoRepository.InserirAsync(produto);

        TempData["Sucesso"] = "Produto / Bonsai cadastrado com sucesso!";
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

        // Upload de nova imagem se fornecido
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

        await _produtoRepository.AtualizarAsync(produto);

        TempData["Sucesso"] = $"Anúncio '{produto.Nome}' alterado com sucesso!";

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
