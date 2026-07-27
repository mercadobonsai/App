using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using BCrypt.Net;
using MercadoBonsai.Domain.Entities;
using MercadoBonsai.Domain.Enums;
using MercadoBonsai.Domain.Interfaces;
using MercadoBonsai.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace MercadoBonsai.Web.Controllers;

public class ContaController : Controller
{
    private readonly IUsuarioRepository _usuarioRepository;

    public ContaController(IUsuarioRepository usuarioRepository)
    {
        _usuarioRepository = usuarioRepository;
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

        var usuario = await _usuarioRepository.ObterPorEmailAsync(model.Email);

        if (usuario == null || !BCrypt.Net.BCrypt.Verify(model.Senha, usuario.SenhaHash))
        {
            ModelState.AddModelError(string.Empty, "E-mail ou senha inválidos.");
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

        var existente = await _usuarioRepository.ObterPorEmailAsync(model.Email);
        if (existente != null)
        {
            ModelState.AddModelError("Email", "Este e-mail já está cadastrado.");
            return View(model);
        }

        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
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

    // GET: /Conta/Logout
    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }
}
