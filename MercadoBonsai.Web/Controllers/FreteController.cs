using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MercadoBonsai.Domain.Interfaces;
using MercadoBonsai.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace MercadoBonsai.Web.Controllers;

public class FreteController : Controller
{
    private readonly IProdutoRepository _produtoRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IMelhorEnvioService _melhorEnvioService;

    public FreteController(
        IProdutoRepository produtoRepository,
        IUsuarioRepository usuarioRepository,
        IMelhorEnvioService melhorEnvioService)
    {
        _produtoRepository = produtoRepository;
        _usuarioRepository = usuarioRepository;
        _melhorEnvioService = melhorEnvioService;
    }

    // POST: /Frete/Calcular
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Calcular(int produtoId, string cepDestino)
    {
        if (produtoId <= 0)
        {
            return Json(new { success = false, message = "Produto inválido ou não informado." });
        }

        var produto = await _produtoRepository.ObterPorIdAsync(produtoId);
        if (produto == null)
        {
            return Json(new { success = false, message = "Anúncio não encontrado." });
        }

        // Validação de Opção de Envio: Se for "A retirar", suprime/bloqueia cálculo
        if (string.Equals(produto.FormaEnvio, "A retirar", System.StringComparison.OrdinalIgnoreCase))
        {
            return Json(new { success = false, message = "Este produto está disponível exclusivamente para a modalidade 'A retirar'. Não há opção de envio por transportadora." });
        }

        // Validação Próvia de Dados do Vendedor (Telefone + CPF/CNPJ)
        var vendedor = await _usuarioRepository.ObterPorIdAsync(produto.VendedorId);
        bool vendedorValido = vendedor != null 
            && !string.IsNullOrWhiteSpace(vendedor.Telefone) 
            && !string.IsNullOrWhiteSpace(vendedor.CpfCnpj);

        if (!vendedorValido)
        {
            return Json(new { 
                success = false, 
                message = "O vendedor responsável por este anúncio precisa cadastrar o telefone e os dados fiscais (CPF/CNPJ) no perfil para habilitar o cálculo de frete por transportadora." 
            });
        }

        // Validação Prévia de Dimensões e Peso do Produto
        bool especificacoesCompletas = produto.Altura > 0 
            && produto.Largura > 0 
            && produto.Comprimento > 0 
            && produto.Peso > 0;

        if (!especificacoesCompletas)
        {
            return Json(new { 
                success = false, 
                message = "Este produto possui especificações físicas (dimensões e/ou peso) incompletas para o cálculo de frete. Fale com o vendedor." 
            });
        }

        // Validação do CEP de Destino
        var cepDestinoLimpo = SomenteNumeros(cepDestino);
        if (string.IsNullOrWhiteSpace(cepDestinoLimpo) || cepDestinoLimpo.Length != 8)
        {
            return Json(new { success = false, message = "Por favor, digite um CEP de destino válido com 8 dígitos." });
        }

        var req = new CalculoFreteRequest
        {
            ProdutoId = produto.Id,
            CepOrigem = vendedor?.Cep ?? "01001-000",
            CepDestino = cepDestinoLimpo,
            Altura = produto.Altura,
            Largura = produto.Largura,
            Comprimento = produto.Comprimento,
            Peso = produto.Peso,
            Preco = produto.Preco // seguro enviado no payload (Requirement 4)
        };

        var resultados = await _melhorEnvioService.CalcularFreteAsync(req);

        return Json(new {
            success = true,
            cepDestino = FormatarCep(cepDestinoLimpo),
            opcoes = resultados
        });
    }

    private static string SomenteNumeros(string? input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        var sb = new StringBuilder();
        foreach (var c in input)
        {
            if (char.IsDigit(c)) sb.Append(c);
        }
        return sb.ToString();
    }

    private static string FormatarCep(string cep)
    {
        if (cep.Length == 8)
        {
            return $"{cep.Substring(0, 5)}-{cep.Substring(5, 3)}";
        }
        return cep;
    }
}
