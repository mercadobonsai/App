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

    // GET: /Rifa/MinhasRifas (Gestão do Vendedor)
    [HttpGet]
    [Authorize(Roles = "Vendedor, Administrador")]
    public async Task<IActionResult> MinhasRifas()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int vendedorId))
        {
            return Unauthorized();
        }

        var rifas = await _rifaRepository.ListarPorVendedorAsync(vendedorId);
        return View(rifas);
    }

    // GET: /Rifa/Criar
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
        var rifasCriadas30Dias = await _rifaRepository.ContarPorVendedorNosUltimos30DiasAsync(vendedorId);

        if (plano != null && rifasCriadas30Dias >= plano.LimiteRifas30Dias && !User.IsInRole("Administrador"))
        {
            TempData["Erro"] = $"Você atingiu o limite de {plano.LimiteRifas30Dias} rifas a cada 30 dias do seu Plano {plano.Nome}. Faça o upgrade de assinatura para continuar!";
            return RedirectToAction("MinhasRifas");
        }

        var rifa = new Rifa
        {
            TotalCotas = 100,
            ValorCota = 25.00m,
            DataSorteio = DateTime.UtcNow.AddDays(15)
        };

        return View(rifa);
    }

    // POST: /Rifa/Criar
    [HttpPost]
    [Authorize(Roles = "Vendedor, Administrador")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(Rifa rifa, Microsoft.AspNetCore.Http.IFormFile? fotoPrincipal, Microsoft.AspNetCore.Http.IFormFile? fotoDetalhe)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int vendedorId))
        {
            return Unauthorized();
        }

        var usuario = await _usuarioRepository.ObterPorIdAsync(vendedorId);
        var plano = await _planoRepository.ObterPorIdAsync(usuario?.PlanoId ?? 1);
        var rifasCriadas30Dias = await _rifaRepository.ContarPorVendedorNosUltimos30DiasAsync(vendedorId);

        if (plano != null && rifasCriadas30Dias >= plano.LimiteRifas30Dias && !User.IsInRole("Administrador"))
        {
            ModelState.AddModelError(string.Empty, $"Limite atingido: Seu Plano {plano.Nome} permite no máximo {plano.LimiteRifas30Dias} rifas a cada 30 dias.");
            return View(rifa);
        }

        if (string.IsNullOrWhiteSpace(rifa.Titulo))
        {
            ModelState.AddModelError("Titulo", "Informe o título da rifa.");
            return View(rifa);
        }

        if (fotoPrincipal != null && fotoPrincipal.Length > 0)
        {
            var folder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "rifas");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            var ext = Path.GetExtension(fotoPrincipal.FileName);
            var name = $"rifa_{vendedorId}_{Guid.NewGuid()}{ext}";
            var path = Path.Combine(folder, name);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await fotoPrincipal.CopyToAsync(stream);
            }
            rifa.FotoPrincipalUrl = $"/uploads/rifas/{name}";
        }

        if (fotoDetalhe != null && fotoDetalhe.Length > 0)
        {
            var folder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "rifas");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            var ext = Path.GetExtension(fotoDetalhe.FileName);
            var name = $"rifa_detalhe_{vendedorId}_{Guid.NewGuid()}{ext}";
            var path = Path.Combine(folder, name);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await fotoDetalhe.CopyToAsync(stream);
            }
            rifa.FotoDetalheUrl = $"/uploads/rifas/{name}";
        }

        rifa.VendedorId = vendedorId;
        rifa.VendedorNome = usuario?.RazaoSocial ?? usuario?.Nome ?? User.Identity?.Name;
        rifa.DataCriacao = DateTime.UtcNow;

        await _rifaRepository.InserirAsync(rifa);

        TempData["Sucesso"] = "Rifa / Ação entre Amigos criada com sucesso!";
        return RedirectToAction("MinhasRifas");
    }

    // GET: /Rifa/Editar/{id}
    [HttpGet]
    [Authorize(Roles = "Vendedor, Administrador")]
    public async Task<IActionResult> Editar(int id)
    {
        var rifa = await _rifaRepository.ObterPorIdAsync(id);
        if (rifa == null) return NotFound();

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!User.IsInRole("Administrador") && (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int vendedorId) || rifa.VendedorId != vendedorId))
        {
            return Forbid();
        }

        return View(rifa);
    }

    // POST: /Rifa/Editar/{id}
    [HttpPost]
    [Authorize(Roles = "Vendedor, Administrador")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, Rifa model)
    {
        var rifa = await _rifaRepository.ObterPorIdAsync(id);
        if (rifa == null) return NotFound();

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!User.IsInRole("Administrador") && (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int vendedorId) || rifa.VendedorId != vendedorId))
        {
            return Forbid();
        }

        rifa.Titulo = model.Titulo;
        rifa.Subtitulo = model.Subtitulo;
        rifa.Descricao = model.Descricao;
        rifa.ValorCota = model.ValorCota;
        rifa.TotalCotas = model.TotalCotas;
        rifa.CotasVendidas = model.CotasVendidas;
        rifa.DataSorteio = model.DataSorteio;
        rifa.Status = model.Status;

        await _rifaRepository.AtualizarAsync(rifa);

        TempData["Sucesso"] = "Rifa atualizada com sucesso!";
        return RedirectToAction("MinhasRifas");
    }
}
