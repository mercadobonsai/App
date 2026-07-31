using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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

        var cepOrigemLimpo = SomenteNumeros(request.CepOrigem);
        var cepDestinoLimpo = SomenteNumeros(request.CepDestino);

        if (string.IsNullOrWhiteSpace(cepOrigemLimpo) || cepOrigemLimpo.Length != 8)
        {
            cepOrigemLimpo = "01001000"; // Fallback para CEP do centro de SP caso origem do vendedor seja nula
        }

        // Se houver um token configurado no appsettings, tenta a API real do Melhor Envio
        if (!string.IsNullOrEmpty(token) && token != "seu_token_aqui")
        {
            try
            {
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

                httpRequest.Headers.Add("User-Agent", userAgent);
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.SendAsync(httpRequest);
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var opcoesApi = ParseResponseApi(responseJson);
                    if (opcoesApi != null && opcoesApi.Count > 0)
                    {
                        return opcoesApi;
                    }
                }
                else
                {
                    _logger.LogWarning("API Melhor Envio retornou StatusCode {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao conectar com a API do Melhor Envio. Utilizando calculador fallback.");
            }
        }

        // Fallback simulado caso o token não esteja configurado ou a API esteja off-line
        return GerarOpcoesSimuladasFallback(request);
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
                int id = element.TryGetProperty("id", out var idProp) ? idProp.GetInt32() : 0;
                string nomeServico = element.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? "" : "";
                
                string erro = element.TryGetProperty("error", out var errProp) && errProp.ValueKind == JsonValueKind.String 
                    ? errProp.GetString() 
                    : null;

                decimal preco = 0;
                if (element.TryGetProperty("custom_price", out var cpProp) && cpProp.ValueKind == JsonValueKind.Number)
                {
                    preco = cpProp.GetDecimal();
                }
                else if (element.TryGetProperty("price", out var pProp) && pProp.ValueKind == JsonValueKind.Number)
                {
                    preco = pProp.GetDecimal();
                }

                int prazo = element.TryGetProperty("delivery_time", out var dtProp) && dtProp.ValueKind == JsonValueKind.Number 
                    ? dtProp.GetInt32() 
                    : 0;

                string transportadoraNome = "Transportadora";
                string transportadoraLogo = "/starter-kit/assets/img/correios.png";

                if (element.TryGetProperty("company", out var companyProp) && companyProp.ValueKind == JsonValueKind.Object)
                {
                    if (companyProp.TryGetProperty("name", out var cNameProp))
                    {
                        transportadoraNome = cNameProp.GetString() ?? "Transportadora";
                    }
                    if (companyProp.TryGetProperty("picture", out var cPicProp))
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

    private IEnumerable<OpcaoFreteResult> GerarOpcoesSimuladasFallback(CalculoFreteRequest req)
    {
        // Cálculo de tarifa estimado com base em peso volumétrico e seguro
        decimal pesoVolumetrico = (req.Altura * req.Largura * req.Comprimento) / 6000m;
        decimal pesoEfetivo = Math.Max(req.Peso, pesoVolumetrico);
        decimal seguroAdicional = req.Preco * 0.0075m; // 0.75% seguro ad valorem

        // Se dimensões excederem limite de transportadora pequena
        bool excedeMedidas = req.Altura > 105 || req.Largura > 105 || req.Comprimento > 105 || (req.Altura + req.Largura + req.Comprimento) > 200;

        return new List<OpcaoFreteResult>
        {
            new OpcaoFreteResult
            {
                Id = 1,
                NomeServico = "PAC Correios",
                NomeTransportadora = "Correios",
                LogoTransportadora = "https://www.melhorenvio.com.br/images/shipping-companies/correios.png",
                Preco = Math.Round(22.50m + (pesoEfetivo * 4.20m) + seguroAdicional, 2),
                PrazoDias = 6,
                Erro = null
            },
            new OpcaoFreteResult
            {
                Id = 2,
                NomeServico = "SEDEX Express",
                NomeTransportadora = "Correios",
                LogoTransportadora = "https://www.melhorenvio.com.br/images/shipping-companies/correios.png",
                Preco = Math.Round(38.90m + (pesoEfetivo * 7.50m) + seguroAdicional, 2),
                PrazoDias = 2,
                Erro = null
            },
            new OpcaoFreteResult
            {
                Id = 3,
                NomeServico = "Jadlog .Package",
                NomeTransportadora = "Jadlog",
                LogoTransportadora = "https://www.melhorenvio.com.br/images/shipping-companies/jadlog.png",
                Preco = Math.Round(19.80m + (pesoEfetivo * 3.90m) + seguroAdicional, 2),
                PrazoDias = 4,
                Erro = null
            },
            new OpcaoFreteResult
            {
                Id = 4,
                NomeServico = "Jadlog .Com",
                NomeTransportadora = "Jadlog",
                LogoTransportadora = "https://www.melhorenvio.com.br/images/shipping-companies/jadlog.png",
                Preco = Math.Round(32.40m + (pesoEfetivo * 6.20m) + seguroAdicional, 2),
                PrazoDias = 3,
                Erro = null
            },
            new OpcaoFreteResult
            {
                Id = 5,
                NomeServico = "Azul Cargo Express",
                NomeTransportadora = "Azul Cargo",
                LogoTransportadora = "https://www.melhorenvio.com.br/images/shipping-companies/azul-cargo.png",
                Preco = excedeMedidas ? 0 : Math.Round(45.00m + (pesoEfetivo * 8.90m) + seguroAdicional, 2),
                PrazoDias = 2,
                Erro = excedeMedidas ? "As dimensões desta planta ultrapassam o limite máximo para a modalidade aérea da transportadora." : null
            }
        };
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
