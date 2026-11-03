using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MercadoBonsai.Domain.Entities;
using MercadoBonsai.Domain.Enums;
using MercadoBonsai.Domain.Interfaces;
using MercadoBonsai.Web.Models;
using MercadoBonsai.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MercadoBonsai.Web.Controllers;

public class ProdutoController : Controller
{
    private readonly IProdutoRepository _produtoRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPlanoRepository _planoRepository;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IStorageService _storageService;

    public ProdutoController(
        IProdutoRepository produtoRepository, 
        IUsuarioRepository usuarioRepository,
        IPlanoRepository planoRepository,
        IWebHostEnvironment webHostEnvironment,
        IStorageService storageService)
    {
        _produtoRepository = produtoRepository;
        _usuarioRepository = usuarioRepository;
        _planoRepository = planoRepository;
        _webHostEnvironment = webHostEnvironment;
        _storageService = storageService;
    }

    private async Task<(int limiteFotos, string planoNome, int limiteAnuncios)> ObterLimitesPlanoVendedorAsync(int vendedorId)
    {
        var vendedor = await _usuarioRepository.ObterPorIdAsync(vendedorId);
        var planoId = vendedor?.PlanoId > 0 ? vendedor.PlanoId : 1;
        var plano = await _planoRepository.ObterPorIdAsync(planoId);

        int limiteFotos = plano?.LimiteFotos > 0 ? plano.LimiteFotos : 3;
        string planoNome = plano?.Nome ?? "Bronze";
        int limiteAnuncios = plano?.LimiteAnuncios > 0 ? plano.LimiteAnuncios : 5;

        return (limiteFotos, planoNome, limiteAnuncios);
    }

    // GET: /Produto (Vitrine de Plantas: apenas Pré-bonsai e Bonsai)
    [HttpGet]
    public async Task<IActionResult> Index(string? busca, string? ordem, int? vendedorId)
    {
        var produtos = await _produtoRepository.ListarPorCategoriasAsync("pré-bonsai", "bonsai");

        // Filtro direto por Vendedor Id
        if (vendedorId.HasValue && vendedorId.Value > 0)
        {
            produtos = produtos.Where(p => p.VendedorId == vendedorId.Value);
            var vendedorFiltro = await _usuarioRepository.ObterPorIdAsync(vendedorId.Value);
            if (vendedorFiltro != null)
            {
                ViewData["VendedorFiltroNome"] = !string.IsNullOrWhiteSpace(vendedorFiltro.RazaoSocial) ? vendedorFiltro.RazaoSocial : vendedorFiltro.Nome;
            }
        }

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim().ToLower();

            // Encontra vendedores cujo nome, razão social ou e-mail correspondam ao termo
            var vendedoresCorrespondentes = (await _usuarioRepository.ListarTodosAsync(termo, null))
                .Select(u => u.Id)
                .ToHashSet();

            produtos = produtos.Where(p => 
                p.Nome.ToLower().Contains(termo) || 
                (p.Descricao != null && p.Descricao.ToLower().Contains(termo)) ||
                vendedoresCorrespondentes.Contains(p.VendedorId));
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
        ViewData["VendedorId"] = vendedorId;

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
            && !string.IsNullOrWhiteSpace(vendedor.CpfCnpj)
            && !string.IsNullOrWhiteSpace(vendedor.Cep);

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
    public async Task<IActionResult> Criar()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        int limiteFotos = 3;
        string planoNome = "Bronze";

        if (int.TryParse(userIdClaim, out int vendedorId))
        {
            var vendedor = await _usuarioRepository.ObterPorIdAsync(vendedorId);
            if (vendedor != null && string.IsNullOrEmpty(vendedor.AsaasAccountId) && !vendedor.EhContaPrincipalMaster)
            {
                TempData["Erro"] = "Sua conta de Vendedor ainda não possui a Subconta Asaas ativada. Por favor, preencha seus dados cadastrais e fiscais no 'Meu Perfil' para habilitar publicações de anúncios.";
                return RedirectToAction("MeuPerfil", "Conta");
            }
            if (vendedor != null && !vendedor.TemAssinaturaValida)
            {
                TempData["Erro"] = "Para publicar novos anúncios é necessário ter uma assinatura ativa com plano regularizado. Por favor, regularize sua assinatura.";
                return RedirectToAction("Assinatura", "Conta");
            }

            (limiteFotos, planoNome, _) = await ObterLimitesPlanoVendedorAsync(vendedorId);
        }

        ViewBag.LimiteFotos = limiteFotos;
        ViewBag.PlanoNome = planoNome;

        return View(new CriarProdutoViewModel { FormaEnvio = "Frete por conta comprador" });
    }

    // POST: /Produto/Criar
    [HttpPost]
    [Authorize(Roles = "Vendedor, Administrador")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(52428800)]
    [RequestFormLimits(MultipartBodyLengthLimit = 52428800)]
    public async Task<IActionResult> Criar(CriarProdutoViewModel model)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int vendedorId))
        {
            return Unauthorized();
        }

        var vendedorObj = await _usuarioRepository.ObterPorIdAsync(vendedorId);
        if (vendedorObj != null && string.IsNullOrEmpty(vendedorObj.AsaasAccountId) && !vendedorObj.EhContaPrincipalMaster)
        {
            TempData["Erro"] = "Sua conta de Vendedor ainda não possui a Subconta Asaas ativada. Por favor, preencha seus dados cadastrais e fiscais no 'Meu Perfil' para habilitar publicações de anúncios.";
            return RedirectToAction("MeuPerfil", "Conta");
        }
        if (vendedorObj != null && !vendedorObj.TemAssinaturaValida)
        {
            TempData["Erro"] = "Para publicar novos anúncios é necessário ter uma assinatura ativa com plano regularizado. Por favor, regularize sua assinatura.";
            return RedirectToAction("Assinatura", "Conta");
        }

        var (limiteFotos, planoNome, _) = await ObterLimitesPlanoVendedorAsync(vendedorId);
        ViewBag.LimiteFotos = limiteFotos;
        ViewBag.PlanoNome = planoNome;

        var arquivosEnviados = new List<IFormFile>();
        if (model.Imagens != null && model.Imagens.Any())
        {
            arquivosEnviados.AddRange(model.Imagens.Where(f => f != null && f.Length > 0));
        }
        if (model.Imagem != null && model.Imagem.Length > 0 && !arquivosEnviados.Contains(model.Imagem))
        {
            arquivosEnviados.Insert(0, model.Imagem);
        }

        if (arquivosEnviados.Count > limiteFotos)
        {
            ModelState.AddModelError("Imagens", $"Seu Plano {planoNome} permite até {limiteFotos} fotos por produto (você selecionou {arquivosEnviados.Count}). Remova algumas fotos ou faça upgrade do seu plano.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var urlsSalvas = new List<string>();
        foreach (var arq in arquivosEnviados.Take(limiteFotos))
        {
            var url = await _storageService.UploadImagemOtimizadaAsync(arq, "produtos", $"prod_{vendedorId}");
            if (!string.IsNullOrWhiteSpace(url))
            {
                urlsSalvas.Add(url);
            }
        }

        var produto = new Produto
        {
            VendedorId = vendedorId,
            Nome = model.Nome,
            Descricao = model.Descricao ?? string.Empty,
            Preco = model.Preco,
            PrecoCusto = model.PrecoCusto,
            QuantidadeEstoque = model.QuantidadeEstoque,
            Status = model.Status,
            Altura = model.Altura,
            Largura = model.Largura,
            Comprimento = model.Comprimento,
            Peso = model.Peso,
            FormaEnvio = string.IsNullOrWhiteSpace(model.FormaEnvio) ? "Frete por conta comprador" : model.FormaEnvio,
            Categoria = model.Categoria,
            ImagemUrl = urlsSalvas.FirstOrDefault() ?? string.Empty,
            ImagensAdicionais = urlsSalvas.Count > 1 ? string.Join(";", urlsSalvas.Skip(1)) : null,
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

        var (limiteFotos, planoNome, _) = await ObterLimitesPlanoVendedorAsync(produto.VendedorId);
        ViewBag.LimiteFotos = limiteFotos;
        ViewBag.PlanoNome = planoNome;

        var model = new EditarProdutoViewModel
        {
            Id = produto.Id,
            Nome = produto.Nome,
            Descricao = produto.Descricao,
            Preco = produto.Preco,
            PrecoCusto = produto.PrecoCusto,
            QuantidadeEstoque = produto.QuantidadeEstoque,
            Status = produto.Status,
            Altura = produto.Altura,
            Largura = produto.Largura,
            Comprimento = produto.Comprimento,
            Peso = produto.Peso,
            FormaEnvio = string.IsNullOrWhiteSpace(produto.FormaEnvio) ? "Frete por conta comprador" : produto.FormaEnvio,
            Categoria = produto.Categoria,
            ImagemUrlAtual = produto.ImagemUrl,
            ImagensAdicionaisAtuais = produto.ImagensAdicionais,
            ImagensManterOrdem = string.Join(";", produto.TodasImagens)
        };

        return View(model);
    }

    // POST: /Produto/Editar/{id}
    [HttpPost]
    [Authorize(Roles = "Vendedor, Administrador")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(52428800)]
    [RequestFormLimits(MultipartBodyLengthLimit = 52428800)]
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

        var (limiteFotos, planoNome, _) = await ObterLimitesPlanoVendedorAsync(produto.VendedorId);
        ViewBag.LimiteFotos = limiteFotos;
        ViewBag.PlanoNome = planoNome;

        // Lista de fotos existentes que o usuário manteve (e na ordem escolhida)
        List<string> fotosFinais;
        if (model.ImagensManterOrdem != null)
        {
            fotosFinais = model.ImagensManterOrdem
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct()
                .ToList();
        }
        else
        {
            fotosFinais = produto.TodasImagens;
        }

        var novosArquivos = new List<IFormFile>();
        if (model.NovasImagens != null && model.NovasImagens.Any())
        {
            novosArquivos.AddRange(model.NovasImagens.Where(f => f != null && f.Length > 0));
        }
        if (model.NovaImagem != null && model.NovaImagem.Length > 0 && !novosArquivos.Contains(model.NovaImagem))
        {
            novosArquivos.Add(model.NovaImagem);
        }

        if (fotosFinais.Count + novosArquivos.Count > limiteFotos)
        {
            ModelState.AddModelError("NovasImagens", $"Seu Plano {planoNome} permite até {limiteFotos} fotos por produto. Você possui {fotosFinais.Count} foto(s) mantida(s) e tentou adicionar {novosArquivos.Count} nova(s).");
        }

        if (!ModelState.IsValid)
        {
            model.ImagemUrlAtual = fotosFinais.FirstOrDefault() ?? string.Empty;
            model.ImagensAdicionaisAtuais = fotosFinais.Count > 1 ? string.Join(";", fotosFinais.Skip(1)) : null;
            return View(model);
        }

        foreach (var arq in novosArquivos)
        {
            if (fotosFinais.Count >= limiteFotos) break;
            var url = await _storageService.UploadImagemOtimizadaAsync(arq, "produtos", $"prod_{produto.VendedorId}");
            if (!string.IsNullOrWhiteSpace(url))
            {
                fotosFinais.Add(url);
            }
        }

        produto.ImagemUrl = fotosFinais.FirstOrDefault() ?? string.Empty;
        produto.ImagensAdicionais = fotosFinais.Count > 1 ? string.Join(";", fotosFinais.Skip(1)) : null;

        produto.Nome = model.Nome;
        produto.Descricao = model.Descricao ?? string.Empty;
        produto.Preco = model.Preco;
        produto.PrecoCusto = model.PrecoCusto;
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
