using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MercadoBonsai.Web.Models;
using MercadoBonsai.Domain.Interfaces;

namespace MercadoBonsai.Web.Controllers;

public class HomeController : Controller
{
    private readonly IProdutoRepository _produtoRepository;

    public HomeController(IProdutoRepository produtoRepository)
    {
        _produtoRepository = produtoRepository;
    }

    public async Task<IActionResult> Index()
    {
        var produtos = await _produtoRepository.ListarParaHomeAsync();
        return View(produtos);
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
