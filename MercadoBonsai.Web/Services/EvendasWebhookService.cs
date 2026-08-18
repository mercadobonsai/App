using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MercadoBonsai.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MercadoBonsai.Web.Services;

public class EvendasWebhookService : IEvendasWebhookService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EvendasWebhookService> _logger;

    public EvendasWebhookService(HttpClient httpClient, IConfiguration configuration, ILogger<EvendasWebhookService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> NotificarMudancaStatusAsync(Pedido pedido)
    {
        if (pedido == null) return false;

        var webhookUrl = _configuration["Webhook:EvendasUrl"] ?? _configuration["Webhook:Url"];

        // Montagem do payload exatamente nos nomes e tipos exigidos pela integracao e-vendas
        var payload = new
        {
            id = pedido.Id,
            NUMERO = pedido.Numero,
            comprador_id = pedido.CompradorId,
            vendedor_id = pedido.VendedorId,
            produto_id = pedido.ProdutoId,
            STATUSPEDIDO = pedido.StatusPedido,
            TIPOPAGAMENTO = pedido.TipoPagamento,
            DATAPEDIDO = pedido.DataPedido,
            DATAPAGAMENTO = pedido.DataPagamento,
            VALORPEDIDO = pedido.ValorPedido,
            valor_frete = pedido.ValorFrete,
            valor_seguro = pedido.ValorSeguro,
            VALOR_TOTAL = pedido.ValorTotal,
            URLCHECKOUT = pedido.UrlCheckout,
            OBSERVACAO = pedido.Observacao,
            CODIGORASTREIO = pedido.CodigoRastreio,
            URLRASTREIO = pedido.UrlRastreio,
            COMPRADORNOME = pedido.CompradorNome,
            COMPRADOREMAIL = pedido.CompradorEmail,
            COMPRADORTELEFONE = pedido.CompradorTelefone,
            COMPRADORENDERECO = pedido.CompradorEndereco,
            COMPRADORANIVERSARIO = pedido.CompradorAniversario,
            URLAVALIACAO = pedido.UrlAvaliacao,
            asaas_payment_id = pedido.AsaasPaymentId
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        _logger.LogInformation("Gatilho de Webhook e-vendas disparado para Pedido #{Numero}. Status: '{Status}'", pedido.Numero, pedido.StatusPedido);

        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            _logger.LogWarning("URL de Webhook e-vendas não configurada no appsettings.json ('Webhook:EvendasUrl'). Payload simulado com sucesso:\n{Payload}", json);
            return true;
        }

        try
        {
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(webhookUrl, content);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Webhook e-vendas entregue com sucesso HTTP {Status}", response.StatusCode);
                return true;
            }
            else
            {
                _logger.LogWarning("Webhook e-vendas respondeu com erro HTTP {Status}", response.StatusCode);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro de conectividade ao disparar Webhook e-vendas para Pedido #{Numero}", pedido.Numero);
            return false;
        }
    }
}
