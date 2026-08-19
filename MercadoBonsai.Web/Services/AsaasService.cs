using System;
using System.Collections.Generic;
using System.Net.Http;
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

    // ETAPA 1: Cadastro de Cliente no Asaas (POST /v3/customers)
    public async Task<AsaasClienteResult> CriarClienteAsync(Usuario usuario)
    {
        var apiKey = _configuration["Asaas:ApiKey"]?.Trim();
        var baseUrl = _configuration["Asaas:ApiUrl"] ?? "https://sandbox.asaas.com/api/v3";

        // Modo Stub / Pré-configurado se a chave não estiver preenchida
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "seu_asaas_token_aqui")
        {
            _logger.LogInformation("Asaas API Key não configurada. Simulando criação de Cliente para Usuário #{Id}", usuario.Id);
            return new AsaasClienteResult
            {
                Sucesso = true,
                AsaasCustomerId = $"cus_simulado_{usuario.Id}"
            };
        }

        try
        {
            var payload = new
            {
                name = usuario.Nome,
                email = usuario.Email,
                cpfCnpj = usuario.CpfCnpj,
                phone = usuario.Telefone,
                mobilePhone = usuario.Telefone,
                postalCode = usuario.Cep,
                address = usuario.Logradouro,
                addressNumber = usuario.Numero,
                complement = usuario.Complemento,
                province = usuario.Bairro,
                city = usuario.Cidade,
                state = usuario.Estado
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/customers");
            request.Headers.Add("access_token", apiKey);
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(responseBody);
                var customerId = doc.RootElement.GetProperty("id").GetString();
                return new AsaasClienteResult { Sucesso = true, AsaasCustomerId = customerId };
            }
            else
            {
                string erroDescrito = ExtrairErrosAsaas(responseBody);
                _logger.LogWarning("Falha ao criar Cliente Asaas HTTP {Status}: {Body}", response.StatusCode, responseBody);
                return new AsaasClienteResult { Sucesso = false, MensagemErro = erroDescrito };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exceção ao comunicar com a API Asaas (CriarCliente)");
            return new AsaasClienteResult { Sucesso = false, MensagemErro = ex.Message };
        }
    }

    // ETAPA 2: Criação de Subconta de Vendedor/Viveirista (POST /v3/accounts)
    public async Task<AsaasSubcontaResult> CriarSubcontaVendedorAsync(Usuario vendedor)
    {
        var apiKey = _configuration["Asaas:ApiKey"]?.Trim();
        var baseUrl = _configuration["Asaas:ApiUrl"] ?? "https://sandbox.asaas.com/api/v3";

        // Modo Stub / Pré-configurado se a chave não estiver preenchida
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "seu_asaas_token_aqui")
        {
            _logger.LogInformation("Asaas API Key não configurada. Simulando criação de Subconta para o vendedor #{VendedorId}", vendedor.Id);
            return new AsaasSubcontaResult
            {
                Sucesso = true,
                AsaasAccountId = $"acc_simulada_vendedor_{vendedor.Id}"
            };
        }

        try
        {
            bool isCnpj = !string.IsNullOrWhiteSpace(vendedor.CpfCnpj) && vendedor.CpfCnpj.Length > 11;
            var personType = isCnpj ? "JURIDICA" : "FISICA";

            var payload = new
            {
                name = string.IsNullOrWhiteSpace(vendedor.RazaoSocial) ? vendedor.Nome : vendedor.RazaoSocial,
                email = vendedor.Email,
                cpfCnpj = vendedor.CpfCnpj,
                mobilePhone = vendedor.Telefone,
                address = vendedor.Logradouro,
                addressNumber = vendedor.Numero,
                complement = vendedor.Complemento,
                province = vendedor.Bairro,
                postalCode = vendedor.Cep,
                personType = personType,
                companyType = isCnpj ? "MEI" : null
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
                string erroDescrito = ExtrairErrosAsaas(responseBody);
                _logger.LogWarning("Falha ao criar Subconta Asaas HTTP {Status}: {Body}", response.StatusCode, responseBody);
                return new AsaasSubcontaResult { Sucesso = false, MensagemErro = erroDescrito };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exceção ao comunicar com a API Asaas (CriarSubconta)");
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
                customer = string.IsNullOrEmpty(vendedor.AsaasCustomerId) ? pedido.CompradorEmail : vendedor.AsaasCustomerId,
                billingType = "UNDEFINED", // Permite Pix, Cartão e Boleto na mesma tela
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
                string erroDescrito = ExtrairErrosAsaas(responseBody);
                _logger.LogWarning("Falha ao criar cobrança Asaas HTTP {Status}: {Body}", response.StatusCode, responseBody);
                return new AsaasCobrancaResult { Sucesso = false, MensagemErro = erroDescrito };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exceção na chamada de cobrança Asaas para Pedido #{Numero}", pedido.Numero);
            return new AsaasCobrancaResult { Sucesso = false, MensagemErro = ex.Message };
        }
    }

    // Método Utilitário para Tratamento e Formatação das Críticas da API Asaas (v3)
    private static string ExtrairErrosAsaas(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
            return "O serviço do Asaas não retornou detalhes do erro.";

        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            if (doc.RootElement.TryGetProperty("errors", out var errorsProp) && errorsProp.ValueKind == JsonValueKind.Array)
            {
                var mensagens = new List<string>();
                foreach (var errElement in errorsProp.EnumerateArray())
                {
                    if (errElement.TryGetProperty("description", out var descProp))
                    {
                        var desc = descProp.GetString();
                        if (!string.IsNullOrWhiteSpace(desc))
                        {
                            mensagens.Add(desc.Trim());
                        }
                    }
                }

                if (mensagens.Count > 0)
                {
                    return string.Join(" | ", mensagens);
                }
            }
        }
        catch
        {
            // Retorna fallback genérico caso o body não seja JSON parseável
        }

        return "Falha no processamento do cadastro junto à plataforma Asaas.";
    }
}
