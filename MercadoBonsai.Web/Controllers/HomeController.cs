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
    private readonly IWebHostEnvironment _webHostEnvironment;

    public HomeController(
        IProdutoRepository produtoRepository,
        IUsuarioRepository usuarioRepository,
        ILeilaoRepository leilaoRepository,
        IRifaRepository rifaRepository,
        IPatrocinioRepository patrocinioRepository,
        IDicaCultivoRepository dicaCultivoRepository,
        IPlanoRepository planoRepository,
        IWebHostEnvironment webHostEnvironment)
    {
        _produtoRepository = produtoRepository;
        _usuarioRepository = usuarioRepository;
        _leilaoRepository = leilaoRepository;
        _rifaRepository = rifaRepository;
        _patrocinioRepository = patrocinioRepository;
        _dicaCultivoRepository = dicaCultivoRepository;
        _planoRepository = planoRepository;
        _webHostEnvironment = webHostEnvironment;
    }

    // GET: / (Home Oficial do Portal com Conexão ao Banco e JSON)
    public async Task<IActionResult> Index()
    {
        var todosProdutos = await _produtoRepository.ListarTodosAsync();
        var todosUsuarios = await _usuarioRepository.ListarTodosAsync(null, null);
        var usuariosMap = todosUsuarios.ToDictionary(u => u.Id);

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

        var viveirosDestaque = await _usuarioRepository.ListarViveirosEmDestaqueAsync();
        var leilaoAtivo = await _leilaoRepository.ObterLeilaoAtivoRecenteAsync();
        var leiloesAtivos = await _leilaoRepository.ListarAtivosAsync();
        var rifaAtiva = await _rifaRepository.ObterRifaAtivaRecenteAsync();
        var patrocinioDestaque = await _patrocinioRepository.ObterPatrocinioDestaqueAsync();

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
            ViveirosEmDestaque = viveirosDestaque,
            LeilaoAtivo = leilaoAtivo,
            LeiloesAtivos = leiloesAtivos,
            RifaAtiva = rifaAtiva,
            PatrocinioDestaque = patrocinioDestaque,
            DicaCultivoSemana = dicaJson ?? await _dicaCultivoRepository.ObterDicaRecenteAsync()
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
