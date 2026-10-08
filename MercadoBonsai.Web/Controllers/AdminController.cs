using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MercadoBonsai.Domain.Entities;
using MercadoBonsai.Domain.Enums;
using MercadoBonsai.Domain.Interfaces;
using MercadoBonsai.Web.Models;
using MercadoBonsai.Web.Services;
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
    private readonly IAsaasService _asaasService;
    private readonly VendedorTokenService _vendedorTokenService;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public AdminController(
        IUsuarioRepository usuarioRepository, 
        IPlanoRepository planoRepository, 
        IPropagandaRepository propagandaRepository,
        IAsaasService asaasService,
        VendedorTokenService vendedorTokenService,
        IWebHostEnvironment webHostEnvironment)
    {
        _usuarioRepository = usuarioRepository;
        _planoRepository = planoRepository;
        _propagandaRepository = propagandaRepository;
        _asaasService = asaasService;
        _vendedorTokenService = vendedorTokenService;
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

        var plano = await _planoRepository.ObterPorIdAsync(usuario.PlanoId);
        var nomePlano = plano?.Nome ?? (usuario.PlanoId == 1 ? "Bronze" : usuario.PlanoId == 2 ? "Prata" : usuario.PlanoId == 3 ? "Ouro" : usuario.PlanoId == 4 ? "Diamante" : "Free");
        bool liberarCartao = usuario.PlanoId >= 2;

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
            DataNascimento = usuario.DataNascimento,
            RendaFaturamento = usuario.RendaFaturamento,
            Cep = usuario.Cep,
            Logradouro = usuario.Logradouro,
            Numero = usuario.Numero,
            Complemento = usuario.Complemento,
            Bairro = usuario.Bairro,
            Cidade = usuario.Cidade,
            Estado = usuario.Estado,
            ChavePix = usuario.ChavePix,
            Banco = usuario.Banco,
            Agencia = usuario.Agencia,
            Conta = usuario.Conta,
            DescricaoViveiro = usuario.DescricaoViveiro,
            LogotipoUrl = usuario.LogotipoUrl,
            PlanoId = usuario.PlanoId,
            NomePlano = nomePlano,
            LiberarCartaoVisitas = liberarCartao,
            IsentoCobranca = usuario.IsentoCobranca,
            AsaasCustomerId = usuario.AsaasCustomerId,
            AsaasAccountId = usuario.AsaasAccountId,
            AsaasSubscriptionId = usuario.AsaasSubscriptionId,
            PercentualRetencaoPersonalizado = usuario.PercentualRetencaoPersonalizado,
            DataUltimaAlteracao = usuario.DataUltimaAlteracao,
            UsuarioAlteracaoNome = usuario.UsuarioAlteracaoNome
        };

        if (liberarCartao)
        {
            viewModel.LinkVitrineCartao = Url.Action("Vitrine", "Cartao", new { token = _vendedorTokenService.GerarToken(usuario.Id, "vitrine") }, Request.Scheme);
            viewModel.LinkInsumosCartao = Url.Action("Insumos", "Cartao", new { token = _vendedorTokenService.GerarToken(usuario.Id, "insumos") }, Request.Scheme);
            viewModel.LinkVasosCartao = Url.Action("Vasos", "Cartao", new { token = _vendedorTokenService.GerarToken(usuario.Id, "vasos") }, Request.Scheme);
            viewModel.LinkEngajamentoCartao = Url.Action("Engajamento", "Cartao", new { token = _vendedorTokenService.GerarToken(usuario.Id, "engajamento") }, Request.Scheme);
        }

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
        usuario.DataNascimento = model.DataNascimento;
        usuario.RendaFaturamento = model.RendaFaturamento;
        usuario.Cep = model.Cep;
        usuario.Logradouro = model.Logradouro;
        usuario.Numero = model.Numero;
        usuario.Complemento = model.Complemento;
        usuario.Bairro = model.Bairro;
        usuario.Cidade = model.Cidade;
        usuario.Estado = model.Estado;
        usuario.ChavePix = model.ChavePix;
        usuario.Banco = model.Banco;
        usuario.Agencia = model.Agencia;
        usuario.Conta = model.Conta;
        usuario.DescricaoViveiro = model.DescricaoViveiro;
        usuario.IsentoCobranca = model.IsentoCobranca;
        usuario.PercentualRetencaoPersonalizado = model.PercentualRetencaoPersonalizado;
        usuario.DataUltimaAlteracao = DateTime.UtcNow;
        usuario.UsuarioAlteracaoId = adminId;
        usuario.UsuarioAlteracaoNome = User.Identity?.Name ?? "Administrador";

        await _usuarioRepository.AtualizarAsync(usuario);

        // Tenta sincronizar Asaas se o admin tiver preenchido os dados e estiver sem subconta
        bool dadosCompletos = !string.IsNullOrWhiteSpace(usuario.CpfCnpj)
            && !string.IsNullOrWhiteSpace(usuario.Telefone)
            && !string.IsNullOrWhiteSpace(usuario.Cep)
            && !string.IsNullOrWhiteSpace(usuario.Logradouro);

        if (dadosCompletos && (usuario.Perfil == PerfilUsuario.Vendedor || usuario.Perfil == PerfilUsuario.Administrador))
        {
            var msgsAsaas = new List<string>();

            if (string.IsNullOrEmpty(usuario.AsaasCustomerId) || usuario.AsaasCustomerId.StartsWith("cus_simulado_"))
            {
                var resCliente = await _asaasService.CriarClienteAsync(usuario);
                if (resCliente.Sucesso && !string.IsNullOrEmpty(resCliente.AsaasCustomerId))
                {
                    usuario.AsaasCustomerId = resCliente.AsaasCustomerId;
                    await _usuarioRepository.AtualizarAsync(usuario);
                }
                else if (!resCliente.Sucesso && !string.IsNullOrEmpty(resCliente.MensagemErro))
                {
                    msgsAsaas.Add($"[Cliente]: {resCliente.MensagemErro}");
                }
            }

            if (string.IsNullOrEmpty(usuario.AsaasAccountId) || usuario.AsaasAccountId.StartsWith("acc_simulada_") || usuario.AsaasAccountId.StartsWith("sub_simulada_"))
            {
                var resSubconta = await _asaasService.CriarSubcontaVendedorAsync(usuario);
                if (resSubconta.Sucesso && !string.IsNullOrEmpty(resSubconta.AsaasAccountId))
                {
                    usuario.AsaasAccountId = resSubconta.AsaasAccountId;
                    await _usuarioRepository.AtualizarAsync(usuario);
                }
                else if (!resSubconta.Sucesso && !string.IsNullOrEmpty(resSubconta.MensagemErro))
                {
                    msgsAsaas.Add($"[Subconta]: {resSubconta.MensagemErro}");
                }
            }

            if (msgsAsaas.Count > 0)
            {
                TempData["Aviso"] = "Dados salvos, porém a integração Asaas alertou: " + string.Join(" | ", msgsAsaas);
            }
        }

        TempData["Sucesso"] = $"Cadastro do cliente '{usuario.Nome}' atualizado com sucesso!";
        return RedirectToAction("Clientes");
    }

    // GET: /Admin/SubcontasAsaas
    [HttpGet]
    public async Task<IActionResult> SubcontasAsaas()
    {
        var todosUsuarios = await _usuarioRepository.ListarTodosAsync(null, null);
        // Filtra vendedores ou administradores com ou sem subconta Asaas
        var vendedores = todosUsuarios
            .Where(u => u.Perfil == PerfilUsuario.Vendedor || u.Perfil == PerfilUsuario.Administrador || !string.IsNullOrEmpty(u.AsaasAccountId))
            .OrderByDescending(u => u.DataCadastro);

        return View(vendedores);
    }

    // POST: /Admin/EncerrarSubconta
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EncerrarSubconta(int usuarioId)
    {
        var usuario = await _usuarioRepository.ObterPorIdAsync(usuarioId);
        if (usuario == null)
        {
            TempData["Erro"] = "Usuário não localizado.";
            return RedirectToAction("SubcontasAsaas");
        }

        if (string.IsNullOrEmpty(usuario.AsaasAccountId))
        {
            TempData["Erro"] = $"O usuário '{usuario.Nome}' não possui nenhuma Subconta Asaas ativa no momento.";
            return RedirectToAction("SubcontasAsaas");
        }

        var accountIdOriginal = usuario.AsaasAccountId;
        var result = await _asaasService.EncerrarSubcontaAsync(accountIdOriginal);

        if (result.Sucesso)
        {
            var adminIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(adminIdClaim, out int adminId);

            usuario.AsaasAccountId = null;
            usuario.DataUltimaAlteracao = DateTime.UtcNow;
            usuario.UsuarioAlteracaoId = adminId;
            usuario.UsuarioAlteracaoNome = User.Identity?.Name ?? "Administrador";

            await _usuarioRepository.AtualizarAsync(usuario);

            TempData["Sucesso"] = $"Subconta Asaas '{accountIdOriginal}' de '{usuario.Nome}' foi encerrada com sucesso na API Asaas!";
        }
        else
        {
            TempData["Erro"] = $"Não foi possível encerrar a Subconta Asaas na API: {result.MensagemErro}";
        }

        return RedirectToAction("SubcontasAsaas");
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
    public async Task<IActionResult> SalvarPlano([FromForm] Microsoft.AspNetCore.Http.IFormCollection form)
    {
        int.TryParse(form["Id"], out int id);
        var plano = await _planoRepository.ObterPorIdAsync(id);
        if (plano == null)
        {
            TempData["Erro"] = "Plano não localizado.";
            return RedirectToAction("Planos");
        }

        var nome = form["Nome"].ToString().Trim();
        if (!string.IsNullOrWhiteSpace(nome))
        {
            plano.Nome = nome;
        }

        plano.Preco = ParseDecimalFlex(form["Preco"], plano.Preco);
        plano.PercentualComissao = ParseDecimalFlex(form["PercentualComissao"], plano.PercentualComissao);

        if (int.TryParse(form["LimiteRifas30Dias"], out int limiteRifas)) plano.LimiteRifas30Dias = limiteRifas;
        if (int.TryParse(form["LimiteLeiloes30Dias"], out int limiteLeiloes)) plano.LimiteLeiloes30Dias = limiteLeiloes;
        if (int.TryParse(form["LimiteAnuncios"], out int limiteAnuncios)) plano.LimiteAnuncios = limiteAnuncios;
        if (int.TryParse(form["LimiteFotos"], out int limiteFotos)) plano.LimiteFotos = limiteFotos;

        var destaquesValues = form["DestaquesHome"];
        plano.DestaquesHome = destaquesValues.Contains("true");

        await _planoRepository.AtualizarAsync(plano);

        TempData["Sucesso"] = $"Configurações do Plano '{plano.Nome}' atualizadas com sucesso!";
        return RedirectToAction("Planos");
    }

    private static decimal ParseDecimalFlex(string? input, decimal defaultValue = 0m)
    {
        if (string.IsNullOrWhiteSpace(input)) return defaultValue;
        input = input.Trim();
        if (input.Contains(',') && !input.Contains('.'))
        {
            input = input.Replace(',', '.');
        }
        else if (input.Contains(',') && input.Contains('.'))
        {
            input = input.Replace(".", "").Replace(',', '.');
        }

        if (decimal.TryParse(input, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal val))
        {
            return val;
        }
        return defaultValue;
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
