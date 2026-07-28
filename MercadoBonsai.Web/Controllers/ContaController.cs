using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using BCrypt.Net;
using MercadoBonsai.Domain.Entities;
using MercadoBonsai.Domain.Enums;
using MercadoBonsai.Domain.Interfaces;
using MercadoBonsai.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace MercadoBonsai.Web.Controllers;

public class ContaController : Controller
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public ContaController(IUsuarioRepository usuarioRepository, IWebHostEnvironment webHostEnvironment)
    {
        _usuarioRepository = usuarioRepository;
        _webHostEnvironment = webHostEnvironment;
    }

    // GET: /Conta/Login
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    // POST: /Conta/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
            return View(model);

        var usuario = await _usuarioRepository.ObterPorEmailAsync(model.Email?.Trim() ?? string.Empty);

        if (usuario == null)
        {
            ModelState.AddModelError(string.Empty, $"Usuário não encontrado com o e-mail '{model.Email}'. Verifique se o cadastro foi realizado com sucesso.");
            return View(model);
        }

        if (string.IsNullOrEmpty(usuario.SenhaHash))
        {
            ModelState.AddModelError(string.Empty, "Erro: A senha gravada no banco para este usuário está vazia.");
            return View(model);
        }

        if (!BCrypt.Net.BCrypt.Verify(model.Senha, usuario.SenhaHash))
        {
            ModelState.AddModelError(string.Empty, "Senha incorreta para este usuário.");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.Nome),
            new Claim(ClaimTypes.Email, usuario.Email),
            new Claim(ClaimTypes.Role, usuario.Perfil.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var authProps = new AuthenticationProperties
        {
            IsPersistent = model.LembrarMe,
            ExpiresUtc = model.LembrarMe ? DateTimeOffset.UtcNow.AddDays(30) : null
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProps);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    // GET: /Conta/Cadastro
    [HttpGet]
    public IActionResult Cadastro()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View();
    }

    // POST: /Conta/Cadastro
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cadastro(CadastroViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        if (model.Perfil == PerfilUsuario.Administrador)
        {
            ModelState.AddModelError("Perfil", "O perfil Administrador não pode ser selecionado.");
            return View(model);
        }

        var existente = await _usuarioRepository.ObterPorEmailAsync(model.Email);
        if (existente != null)
        {
            ModelState.AddModelError("Email", "Este e-mail já está cadastrado.");
            return View(model);
        }

        var usuario = new Usuario
        {
            Nome = model.Nome,
            Email = model.Email,
            SenhaHash = BCrypt.Net.BCrypt.HashPassword(model.Senha),
            Telefone = string.Empty,
            Perfil = model.Perfil,
            DataCadastro = DateTime.UtcNow
        };

        await _usuarioRepository.InserirAsync(usuario);

        TempData["Sucesso"] = "Conta criada com sucesso! Faça o login para continuar.";
        return RedirectToAction("Login");
    }

    // GET: /Conta/MeuPerfil
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> MeuPerfil()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized();
        }

        var usuario = await _usuarioRepository.ObterPorIdAsync(userId);
        if (usuario == null)
        {
            return NotFound();
        }

        var model = new PerfilViewModel
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
            DataUltimaAlteracao = usuario.DataUltimaAlteracao,
            UsuarioAlteracaoNome = usuario.UsuarioAlteracaoNome
        };

        return View(model);
    }

    // POST: /Conta/MeuPerfil
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MeuPerfil(PerfilViewModel model)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId) || userId != model.Id)
        {
            return Unauthorized();
        }

        var usuario = await _usuarioRepository.ObterPorIdAsync(userId);
        if (usuario == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Upload de logotipo do viveiro se enviado
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

        usuario.Nome = model.Nome;
        usuario.Telefone = model.Telefone ?? string.Empty;
        usuario.RazaoSocial = model.RazaoSocial;
        usuario.CpfCnpj = model.CpfCnpj;
        usuario.InscricaoEstadual = model.InscricaoEstadual;
        usuario.ChavePix = model.ChavePix;
        usuario.Banco = model.Banco;
        usuario.Agencia = model.Agencia;
        usuario.Conta = model.Conta;
        usuario.DescricaoViveiro = model.DescricaoViveiro;
        usuario.DataUltimaAlteracao = DateTime.UtcNow;
        usuario.UsuarioAlteracaoId = userId;
        usuario.UsuarioAlteracaoNome = User.Identity?.Name ?? usuario.Nome;

        await _usuarioRepository.AtualizarAsync(usuario);

        TempData["Sucesso"] = "Seus dados de perfil e viveiro foram atualizados com sucesso!";
        return RedirectToAction("MeuPerfil");
    }

    // GET: /Conta/Logout
    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }
}
