using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using MercadoBonsai.Domain.Enums;
using MercadoBonsai.Domain.Interfaces;
using MercadoBonsai.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MercadoBonsai.Web.Controllers;

[ApiController]
[Route("api/webhooks/asaas")]
public class AsaasWebhookController : ControllerBase
{
    private readonly IPedidoRepository _pedidoRepository;
    private readonly IProdutoRepository _produtoRepository;
    private readonly IEvendasWebhookService _evendasWebhookService;
    private readonly ILeilaoService _leilaoService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AsaasWebhookController> _logger;

    public AsaasWebhookController(
        IPedidoRepository pedidoRepository,
        IProdutoRepository produtoRepository,
        IEvendasWebhookService evendasWebhookService,
        ILeilaoService leilaoService,
        IConfiguration configuration,
        ILogger<AsaasWebhookController> logger)
    {
        _pedidoRepository = pedidoRepository;
        _produtoRepository = produtoRepository;
        _evendasWebhookService = evendasWebhookService;
        _leilaoService = leilaoService;
        _configuration = configuration;
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

        // Validação Opcional de Token do Webhook (Asaas-Access-Token)
        string? secretConfig = _configuration["Asaas:WebhookSecret"];
        if (!string.IsNullOrWhiteSpace(secretConfig))
        {
            if (!Request.Headers.TryGetValue("asaas-access-token", out var headerToken) || headerToken != secretConfig)
            {
                _logger.LogWarning("Webhook Asaas rejeitado: Token de acesso inválido ou ausente no header 'asaas-access-token'.");
                return Unauthorized(new { message = "Token de acesso inválido." });
            }
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

            if (string.IsNullOrEmpty(evento))
            {
                return Ok(new { success = true, message = "Evento sem identificador de ação ignorado." });
            }

            // Localiza o pedido correspondente por AsaasPaymentId ou por Número do Pedido (externalReference)
            Domain.Entities.Pedido? pedido = null;
            if (!string.IsNullOrEmpty(paymentId))
            {
                pedido = await _pedidoRepository.ObterPorAsaasPaymentIdAsync(paymentId);
            }

            if (pedido == null && numeroPedido > 0)
            {
                pedido = await _pedidoRepository.ObterPorNumeroAsync(numeroPedido);
            }

            if (pedido == null)
            {
                _logger.LogWarning("Webhook Asaas (Evento: {Evento}): Pedido não localizado no banco (PaymentId: {PaymentId}, ExternalRef: {Ref})", evento, paymentId, numeroPedido);
                return Ok(new { success = true, message = "Webhook recebido, porém pedido correspondente não foi localizado no sistema." });
            }

            // Preserva AsaasPaymentId se não estiver preenchido no pedido
            if (!string.IsNullOrEmpty(paymentId) && string.IsNullOrEmpty(pedido.AsaasPaymentId))
            {
                pedido.AsaasPaymentId = paymentId;
            }

            // 1. EVENTO: Confirmação ou Recebimento de Pagamento (PAYMENT_RECEIVED / PAYMENT_CONFIRMED)
            if (string.Equals(evento, "PAYMENT_RECEIVED", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(evento, "PAYMENT_CONFIRMED", StringComparison.OrdinalIgnoreCase))
            {
                pedido.StatusPedido = StatusPedido.Pago;
                pedido.DataPagamento = DateTime.Now;
                await _pedidoRepository.AtualizarAsync(pedido);

                // Atualiza status do produto (Disponível se estoque > 0, Vendido se estoque == 0)
                var produtoPago = await _produtoRepository.ObterPorIdAsync(pedido.ProdutoId);
                int estoquePago = produtoPago != null ? produtoPago.QuantidadeEstoque : 0;
                StatusProduto statusPago = estoquePago > 0 ? StatusProduto.Disponivel : StatusProduto.Vendido;
                await _produtoRepository.AtualizarStatusDisponibilidadeAsync(pedido.ProdutoId, statusPago, estoquePago);

                _logger.LogInformation("Pedido #{Numero} atualizado para PAGO via Webhook Asaas! Produto #{ProdutoId} (Status: {Status}, Estoque: {Estoque})", 
                    pedido.Numero, pedido.ProdutoId, statusPago, estoquePago);

                // Disparo Automático HTTP POST para o e-vendas (URL do vendedor ou da plataforma)
                await _evendasWebhookService.NotificarMudancaStatusAsync(pedido);

                return Ok(new { success = true, message = $"Pedido #{pedido.Numero} atualizado para PAGO e webhook e-vendas notificado com sucesso." });
            }

            // 2. EVENTO: Pagamento Vencido, Estornado ou Excluído (PAYMENT_OVERDUE / PAYMENT_REFUNDED / PAYMENT_DELETED)
            if (string.Equals(evento, "PAYMENT_OVERDUE", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(evento, "PAYMENT_REFUNDED", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(evento, "PAYMENT_DELETED", StringComparison.OrdinalIgnoreCase))
            {
                pedido.StatusPedido = StatusPedido.Cancelado;
                await _pedidoRepository.AtualizarAsync(pedido);

                // Restaura o estoque se for um produto físico tradicional
                if (pedido.ProdutoId > 0)
                {
                    var produtoCancelado = await _produtoRepository.ObterPorIdAsync(pedido.ProdutoId);
                    int estoqueRestaurado = (produtoCancelado != null ? produtoCancelado.QuantidadeEstoque : 0) + 1;
                    await _produtoRepository.AtualizarStatusDisponibilidadeAsync(pedido.ProdutoId, StatusProduto.Disponivel, estoqueRestaurado);
                }

                // Se o pedido pertencia a um LEILÃO, aciona o FALLBACK em cascata para o próximo colocado (2º, 3º... colocados)
                if (pedido.LeilaoId.HasValue && pedido.LeilaoId.Value > 0)
                {
                    int posicaoAtual = pedido.PosicaoVencedorLeilao ?? 1;
                    _logger.LogInformation("Webhook Asaas: Pedido #{Numero} do Leilão #{LeilaoId} cancelado. Acionando fallback para o {ProximaPosicao}º colocado...", 
                        pedido.Numero, pedido.LeilaoId.Value, posicaoAtual + 1);

                    await _leilaoService.ChamarProximoColocadoAsync(pedido.LeilaoId.Value, posicaoAtual);
                }

                _logger.LogInformation("Pedido #{Numero} atualizado para CANCELADO ({Evento}) via Webhook Asaas.", pedido.Numero, evento);

                // Disparo Automático HTTP POST para o e-vendas
                await _evendasWebhookService.NotificarMudancaStatusAsync(pedido);

                return Ok(new { success = true, message = $"Pedido #{pedido.Numero} marcado como CANCELADO ({evento}) e fallback de leilão processado." });
            }

            _logger.LogInformation("Webhook Asaas (Evento: {Evento}) processado sem alteração de estado direta no pedido #{Numero}.", evento, pedido.Numero);
            return Ok(new { success = true, message = $"Evento '{evento}' recebido e registrado." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exceção ao processar payload do Webhook Asaas.");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
