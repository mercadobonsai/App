using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MercadoBonsai.Domain.Entities;
using MercadoBonsai.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MercadoBonsai.Web.Services;

public class EvendasWebhookService : IEvendasWebhookService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ILogger<EvendasWebhookService> _logger;

    public EvendasWebhookService(
        HttpClient httpClient, 
        IConfiguration configuration, 
        IUsuarioRepository usuarioRepository, 
        ILogger<EvendasWebhookService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _usuarioRepository = usuarioRepository;
        _logger = logger;
    }

    public async Task<bool> NotificarMudancaStatusAsync(Pedido pedido)
    {
        if (pedido == null) return false;

        // 1. Resolução Flexível do Webhook de Destino:
        //    Regra 1: Verificar se o vendedor possui URL de webhook personalizada no perfil
        //    Regra 2: Caso contrário, utilizar a URL genérica/padrão da plataforma Mercado Bonsai
        string? webhookUrl = null;
        if (pedido.VendedorId > 0)
        {
            var vendedor = await _usuarioRepository.ObterPorIdAsync(pedido.VendedorId);
            if (vendedor != null && !string.IsNullOrWhiteSpace(vendedor.WebhookUrl))
            {
                webhookUrl = vendedor.WebhookUrl.Trim();
                _logger.LogInformation("Utilizando URL de Webhook personalizada do Vendedor #{VendedorId} ({Nome}): '{Url}'", vendedor.Id, vendedor.Nome, webhookUrl);
            }
        }

        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            webhookUrl = _configuration["Webhook:EvendasUrl"] ?? _configuration["Webhook:Url"];
            if (!string.IsNullOrWhiteSpace(webhookUrl))
            {
                _logger.LogInformation("Utilizando URL de Webhook padrão/genérica da Plataforma Mercado Bonsai: '{Url}'", webhookUrl);
            }
        }

        // 2. Montagem do payload JSON com os campos integrados ao e-vendas
        var payload = new
        {
            id = pedido.Id,
            NUMERO = pedido.Numero,
            TRANSACAO = pedido.Numero.ToString(),
            IDPLATAFORMA = 17,
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

        _logger.LogInformation("Disparando Webhook e-vendas para Pedido #{Numero}. Status: '{Status}'", pedido.Numero, pedido.StatusPedido);

        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            _logger.LogWarning("Nenhuma URL de Webhook configurada para o Vendedor #{VendedorId} nem na Plataforma ('Webhook:EvendasUrl'). Payload simulado com sucesso:\n{Payload}", pedido.VendedorId, json);
            return true;
        }

        // 3. Disparo Automático HTTP POST Assíncrono com Header 'token' condicional
        string urlPadraoPlataforma = _configuration["Webhook:EvendasUrl"] ?? "http://api.e-vendas.net.br/api/pedidos/";
        string tokenPadraoPlataforma = _configuration["Webhook:EvendasToken"] ?? "c01c472d-7c6c-4e85-8398-884a44a7c4c1";

        bool isUrlPadrao = !string.IsNullOrWhiteSpace(webhookUrl) 
            && string.Equals(webhookUrl.Trim().TrimEnd('/'), urlPadraoPlataforma.Trim().TrimEnd('/'), StringComparison.OrdinalIgnoreCase);

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, webhookUrl);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            if (isUrlPadrao)
            {
                request.Headers.Add("token", tokenPadraoPlataforma);
                _logger.LogInformation("Header 'token' adicionado ao disparo para a URL padrão do e-vendas (Token: {Token}).", tokenPadraoPlataforma);
            }
            else
            {
                _logger.LogInformation("URL de destino é personalizada do vendedor ('{Url}'). Nenhum header 'token' enviado.", webhookUrl);
            }

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Webhook e-vendas para Pedido #{Numero} entregue com sucesso HTTP {Status} na URL '{Url}'. Resposta e-vendas: '{Body}'", 
                    pedido.Numero, response.StatusCode, webhookUrl, responseBody);
                return true;
            }
            else
            {
                _logger.LogWarning("Webhook e-vendas para Pedido #{Numero} respondeu com erro HTTP {Status} na URL '{Url}'. Resposta e-vendas: '{Body}'", 
                    pedido.Numero, response.StatusCode, webhookUrl, responseBody);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro de conectividade ao disparar Webhook e-vendas para Pedido #{Numero} na URL '{Url}'", pedido.Numero, webhookUrl);
            return false;
        }
    }
}
