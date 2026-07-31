using System.Threading.Tasks;
using MercadoBonsai.Domain.Interfaces;
using MercadoBonsai.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace MercadoBonsai.Web.Controllers;

public class CartaoController : Controller
{
    private readonly VendedorTokenService _vendedorTokenService;
    private readonly IUsuarioRepository _usuarioRepository;

    public CartaoController(
        VendedorTokenService vendedorTokenService,
        IUsuarioRepository usuarioRepository)
    {
        _vendedorTokenService = vendedorTokenService;
        _usuarioRepository = usuarioRepository;
    }

    // GET: /Cartao/Vitrine/{token}
    [HttpGet]
    public async Task<IActionResult> Vitrine(string token)
    {
        if (!_vendedorTokenService.TentarDecodificarToken(token, out int vendedorId, out _))
        {
            TempData["Erro"] = "Cartão de Visitas Digital ou link de acesso inválido.";
            return RedirectToAction("Index", "Home");
        }

        var vendedor = await _usuarioRepository.ObterPorIdAsync(vendedorId);
        if (vendedor == null)
        {
            TempData["Erro"] = "Vendedor não encontrado na plataforma.";
            return RedirectToAction("Index", "Home");
        }

        var busca = string.IsNullOrEmpty(vendedor.RazaoSocial) ? vendedor.Nome : vendedor.RazaoSocial;
        return RedirectToAction("Index", "Produto", new { busca = busca });
    }

    // GET: /Cartao/Insumos/{token}
    [HttpGet]
    public async Task<IActionResult> Insumos(string token)
    {
        if (!_vendedorTokenService.TentarDecodificarToken(token, out int vendedorId, out _))
        {
            TempData["Erro"] = "Link direto do vendedor inválido.";
            return RedirectToAction("Index", "Home");
        }

        var vendedor = await _usuarioRepository.ObterPorIdAsync(vendedorId);
        var busca = vendedor != null ? (string.IsNullOrEmpty(vendedor.RazaoSocial) ? vendedor.Nome : vendedor.RazaoSocial) : string.Empty;
        return RedirectToAction("Index", "Insumo", new { busca = busca });
    }

    // GET: /Cartao/Vasos/{token}
    [HttpGet]
    public async Task<IActionResult> Vasos(string token)
    {
        if (!_vendedorTokenService.TentarDecodificarToken(token, out int vendedorId, out _))
        {
            TempData["Erro"] = "Link direto do vendedor inválido.";
            return RedirectToAction("Index", "Home");
        }

        var vendedor = await _usuarioRepository.ObterPorIdAsync(vendedorId);
        var busca = vendedor != null ? (string.IsNullOrEmpty(vendedor.RazaoSocial) ? vendedor.Nome : vendedor.RazaoSocial) : string.Empty;
        return RedirectToAction("Index", "Vaso", new { busca = busca });
    }

    // GET: /Cartao/Engajamento/{token}
    [HttpGet]
    public async Task<IActionResult> Engajamento(string token)
    {
        if (!_vendedorTokenService.TentarDecodificarToken(token, out int vendedorId, out _))
        {
            TempData["Erro"] = "Link direto do vendedor inválido.";
            return RedirectToAction("Index", "Home");
        }

        return RedirectToAction("Index", "Home");
    }
}
