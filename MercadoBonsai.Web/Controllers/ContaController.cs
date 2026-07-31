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
using MercadoBonsai.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace MercadoBonsai.Web.Controllers;

public class ContaController : Controller
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPlanoRepository _planoRepository;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly VendedorTokenService _vendedorTokenService;

    public ContaController(
        IUsuarioRepository usuarioRepository, 
        IPlanoRepository planoRepository, 
        IWebHostEnvironment webHostEnvironment,
        VendedorTokenService vendedorTokenService)
    {
        _usuarioRepository = usuarioRepository;
        _planoRepository = planoRepository;
        _webHostEnvironment = webHostEnvironment;
        _vendedorTokenService = vendedorTokenService;
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

        // Ajuste de Segurança: Resposta unificada genérica "Dados de acesso inválidos" para evitar enumaracao/exposicao
        if (usuario == null || string.IsNullOrEmpty(usuario.SenhaHash) || !BCrypt.Net.BCrypt.Verify(model.Senha, usuario.SenhaHash))
        {
            ModelState.AddModelError(string.Empty, "Dados de acesso inválidos");
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

        var existe = await _usuarioRepository.ObterPorEmailAsync(model.Email);
        if (existe != null)
        {
            ModelState.AddModelError("Email", "Já existe um usuário cadastrado com este e-mail.");
            return View(model);
        }

        var senhaHash = BCrypt.Net.BCrypt.HashPassword(model.Senha);

        var novoUsuario = new Usuario
        {
            Nome = model.Nome.Trim(),
            Email = model.Email.Trim().ToLower(),
            SenhaHash = senhaHash,
            Telefone = string.Empty,
            Perfil = model.Perfil,
            DataCadastro = DateTime.UtcNow,
            PlanoId = 1, // Plano Padrão Pago (Bronze)
            Reputacao = 100
        };

        await _usuarioRepository.InserirAsync(novoUsuario);

        TempData["Sucesso"] = "Cadastro realizado com sucesso! Faça seu login para continuar.";
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

        var plano = await _planoRepository.ObterPorIdAsync(usuario.PlanoId);
        var nomePlano = plano?.Nome ?? (usuario.PlanoId == 1 ? "Bronze" : usuario.PlanoId == 2 ? "Prata" : usuario.PlanoId == 3 ? "Ouro" : "Free");
        bool liberarCartao = usuario.PlanoId >= 2;

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
            PlanoId = usuario.PlanoId,
            NomePlano = nomePlano,
            LiberarCartaoVisitas = liberarCartao,
            DataUltimaAlteracao = usuario.DataUltimaAlteracao,
            UsuarioAlteracaoNome = usuario.UsuarioAlteracaoNome
        };

        if (liberarCartao)
        {
            model.LinkVitrineCartao = Url.Action("Vitrine", "Cartao", new { token = _vendedorTokenService.GerarToken(usuario.Id, "vitrine") }, Request.Scheme);
            model.LinkInsumosCartao = Url.Action("Insumos", "Cartao", new { token = _vendedorTokenService.GerarToken(usuario.Id, "insumos") }, Request.Scheme);
            model.LinkVasosCartao = Url.Action("Vasos", "Cartao", new { token = _vendedorTokenService.GerarToken(usuario.Id, "vasos") }, Request.Scheme);
            model.LinkEngajamentoCartao = Url.Action("Engajamento", "Cartao", new { token = _vendedorTokenService.GerarToken(usuario.Id, "engajamento") }, Request.Scheme);
        }

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

    // GET: /Conta/Assinatura (Planos e Assinatura do Vendedor)
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Assinatura()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized();
        }

        var usuario = await _usuarioRepository.ObterPorIdAsync(userId);
        var planos = await _planoRepository.ListarTodosAsync();

        ViewData["PlanoAtualId"] = usuario?.PlanoId ?? 1;
        return View(planos);
    }

    // POST: /Conta/TrocarPlano
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TrocarPlano(int planoId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized();
        }

        var usuario = await _usuarioRepository.ObterPorIdAsync(userId);
        var plano = await _planoRepository.ObterPorIdAsync(planoId);

        if (usuario == null || plano == null)
        {
            return NotFound();
        }

        usuario.PlanoId = plano.Id;
        usuario.DataUltimaAlteracao = DateTime.UtcNow;
        usuario.UsuarioAlteracaoId = userId;
        usuario.UsuarioAlteracaoNome = usuario.Nome;

        await _usuarioRepository.AtualizarAsync(usuario);

        TempData["Sucesso"] = $"Parabéns! Sua assinatura foi atualizada com sucesso para o Plano {plano.Nome}.";
        return RedirectToAction("Assinatura");
    }

    // GET: /Conta/Logout
    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }
}
