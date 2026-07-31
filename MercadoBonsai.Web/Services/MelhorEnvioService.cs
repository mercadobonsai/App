using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MercadoBonsai.Web.Services;

public class MelhorEnvioService : IMelhorEnvioService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MelhorEnvioService> _logger;

    public MelhorEnvioService(HttpClient httpClient, IConfiguration configuration, ILogger<MelhorEnvioService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IEnumerable<OpcaoFreteResult>> CalcularFreteAsync(CalculoFreteRequest request)
    {
        var token = _configuration["MelhorEnvio:Token"]?.Trim();
        var apiUrl = _configuration["MelhorEnvio:ApiUrl"] ?? "https://www.melhorenvio.com.br/api/v2/me/shipment/calculate";
        var userAgent = _configuration["MelhorEnvio:UserAgent"] ?? "suporte@mercadobonsai.com.br";

        // 1. Validação Obrigatória do Token Real
        if (string.IsNullOrEmpty(token) || token == "seu_token_aqui")
        {
            throw new InvalidOperationException(
                "O Token de acesso da API do Melhor Envio não está configurado. " +
                "Por favor, insira um Token válido no arquivo appsettings.json ('MelhorEnvio:Token') para realizar cotações reais em tempo real."
            );
        }

        // 2. Higienização e Validação do CEP do Vendedor (Origem)
        var cepOrigemLimpo = SomenteNumeros(request.CepOrigem);
        if (string.IsNullOrWhiteSpace(cepOrigemLimpo) || cepOrigemLimpo.Length != 8)
        {
            throw new InvalidOperationException("O CEP de origem do vendedor está incompleto ou inválido no perfil do usuário.");
        }

        // 3. Higienização e Validação do CEP do Comprador (Destino)
        var cepDestinoLimpo = SomenteNumeros(request.CepDestino);
        if (string.IsNullOrWhiteSpace(cepDestinoLimpo) || cepDestinoLimpo.Length != 8)
        {
            throw new InvalidOperationException("O CEP de destino digitado deve conter exatamente 8 dígitos válidos.");
        }

        // 4. Montagem do Payload Dinâmico com Dimensões da Planta e Seguro (Preço)
        var payload = new
        {
            from = new { postal_code = cepOrigemLimpo },
            to = new { postal_code = cepDestinoLimpo },
            products = new[]
            {
                new
                {
                    id = request.ProdutoId.ToString(),
                    width = request.Largura,
                    height = request.Altura,
                    length = request.Comprimento,
                    weight = request.Peso,
                    insurance_value = request.Preco,
                    quantity = 1
                }
            }
        };

        var jsonPayload = JsonSerializer.Serialize(payload);
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, apiUrl)
        {
            Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
        };

        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpRequest.Headers.Add("User-Agent", userAgent);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        _logger.LogInformation("Enviando requisição de cotação para API do Melhor Envio. Origem: {Origem}, Destino: {Destino}", cepOrigemLimpo, cepDestinoLimpo);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(httpRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro de conectividade ao enviar chamada HTTP para o Melhor Envio.");
            throw new InvalidOperationException("Não foi possível conectar aos servidores do Melhor Envio. Verifique a conexão com a internet ou tente novamente em instantes.", ex);
        }

        var responseBody = await response.Content.ReadAsStringAsync();

        // 5. Tratamento de Respostas de Erro da API
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("API Melhor Envio retornou HTTP {StatusCode}: {ResponseBody}", response.StatusCode, responseBody);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                throw new UnauthorizedAccessException("A autenticação com a API do Melhor Envio falhou. Verifique se o Token Bearer no appsettings.json está correto e ativo.");
            }

            var mensagemErro = ExtrairMensagemErroJson(responseBody);
            throw new InvalidOperationException($"A API do Melhor Envio recusou a cotação (HTTP {(int)response.StatusCode}): {mensagemErro}");
        }

        // 6. Deserialização dos Resultados Reais Retornados
        var opcoes = ParseResponseApi(responseBody);
        if (opcoes == null || !opcoes.Count.Equals(0) == false)
        {
            _logger.LogInformation("API do Melhor Envio processada com sucesso. Total de modalidades: {Count}", opcoes.Count);
        }

        return opcoes;
    }

    private List<OpcaoFreteResult> ParseResponseApi(string json)
    {
        var resultados = new List<OpcaoFreteResult>();
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Array) return resultados;

        foreach (var element in doc.RootElement.EnumerateArray())
        {
            try
            {
                int id = element.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.Number ? idProp.GetInt32() : 0;
                string nomeServico = element.TryGetProperty("name", out var nameProp) && nameProp.ValueKind == JsonValueKind.String ? nameProp.GetString() ?? "" : "";
                
                string? erro = null;
                if (element.TryGetProperty("error", out var errProp))
                {
                    if (errProp.ValueKind == JsonValueKind.String)
                    {
                        erro = errProp.GetString();
                    }
                    else if (errProp.ValueKind == JsonValueKind.Object && errProp.TryGetProperty("message", out var msgProp) && msgProp.ValueKind == JsonValueKind.String)
                    {
                        erro = msgProp.GetString();
                    }
                }

                decimal preco = 0;
                if (element.TryGetProperty("custom_price", out var cpProp) && cpProp.ValueKind == JsonValueKind.Number)
                {
                    preco = cpProp.GetDecimal();
                }
                else if (element.TryGetProperty("price", out var pProp) && pProp.ValueKind == JsonValueKind.Number)
                {
                    preco = pProp.GetDecimal();
                }

                int prazo = 0;
                if (element.TryGetProperty("delivery_time", out var dtProp) && dtProp.ValueKind == JsonValueKind.Number)
                {
                    prazo = dtProp.GetInt32();
                }
                else if (element.TryGetProperty("custom_delivery_time", out var cdtProp) && cdtProp.ValueKind == JsonValueKind.Number)
                {
                    prazo = cdtProp.GetInt32();
                }

                string transportadoraNome = "Transportadora";
                string transportadoraLogo = "https://www.melhorenvio.com.br/images/shipping-companies/correios.png";

                if (element.TryGetProperty("company", out var companyProp) && companyProp.ValueKind == JsonValueKind.Object)
                {
                    if (companyProp.TryGetProperty("name", out var cNameProp) && cNameProp.ValueKind == JsonValueKind.String)
                    {
                        transportadoraNome = cNameProp.GetString() ?? "Transportadora";
                    }
                    if (companyProp.TryGetProperty("picture", out var cPicProp) && cPicProp.ValueKind == JsonValueKind.String)
                    {
                        transportadoraLogo = cPicProp.GetString() ?? "";
                    }
                }

                resultados.Add(new OpcaoFreteResult
                {
                    Id = id,
                    NomeServico = nomeServico,
                    NomeTransportadora = transportadoraNome,
                    LogoTransportadora = transportadoraLogo,
                    Preco = preco,
                    PrazoDias = prazo,
                    Erro = erro
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao deserializar item da API do Melhor Envio.");
            }
        }

        return resultados;
    }

    private static string ExtrairMensagemErroJson(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody)) return "Sem detalhes adicionais da API.";
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            if (doc.RootElement.TryGetProperty("message", out var msgProp) && msgProp.ValueKind == JsonValueKind.String)
            {
                return msgProp.GetString() ?? responseBody;
            }
            if (doc.RootElement.TryGetProperty("error", out var errProp) && errProp.ValueKind == JsonValueKind.String)
            {
                return errProp.GetString() ?? responseBody;
            }
        }
        catch
        {
            // Ignora erro de parse se o corpo não for um objeto JSON padrão
        }

        return responseBody.Length > 250 ? responseBody.Substring(0, 250) + "..." : responseBody;
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
