using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MercadoBonsai.Domain.Entities;
using MercadoBonsai.Domain.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MercadoBonsai.Web.Controllers;

public class ProntuarioController : Controller
{
    private readonly IProntuarioRepository _prontuarioRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public ProntuarioController(
        IProntuarioRepository prontuarioRepository,
        IUsuarioRepository usuarioRepository,
        IWebHostEnvironment webHostEnvironment)
    {
        _prontuarioRepository = prontuarioRepository;
        _usuarioRepository = usuarioRepository;
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
            var folder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "prontuario");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var uniqueFileName = $"planta_{userId}_{Guid.NewGuid()}{Path.GetExtension(fotoArquivo.FileName)}";
            var filePath = Path.Combine(folder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await fotoArquivo.CopyToAsync(stream);
            }

            model.FotoPrincipalUrl = $"/uploads/prontuario/{uniqueFileName}";
        }
        else
        {
            model.FotoPrincipalUrl = "/starter-kit/assets/img/shimpaku_leilao.png";
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
    public async Task<IActionResult> AdicionarEvento(int plantaId, string titulo, string descricao, DateTime dataEvento, string? nomeAdubo, string? nomeremedio, IFormFile? fotoEvento)
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
            var folder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "prontuario");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var uniqueFileName = $"evento_{plantaId}_{Guid.NewGuid()}{Path.GetExtension(fotoEvento.FileName)}";
            var filePath = Path.Combine(folder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await fotoEvento.CopyToAsync(stream);
            }

            fotoUrl = $"/uploads/prontuario/{uniqueFileName}";
        }

        var evento = new ProntuarioEvento
        {
            PlantaId = plantaId,
            Titulo = string.IsNullOrWhiteSpace(titulo) ? "Manutenção Registrada" : titulo,
            Descricao = descricao,
            DataEvento = dataEvento != default ? dataEvento : DateTime.Now,
            FotoUrl = fotoUrl,
            NomeAdubo = planoPago ? nomeAdubo : null,
            NomeRemedio = planoPago ? nomeremedio : null,
            DataCriacao = DateTime.Now
        };

        await _prontuarioRepository.InserirEventoAsync(evento);

        // Atualizar datas na planta e renovar lock do usuário
        planta.DataUltimaManutencao = evento.DataEvento;
        if (!string.IsNullOrEmpty(nomeAdubo))
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
                FotoPrincipalUrl = "/starter-kit/assets/img/pinus_detalhe.png",
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
                FotoUrl = "/starter-kit/assets/img/pinus_rifa.png",
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
                FotoUrl = "/starter-kit/assets/img/shimpaku_detalhe.png",
                NomeAdubo = "Osmocote Plus 15-9-12",
                NomeRemedio = null,
                DataCriacao = DateTime.Now.AddMonths(-6)
            }
        };
    }
}
