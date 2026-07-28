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
    private readonly IRifaRepository _rifaRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPlanoRepository _planoRepository;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public LeilaoController(
        ILeilaoRepository leilaoRepository,
        IRifaRepository rifaRepository,
        IUsuarioRepository usuarioRepository,
        IPlanoRepository planoRepository,
        IWebHostEnvironment webHostEnvironment)
    {
        _leilaoRepository = leilaoRepository;
        _rifaRepository = rifaRepository;
        _usuarioRepository = usuarioRepository;
        _planoRepository = planoRepository;
        _webHostEnvironment = webHostEnvironment;
    }

    // GET: /Leilao/Encerrados (Consulta de leilões e ações entre amigos, em andamento ou encerrados)
    [HttpGet]
    public async Task<IActionResult> Encerrados()
    {
        var leiloes = await _leilaoRepository.ListarEncerradosAsync();
        var rifaAtiva = await _rifaRepository.ObterRifaAtivaRecenteAsync();
        var leilaoAtivo = await _leilaoRepository.ObterLeilaoAtivoRecenteAsync();

        ViewData["RifaAtiva"] = rifaAtiva;
        ViewData["LeilaoAtivo"] = leilaoAtivo;

        return View(leiloes);
    }

    // POST: /Leilao/RegistrarLance (Fluxo Completo de Registro e Persistência no Banco)
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> RegistrarLance(int leilaoId, decimal valorLance)
    {
        var leilao = await _leilaoRepository.ObterPorIdAsync(leilaoId);
        if (leilao == null)
        {
            return Json(new { sucesso = false, mensagem = "Leilão não encontrado." });
        }

        // Validação Crítica 1: Garantir que o leilão esteja com o status Iniciado (StatusLeilao.Iniciado = 2)
        if (leilao.Status != StatusLeilao.Iniciado)
        {
            return Json(new { sucesso = false, mensagem = "Lances bloqueados: Este leilão não está atualmente com o status 'Iniciado / Ao Vivo'." });
        }

        // Validação de Término do Leilão
        if (leilao.DataFinalizacao < DateTime.UtcNow)
        {
            return Json(new { sucesso = false, mensagem = "Leilão encerrado! O prazo limite para recebimento de lances foi atingido." });
        }

        // Validação Crítica 2: Validar se o novo lance é estritamente superior ao lance atual
        var lanceMinimoExigido = leilao.LanceAtual + leilao.IncrementoMinimo;
        if (valorLance <= leilao.LanceAtual || valorLance < lanceMinimoExigido)
        {
            return Json(new { 
                sucesso = false, 
                mensagem = $"O valor ofertado (R$ {valorLance:N2}) deve ser estritamente superior ao lance atual. Mínimo exigido: R$ {lanceMinimoExigido:N2}." 
            });
        }

        // Obter comprador logado e timestamp exato
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        int.TryParse(userIdClaim, out int compradorId);
        var compradorNome = User.Identity?.Name ?? "Comprador Mercado Bonsai";

        var novoLance = new LanceLeilao
        {
            LeilaoId = leilao.Id,
            UsuarioId = compradorId,
            UsuarioNome = compradorNome,
            Valor = valorLance,
            DataLance = DateTime.UtcNow
        };

        // 1. Gravar novo lance na tabela lancesleilao
        await _leilaoRepository.InserirLanceAsync(novoLance);

        // 2. Atualizar valores do leilão no banco de dados
        leilao.LanceAtual = valorLance;
        leilao.ProximoLanceMinimo = valorLance + leilao.IncrementoMinimo;
        await _leilaoRepository.AtualizarAsync(leilao);

        // Obter leilão atualizado com a lista de lances persistidos
        var leilaoAtualizado = await _leilaoRepository.ObterPorIdAsync(leilao.Id);

        return Json(new {
            sucesso = true,
            mensagem = $"🎉 Parabéns {compradorNome}! Seu lance de R$ {valorLance:N2} foi registrado com sucesso!",
            novoLanceAtual = $"R$ {leilaoAtualizado?.LanceAtual.ToString("N2")}",
            novoProximoMinimo = leilaoAtualizado?.ProximoLanceMinimo,
            vendedorNome = leilaoAtualizado?.VendedorNome,
            historicoLances = leilaoAtualizado?.Lances.Select(l => new {
                usuario = l.UsuarioNome,
                valor = $"R$ {l.Valor:N2}",
                hora = l.DataLance.ToString("dd/MM HH:mm")
            })
        });
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
            IncrementoMinimo = 50.00m,
            Status = StatusLeilao.Criado
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

        if (!leilao.PodeEditarDadosGerais)
        {
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
