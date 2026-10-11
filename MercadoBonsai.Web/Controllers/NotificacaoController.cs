using System;
using System.Security.Claims;
using System.Threading.Tasks;
using MercadoBonsai.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MercadoBonsai.Web.Controllers;

[Authorize]
public class NotificacaoController : Controller
{
    private readonly INotificacaoRepository _notificacaoRepository;

    public NotificacaoController(INotificacaoRepository notificacaoRepository)
    {
        _notificacaoRepository = notificacaoRepository;
    }

    private int ObterUsuarioIdLogado()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idClaim, out int id) ? id : 0;
    }

    // GET: /Notificacao ou /Notificacao/Index
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        int usuarioId = ObterUsuarioIdLogado();
        if (usuarioId == 0) return RedirectToAction("Login", "Conta");

        var notificacoes = await _notificacaoRepository.ListarPorUsuarioAsync(usuarioId, limite: 100);
        int totalNaoLidas = await _notificacaoRepository.ObterTotalNaoLidasAsync(usuarioId);
        ViewBag.TotalNaoLidas = totalNaoLidas;

        return View(notificacoes);
    }

    // GET: /Notificacao/ObterResumo (API para o menu suspenso no canto superior direito)
    [HttpGet]
    public async Task<IActionResult> ObterResumo()
    {
        int usuarioId = ObterUsuarioIdLogado();
        if (usuarioId == 0)
        {
            return Json(new { totalNaoLidas = 0, notificacoes = Array.Empty<object>() });
        }

        var notificacoes = await _notificacaoRepository.ListarPorUsuarioAsync(usuarioId, limite: 8);
        int totalNaoLidas = await _notificacaoRepository.ObterTotalNaoLidasAsync(usuarioId);

        var lista = new System.Collections.Generic.List<object>();
        foreach (var n in notificacoes)
        {
            var tempoAtras = FormatarTempoAtras(n.DataCriacao);
            lista.Add(new
            {
                id = n.Id,
                titulo = n.Titulo,
                mensagem = n.Mensagem,
                tipo = n.Tipo,
                linkUrl = n.LinkUrl,
                lida = n.Lida,
                tempoAtras = tempoAtras,
                dataCriacao = n.DataCriacao.ToString("dd/MM/yyyy HH:mm")
            });
        }

        return Json(new { totalNaoLidas = totalNaoLidas, notificacoes = lista });
    }

    // POST: /Notificacao/MarcarLida
    [HttpPost]
    public async Task<IActionResult> MarcarLida([FromBody] MarcarLidaRequest request)
    {
        int usuarioId = ObterUsuarioIdLogado();
        if (usuarioId == 0 || request == null || request.Id <= 0)
        {
            return BadRequest();
        }

        await _notificacaoRepository.MarcarComoLidaAsync(request.Id, usuarioId);
        int totalNaoLidas = await _notificacaoRepository.ObterTotalNaoLidasAsync(usuarioId);

        return Json(new { sucesso = true, totalNaoLidas });
    }

    // POST: /Notificacao/MarcarTodasLidas
    [HttpPost]
    public async Task<IActionResult> MarcarTodasLidas()
    {
        int usuarioId = ObterUsuarioIdLogado();
        if (usuarioId == 0) return BadRequest();

        await _notificacaoRepository.MarcarTodasComoLidasAsync(usuarioId);
        return Json(new { sucesso = true, totalNaoLidas = 0 });
    }

    private static string FormatarTempoAtras(DateTime data)
    {
        var diff = DateTime.UtcNow - data;
        if (diff.TotalMinutes < 1) return "agora mesmo";
        if (diff.TotalMinutes < 60) return $"há {(int)diff.TotalMinutes} min";
        if (diff.TotalHours < 24) return $"há {(int)diff.TotalHours} h";
        if (diff.TotalDays < 7) return $"há {(int)diff.TotalDays} d";
        return data.ToString("dd/MM/yyyy");
    }

    public class MarcarLidaRequest
    {
        public int Id { get; set; }
    }
}
