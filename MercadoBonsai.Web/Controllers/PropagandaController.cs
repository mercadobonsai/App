using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MercadoBonsai.Domain.Entities;
using MercadoBonsai.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MercadoBonsai.Web.Controllers;

[Authorize]
public class PropagandaController : Controller
{
    private readonly IPropagandaRepository _propagandaRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public PropagandaController(
        IPropagandaRepository propagandaRepository,
        IUsuarioRepository usuarioRepository,
        IWebHostEnvironment webHostEnvironment)
    {
        _propagandaRepository = propagandaRepository;
        _usuarioRepository = usuarioRepository;
        _webHostEnvironment = webHostEnvironment;
    }

    // GET: /Propaganda/MinhasPropagandas
    [HttpGet]
    public async Task<IActionResult> MinhasPropagandas()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int usuarioId))
        {
            return Unauthorized();
        }

        var propagandas = await _propagandaRepository.ListarPorUsuarioAsync(usuarioId);
        return View(propagandas);
    }

    // POST: /Propaganda/SolicitarComArte (Fluxo Unificado por Modal com Upload e Preview)
    [HttpPost]
    public async Task<IActionResult> SolicitarComArte(string tipoEspaco, string? linkDestino, IFormFile? arteImagem)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int usuarioId))
        {
            return Json(new { sucesso = false, mensagem = "Você precisa estar logado no portal para solicitar espaços publicitários." });
        }

        var usuario = await _usuarioRepository.ObterPorIdAsync(usuarioId);
        var usuarioNome = usuario?.RazaoSocial ?? usuario?.Nome ?? User.Identity?.Name ?? "Anunciante Mercado Bonsai";

        if (string.IsNullOrWhiteSpace(tipoEspaco))
        {
            return Json(new { sucesso = false, mensagem = "Por favor, selecione uma modalidade visual de anúncio." });
        }

        decimal precoMensal = tipoEspaco.ToLower() switch
        {
            "economico" => 49.90m,
            "basico" => 79.90m,
            "intermediario" => 149.90m,
            "avancado" => 299.90m,
            _ => 79.90m
        };

        string? imagemUrl = null;
        if (arteImagem != null && arteImagem.Length > 0)
        {
            var folder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "propagandas");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            var ext = Path.GetExtension(arteImagem.FileName);
            var name = $"ad_{usuarioId}_{Guid.NewGuid()}{ext}";
            var path = Path.Combine(folder, name);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await arteImagem.CopyToAsync(stream);
            }
            imagemUrl = $"/uploads/propagandas/{name}";
        }

        var propaganda = new Propaganda
        {
            UsuarioId = usuarioId,
            UsuarioNome = usuarioNome,
            TipoEspaco = tipoEspaco,
            PrecoMensal = precoMensal,
            ImagemUrl = imagemUrl,
            LinkDestino = string.IsNullOrWhiteSpace(linkDestino) ? "https://mercadobonsai.com.br" : linkDestino,
            Status = "Pendente",
            DataCriacao = DateTime.UtcNow
        };

        await _propagandaRepository.InserirAsync(propaganda);

        return Json(new {
            sucesso = true,
            mensagem = $"Solicitação de Espaço {tipoEspaco} efetuada com sucesso! O pedido está gravado como 'Pendente' e enviado para auditoria financeira e ativação do Administrador.",
            tipoEspaco = propaganda.TipoEspaco,
            precoMensal = $"R$ {propaganda.PrecoMensal:N2}"
        });
    }
}
