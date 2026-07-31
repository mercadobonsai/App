using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using MercadoBonsai.Domain.Entities;
using MercadoBonsai.Domain.Interfaces;
using MercadoBonsai.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace MercadoBonsai.Web.Controllers;

[Authorize(Roles = "Administrador")]
public class AdminController : Controller
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPlanoRepository _planoRepository;
    private readonly IPropagandaRepository _propagandaRepository;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public AdminController(
        IUsuarioRepository usuarioRepository, 
        IPlanoRepository planoRepository, 
        IPropagandaRepository propagandaRepository,
        IWebHostEnvironment webHostEnvironment)
    {
        _usuarioRepository = usuarioRepository;
        _planoRepository = planoRepository;
        _propagandaRepository = propagandaRepository;
        _webHostEnvironment = webHostEnvironment;
    }

    // GET: /Admin/Clientes
    [HttpGet]
    public async Task<IActionResult> Clientes(string? busca, int? perfil)
    {
        var usuarios = await _usuarioRepository.ListarTodosAsync(busca, perfil);
        ViewData["Busca"] = busca;
        ViewData["PerfilFilter"] = perfil;
        return View(usuarios);
    }

    // GET: /Admin/EditarCliente/{id}
    [HttpGet]
    public async Task<IActionResult> EditarCliente(int id)
    {
        var usuario = await _usuarioRepository.ObterPorIdAsync(id);
        if (usuario == null)
        {
            return NotFound();
        }

        var viewModel = new PerfilViewModel
        {
            Id = usuario.Id,
            Nome = usuario.Nome,
            Email = usuario.Email,
            Telefone = usuario.Telefone,
            Perfil = usuario.Perfil,
            RazaoSocial = usuario.RazaoSocial,
            CpfCnpj = usuario.CpfCnpj,
            InscricaoEstadual = usuario.InscricaoEstadual,
            ChavePix = usuario.ChavePix,
            Banco = usuario.Banco,
            Agencia = usuario.Agencia,
            Conta = usuario.Conta,
            DescricaoViveiro = usuario.DescricaoViveiro,
            LogotipoUrl = usuario.LogotipoUrl,
            PlanoId = usuario.PlanoId,
            IsentoCobranca = usuario.IsentoCobranca,
            DataUltimaAlteracao = usuario.DataUltimaAlteracao,
            UsuarioAlteracaoNome = usuario.UsuarioAlteracaoNome
        };

        return View(viewModel);
    }

    // POST: /Admin/EditarCliente/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarCliente(int id, PerfilViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var usuario = await _usuarioRepository.ObterPorIdAsync(id);
        if (usuario == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (model.LogotipoArquivo != null && model.LogotipoArquivo.Length > 0)
        {
            var folder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "logotipos");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var extension = Path.GetExtension(model.LogotipoArquivo.FileName);
            var uniqueFileName = $"logo_{usuario.Id}_{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(folder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await model.LogotipoArquivo.CopyToAsync(stream);
            }

            usuario.LogotipoUrl = $"/uploads/logotipos/{uniqueFileName}";
        }

        var adminIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        int.TryParse(adminIdClaim, out int adminId);

        usuario.Nome = model.Nome;
        usuario.Email = model.Email;
        usuario.Telefone = model.Telefone ?? string.Empty;
        usuario.Perfil = model.Perfil;
        usuario.RazaoSocial = model.RazaoSocial;
        usuario.CpfCnpj = model.CpfCnpj;
        usuario.InscricaoEstadual = model.InscricaoEstadual;
        usuario.ChavePix = model.ChavePix;
        usuario.Banco = model.Banco;
        usuario.Agencia = model.Agencia;
        usuario.Conta = model.Conta;
        usuario.DescricaoViveiro = model.DescricaoViveiro;
        usuario.IsentoCobranca = model.IsentoCobranca;
        usuario.DataUltimaAlteracao = DateTime.UtcNow;
        usuario.UsuarioAlteracaoId = adminId;
        usuario.UsuarioAlteracaoNome = User.Identity?.Name ?? "Administrador";

        await _usuarioRepository.AtualizarAsync(usuario);

        TempData["Sucesso"] = $"Cadastro do cliente '{usuario.Nome}' atualizado com sucesso!";
        return RedirectToAction("Clientes");
    }

    // GET: /Admin/Planos
    [HttpGet]
    public async Task<IActionResult> Planos()
    {
        var planos = await _planoRepository.ListarTodosAsync();
        return View(planos);
    }

    // POST: /Admin/SalvarPlano
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SalvarPlano(Plano plano)
    {
        if (!ModelState.IsValid)
        {
            var planos = await _planoRepository.ListarTodosAsync();
            return View("Planos", planos);
        }

        var adminIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        int.TryParse(adminIdClaim, out int adminId);

        await _planoRepository.AtualizarAsync(plano);

        TempData["Sucesso"] = $"Configurações do Plano '{plano.Nome}' atualizadas com sucesso!";
        return RedirectToAction("Planos");
    }

    // GET: /Admin/Propagandas
    [HttpGet]
    public async Task<IActionResult> Propagandas()
    {
        var propagandas = await _propagandaRepository.ListarTodasAsync();
        return View(propagandas);
    }

    // POST: /Admin/AprovarPropaganda
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AprovarPropaganda(int id)
    {
        var prop = await _propagandaRepository.ObterPorIdAsync(id);
        if (prop == null)
        {
            return NotFound();
        }

        prop.Status = "Ativo";
        prop.DataInicio = DateTime.Now;
        prop.DataExpiracao = DateTime.Now.AddDays(30);

        await _propagandaRepository.AtualizarAsync(prop);

        TempData["Sucesso"] = $"Propaganda #{id} de {prop.UsuarioNome} aprovada e ativada no portal com sucesso!";
        return RedirectToAction("Propagandas");
    }

    // POST: /Admin/RejeitarPropaganda
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejeitarPropaganda(int id)
    {
        var prop = await _propagandaRepository.ObterPorIdAsync(id);
        if (prop == null)
        {
            return NotFound();
        }

        prop.Status = "Rejeitado";
        await _propagandaRepository.AtualizarAsync(prop);

        TempData["Sucesso"] = $"Solicitação de propaganda #{id} de {prop.UsuarioNome} foi rejeitada.";
        return RedirectToAction("Propagandas");
    }
}
