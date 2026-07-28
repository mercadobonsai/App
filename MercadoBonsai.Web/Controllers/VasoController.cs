using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MercadoBonsai.Domain.Entities;
using MercadoBonsai.Domain.Interfaces;
using MercadoBonsai.Web.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace MercadoBonsai.Web.Controllers;

public class VasoController : Controller
{
    private readonly IProdutoRepository _produtoRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ILeilaoRepository _leilaoRepository;
    private readonly IRifaRepository _rifaRepository;
    private readonly IPatrocinioRepository _patrocinioRepository;
    private readonly IDicaCultivoRepository _dicaCultivoRepository;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public VasoController(
        IProdutoRepository produtoRepository,
        IUsuarioRepository usuarioRepository,
        ILeilaoRepository leilaoRepository,
        IRifaRepository rifaRepository,
        IPatrocinioRepository patrocinioRepository,
        IDicaCultivoRepository dicaCultivoRepository,
        IWebHostEnvironment webHostEnvironment)
    {
        _produtoRepository = produtoRepository;
        _usuarioRepository = usuarioRepository;
        _leilaoRepository = leilaoRepository;
        _rifaRepository = rifaRepository;
        _patrocinioRepository = patrocinioRepository;
        _dicaCultivoRepository = dicaCultivoRepository;
        _webHostEnvironment = webHostEnvironment;
    }

    // GET: /Vaso (Página Segmentada de Vasos e Cerâmicas em 2 Colunas)
    [HttpGet]
    public async Task<IActionResult> Index(string? busca)
    {
        var produtosVasos = await _produtoRepository.ListarPorCategoriasAsync("vaso");

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim().ToLower();
            produtosVasos = produtosVasos.Where(p => 
                p.Nome.ToLower().Contains(termo) || 
                (p.Descricao != null && p.Descricao.ToLower().Contains(termo)));
        }

        var viveirosDestaque = await _usuarioRepository.ListarViveirosEmDestaqueAsync();
        var leilaoAtivo = await _leilaoRepository.ObterLeilaoAtivoRecenteAsync();
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

        ViewData["Busca"] = busca;

        var viewModel = new HomeEngajamentoViewModel
        {
            ProdutosDestaque = produtosVasos,
            ViveirosEmDestaque = viveirosDestaque,
            LeilaoAtivo = leilaoAtivo,
            RifaAtiva = rifaAtiva,
            PatrocinioDestaque = patrocinioDestaque,
            DicaCultivoSemana = dicaJson ?? await _dicaCultivoRepository.ObterDicaRecenteAsync()
        };

        return View(viewModel);
    }
}
