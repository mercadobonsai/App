using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MercadoBonsai.Domain.Entities;
using MercadoBonsai.Domain.Interfaces;
using MercadoBonsai.Web.Models;
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

                if (responseBody.Contains("já está em uso", StringComparison.OrdinalIgnoreCase) || responseBody.Contains("ja esta em uso", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Email ou CPF/CNPJ de cliente já cadastrado no Asaas. Buscando cliente existente via GET /v3/customers...");
                    var existingCustomerId = await ObterClienteExistenteAsync(usuario.Email, cpfCnpjLimpo);
                    if (!string.IsNullOrEmpty(existingCustomerId))
                    {
                        _logger.LogInformation("Cliente existente recuperado no Asaas com ID {CustomerId}", existingCustomerId);
                        return new AsaasClienteResult { Sucesso = true, AsaasCustomerId = existingCustomerId };
                    }
                }

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
                var root = doc.RootElement;
                string? accountId = root.TryGetProperty("walletId", out var wProp) && !string.IsNullOrEmpty(wProp.GetString())
                    ? wProp.GetString()
                    : root.GetProperty("id").GetString();
                return new AsaasSubcontaResult { Sucesso = true, AsaasAccountId = accountId };
            }
            else
            {
                string erroDescrito = ExtrairErrosAsaas(responseBody);
                _logger.LogWarning("Falha ao criar Subconta Asaas HTTP {Status}: {Body}", response.StatusCode, responseBody);

                if (responseBody.Contains("já está em uso", StringComparison.OrdinalIgnoreCase) || responseBody.Contains("ja esta em uso", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Email ou CPF/CNPJ de subconta já cadastrado no Asaas. Buscando subconta existente via GET /v3/accounts...");
                    var existingAccountId = await ObterSubcontaExistenteAsync(vendedor.Email, cpfCnpjLimpo);
                    if (!string.IsNullOrEmpty(existingAccountId))
                    {
                        _logger.LogInformation("Subconta existente recuperada no Asaas com ID {AccountId}", existingAccountId);
                        return new AsaasSubcontaResult { Sucesso = true, AsaasAccountId = existingAccountId };
                    }
                }

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

                // Auto-recuperação se a wallet/subconta tiver sido excluída ou for rejeitada no Asaas Sandbox
                if (responseBody.Contains("Wallet", StringComparison.OrdinalIgnoreCase) && (responseBody.Contains("inexistente", StringComparison.OrdinalIgnoreCase) || responseBody.Contains("not found", StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogWarning("Wallet {AccountId} do vendedor #{VendedorId} não foi aceita no Asaas para split.", vendedor.AsaasAccountId, vendedor.Id);
                    
                    var novaWallet = await ObterSubcontaExistenteAsync(vendedor.Email, SomenteNumeros(vendedor.CpfCnpj));
                    if (!string.IsNullOrEmpty(novaWallet) && novaWallet != vendedor.AsaasAccountId)
                    {
                        _logger.LogInformation("Nova wallet recuperada: {NovaWallet}. Atualizando cadastro e re-tentando split...", novaWallet);
                        vendedor.AsaasAccountId = novaWallet;
                        await _usuarioRepository.AtualizarAsync(vendedor);
                        return await CriarCobrancaAsync(pedido, vendedor, percentualComissao);
                    }

                    // Se o Asaas Sandbox mantiver a wallet indisponível para split, gera cobrança direta na conta master para liberar o checkout do comprador
                    _logger.LogInformation("Emitindo cobrança direta no Asaas para liberar checkout do Pedido #{Numero}...", pedido.Numero);
                    var payloadSemSplit = new
                    {
                        customer = vendedor.AsaasCustomerId,
                        billingType = "UNDEFINED",
                        value = pedido.ValorTotal,
                        dueDate = DateTime.Now.AddDays(3).ToString("yyyy-MM-dd"),
                        description = $"Pedido #{pedido.Numero} - Mercado Bonsai",
                        externalReference = pedido.Numero.ToString()
                    };

                    var reqFallback = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/payments");
                    reqFallback.Headers.Add("access_token", apiKey);
                    reqFallback.Headers.TryAddWithoutValidation("User-Agent", userAgent);
                    reqFallback.Content = new StringContent(JsonSerializer.Serialize(payloadSemSplit), Encoding.UTF8, "application/json");

                    var respFallback = await _httpClient.SendAsync(reqFallback);
                    var bodyFallback = await respFallback.Content.ReadAsStringAsync();

                    if (respFallback.IsSuccessStatusCode)
                    {
                        using var docFallback = JsonDocument.Parse(bodyFallback);
                        var paymentId = docFallback.RootElement.GetProperty("id").GetString();
                        var invoiceUrl = docFallback.RootElement.TryGetProperty("invoiceUrl", out var invProp) ? invProp.GetString() : null;
                        var bankSlipUrl = docFallback.RootElement.TryGetProperty("bankSlipUrl", out var bsProp) ? bsProp.GetString() : null;

                        _logger.LogInformation("Cobrança emitida com sucesso no Asaas para Pedido #{Numero}! PaymentId: {PaymentId}", pedido.Numero, paymentId);
                        return new AsaasCobrancaResult
                        {
                            Sucesso = true,
                            AsaasPaymentId = paymentId,
                            UrlCheckout = invoiceUrl ?? bankSlipUrl ?? $"https://www.asaas.com/i/{paymentId}"
                        };
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

    private async Task<string?> ObterSubcontaExistenteAsync(string email, string cpfCnpj)
    {
        var apiKey = _configuration["Asaas:ApiKey"]?.Trim();
        var baseUrl = _configuration["Asaas:ApiUrl"] ?? "https://sandbox.asaas.com/api/v3";
        var userAgent = _configuration["Asaas:UserAgent"] ?? "MercadoBonsai/1.0 (suporte@mercadobonsai.com.br)";

        try
        {
            string url = !string.IsNullOrEmpty(cpfCnpj) 
                ? $"{baseUrl}/accounts?cpfCnpj={cpfCnpj}" 
                : $"{baseUrl}/accounts?email={Uri.EscapeDataString(email)}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("access_token", apiKey);
            request.Headers.TryAddWithoutValidation("User-Agent", userAgent);

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(responseBody);
                if (doc.RootElement.TryGetProperty("data", out var dataArray) && dataArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in dataArray.EnumerateArray())
                    {
                        if (item.TryGetProperty("id", out var idProp))
                        {
                            var id = idProp.GetString();
                            if (!string.IsNullOrEmpty(id)) return id;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar subconta existente no Asaas.");
        }

        return null;
    }

    private async Task<string?> ObterClienteExistenteAsync(string email, string cpfCnpj)
    {
        var apiKey = _configuration["Asaas:ApiKey"]?.Trim();
        var baseUrl = _configuration["Asaas:ApiUrl"] ?? "https://sandbox.asaas.com/api/v3";
        var userAgent = _configuration["Asaas:UserAgent"] ?? "MercadoBonsai/1.0 (suporte@mercadobonsai.com.br)";

        try
        {
            string url = !string.IsNullOrEmpty(cpfCnpj) 
                ? $"{baseUrl}/customers?cpfCnpj={cpfCnpj}" 
                : $"{baseUrl}/customers?email={Uri.EscapeDataString(email)}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("access_token", apiKey);
            request.Headers.TryAddWithoutValidation("User-Agent", userAgent);

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(responseBody);
                if (doc.RootElement.TryGetProperty("data", out var dataArray) && dataArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in dataArray.EnumerateArray())
                    {
                        if (item.TryGetProperty("id", out var idProp))
                        {
                            var id = idProp.GetString();
                            if (!string.IsNullOrEmpty(id)) return id;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar cliente existente no Asaas.");
        }

        return null;
    }

    public async Task<AsaasCobrancasPaginadasResult> ListarCobrancasAsync(CobrancaFiltroViewModel filtro, string? asaasCustomerId = null, string? asaasAccountId = null)
    {
        var apiKey = _configuration["Asaas:ApiKey"]?.Trim();
        var baseUrl = _configuration["Asaas:ApiUrl"] ?? "https://sandbox.asaas.com/api/v3";
        var userAgent = _configuration["Asaas:UserAgent"] ?? "MercadoBonsai/1.0 (suporte@mercadobonsai.com.br)";

        // Modo Stub / Chave não configurada
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "seu_asaas_token_aqui")
        {
            _logger.LogInformation("Asaas API Key não configurada. Retornando dados simulados para ListarCobrancasAsync.");
            var listaSimulada = new List<AsaasCobrancaItemDto>
            {
                new AsaasCobrancaItemDto
                {
                    Id = "pay_simulado_1005",
                    DateCreated = DateTime.Now.AddDays(-1),
                    Customer = asaasCustomerId ?? "cus_simulado_vendedor",
                    Value = 373.00m,
                    NetValue = 335.70m,
                    BillingType = "PIX",
                    Status = "PENDING",
                    DueDate = DateTime.Now.AddDays(2),
                    InvoiceUrl = "https://sandbox.asaas.com/i/pay_simulado_1005",
                    ExternalReference = "1005",
                    Description = "Pedido #1005 - Mercado Bonsai (Simulado)"
                }
            };

            return new AsaasCobrancasPaginadasResult
            {
                Sucesso = true,
                TotalCount = 1,
                Offset = filtro.Offset,
                Limit = filtro.Limit,
                HasMore = false,
                Data = listaSimulada
            };
        }

        try
        {
            var queryParams = new List<string>
            {
                $"offset={filtro.Offset}",
                $"limit={filtro.Limit}"
            };

            if (!string.IsNullOrWhiteSpace(filtro.Status))
            {
                queryParams.Add($"status={Uri.EscapeDataString(filtro.Status.Trim())}");
            }

            if (!string.IsNullOrWhiteSpace(filtro.BillingType))
            {
                queryParams.Add($"billingType={Uri.EscapeDataString(filtro.BillingType.Trim())}");
            }

            if (!string.IsNullOrWhiteSpace(filtro.ExternalReference))
            {
                queryParams.Add($"externalReference={Uri.EscapeDataString(filtro.ExternalReference.Trim())}");
            }

            // Filtro restritivo de cliente/vendedor
            string? clienteFinal = !string.IsNullOrWhiteSpace(asaasCustomerId) ? asaasCustomerId : filtro.Customer;
            if (!string.IsNullOrWhiteSpace(clienteFinal))
            {
                queryParams.Add($"customer={Uri.EscapeDataString(clienteFinal.Trim())}");
            }

            if (filtro.DataInicio.HasValue)
            {
                queryParams.Add($"dateCreated[ge]={filtro.DataInicio.Value:yyyy-MM-dd}");
            }

            if (filtro.DataFim.HasValue)
            {
                queryParams.Add($"dateCreated[le]={filtro.DataFim.Value:yyyy-MM-dd}");
            }

            string queryString = string.Join("&", queryParams);
            string requestUri = $"{baseUrl}/payments?{queryString}";

            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Add("access_token", apiKey);
            request.Headers.TryAddWithoutValidation("User-Agent", userAgent);

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(responseBody);
                var root = doc.RootElement;

                int totalCount = root.TryGetProperty("totalCount", out var tcProp) ? tcProp.GetInt32() : 0;
                bool hasMore = root.TryGetProperty("hasMore", out var hmProp) && hmProp.GetBoolean();
                int offset = root.TryGetProperty("offset", out var offProp) ? offProp.GetInt32() : filtro.Offset;
                int limit = root.TryGetProperty("limit", out var limProp) ? limProp.GetInt32() : filtro.Limit;

                var lista = new List<AsaasCobrancaItemDto>();
                if (root.TryGetProperty("data", out var dataArray) && dataArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in dataArray.EnumerateArray())
                    {
                        var dto = new AsaasCobrancaItemDto
                        {
                            Id = item.TryGetProperty("id", out var idP) ? idP.GetString() ?? "" : "",
                            DateCreated = item.TryGetProperty("dateCreated", out var dcP) && DateTime.TryParse(dcP.GetString(), out var dcVal) ? dcVal : DateTime.MinValue,
                            Customer = item.TryGetProperty("customer", out var cP) ? cP.GetString() : null,
                            Value = item.TryGetProperty("value", out var vP) ? vP.GetDecimal() : 0.00m,
                            NetValue = item.TryGetProperty("netValue", out var nvP) && nvP.ValueKind != JsonValueKind.Null ? nvP.GetDecimal() : null,
                            BillingType = item.TryGetProperty("billingType", out var btP) ? btP.GetString() ?? "" : "",
                            Status = item.TryGetProperty("status", out var stP) ? stP.GetString() ?? "" : "",
                            DueDate = item.TryGetProperty("dueDate", out var ddP) && DateTime.TryParse(ddP.GetString(), out var ddVal) ? ddVal : null,
                            PaymentDate = item.TryGetProperty("paymentDate", out var pdP) && DateTime.TryParse(pdP.GetString(), out var pdVal) ? pdVal : null,
                            InvoiceUrl = item.TryGetProperty("invoiceUrl", out var iuP) ? iuP.GetString() : (item.TryGetProperty("bankSlipUrl", out var bsP) ? bsP.GetString() : null),
                            ExternalReference = item.TryGetProperty("externalReference", out var erP) ? erP.GetString() : null,
                            Description = item.TryGetProperty("description", out var descP) ? descP.GetString() : null
                        };

                        if (item.TryGetProperty("split", out var splitArray) && splitArray.ValueKind == JsonValueKind.Array && splitArray.GetArrayLength() > 0)
                        {
                            dto.HasSplit = true;
                            var firstSplit = splitArray[0];
                            dto.SplitWalletId = firstSplit.TryGetProperty("walletId", out var wProp) ? wProp.GetString() : null;
                            dto.SplitFixedValue = firstSplit.TryGetProperty("fixedValue", out var fvProp) && fvProp.ValueKind != JsonValueKind.Null ? fvProp.GetDecimal() : null;
                            dto.SplitPercentualValue = firstSplit.TryGetProperty("percentualValue", out var pvProp) && pvProp.ValueKind != JsonValueKind.Null ? pvProp.GetDecimal() : null;
                            dto.SplitTotalValue = firstSplit.TryGetProperty("totalValue", out var tvProp) && tvProp.ValueKind != JsonValueKind.Null ? tvProp.GetDecimal() : (firstSplit.TryGetProperty("value", out var valProp) && valProp.ValueKind != JsonValueKind.Null ? valProp.GetDecimal() : dto.SplitFixedValue);
                            dto.SplitStatus = firstSplit.TryGetProperty("status", out var stSplitProp) ? stSplitProp.GetString() : null;

                            if (dto.SplitTotalValue.HasValue && dto.SplitTotalValue.Value > 0)
                            {
                                dto.ValorRetidoPlataforma = Math.Max(0.00m, dto.Value - dto.SplitTotalValue.Value);
                            }
                            else if (dto.SplitPercentualValue.HasValue)
                            {
                                decimal percentualRetencao = Math.Max(0.00m, 100.00m - dto.SplitPercentualValue.Value);
                                dto.ValorRetidoPlataforma = Math.Round(dto.Value * (percentualRetencao / 100.00m), 2);
                            }
                        }

                        lista.Add(dto);
                    }
                }

                return new AsaasCobrancasPaginadasResult
                {
                    Sucesso = true,
                    TotalCount = totalCount,
                    Offset = offset,
                    Limit = limit,
                    HasMore = hasMore,
                    Data = lista
                };
            }
            else
            {
                string erroDescrito = ExtrairErrosAsaas(responseBody);
                _logger.LogWarning("Falha ao listar cobranças Asaas HTTP {Status}: {Body}", response.StatusCode, responseBody);
                return new AsaasCobrancasPaginadasResult { Sucesso = false, MensagemErro = erroDescrito };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exceção ao comunicar com a API Asaas (ListarCobrancas)");
            return new AsaasCobrancasPaginadasResult { Sucesso = false, MensagemErro = ex.Message };
        }
    }
}
