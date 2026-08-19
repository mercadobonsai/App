using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using MercadoBonsai.Domain.Entities;
using MercadoBonsai.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace MercadoBonsai.Web.Controllers;

/* 
 * FUNCIONALIDADE DE RIFAS / AÇÕES ENTRE AMIGOS DESCONTINUADA TEMPORARIAMENTE
 * O projeto focará exclusivamente na modalidade de Leilões.
 * Todo o código funcional das Rifas foi preservado abaixo para eventual reaproveitamento no futuro.
 */
public class RifaController : Controller
{
    private readonly IRifaRepository _rifaRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPlanoRepository _planoRepository;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public RifaController(
        IRifaRepository rifaRepository,
        IUsuarioRepository usuarioRepository,
        IPlanoRepository planoRepository,
        IWebHostEnvironment webHostEnvironment)
    {
        _rifaRepository = rifaRepository;
        _usuarioRepository = usuarioRepository;
        _planoRepository = planoRepository;
        _webHostEnvironment = webHostEnvironment;
    }

    [HttpGet]
    public IActionResult Index()
    {
        TempData["Aviso"] = "A funcionalidade de Rifas / Ações entre Amigos está temporariamente desativada. Foque nos Leilões ativos!";
        return RedirectToAction("Index", "Leilao");
    }

    // GET: /Rifa/MinhasRifas (Gestão do Vendedor - Redireciona para Leilões)
    [HttpGet]
    [Authorize(Roles = "Vendedor, Administrador")]
    public IActionResult MinhasRifas()
    {
        TempData["Aviso"] = "A funcionalidade de Rifas / Ações entre Amigos está desativada temporariamente. Foque na gestão dos seus Leilões!";
        return RedirectToAction("MeusLeiloes", "Leilao");
    }

    // GET: /Rifa/Criar (Redireciona para Leilões)
    [HttpGet]
    [Authorize(Roles = "Vendedor, Administrador")]
    public IActionResult Criar()
    {
        TempData["Aviso"] = "A criação de Rifas / Ações está temporariamente desativada. Crie e gerencie seus Leilões ativos!";
        return RedirectToAction("MeusLeiloes", "Leilao");
    }

    // POST: /Rifa/AdquirirCotas
    [HttpPost]
    [Authorize]
    public IActionResult AdquirirCotas(int rifaId, int quantidadeCotas)
    {
        return Json(new { sucesso = false, mensagem = "A modalidade de Rifas está temporariamente suspensa no Mercado Bonsai." });
    }

    /*
    ===========================================================================================
    CÓDIGO ORIGINAL DAS RIFAS PRESERVADO PARA USO FUTURO SE REQUERIDO
    ===========================================================================================

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> AdquirirCotasOriginal(int rifaId, int quantidadeCotas)
    {
        var rifa = await _rifaRepository.ObterPorIdAsync(rifaId);
        if (rifa == null)
        {
            return Json(new { sucesso = false, mensagem = "Ação entre Amigos não encontrada." });
        }

        if (rifa.Status != 1)
        {
            return Json(new { sucesso = false, mensagem = "Esta Ação entre Amigos não está ativa para aquisição de cotas." });
        }

        var cotasDisponiveis = rifa.TotalCotas - rifa.CotasVendidas;
        if (quantidadeCotas <= 0 || quantidadeCotas > cotasDisponiveis)
        {
            return Json(new { 
                sucesso = false, 
                mensagem = $"Quantidade de cotas inválida. Cotas disponíveis no momento: {cotasDisponiveis}." 
            });
        }

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        int.TryParse(userIdClaim, out int compradorId);
        var compradorNome = User.Identity?.Name ?? "Comprador Mercado Bonsai";

        var valorTotal = quantidadeCotas * rifa.ValorCota;
        var pixGuid = Guid.NewGuid().ToString("N").Substring(0, 16);
        var chavePixFormatada = $"00020126580014BR.GOV.BCB.PIX0136{pixGuid}52040000530398654{valorTotal:F2}5802BR5915MERCADOBONSAI6009SAO_PAULO62070503***6304";

        var pedido = new PedidoRifa
        {
            RifaId = rifa.Id,
            UsuarioId = compradorId,
            UsuarioNome = compradorNome,
            QuantidadeCotas = quantidadeCotas,
            ValorTotal = valorTotal,
            ChavePix = chavePixFormatada,
            Status = "Pendente",
            DataReserva = DateTime.UtcNow
        };

        await _rifaRepository.InserirPedidoAsync(pedido);

        rifa.CotasVendidas += quantidadeCotas;
        await _rifaRepository.AtualizarAsync(rifa);

        var rifaAtualizada = await _rifaRepository.ObterPorIdAsync(rifa.Id);

        return Json(new {
            sucesso = true,
            mensagem = $"Reserva efetuada! {quantidadeCotas} cota(s) reservada(s) para {compradorNome}.",
            rifaId = rifa.Id,
            quantidadeCotas = quantidadeCotas,
            valorTotal = $"R$ {valorTotal:N2}",
            chavePix = chavePixFormatada,
            cotasVendidas = rifaAtualizada?.CotasVendidas ?? rifa.CotasVendidas,
            totalCotas = rifaAtualizada?.TotalCotas ?? rifa.TotalCotas,
            porcentagemVendida = rifaAtualizada?.PorcentagemVendida ?? rifa.PorcentagemVendida,
            statusPedido = "Pendente"
        });
    }
    */
}
