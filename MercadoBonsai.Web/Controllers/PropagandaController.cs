using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MercadoBonsai.Domain.Entities;
using MercadoBonsai.Domain.Enums;
using MercadoBonsai.Domain.Interfaces;
using MercadoBonsai.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MercadoBonsai.Web.Controllers;

[Authorize]
public class PropagandaController : Controller
{
    private readonly IPropagandaRepository _propagandaRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPlanoRepository _planoRepository;
    private readonly IStorageService _storageService;

    public PropagandaController(
        IPropagandaRepository propagandaRepository,
        IUsuarioRepository usuarioRepository,
        IPlanoRepository planoRepository,
        IStorageService storageService)
    {
        _propagandaRepository = propagandaRepository;
        _usuarioRepository = usuarioRepository;
        _planoRepository = planoRepository;
        _storageService = storageService;
    }

    private async Task<(Usuario? usuario, Plano? plano, bool permitePropaganda)> ObterPermissaoPlanoAsync(int usuarioId)
    {
        var usuario = await _usuarioRepository.ObterPorIdAsync(usuarioId);
        var planoId = usuario?.PlanoId > 0 ? usuario.PlanoId : 1;
        var plano = await _planoRepository.ObterPorIdAsync(planoId);

        bool isAdmin = User.IsInRole("Administrador") || usuario?.Perfil == PerfilUsuario.Administrador;
        bool permitePropaganda = isAdmin
            || (usuario?.IsentoCobranca == true)
            || (plano != null && plano.DestaquesHome)
            || planoId >= 2;

        return (usuario, plano, permitePropaganda);
    }

    private static decimal ObterPrecoEspaco(string? tipoEspaco)
    {
        return (tipoEspaco ?? "Basico").Trim().ToLowerInvariant() switch
        {
            "economico" => 49.90m,
            "basico" => 79.90m,
            "intermediario" => 149.90m,
            "avancado" => 299.90m,
            _ => 79.90m
        };
    }

    private static string NormalizarTipoEspaco(string? tipoEspaco)
    {
        return (tipoEspaco ?? "Basico").Trim().ToLowerInvariant() switch
        {
            "economico" => "Economico",
            "basico" => "Basico",
            "intermediario" => "Intermediario",
            "avancado" => "Avancado",
            _ => "Basico"
        };
    }

    private static string NormalizarLinkDestino(string? linkDestino, int usuarioId)
    {
        if (string.IsNullOrWhiteSpace(linkDestino))
        {
            return $"/Produto?vendedorId={usuarioId}";
        }

        var trimmed = linkDestino.Trim();
        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("/"))
        {
            return trimmed;
        }

        return "https://" + trimmed;
    }

    // GET: /Propaganda/MinhasPropagandas
    [HttpGet]
    public async Task<IActionResult> MinhasPropagandas(bool novo = false)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int usuarioId))
        {
            return Unauthorized();
        }

        var (usuario, plano, permitePropaganda) = await ObterPermissaoPlanoAsync(usuarioId);
        ViewBag.PermitePropaganda = permitePropaganda;
        ViewBag.PlanoNome = plano?.Nome ?? "Bronze";
        ViewBag.PlanoId = plano?.Id ?? 1;
        ViewBag.UsuarioId = usuarioId;
        ViewBag.AbrirModalNovo = novo;
        ViewBag.LinkPadraoVitrine = $"/Produto?vendedorId={usuarioId}";

        var propagandas = await _propagandaRepository.ListarPorUsuarioAsync(usuarioId);
        return View(propagandas);
    }

    // GET: /Propaganda/Criar -> Redireciona para MinhasPropagandas abrindo o modal de cadastro
    [HttpGet]
    public IActionResult Criar()
    {
        return RedirectToAction(nameof(MinhasPropagandas), new { novo = true });
    }

    // POST: /Propaganda/SolicitarComArte
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> SolicitarComArte(string tipoEspaco, string? linkDestino, IFormFile? arteImagem)
    {
        bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int usuarioId))
        {
            if (isAjax)
                return Json(new { sucesso = false, naoAutenticado = true, mensagem = "Você precisa estar logado no portal para solicitar espaços publicitários." });
            return RedirectToAction("Login", "Conta", new { returnUrl = "/Propaganda/MinhasPropagandas?novo=true" });
        }

        var (usuario, plano, permitePropaganda) = await ObterPermissaoPlanoAsync(usuarioId);
        if (!permitePropaganda)
        {
            var msgPlano = $"Seu plano atual (Plano {plano?.Nome ?? "Bronze"}) não contempla o cadastro de Propagandas e Destaques na Home. Faça o upgrade para os planos Prata, Ouro ou Diamante para anunciar!";
            TempData["Erro"] = msgPlano;
            if (isAjax)
                return Json(new { sucesso = false, mensagem = msgPlano });
            return RedirectToAction(nameof(MinhasPropagandas));
        }

        if (string.IsNullOrWhiteSpace(tipoEspaco))
        {
            var msgTipo = "Por favor, selecione uma modalidade de espaço publicitário.";
            TempData["Erro"] = msgTipo;
            if (isAjax)
                return Json(new { sucesso = false, mensagem = msgTipo });
            return RedirectToAction(nameof(MinhasPropagandas));
        }

        var tipoNormalizado = NormalizarTipoEspaco(tipoEspaco);
        var precoMensal = ObterPrecoEspaco(tipoNormalizado);
        var usuarioNome = !string.IsNullOrWhiteSpace(usuario?.RazaoSocial)
            ? usuario.RazaoSocial
            : (usuario?.Nome ?? User.Identity?.Name ?? "Anunciante Mercado Bonsai");

        string? imagemUrl = null;
        if (arteImagem != null && arteImagem.Length > 0)
        {
            imagemUrl = await _storageService.UploadImagemOtimizadaAsync(arteImagem, "propagandas", $"ad_{usuarioId}");
        }

        var propaganda = new Propaganda
        {
            UsuarioId = usuarioId,
            UsuarioNome = usuarioNome,
            TipoEspaco = tipoNormalizado,
            PrecoMensal = precoMensal,
            ImagemUrl = imagemUrl,
            LinkDestino = NormalizarLinkDestino(linkDestino, usuarioId),
            Status = "Pendente",
            DataCriacao = DateTime.UtcNow
        };

        await _propagandaRepository.InserirAsync(propaganda);

        var msgSucesso = $"Solicitação do Espaço {tipoNormalizado} (R$ {precoMensal:N2}/mês) cadastrada com sucesso! O anúncio foi enviado para a Auditoria de Anúncios do Administrador.";
        TempData["Sucesso"] = msgSucesso;

        if (isAjax)
        {
            return Json(new
            {
                sucesso = true,
                mensagem = msgSucesso,
                tipoEspaco = propaganda.TipoEspaco,
                precoMensal = $"R$ {propaganda.PrecoMensal:N2}"
            });
        }

        return RedirectToAction(nameof(MinhasPropagandas));
    }

    // POST: /Propaganda/AtualizarArte
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AtualizarArte(int id, string? linkDestino, IFormFile? arteImagem)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int usuarioId))
        {
            return Unauthorized();
        }

        var propaganda = await _propagandaRepository.ObterPorIdAsync(id);
        if (propaganda == null || (propaganda.UsuarioId != usuarioId && !User.IsInRole("Administrador")))
        {
            return NotFound();
        }

        if (arteImagem != null && arteImagem.Length > 0)
        {
            propaganda.ImagemUrl = await _storageService.UploadImagemOtimizadaAsync(arteImagem, "propagandas", $"ad_{usuarioId}");

            if (propaganda.Status == "Rejeitado" || propaganda.Status == "Reprovado")
            {
                propaganda.Status = "Pendente";
            }
        }

        if (linkDestino != null)
        {
            propaganda.LinkDestino = NormalizarLinkDestino(linkDestino, usuarioId);
        }

        await _propagandaRepository.AtualizarAsync(propaganda);
        TempData["Sucesso"] = $"Dados do anúncio #{id} atualizados com sucesso!";
        return RedirectToAction(nameof(MinhasPropagandas));
    }

    // POST: /Propaganda/Excluir
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Excluir(int id)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int usuarioId))
        {
            return Unauthorized();
        }

        var propaganda = await _propagandaRepository.ObterPorIdAsync(id);
        if (propaganda == null || (propaganda.UsuarioId != usuarioId && !User.IsInRole("Administrador")))
        {
            return NotFound();
        }

        await _propagandaRepository.ExcluirAsync(id);
        TempData["Sucesso"] = $"Solicitação de anúncio #{id} removida com sucesso.";
        return RedirectToAction(nameof(MinhasPropagandas));
    }
}
