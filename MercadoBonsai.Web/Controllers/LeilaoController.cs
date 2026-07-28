using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MercadoBonsai.Domain.Entities;
using MercadoBonsai.Domain.Enums;
using MercadoBonsai.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace MercadoBonsai.Web.Controllers;

public class LeilaoController : Controller
{
    private readonly ILeilaoRepository _leilaoRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPlanoRepository _planoRepository;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public LeilaoController(
        ILeilaoRepository leilaoRepository,
        IUsuarioRepository usuarioRepository,
        IPlanoRepository planoRepository,
        IWebHostEnvironment webHostEnvironment)
    {
        _leilaoRepository = leilaoRepository;
        _usuarioRepository = usuarioRepository;
        _planoRepository = planoRepository;
        _webHostEnvironment = webHostEnvironment;
    }

    // GET: /Leilao/Encerrados (Consulta pública de Leilões Encerrados/Arrematados)
    [HttpGet]
    public async Task<IActionResult> Encerrados()
    {
        var leiloesEncerrados = await _leilaoRepository.ListarEncerradosAsync();
        return View(leiloesEncerrados);
    }

    // GET: /Leilao/MeusLeiloes (Gestão do Vendedor)
    [HttpGet]
    [Authorize(Roles = "Vendedor, Administrador")]
    public async Task<IActionResult> MeusLeiloes()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int vendedorId))
        {
            return Unauthorized();
        }

        var leiloes = await _leilaoRepository.ListarPorVendedorAsync(vendedorId);
        return View(leiloes);
    }

    // GET: /Leilao/Criar
    [HttpGet]
    [Authorize(Roles = "Vendedor, Administrador")]
    public async Task<IActionResult> Criar()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int vendedorId))
        {
            return Unauthorized();
        }

        var usuario = await _usuarioRepository.ObterPorIdAsync(vendedorId);
        var plano = await _planoRepository.ObterPorIdAsync(usuario?.PlanoId ?? 1);
        var leiloesCriados30Dias = await _leilaoRepository.ContarPorVendedorNosUltimos30DiasAsync(vendedorId);

        if (plano != null && leiloesCriados30Dias >= plano.LimiteLeiloes30Dias && !User.IsInRole("Administrador"))
        {
            TempData["Erro"] = $"Você atingiu o limite de {plano.LimiteLeiloes30Dias} leilões a cada 30 dias do seu Plano {plano.Nome}. Faça o upgrade de assinatura para continuar!";
            return RedirectToAction("MeusLeiloes");
        }

        var leilao = new Leilao
        {
            DataFinalizacao = DateTime.UtcNow.AddDays(7),
            IncrementoMinimo = 50.00m
        };

        return View(leilao);
    }

    // POST: /Leilao/Criar
    [HttpPost]
    [Authorize(Roles = "Vendedor, Administrador")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(Leilao leilao, Microsoft.AspNetCore.Http.IFormFile? fotoPrincipal, Microsoft.AspNetCore.Http.IFormFile? fotoDetalhe)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int vendedorId))
        {
            return Unauthorized();
        }

        var usuario = await _usuarioRepository.ObterPorIdAsync(vendedorId);
        var plano = await _planoRepository.ObterPorIdAsync(usuario?.PlanoId ?? 1);
        var leiloesCriados30Dias = await _leilaoRepository.ContarPorVendedorNosUltimos30DiasAsync(vendedorId);

        if (plano != null && leiloesCriados30Dias >= plano.LimiteLeiloes30Dias && !User.IsInRole("Administrador"))
        {
            ModelState.AddModelError(string.Empty, $"Limite atingido: Seu Plano {plano.Nome} permite no máximo {plano.LimiteLeiloes30Dias} leilões a cada 30 dias.");
            return View(leilao);
        }

        if (string.IsNullOrWhiteSpace(leilao.Titulo))
        {
            ModelState.AddModelError("Titulo", "Informe o título do leilão.");
            return View(leilao);
        }

        if (fotoPrincipal != null && fotoPrincipal.Length > 0)
        {
            var folder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "leiloes");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            var ext = Path.GetExtension(fotoPrincipal.FileName);
            var name = $"leilao_{vendedorId}_{Guid.NewGuid()}{ext}";
            var path = Path.Combine(folder, name);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await fotoPrincipal.CopyToAsync(stream);
            }
            leilao.FotoPrincipalUrl = $"/uploads/leiloes/{name}";
        }

        if (fotoDetalhe != null && fotoDetalhe.Length > 0)
        {
            var folder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "leiloes");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            var ext = Path.GetExtension(fotoDetalhe.FileName);
            var name = $"leilao_detalhe_{vendedorId}_{Guid.NewGuid()}{ext}";
            var path = Path.Combine(folder, name);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await fotoDetalhe.CopyToAsync(stream);
            }
            leilao.FotoDetalheUrl = $"/uploads/leiloes/{name}";
        }

        leilao.VendedorId = vendedorId;
        leilao.VendedorNome = usuario?.RazaoSocial ?? usuario?.Nome ?? User.Identity?.Name;
        leilao.ProximoLanceMinimo = leilao.LanceAtual + leilao.IncrementoMinimo;
        leilao.DataCriacao = DateTime.UtcNow;

        await _leilaoRepository.InserirAsync(leilao);

        TempData["Sucesso"] = "Leilão cadastrado com sucesso!";
        return RedirectToAction("MeusLeiloes");
    }

    // GET: /Leilao/Editar/{id}
    [HttpGet]
    [Authorize(Roles = "Vendedor, Administrador")]
    public async Task<IActionResult> Editar(int id)
    {
        var leilao = await _leilaoRepository.ObterPorIdAsync(id);
        if (leilao == null) return NotFound();

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!User.IsInRole("Administrador") && (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int vendedorId) || leilao.VendedorId != vendedorId))
        {
            return Forbid();
        }

        return View(leilao);
    }

    // POST: /Leilao/Editar/{id}
    [HttpPost]
    [Authorize(Roles = "Vendedor, Administrador")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, Leilao model)
    {
        var leilao = await _leilaoRepository.ObterPorIdAsync(id);
        if (leilao == null) return NotFound();

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!User.IsInRole("Administrador") && (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int vendedorId) || leilao.VendedorId != vendedorId))
        {
            return Forbid();
        }

        // Regra de Negócio: Se o leilão já foi iniciado e possui lances registrados, bloqueia alteração de informações gerais
        if (!leilao.PodeEditarDadosGerais)
        {
            // Permite APENAS a prorrogação da data de término
            if (model.DataFinalizacao < leilao.DataFinalizacao)
            {
                ModelState.AddModelError("DataFinalizacao", "Não é permitido antecipar o término de um leilão em andamento com lances. Apenas prorrogações são aceitas.");
                return View(leilao);
            }

            leilao.DataFinalizacao = model.DataFinalizacao;
            leilao.Status = model.Status;
            await _leilaoRepository.AtualizarAsync(leilao);

            TempData["Sucesso"] = "Prorrogação de término do leilão realizada com sucesso!";
            return RedirectToAction("MeusLeiloes");
        }

        leilao.Titulo = model.Titulo;
        leilao.Subtitulo = model.Subtitulo;
        leilao.Descricao = model.Descricao;
        leilao.Badge = model.Badge;
        leilao.LanceAtual = model.LanceAtual;
        leilao.IncrementoMinimo = model.IncrementoMinimo;
        leilao.ProximoLanceMinimo = leilao.LanceAtual + leilao.IncrementoMinimo;
        leilao.DataFinalizacao = model.DataFinalizacao;
        leilao.Status = model.Status;

        await _leilaoRepository.AtualizarAsync(leilao);

        TempData["Sucesso"] = "Leilão alterado com sucesso!";
        return RedirectToAction("MeusLeiloes");
    }
}
