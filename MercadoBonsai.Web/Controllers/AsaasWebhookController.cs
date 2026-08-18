using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using MercadoBonsai.Domain.Enums;
using MercadoBonsai.Domain.Interfaces;
using MercadoBonsai.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MercadoBonsai.Web.Controllers;

[ApiController]
[Route("api/webhooks/asaas")]
public class AsaasWebhookController : ControllerBase
{
    private readonly IPedidoRepository _pedidoRepository;
    private readonly IProdutoRepository _produtoRepository;
    private readonly IEvendasWebhookService _evendasWebhookService;
    private readonly ILogger<AsaasWebhookController> _logger;

    public AsaasWebhookController(
        IPedidoRepository pedidoRepository,
        IProdutoRepository produtoRepository,
        IEvendasWebhookService evendasWebhookService,
        ILogger<AsaasWebhookController> logger)
    {
        _pedidoRepository = pedidoRepository;
        _produtoRepository = produtoRepository;
        _evendasWebhookService = evendasWebhookService;
        _logger = logger;
    }

    // POST: /api/webhooks/asaas
    [HttpPost]
    public async Task<IActionResult> ReceberWebhookAsaas()
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();

        _logger.LogInformation("Webhook Asaas Recebido: {Body}", body);

        if (string.IsNullOrWhiteSpace(body))
        {
            return BadRequest(new { message = "Corpo da requisição vazio." });
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            string? evento = root.TryGetProperty("event", out var evProp) ? evProp.GetString() : null;
            
            string? paymentId = null;
            int numeroPedido = 0;

            if (root.TryGetProperty("payment", out var payObj) && payObj.ValueKind == JsonValueKind.Object)
            {
                if (payObj.TryGetProperty("id", out var idProp)) paymentId = idProp.GetString();
                if (payObj.TryGetProperty("externalReference", out var extProp) && int.TryParse(extProp.GetString(), out int numParsed))
                {
                    numeroPedido = numParsed;
                }
            }

            // Verifica se o evento refere-se a recebimento/confirmação de pagamento
            if (string.Equals(evento, "PAYMENT_RECEIVED", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(evento, "PAYMENT_CONFIRMED", StringComparison.OrdinalIgnoreCase))
            {
                Domain.Entities.Pedido? pedido = null;

                if (!string.IsNullOrEmpty(paymentId))
                {
                    pedido = await _pedidoRepository.ObterPorAsaasPaymentIdAsync(paymentId);
                }

                if (pedido == null && numeroPedido > 0)
                {
                    pedido = await _pedidoRepository.ObterPorNumeroAsync(numeroPedido);
                }

                if (pedido != null)
                {
                    pedido.StatusPedido = StatusPedido.Pago;
                    pedido.DataPagamento = DateTime.Now;
                    await _pedidoRepository.AtualizarAsync(pedido);

                    // Regra de Negócio: Marca o produto como VENDIDO no estoque/vitrine (Status 3 = Vendido, Estoque = 0)
                    await _produtoRepository.AtualizarStatusDisponibilidadeAsync(pedido.ProdutoId, StatusProduto.Vendido, 0);

                    _logger.LogInformation("Pedido #{Numero} atualizado para PAGO e produto #{ProdutoId} marcado como VENDIDO!", pedido.Numero, pedido.ProdutoId);

                    // Notifica imediatamente o e-vendas sobre a transição para 'Pago'
                    await _evendasWebhookService.NotificarMudancaStatusAsync(pedido);

                    return Ok(new { success = true, message = $"Pedido #{pedido.Numero} atualizado para PAGO com sucesso." });
                }
                else
                {
                    _logger.LogWarning("Webhook Asaas recebido mas pedido não foi localizado no banco (PaymentId: {PaymentId}, ExternalRef: {Ref})", paymentId, numeroPedido);
                }
            }

            return Ok(new { success = true, message = "Evento processado." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar payload do Webhook Asaas");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
