using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MercadoBonsai.Domain.Entities;
using MercadoBonsai.Domain.Interfaces;
using MercadoBonsai.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MercadoBonsai.Web.Controllers;

public class ProntuarioController : Controller
{
    private readonly IProntuarioRepository _prontuarioRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IStorageService _storageService;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public ProntuarioController(
        IProntuarioRepository prontuarioRepository,
        IUsuarioRepository usuarioRepository,
        IStorageService storageService,
        IWebHostEnvironment webHostEnvironment)
    {
        _prontuarioRepository = prontuarioRepository;
        _usuarioRepository = usuarioRepository;
        _storageService = storageService;
        _webHostEnvironment = webHostEnvironment;
    }

    // GET: /Prontuario
    public async Task<IActionResult> Index()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        int userId = 0;
        bool estaLogado = !string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out userId);

        IEnumerable<ProntuarioPlanta> plantas = new List<ProntuarioPlanta>();
        bool modoDemonstracao = false;

        if (estaLogado)
        {
            plantas = await _prontuarioRepository.ListarPlantasPorUsuarioAsync(userId);
        }

        // Se o usuário não possui plantas (ou não está logado), ativa o Modo de Demonstração com JSON Fictício
        if (!plantas.Any())
        {
            modoDemonstracao = true;
            plantas = ObterPlantasDemonstracaoFicticia();
        }

        ViewData["ModoDemonstracao"] = modoDemonstracao;
        return View(plantas);
    }

    // GET: /Prontuario/Detalhes/{id}
    public async Task<IActionResult> Detalhes(int id)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        int.TryParse(userIdClaim, out int userId);

        ProntuarioPlanta? planta = null;
        IEnumerable<ProntuarioEvento> eventos = new List<ProntuarioEvento>();
        bool modoDemonstracao = false;
        bool somenteLeitura = false;
        string? lockMensagem = null;

        if (id == 0 || id == 999) // ID da Planta Fictícia de Demonstração
        {
            modoDemonstracao = true;
            planta = ObterPlantasDemonstracaoFicticia().First();
            eventos = ObterEventosDemonstracaoFicticia();
        }
        else
        {
            planta = await _prontuarioRepository.ObterPlantaPorIdAsync(id);
            if (planta == null)
            {
                return NotFound();
            }

            // Controle de Concorrência: Verifica se a mesma planta está sob lock de edição por outro usuário há menos de 10 minutos
            bool lockAtivoPorOutro = planta.LockUsuarioId.HasValue 
                && planta.LockUsuarioId.Value != userId 
                && planta.LockTimestamp.HasValue 
                && planta.LockTimestamp.Value > DateTime.Now.AddMinutes(-10);

            if (lockAtivoPorOutro)
            {
                somenteLeitura = true;
                lockMensagem = $"Esta planta está sendo editada/atualizada pelo cultivador '{planta.LockUsuarioNome}' no momento. O acesso foi concedido em modo de Somente Leitura. Tente novamente mais tarde.";
            }
            else
            {
                // Registra ou renova o lock de edição para a sessão do usuário atual se ele estiver logado
                if (userId > 0)
                {
                    var nomeUsuario = User.Identity?.Name ?? "Cultivador";
                    await _prontuarioRepository.AdquirirOuRenovarLockAsync(id, userId, nomeUsuario);
                }
            }

            eventos = await _prontuarioRepository.ListarEventosPorPlantaAsync(id);
        }

        var usuario = userId > 0 ? await _usuarioRepository.ObterPorIdAsync(userId) : null;
        bool planoPago = (usuario?.PlanoId ?? 0) >= 1; // Plano Bronze (1), Prata (2) ou Ouro (3)

        ViewData["ModoDemonstracao"] = modoDemonstracao;
        ViewData["SomenteLeitura"] = somenteLeitura;
        ViewData["LockMensagem"] = lockMensagem;
        ViewData["PlanoPago"] = planoPago;
        ViewData["Eventos"] = eventos;

        return View(planta);
    }

    // GET: /Prontuario/Criar
    public IActionResult Criar()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            TempData["Erro"] = "Para cadastrar plantas reais no seu Prontuário, faça login ou cadastre-se.";
            return RedirectToAction("Login", "Conta");
        }
        return View();
    }

    // POST: /Prontuario/Criar
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(52428800)]
    [RequestFormLimits(MultipartBodyLengthLimit = 52428800)]
    public async Task<IActionResult> Criar(ProntuarioPlanta model, IFormFile? fotoArquivo)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            TempData["Erro"] = "Sua sessão expirou. Faça login para cadastrar a planta.";
            return RedirectToAction("Login", "Conta");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (fotoArquivo != null && fotoArquivo.Length > 0)
        {
            model.FotoPrincipalUrl = await _storageService.UploadImagemOtimizadaAsync(fotoArquivo, "prontuario", $"planta_{userId}");
        }
        else
        {
            model.FotoPrincipalUrl = "https://cdn.mercadobonsai.com.br/padrao/shimpaku_leilao.webp";
        }

        model.UsuarioId = userId;
        model.DataCriacao = DateTime.Now;

        int plantaId = await _prontuarioRepository.InserirPlantaAsync(model);

        // Inserir primeiro evento automático de registro
        var eventoInicial = new ProntuarioEvento
        {
            PlantaId = plantaId,
            Titulo = "🌱 Cadastro Inicial no Prontuário",
            Descricao = $"Planta cadastrada com sucesso no portal Mercado Bonsai. Espécie: {model.Especie}.",
            DataEvento = model.DataInicial,
            DataCriacao = DateTime.Now
        };
        await _prontuarioRepository.InserirEventoAsync(eventoInicial);

        TempData["Sucesso"] = $"Planta '{model.NomePopular}' cadastrada no Prontuário do Bonsai com sucesso!";
        return RedirectToAction("Detalhes", new { id = plantaId });
    }

    // POST: /Prontuario/AdicionarEvento
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(52428800)]
    [RequestFormLimits(MultipartBodyLengthLimit = 52428800)]
    public async Task<IActionResult> AdicionarEvento(
        int plantaId, 
        string titulo, 
        string descricao, 
        DateTime dataEvento, 
        string? nomeAdubo, 
        string? nomeRemedio, 
        string? nomeremedio, 
        IFormFile? fotoEvento)
    {
        if (plantaId == 0 || plantaId == 999)
        {
            TempData["Erro"] = "No modo de demonstração não é possível salvar novos eventos reais. Cadastre sua primeira planta para salvar!";
            return RedirectToAction("Detalhes", new { id = 999 });
        }

        var planta = await _prontuarioRepository.ObterPlantaPorIdAsync(plantaId);
        if (planta == null)
        {
            return NotFound();
        }

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized();
        }

        // Valida se a planta está bloqueada por outro usuário
        if (planta.LockUsuarioId.HasValue && planta.LockUsuarioId.Value != userId && planta.LockTimestamp.HasValue && planta.LockTimestamp.Value > DateTime.Now.AddMinutes(-10))
        {
            TempData["Erro"] = $"Esta planta está sendo editada/atualizada por outro cultivador ({planta.LockUsuarioNome}) no momento. Tente novamente mais tarde.";
            return RedirectToAction("Detalhes", new { id = plantaId });
        }

        var usuario = await _usuarioRepository.ObterPorIdAsync(userId);
        bool planoPago = (usuario?.PlanoId ?? 0) >= 1;

        string? fotoUrl = null;
        if (planoPago && fotoEvento != null && fotoEvento.Length > 0)
        {
            fotoUrl = await _storageService.UploadImagemOtimizadaAsync(fotoEvento, "prontuario", $"evento_{plantaId}");
        }

        string? remedioFinal = !string.IsNullOrWhiteSpace(nomeRemedio) ? nomeRemedio : nomeremedio;

        var evento = new ProntuarioEvento
        {
            PlantaId = plantaId,
            Titulo = string.IsNullOrWhiteSpace(titulo) ? "Manutenção Registrada" : titulo,
            Descricao = descricao,
            DataEvento = dataEvento != default ? dataEvento : DateTime.Now,
            FotoUrl = fotoUrl,
            NomeAdubo = string.IsNullOrWhiteSpace(nomeAdubo) ? null : nomeAdubo.Trim(),
            NomeRemedio = string.IsNullOrWhiteSpace(remedioFinal) ? null : remedioFinal.Trim(),
            DataCriacao = DateTime.Now
        };

        await _prontuarioRepository.InserirEventoAsync(evento);

        // Atualizar datas na planta e renovar lock do usuário
        planta.DataUltimaManutencao = evento.DataEvento;
        if (!string.IsNullOrEmpty(evento.NomeAdubo))
        {
            planta.DataUltimaAdubacao = evento.DataEvento;
        }
        await _prontuarioRepository.AtualizarPlantaAsync(planta);
        await _prontuarioRepository.AdquirirOuRenovarLockAsync(plantaId, userId, User.Identity?.Name ?? "Cultivador");

        TempData["Sucesso"] = "Novo evento de manutenção registrado na linha do tempo com sucesso!";
        return RedirectToAction("Detalhes", new { id = plantaId });
    }

    // POST: /Prontuario/LiberarEdicao
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LiberarEdicao(int id)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
        {
            await _prontuarioRepository.LiberarLockAsync(id, userId);
        }

        return RedirectToAction("Detalhes", new { id = id });
    }

    // GET: /Prontuario/Editar/{id}
    public async Task<IActionResult> Editar(int id)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            TempData["Erro"] = "Faça login para editar os dados da planta.";
            return RedirectToAction("Login", "Conta");
        }

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        int.TryParse(userIdClaim, out int userId);

        if (id == 0 || id == 999)
        {
            TempData["Erro"] = "No modo de demonstração não é possível editar a planta de exemplo.";
            return RedirectToAction("Index");
        }

        var planta = await _prontuarioRepository.ObterPlantaPorIdAsync(id);
        if (planta == null)
        {
            return NotFound();
        }

        if (planta.UsuarioId != userId)
        {
            TempData["Erro"] = "Você só pode editar plantas cadastradas no seu próprio prontuário.";
            return RedirectToAction("Index");
        }

        // Verifica concorrência
        bool lockAtivoPorOutro = planta.LockUsuarioId.HasValue 
            && planta.LockUsuarioId.Value != userId 
            && planta.LockTimestamp.HasValue 
            && planta.LockTimestamp.Value > DateTime.Now.AddMinutes(-10);

        if (lockAtivoPorOutro)
        {
            TempData["Erro"] = $"Esta planta está sendo editada pelo cultivador '{planta.LockUsuarioNome}' no momento.";
            return RedirectToAction("Detalhes", new { id = id });
        }

        await _prontuarioRepository.AdquirirOuRenovarLockAsync(id, userId, User.Identity?.Name ?? "Cultivador");
        return View(planta);
    }

    // POST: /Prontuario/Editar
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(52428800)]
    [RequestFormLimits(MultipartBodyLengthLimit = 52428800)]
    public async Task<IActionResult> Editar(ProntuarioPlanta model, IFormFile? novaFoto)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            TempData["Erro"] = "Sua sessão expirou. Faça login novamente.";
            return RedirectToAction("Login", "Conta");
        }

        if (model.Id == 0 || model.Id == 999)
        {
            TempData["Erro"] = "A planta de demonstração não pode ser alterada.";
            return RedirectToAction("Index");
        }

        var plantaExistente = await _prontuarioRepository.ObterPlantaPorIdAsync(model.Id);
        if (plantaExistente == null)
        {
            return NotFound();
        }

        if (plantaExistente.UsuarioId != userId)
        {
            return Unauthorized();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Se uma nova foto foi enviada, faz upload otimizado para o Cloudflare R2
        if (novaFoto != null && novaFoto.Length > 0)
        {
            string urlFoto = await _storageService.UploadImagemOtimizadaAsync(novaFoto, "prontuario", $"planta_{model.Id}");
            if (!string.IsNullOrEmpty(urlFoto))
            {
                plantaExistente.FotoPrincipalUrl = urlFoto;
            }
        }
        else if (!string.IsNullOrEmpty(model.FotoPrincipalUrl))
        {
            plantaExistente.FotoPrincipalUrl = model.FotoPrincipalUrl;
        }

        // Atualiza campos
        plantaExistente.NomePopular = model.NomePopular;
        plantaExistente.NomeCientifico = model.NomeCientifico;
        plantaExistente.Especie = model.Especie;
        plantaExistente.Altura = model.Altura;
        plantaExistente.Largura = model.Largura;
        plantaExistente.Comprimento = model.Comprimento;
        plantaExistente.Peso = model.Peso;
        plantaExistente.DescricaoLivre = model.DescricaoLivre;
        plantaExistente.DataInicial = model.DataInicial;
        plantaExistente.DataProximaManutencao = model.DataProximaManutencao;
        plantaExistente.DataProximaAdubacao = model.DataProximaAdubacao;

        await _prontuarioRepository.AtualizarPlantaAsync(plantaExistente);
        await _prontuarioRepository.LiberarLockAsync(model.Id, userId);

        TempData["Sucesso"] = $"Dados e foto de '{plantaExistente.NomePopular}' atualizados com sucesso!";
        return RedirectToAction("Detalhes", new { id = model.Id });
    }

    // POST: /Prontuario/EditarEvento
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(52428800)]
    [RequestFormLimits(MultipartBodyLengthLimit = 52428800)]
    public async Task<IActionResult> EditarEvento(
        int id, 
        int plantaId, 
        string titulo, 
        string descricao, 
        DateTime dataEvento, 
        string? nomeAdubo, 
        string? nomeRemedio, 
        string? nomeremedio, 
        IFormFile? novaFotoEvento, 
        bool removerFoto = false)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized();
        }

        var planta = await _prontuarioRepository.ObterPlantaPorIdAsync(plantaId);
        if (planta == null)
        {
            return NotFound();
        }

        if (planta.UsuarioId != userId)
        {
            return Unauthorized();
        }

        var evento = await _prontuarioRepository.ObterEventoPorIdAsync(id);
        if (evento == null || evento.PlantaId != plantaId)
        {
            return NotFound();
        }

        var usuario = await _usuarioRepository.ObterPorIdAsync(userId);
        bool planoPago = (usuario?.PlanoId ?? 0) >= 1;

        if (removerFoto)
        {
            evento.FotoUrl = null;
        }
        else if (planoPago && novaFotoEvento != null && novaFotoEvento.Length > 0)
        {
            string fotoUrl = await _storageService.UploadImagemOtimizadaAsync(novaFotoEvento, "prontuario", $"evento_{plantaId}");
            if (!string.IsNullOrEmpty(fotoUrl))
            {
                evento.FotoUrl = fotoUrl;
            }
        }

        string? remedioFinal = !string.IsNullOrWhiteSpace(nomeRemedio) ? nomeRemedio : nomeremedio;

        evento.Titulo = string.IsNullOrWhiteSpace(titulo) ? "Manutenção Registrada" : titulo.Trim();
        evento.Descricao = descricao;
        evento.DataEvento = dataEvento != default ? dataEvento : DateTime.Now;
        evento.NomeAdubo = string.IsNullOrWhiteSpace(nomeAdubo) ? null : nomeAdubo.Trim();
        evento.NomeRemedio = string.IsNullOrWhiteSpace(remedioFinal) ? null : remedioFinal.Trim();

        await _prontuarioRepository.AtualizarEventoAsync(evento);

        TempData["Sucesso"] = "Ação de manutenção atualizada com sucesso!";
        return RedirectToAction("Detalhes", new { id = plantaId });
    }

    // POST: /Prontuario/ExcluirEvento
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExcluirEvento(int id, int plantaId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized();
        }

        var planta = await _prontuarioRepository.ObterPlantaPorIdAsync(plantaId);
        if (planta == null)
        {
            return NotFound();
        }

        if (planta.UsuarioId != userId)
        {
            return Unauthorized();
        }

        var evento = await _prontuarioRepository.ObterEventoPorIdAsync(id);
        if (evento == null || evento.PlantaId != plantaId)
        {
            return NotFound();
        }

        await _prontuarioRepository.DeletarEventoAsync(id);

        TempData["Sucesso"] = "Ação de manutenção excluída do histórico com sucesso!";
        return RedirectToAction("Detalhes", new { id = plantaId });
    }

    // GET: /Prontuario/VenderPlanta/{id}
    public async Task<IActionResult> VenderPlanta(int id)
    {
        ProntuarioPlanta? planta = null;
        if (id == 999 || id == 0)
        {
            planta = ObterPlantasDemonstracaoFicticia().First();
        }
        else
        {
            planta = await _prontuarioRepository.ObterPlantaPorIdAsync(id);
        }

        if (planta == null)
        {
            return NotFound();
        }

        // Redireciona para /Produto/Criar preenchendo os dados automaticamente
        return RedirectToAction("Criar", "Produto", new
        {
            nome = planta.NomePopular,
            descricao = $"Exemplar de Prontuário: {planta.NomePopular} ({planta.Especie}). {planta.DescricaoLivre}",
            altura = planta.Altura,
            largura = planta.Largura,
            comprimento = planta.Comprimento,
            peso = planta.Peso,
            imagemUrl = planta.FotoPrincipalUrl
        });
    }

    // Mock Fictício para Modo de Demonstração
    private IEnumerable<ProntuarioPlanta> ObterPlantasDemonstracaoFicticia()
    {
        return new List<ProntuarioPlanta>
        {
            new ProntuarioPlanta
            {
                Id = 999,
                UsuarioId = 0,
                NomePopular = "Pinus Kuromatsu Imponente",
                NomeCientifico = "Pinus thunbergii",
                Especie = "Pinus Negro Japonês",
                Altura = 65.00m,
                Largura = 48.00m,
                Comprimento = 52.00m,
                Peso = 8.500m,
                DescricaoLivre = "Exemplar de demonstração do Prontuário. Importado com estilo Moyogi (Ereto Informal), casca craquelada bem definida e agulhas compactadas.",
                FotoPrincipalUrl = "https://cdn.mercadobonsai.com.br/padrao/pinus_detalhe.webp",
                DataInicial = DateTime.Now.AddYears(-3),
                DataUltimaManutencao = DateTime.Now.AddDays(-15),
                DataProximaManutencao = DateTime.Now.AddDays(45),
                DataUltimaAdubacao = DateTime.Now.AddDays(-30),
                DataProximaAdubacao = DateTime.Now.AddDays(30),
                DataCriacao = DateTime.Now.AddYears(-3)
            }
        };
    }

    private IEnumerable<ProntuarioEvento> ObterEventosDemonstracaoFicticia()
    {
        return new List<ProntuarioEvento>
        {
            new ProntuarioEvento
            {
                Id = 101,
                PlantaId = 999,
                Titulo = "✂️ Poda Estrutural e Desfolha de Primavera",
                Descricao = "Realizada poda de seleção dos brotos fortes do topo para balancear o vigor com os galhos inferiores. Retirada de agulhas velhas de 2 anos.",
                DataEvento = DateTime.Now.AddDays(-15),
                FotoUrl = "https://cdn.mercadobonsai.com.br/padrao/pinus_rifa.webp",
                NomeAdubo = "Hanagokoro Orgânico 5-5-5",
                NomeRemedio = "Calda Sulfocálcica (Preventivo)",
                DataCriacao = DateTime.Now.AddDays(-15)
            },
            new ProntuarioEvento
            {
                Id = 102,
                PlantaId = 999,
                Titulo = "🪴 Transplante com Substrato Importado (Akadama + Kiryu)",
                Descricao = "Troca de vaso tradicional de cerâmica Yixing. Limpeza de 30% da macega de raízes e renovação do substrato (70% Akadama + 30% Kiryu).",
                DataEvento = DateTime.Now.AddMonths(-6),
                FotoUrl = "https://cdn.mercadobonsai.com.br/padrao/shimpaku_detalhe.webp",
                NomeAdubo = "Osmocote Plus 15-9-12",
                NomeRemedio = null,
                DataCriacao = DateTime.Now.AddMonths(-6)
            }
        };
    }
}
