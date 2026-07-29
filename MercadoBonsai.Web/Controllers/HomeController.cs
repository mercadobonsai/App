using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using MercadoBonsai.Web.Models;
using MercadoBonsai.Domain.Entities;
using MercadoBonsai.Domain.Enums;
using MercadoBonsai.Domain.Interfaces;

namespace MercadoBonsai.Web.Controllers;

public class HomeController : Controller
{
    private readonly IProdutoRepository _produtoRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ILeilaoRepository _leilaoRepository;
    private readonly IRifaRepository _rifaRepository;
    private readonly IPatrocinioRepository _patrocinioRepository;
    private readonly IDicaCultivoRepository _dicaCultivoRepository;
    private readonly IPlanoRepository _planoRepository;
    private readonly IPropagandaRepository _propagandaRepository;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public HomeController(
        IProdutoRepository produtoRepository,
        IUsuarioRepository usuarioRepository,
        ILeilaoRepository leilaoRepository,
        IRifaRepository rifaRepository,
        IPatrocinioRepository patrocinioRepository,
        IDicaCultivoRepository dicaCultivoRepository,
        IPlanoRepository planoRepository,
        IPropagandaRepository propagandaRepository,
        IWebHostEnvironment webHostEnvironment)
    {
        _produtoRepository = produtoRepository;
        _usuarioRepository = usuarioRepository;
        _leilaoRepository = leilaoRepository;
        _rifaRepository = rifaRepository;
        _patrocinioRepository = patrocinioRepository;
        _dicaCultivoRepository = dicaCultivoRepository;
        _planoRepository = planoRepository;
        _propagandaRepository = propagandaRepository;
        _webHostEnvironment = webHostEnvironment;
    }

    // GET: / (Home Oficial do Portal com Conexão ao Banco e JSON)
    public async Task<IActionResult> Index()
    {
        // 1. Busca produtos principais
        var todosProdutos = await _produtoRepository.ListarTodosAsync();

        // Otimização de Performance: Buscar apenas os vendedores dos produtos carregados para a Home
        var vendedorIds = todosProdutos.Select(p => p.VendedorId).Distinct().ToList();
        var usuariosMap = new Dictionary<int, Usuario>();
        foreach (var vendedorId in vendedorIds)
        {
            var seller = await _usuarioRepository.ObterPorIdAsync(vendedorId);
            if (seller != null)
            {
                usuariosMap[seller.Id] = seller;
            }
        }

        // Regra de Negócio: Produtos do Plano Bronze (PlanoId = 1) NÃO aparecem nos Destaques da Semana da Home
        var produtosDestaque = todosProdutos
            .Where(p => p.Status != StatusProduto.Vendido)
            .Where(p => {
                if (usuariosMap.TryGetValue(p.VendedorId, out var vendedor))
                {
                    return vendedor.PlanoId > 1; // Apenas Plano Prata e Ouro aparecem na Home
                }
                return true;
            })
            .Take(6);

        if (!produtosDestaque.Any())
        {
            produtosDestaque = todosProdutos.Where(p => p.Status != StatusProduto.Vendido).Take(6);
        }

        // 2. Executa em paralelo as demais consultas leves da Home para garantir tempo de resposta rápido e evitar Timeouts
        var taskViveiros = _usuarioRepository.ListarViveirosEmDestaqueAsync();
        var taskLeilaoAtivo = _leilaoRepository.ObterLeilaoAtivoRecenteAsync();
        var taskLeiloesAtivos = _leilaoRepository.ListarAtivosAsync();
        var taskRifaAtiva = _rifaRepository.ObterRifaAtivaRecenteAsync();
        var taskPatrocinio = _patrocinioRepository.ObterPatrocinioDestaqueAsync();

        var taskPropEconomico = _propagandaRepository.ListarAtivasPorTipoAsync("Economico");
        var taskPropBasico = _propagandaRepository.ListarAtivasPorTipoAsync("Basico");
        var taskPropIntermediario = _propagandaRepository.ListarAtivasPorTipoAsync("Intermediario");
        var taskPropAvancado = _propagandaRepository.ListarAtivasPorTipoAsync("Avancado");

        await Task.WhenAll(
            taskViveiros, taskLeilaoAtivo, taskLeiloesAtivos, taskRifaAtiva, taskPatrocinio,
            taskPropEconomico, taskPropBasico, taskPropIntermediario, taskPropAvancado
        );

        DicaCultivo? dicaJson = null;
        try
        {
            var jsonPath = Path.Combine(_webHostEnvironment.WebRootPath, "data", "dicas_cultivo.json");
            if (System.IO.File.Exists(jsonPath))
            {
                using var stream = System.IO.File.OpenRead(jsonPath);
                var listaDicas = await JsonSerializer.DeserializeAsync<List<DicaCultivo>>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                dicaJson = listaDicas?.FirstOrDefault();
            }
        }
        catch
        {
            dicaJson = await _dicaCultivoRepository.ObterDicaRecenteAsync();
        }

        var viewModel = new HomeEngajamentoViewModel
        {
            ProdutosDestaque = produtosDestaque,
            ViveirosEmDestaque = await taskViveiros,
            LeilaoAtivo = await taskLeilaoAtivo,
            LeiloesAtivos = await taskLeiloesAtivos,
            RifaAtiva = await taskRifaAtiva,
            PatrocinioDestaque = await taskPatrocinio,
            DicaCultivoSemana = dicaJson ?? await _dicaCultivoRepository.ObterDicaRecenteAsync(),
            PropagandasEconomico = await taskPropEconomico,
            PropagandasBasico = await taskPropBasico,
            PropagandasIntermediario = await taskPropIntermediario,
            PropagandasAvancado = await taskPropAvancado
        };

        return View(viewModel);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
