using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MercadoBonsai.Domain.Interfaces;
using MercadoBonsai.Web.Models;
using MercadoBonsai.Web.Services;

namespace MercadoBonsai.Web.Controllers;

[Authorize]
public class FinanceiroController : Controller
{
    private readonly IAsaasService _asaasService;
    private readonly IUsuarioRepository _usuarioRepository;

    public FinanceiroController(IAsaasService asaasService, IUsuarioRepository usuarioRepository)
    {
        _asaasService = asaasService;
        _usuarioRepository = usuarioRepository;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CobrancaFiltroViewModel filtro)
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(idClaim, out int usuarioId))
        {
            return RedirectToAction("Login", "Conta");
        }

        var usuario = await _usuarioRepository.ObterPorIdAsync(usuarioId);
        if (usuario == null)
        {
            return NotFound();
        }

        bool isAdmin = User.IsInRole("Administrador");

        // Se não for Admin (for Vendedor ou Comprador), restringe as cobranças ao seu AsaasCustomerId/AsaasAccountId
        string? customerIdRestrito = isAdmin ? null : usuario.AsaasCustomerId;
        string? accountIdRestrito = isAdmin ? null : usuario.AsaasAccountId;

        // Se o vendedor não possui subconta/cliente Asaas associado ainda
        if (!isAdmin && string.IsNullOrEmpty(customerIdRestrito) && string.IsNullOrEmpty(accountIdRestrito))
        {
            filtro.Cobrancas = new();
            filtro.TotalCount = 0;
            filtro.HasMore = false;
            ViewBag.IsAdmin = false;
            return View(filtro);
        }

        var result = await _asaasService.ListarCobrancasAsync(filtro, customerIdRestrito, accountIdRestrito);
        if (result.Sucesso)
        {
            filtro.Cobrancas = result.Data;
            filtro.TotalCount = result.TotalCount;
            filtro.Offset = result.Offset;
            filtro.Limit = result.Limit;
            filtro.HasMore = result.HasMore;

            // Calcular Métricas Rápidas
            filtro.TotalCobrado = 0;
            filtro.TotalRecebido = 0;
            filtro.TotalPendente = 0;

            foreach (var item in result.Data)
            {
                filtro.TotalCobrado += item.Value;
                if (item.Status == "RECEIVED" || item.Status == "CONFIRMED" || item.Status == "RECEIVED_IN_CASH")
                {
                    filtro.TotalRecebido += item.Value;
                }
                else if (item.Status == "PENDING")
                {
                    filtro.TotalPendente += item.Value;
                }
            }
        }
        else
        {
            TempData["Erro"] = $"Falha ao consultar cobranças no Asaas: {result.MensagemErro}";
        }

        ViewBag.IsAdmin = isAdmin;
        return View(filtro);
    }
}
