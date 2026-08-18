using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MercadoBonsai.Domain.Entities;
using MercadoBonsai.Domain.Enums;
using MercadoBonsai.Domain.Interfaces;
using MercadoBonsai.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MercadoBonsai.Web.Controllers;

[Authorize]
public class PedidoController : Controller
{
    private readonly IPedidoRepository _pedidoRepository;
    private readonly IProdutoRepository _produtoRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IEvendasWebhookService _evendasWebhookService;
    private readonly IAsaasService _asaasService;

    public PedidoController(
        IPedidoRepository pedidoRepository,
        IProdutoRepository produtoRepository,
        IUsuarioRepository usuarioRepository,
        IEvendasWebhookService evendasWebhookService,
        IAsaasService asaasService)
    {
        _pedidoRepository = pedidoRepository;
        _produtoRepository = produtoRepository;
        _usuarioRepository = usuarioRepository;
        _evendasWebhookService = evendasWebhookService;
        _asaasService = asaasService;
    }

    private int ObterUsuarioLogadoId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null && int.TryParse(claim.Value, out int id) ? id : 0;
    }

    // POST: /Pedido/Criar
    [HttpPost]
    public async Task<IActionResult> Criar(int produtoId, decimal? valorFreteInformado)
    {
        int compradorId = ObterUsuarioLogadoId();
        if (compradorId <= 0)
        {
            TempData["Erro"] = "Sessão expirada. Por favor, faça login novamente.";
            return RedirectToAction("Login", "Conta");
        }

        var produto = await _produtoRepository.ObterPorIdAsync(produtoId);
        if (produto == null)
        {
            TempData["Erro"] = "Produto não encontrado.";
            return RedirectToAction("Index", "Produto");
        }

        if (produto.VendedorId == compradorId)
        {
            TempData["Erro"] = "Você não pode comprar um anúncio cadastrado por você mesmo.";
            return RedirectToAction("Detalhes", "Produto", new { id = produtoId });
        }

        var comprador = await _usuarioRepository.ObterPorIdAsync(compradorId);
        var vendedor = await _usuarioRepository.ObterPorIdAsync(produto.VendedorId);

        if (comprador == null || vendedor == null)
        {
            TempData["Erro"] = "Dados dos usuários incompletos para gerar o pedido.";
            return RedirectToAction("Detalhes", "Produto", new { id = produtoId });
        }

        int proximoNumero = await _pedidoRepository.ObterProximoNumeroPedidoAsync();
        decimal freteInicial = valorFreteInformado ?? 0.00m;
        decimal valorTotal = produto.Preco + freteInicial;

        var pedido = new Pedido
        {
            Numero = proximoNumero,
            CompradorId = comprador.Id,
            VendedorId = vendedor.Id,
            ProdutoId = produto.Id,
            StatusPedido = StatusPedido.Criado,
            TipoPagamento = "PIX / Asaas",
            DataPedido = DateTime.Now,
            ValorPedido = produto.Preco,
            ValorFrete = freteInicial,
            ValorSeguro = 0.00m,
            ValorTotal = valorTotal,
            CompradorNome = comprador.Nome,
            CompradorEmail = comprador.Email,
            CompradorTelefone = comprador.Telefone,
            CompradorEndereco = $"{comprador.Logradouro}, {comprador.Numero} {(string.IsNullOrWhiteSpace(comprador.Complemento) ? "" : "- " + comprador.Complemento)} - {comprador.Bairro}, {comprador.Cidade}/{comprador.Estado} - CEP: {comprador.Cep}",
            UrlAvaliacao = $"https://mercadobonsai.com.br/avaliar/{proximoNumero}"
        };

        int pedidoId = await _pedidoRepository.CriarAsync(pedido);
        pedido.Id = pedidoId;

        // Dispara Webhook e-vendas imediatamente no status 'Criado'
        await _evendasWebhookService.NotificarMudancaStatusAsync(pedido);

        TempData["Sucesso"] = $"Pedido #{pedido.Numero} gerado com sucesso! Acompanhe o ciclo de aceite no seu painel.";
        return RedirectToAction("MinhasCompras");
    }

    // GET: /Pedido/MinhasCompras (Painel do Comprador)
    [HttpGet]
    public async Task<IActionResult> MinhasCompras()
    {
        int compradorId = ObterUsuarioLogadoId();
        var pedidos = await _pedidoRepository.ObterPorCompradorAsync(compradorId);
        return View(pedidos);
    }

    // GET: /Pedido/MinhasVendas (Painel do Vendedor / Viveirista)
    [HttpGet]
    public async Task<IActionResult> MinhasVendas(string? statusFiltro)
    {
        int vendedorId = ObterUsuarioLogadoId();
        var pedidos = await _pedidoRepository.ObterPorVendedorAsync(vendedorId);

        if (!string.IsNullOrWhiteSpace(statusFiltro))
        {
            pedidos = pedidos.Where(p => string.Equals(p.StatusPedido, statusFiltro, StringComparison.OrdinalIgnoreCase));
        }

        ViewBag.StatusFiltro = statusFiltro;
        return View(pedidos);
    }

    // POST: /Pedido/AceitarPedido (Ação do Vendedor)
    [HttpPost]
    public async Task<IActionResult> AceitarPedido(int id, decimal valorFrete)
    {
        int vendedorId = ObterUsuarioLogadoId();
        var pedido = await _pedidoRepository.ObterPorIdAsync(id);

        if (pedido == null || pedido.VendedorId != vendedorId)
        {
            TempData["Erro"] = "Pedido não localizado ou acesso negado.";
            return RedirectToAction("MinhasVendas");
        }

        var vendedor = await _usuarioRepository.ObterPorIdAsync(vendedorId);
        if (vendedor == null)
        {
            TempData["Erro"] = "Vendedor não localizado.";
            return RedirectToAction("MinhasVendas");
        }

        // 1. Cria Subconta Asaas se ainda não existir
        if (string.IsNullOrWhiteSpace(vendedor.AsaasAccountId))
        {
            var subcontaResult = await _asaasService.CriarSubcontaVendedorAsync(vendedor);
            if (subcontaResult.Sucesso && !string.IsNullOrEmpty(subcontaResult.AsaasAccountId))
            {
                vendedor.AsaasAccountId = subcontaResult.AsaasAccountId;
                await _usuarioRepository.AtualizarAsync(vendedor);
            }
        }

        // 2. Calcula novo valor total com o valor real do frete
        decimal freteCalculado = Math.Max(0.00m, valorFrete);
        decimal valorTotalNovo = pedido.ValorPedido + freteCalculado;

        pedido.ValorFrete = freteCalculado;
        pedido.ValorTotal = valorTotalNovo;
        pedido.StatusPedido = StatusPedido.AguardandoPagamento;

        // 3. Gera Cobrança no Asaas
        var cobrancaResult = await _asaasService.CriarCobrancaAsync(pedido, vendedor);
        if (cobrancaResult.Sucesso)
        {
            pedido.UrlCheckout = cobrancaResult.UrlCheckout;
            pedido.AsaasPaymentId = cobrancaResult.AsaasPaymentId;
        }

        await _pedidoRepository.AtualizarFreteECheckoutAsync(pedido.Id, freteCalculado, valorTotalNovo, pedido.UrlCheckout, pedido.AsaasPaymentId);

        // 4. Dispara Webhook e-vendas com o novo status 'Aguardando Pagamento' e URLCHECKOUT
        await _evendasWebhookService.NotificarMudancaStatusAsync(pedido);

        TempData["Sucesso"] = $"Pedido #{pedido.Numero} aceito com sucesso! Frete de R$ {freteCalculado:N2} adicionado e link de pagamento gerado.";
        return RedirectToAction("MinhasVendas");
    }

    // POST: /Pedido/RecusarPedido (Ação do Vendedor)
    [HttpPost]
    public async Task<IActionResult> RecusarPedido(int id, string observacao)
    {
        int vendedorId = ObterUsuarioLogadoId();
        var pedido = await _pedidoRepository.ObterPorIdAsync(id);

        if (pedido == null || pedido.VendedorId != vendedorId)
        {
            TempData["Erro"] = "Pedido não localizado ou acesso negado.";
            return RedirectToAction("MinhasVendas");
        }

        if (string.IsNullOrWhiteSpace(observacao))
        {
            TempData["Erro"] = "É obrigatório registrar a justificativa para a recusa do pedido.";
            return RedirectToAction("MinhasVendas");
        }

        pedido.StatusPedido = StatusPedido.Recusado;
        pedido.Observacao = observacao.Trim();

        await _pedidoRepository.AtualizarStatusAsync(pedido.Id, StatusPedido.Recusado, pedido.Observacao);

        // Dispara Webhook e-vendas no status 'Recusado'
        await _evendasWebhookService.NotificarMudancaStatusAsync(pedido);

        TempData["Sucesso"] = $"Pedido #{pedido.Numero} recusado com sucesso.";
        return RedirectToAction("MinhasVendas");
    }

    // POST: /Pedido/IniciarDespacho (Ação do Vendedor)
    [HttpPost]
    public async Task<IActionResult> IniciarDespacho(int id)
    {
        int vendedorId = ObterUsuarioLogadoId();
        var pedido = await _pedidoRepository.ObterPorIdAsync(id);

        if (pedido == null || pedido.VendedorId != vendedorId)
        {
            TempData["Erro"] = "Pedido não localizado ou acesso negado.";
            return RedirectToAction("MinhasVendas");
        }

        pedido.StatusPedido = StatusPedido.EmDespacho;
        await _pedidoRepository.AtualizarStatusAsync(pedido.Id, StatusPedido.EmDespacho);

        // Dispara Webhook e-vendas
        await _evendasWebhookService.NotificarMudancaStatusAsync(pedido);

        TempData["Sucesso"] = $"Pedido #{pedido.Numero} alterado para 'Em Despacho'.";
        return RedirectToAction("MinhasVendas");
    }

    // POST: /Pedido/InformarRastreio (Ação do Vendedor)
    [HttpPost]
    public async Task<IActionResult> InformarRastreio(int id, string codigoRastreio, string? urlRastreio)
    {
        int vendedorId = ObterUsuarioLogadoId();
        var pedido = await _pedidoRepository.ObterPorIdAsync(id);

        if (pedido == null || pedido.VendedorId != vendedorId)
        {
            TempData["Erro"] = "Pedido não localizado ou acesso negado.";
            return RedirectToAction("MinhasVendas");
        }

        if (string.IsNullOrWhiteSpace(codigoRastreio))
        {
            TempData["Erro"] = "Por favor, digite o código de rastreamento.";
            return RedirectToAction("MinhasVendas");
        }

        var codLimpo = codigoRastreio.Trim();
        var urlFinal = !string.IsNullOrWhiteSpace(urlRastreio) 
            ? urlRastreio.Trim() 
            : $"https://rastreamento.correios.com.br/app/index.php?codigo={codLimpo}";

        pedido.CodigoRastreio = codLimpo;
        pedido.UrlRastreio = urlFinal;
        pedido.StatusPedido = StatusPedido.EmTransito;

        await _pedidoRepository.AtualizarRastreioAsync(pedido.Id, codLimpo, urlFinal);

        // Dispara Webhook e-vendas no status 'Em Transito'
        await _evendasWebhookService.NotificarMudancaStatusAsync(pedido);

        TempData["Sucesso"] = $"Rastreio informado! Pedido #{pedido.Numero} alterado para 'Em Transito'.";
        return RedirectToAction("MinhasVendas");
    }

    // POST: /Pedido/ConfirmarEntrega (Ação do Vendedor ou Sistema)
    [HttpPost]
    public async Task<IActionResult> ConfirmarEntrega(int id)
    {
        int vendedorId = ObterUsuarioLogadoId();
        var pedido = await _pedidoRepository.ObterPorIdAsync(id);

        if (pedido == null || pedido.VendedorId != vendedorId)
        {
            TempData["Erro"] = "Pedido não localizado ou acesso negado.";
            return RedirectToAction("MinhasVendas");
        }

        pedido.StatusPedido = StatusPedido.Entregue;
        await _pedidoRepository.AtualizarStatusAsync(pedido.Id, StatusPedido.Entregue);

        // Dispara Webhook e-vendas
        await _evendasWebhookService.NotificarMudancaStatusAsync(pedido);

        TempData["Sucesso"] = $"Pedido #{pedido.Numero} marcado como 'Entregue'. Aguardando conferência do comprador.";
        return RedirectToAction("MinhasVendas");
    }

    // POST: /Pedido/Conferir (Ação do Comprador)
    [HttpPost]
    public async Task<IActionResult> Conferir(int id)
    {
        int compradorId = ObterUsuarioLogadoId();
        var pedido = await _pedidoRepository.ObterPorIdAsync(id);

        if (pedido == null || pedido.CompradorId != compradorId)
        {
            TempData["Erro"] = "Pedido não localizado ou acesso negado.";
            return RedirectToAction("MinhasCompras");
        }

        // 1. Muda para Conferido
        pedido.StatusPedido = StatusPedido.Conferido;
        await _pedidoRepository.AtualizarStatusAsync(pedido.Id, StatusPedido.Conferido);
        await _evendasWebhookService.NotificarMudancaStatusAsync(pedido);

        // 2. Finaliza automaticamente o pedido
        pedido.StatusPedido = StatusPedido.Finalizado;
        await _pedidoRepository.AtualizarStatusAsync(pedido.Id, StatusPedido.Finalizado);
        await _evendasWebhookService.NotificarMudancaStatusAsync(pedido);

        TempData["Sucesso"] = $"Obrigado por confirmar o recebimento do Pedido #{pedido.Numero}! O ciclo da venda foi Finalizado com sucesso.";
        return RedirectToAction("MinhasCompras");
    }
}
