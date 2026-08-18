using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MercadoBonsai.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MercadoBonsai.Web.Services;

public class AsaasService : IAsaasService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AsaasService> _logger;

    public AsaasService(HttpClient httpClient, IConfiguration configuration, ILogger<AsaasService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AsaasSubcontaResult> CriarSubcontaVendedorAsync(Usuario vendedor)
    {
        var apiKey = _configuration["Asaas:ApiKey"]?.Trim();
        var baseUrl = _configuration["Asaas:ApiUrl"] ?? "https://sandbox.asaas.com/api/v3";

        // Se o token real não estiver configurado, executa no modo pré-configurado / stub de desenvolvimento
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "seu_asaas_token_aqui")
        {
            _logger.LogInformation("Asaas API Key não configurada. Executando stub pré-configurado de Subconta para o vendedor {VendedorId}", vendedor.Id);
            return new AsaasSubcontaResult
            {
                Sucesso = true,
                AsaasAccountId = $"acc_simulada_vendedor_{vendedor.Id}"
            };
        }

        try
        {
            var payload = new
            {
                name = vendedor.Nome,
                email = vendedor.Email,
                cpfCnpj = vendedor.CpfCnpj,
                mobilePhone = vendedor.Telefone,
                address = vendedor.Logradouro,
                addressNumber = vendedor.Numero,
                province = vendedor.Bairro,
                postalCode = vendedor.Cep
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/accounts");
            request.Headers.Add("access_token", apiKey);
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(responseBody);
                var accountId = doc.RootElement.GetProperty("id").GetString();
                return new AsaasSubcontaResult { Sucesso = true, AsaasAccountId = accountId };
            }
            else
            {
                _logger.LogWarning("Falha ao criar subconta Asaas HTTP {Status}: {Body}", response.StatusCode, responseBody);
                return new AsaasSubcontaResult { Sucesso = false, MensagemErro = "Erro ao registrar subconta na API Asaas." };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exceção na chamada de subconta Asaas");
            return new AsaasSubcontaResult { Sucesso = false, MensagemErro = ex.Message };
        }
    }

    public async Task<AsaasCobrancaResult> CriarCobrancaAsync(Pedido pedido, Usuario vendedor)
    {
        var apiKey = _configuration["Asaas:ApiKey"]?.Trim();
        var baseUrl = _configuration["Asaas:ApiUrl"] ?? "https://sandbox.asaas.com/api/v3";

        // Modo Stub / Pré-configurado quando o token não foi preenchido
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "seu_asaas_token_aqui")
        {
            _logger.LogInformation("Asaas API Key não configurada. Gerando cobrança pré-configurada para Pedido #{Numero}", pedido.Numero);
            var paymentIdSimulado = $"pay_{pedido.Numero}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var urlCheckoutSimulado = $"https://sandbox.asaas.com/i/{paymentIdSimulado}";

            return new AsaasCobrancaResult
            {
                Sucesso = true,
                AsaasPaymentId = paymentIdSimulado,
                UrlCheckout = urlCheckoutSimulado
            };
        }

        try
        {
            var payload = new
            {
                customer = pedido.CompradorEmail, // ou asaas_customer_id
                billingType = "UNDEFINED", // Permite Pix, Cartao e Boleto na mesma tela
                value = pedido.ValorTotal,
                dueDate = DateTime.Now.AddDays(3).ToString("yyyy-MM-dd"),
                description = $"Pedido #{pedido.Numero} - Mercado Bonsai",
                externalReference = pedido.Numero.ToString()
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/payments");
            request.Headers.Add("access_token", apiKey);
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(responseBody);
                var paymentId = doc.RootElement.GetProperty("id").GetString();
                var invoiceUrl = doc.RootElement.TryGetProperty("invoiceUrl", out var invProp) ? invProp.GetString() : null;
                var bankSlipUrl = doc.RootElement.TryGetProperty("bankSlipUrl", out var bsProp) ? bsProp.GetString() : null;

                return new AsaasCobrancaResult
                {
                    Sucesso = true,
                    AsaasPaymentId = paymentId,
                    UrlCheckout = invoiceUrl ?? bankSlipUrl ?? $"https://www.asaas.com/i/{paymentId}"
                };
            }
            else
            {
                _logger.LogWarning("Falha ao criar cobrança Asaas HTTP {Status}: {Body}", response.StatusCode, responseBody);
                return new AsaasCobrancaResult { Sucesso = false, MensagemErro = "Erro ao gerar cobrança no Asaas." };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exceção na chamada de cobrança Asaas para Pedido #{Numero}", pedido.Numero);
            return new AsaasCobrancaResult { Sucesso = false, MensagemErro = ex.Message };
        }
    }
}
