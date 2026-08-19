using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MercadoBonsai.Domain.Entities;
using MercadoBonsai.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MercadoBonsai.Web.Services;

public class AsaasService : IAsaasService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AsaasService> _logger;
    private readonly IUsuarioRepository _usuarioRepository;

    public AsaasService(HttpClient httpClient, IConfiguration configuration, ILogger<AsaasService> logger, IUsuarioRepository usuarioRepository)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _usuarioRepository = usuarioRepository;
    }

    // ETAPA 1: Cadastro de Cliente no Asaas (POST /v3/customers)
    public async Task<AsaasClienteResult> CriarClienteAsync(Usuario usuario)
    {
        var apiKey = _configuration["Asaas:ApiKey"]?.Trim();
        var baseUrl = _configuration["Asaas:ApiUrl"] ?? "https://sandbox.asaas.com/api/v3";

        // Modo Stub / Pré-configurado se a chave não estiver preenchida
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "seu_asaas_token_aqui")
        {
            _logger.LogInformation("Asaas API Key não configurada. Simulando criação de Cliente para #{UsuarioId}", usuario.Id);
            return new AsaasClienteResult
            {
                Sucesso = true,
                AsaasCustomerId = $"cus_simulado_{usuario.Id}"
            };
        }

        try
        {
            var cpfCnpjLimpo = SomenteNumeros(usuario.CpfCnpj);
            var foneLimpo = SomenteNumeros(usuario.Telefone);
            var cepLimpo = SomenteNumeros(usuario.Cep);

            var payload = new
            {
                name = usuario.Nome,
                cpfCnpj = cpfCnpjLimpo,
                email = usuario.Email,
                phone = foneLimpo,
                mobilePhone = foneLimpo,
                postalCode = cepLimpo,
                address = usuario.Logradouro,
                addressNumber = string.IsNullOrWhiteSpace(usuario.Numero) ? "SN" : usuario.Numero,
                complement = usuario.Complemento,
                province = usuario.Bairro,
                city = usuario.Cidade,
                state = usuario.Estado
            };

            var userAgent = _configuration["Asaas:UserAgent"] ?? "MercadoBonsai/1.0 (suporte@mercadobonsai.com.br)";

            var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/customers");
            request.Headers.Add("access_token", apiKey);
            request.Headers.TryAddWithoutValidation("User-Agent", userAgent);
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
        var userAgent = _configuration["Asaas:UserAgent"] ?? "MercadoBonsai/1.0 (suporte@mercadobonsai.com.br)";

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
            var cpfCnpjLimpo = SomenteNumeros(vendedor.CpfCnpj);
            var foneLimpo = SomenteNumeros(vendedor.Telefone);
            var cepLimpo = SomenteNumeros(vendedor.Cep);
            bool isCnpj = cpfCnpjLimpo.Length > 11;
            var personType = isCnpj ? "JURIDICA" : "FISICA";

            decimal rendaInformada = (vendedor.RendaFaturamento.HasValue && vendedor.RendaFaturamento.Value > 0) 
                ? vendedor.RendaFaturamento.Value 
                : 5000.00m;

            var payload = new
            {
                name = string.IsNullOrWhiteSpace(vendedor.RazaoSocial) ? vendedor.Nome : vendedor.RazaoSocial,
                email = vendedor.Email,
                cpfCnpj = cpfCnpjLimpo,
                mobilePhone = foneLimpo,
                address = vendedor.Logradouro,
                addressNumber = string.IsNullOrWhiteSpace(vendedor.Numero) ? "SN" : vendedor.Numero,
                complement = vendedor.Complemento,
                province = vendedor.Bairro,
                postalCode = cepLimpo,
                personType = personType,
                birthDate = isCnpj ? null : vendedor.DataNascimento?.ToString("yyyy-MM-dd"),
                incomeValue = rendaInformada,
                companyType = isCnpj ? "MEI" : null
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/accounts");
            request.Headers.Add("access_token", apiKey);
            request.Headers.TryAddWithoutValidation("User-Agent", userAgent);
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

    // Encerrar / Desativar Subconta de Vendedor (DELETE /v3/accounts/{id})
    public async Task<AsaasSubcontaResult> EncerrarSubcontaAsync(string asaasAccountId)
    {
        var apiKey = _configuration["Asaas:ApiKey"]?.Trim();
        var baseUrl = _configuration["Asaas:ApiUrl"] ?? "https://sandbox.asaas.com/api/v3";
        var userAgent = _configuration["Asaas:UserAgent"] ?? "MercadoBonsai/1.0 (suporte@mercadobonsai.com.br)";

        if (string.IsNullOrWhiteSpace(asaasAccountId))
        {
            return new AsaasSubcontaResult { Sucesso = false, MensagemErro = "ID da Subconta Asaas é inválido ou não informado." };
        }

        // Modo Stub / Pré-configurado se a chave não estiver preenchida ou ID for simulado
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "seu_asaas_token_aqui" || asaasAccountId.StartsWith("acc_simulada_"))
        {
            _logger.LogInformation("Asaas API Key não configurada ou conta simulada. Concluindo encerramento stub para a subconta {AccountId}", asaasAccountId);
            return new AsaasSubcontaResult { Sucesso = true, AsaasAccountId = asaasAccountId };
        }

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, $"{baseUrl}/accounts/{asaasAccountId}");
            request.Headers.Add("access_token", apiKey);
            request.Headers.TryAddWithoutValidation("User-Agent", userAgent);

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Subconta Asaas {AccountId} encerrada com sucesso via API!", asaasAccountId);
                return new AsaasSubcontaResult { Sucesso = true, AsaasAccountId = asaasAccountId };
            }
            else
            {
                string erroDescrito = ExtrairErrosAsaas(responseBody);
                _logger.LogWarning("Falha ao encerrar Subconta Asaas {AccountId} HTTP {Status}: {Body}", asaasAccountId, response.StatusCode, responseBody);
                return new AsaasSubcontaResult { Sucesso = false, MensagemErro = erroDescrito };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exceção ao encerrar Subconta Asaas {AccountId}", asaasAccountId);
            return new AsaasSubcontaResult { Sucesso = false, MensagemErro = ex.Message };
        }
    }

    public async Task<AsaasCobrancaResult> CriarCobrancaAsync(Pedido pedido, Usuario vendedor, decimal percentualComissao = 10.00m)
    {
        var apiKey = _configuration["Asaas:ApiKey"]?.Trim();
        var baseUrl = _configuration["Asaas:ApiUrl"] ?? "https://sandbox.asaas.com/api/v3";
        var userAgent = _configuration["Asaas:UserAgent"] ?? "MercadoBonsai/1.0 (suporte@mercadobonsai.com.br)";

        // Cálculo da Retenção (Comissão da Plataforma) e Repasse Líquido para a Subconta
        decimal comissaoValida = Math.Clamp(percentualComissao, 0.00m, 100.00m);
        decimal percentualVendedor = 100.00m - comissaoValida;
        decimal valorRetidoPlataforma = Math.Round(pedido.ValorTotal * (comissaoValida / 100.00m), 2);
        decimal valorLiquidoVendedor = Math.Round(pedido.ValorTotal - valorRetidoPlataforma, 2);

        // Modo Stub / Pré-configurado quando o token não foi preenchido
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "seu_asaas_token_aqui")
        {
            _logger.LogInformation("Asaas API Key não configurada. Gerando cobrança com split pré-configurada para Pedido #{Numero} (Total R$ {Total}, Comissão {Comissao}%, Retenção R$ {Retencao}, Vendedor R$ {Repasse})", pedido.Numero, pedido.ValorTotal, comissaoValida, valorRetidoPlataforma, valorLiquidoVendedor);
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
            if (string.IsNullOrEmpty(vendedor.AsaasCustomerId))
            {
                var resCliente = await CriarClienteAsync(vendedor);
                if (resCliente.Sucesso && !string.IsNullOrEmpty(resCliente.AsaasCustomerId))
                {
                    vendedor.AsaasCustomerId = resCliente.AsaasCustomerId;
                    await _usuarioRepository.AtualizarAsync(vendedor);
                }
            }

            _logger.LogInformation("Gerando cobrança Asaas Pedido #{Numero}: Total R$ {Total}, Comissão Plataforma {Comissao}% (R$ {Retencao}), Repasse Subconta R$ {Repasse} (Wallet: {Wallet})", 
                pedido.Numero, pedido.ValorTotal, comissaoValida, valorRetidoPlataforma, valorLiquidoVendedor, vendedor.AsaasAccountId ?? "Sem Wallet");

            object payload;
            if (!string.IsNullOrEmpty(vendedor.AsaasAccountId))
            {
                payload = new
                {
                    customer = vendedor.AsaasCustomerId,
                    billingType = "UNDEFINED", // Permite Pix, Cartão e Boleto na mesma tela
                    value = pedido.ValorTotal,
                    dueDate = DateTime.Now.AddDays(3).ToString("yyyy-MM-dd"),
                    description = $"Pedido #{pedido.Numero} - Mercado Bonsai",
                    externalReference = pedido.Numero.ToString(),
                    split = new[]
                    {
                        new
                        {
                            walletId = vendedor.AsaasAccountId,
                            fixedValue = valorLiquidoVendedor,
                            percentualValue = percentualVendedor
                        }
                    }
                };
            }
            else
            {
                payload = new
                {
                    customer = vendedor.AsaasCustomerId,
                    billingType = "UNDEFINED",
                    value = pedido.ValorTotal,
                    dueDate = DateTime.Now.AddDays(3).ToString("yyyy-MM-dd"),
                    description = $"Pedido #{pedido.Numero} - Mercado Bonsai",
                    externalReference = pedido.Numero.ToString()
                };
            }

            var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/payments");
            request.Headers.Add("access_token", apiKey);
            request.Headers.TryAddWithoutValidation("User-Agent", userAgent);
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

                // Auto-recuperação se a wallet/subconta tiver sido excluída ou for inválida no ambiente ativo
                if (responseBody.Contains("Wallet") && (responseBody.Contains("inexistente") || responseBody.Contains("not found")))
                {
                    _logger.LogWarning("Wallet {AccountId} do vendedor #{VendedorId} não existe no Asaas. Resetando wallet e recriando subconta...", vendedor.AsaasAccountId, vendedor.Id);
                    vendedor.AsaasAccountId = null;
                    var subcontaNova = await CriarSubcontaVendedorAsync(vendedor);
                    if (subcontaNova.Sucesso && !string.IsNullOrEmpty(subcontaNova.AsaasAccountId))
                    {
                        vendedor.AsaasAccountId = subcontaNova.AsaasAccountId;
                        await _usuarioRepository.AtualizarAsync(vendedor);
                        // Re-tenta gerar a cobrança automaticamente com a nova wallet criada
                        return await CriarCobrancaAsync(pedido, vendedor, percentualComissao);
                    }
                }

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
            var root = doc.RootElement;
            var mensagens = new List<string>();

            if (root.TryGetProperty("errors", out var errorsProp) && errorsProp.ValueKind == JsonValueKind.Array)
            {
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
            }
            else if (root.TryGetProperty("message", out var msgProp))
            {
                var msg = msgProp.GetString();
                if (!string.IsNullOrWhiteSpace(msg)) mensagens.Add(msg.Trim());
            }
            else if (root.TryGetProperty("error", out var errProp))
            {
                var err = errProp.GetString();
                if (!string.IsNullOrWhiteSpace(err)) mensagens.Add(err.Trim());
            }

            if (mensagens.Count > 0)
            {
                return string.Join(" | ", mensagens);
            }
        }
        catch
        {
            // Retorna o corpo truncado da resposta caso não seja um JSON parseável
        }

        var limpo = responseBody.Trim();
        return limpo.Length > 200 ? limpo.Substring(0, 200) + "..." : limpo;
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
}
