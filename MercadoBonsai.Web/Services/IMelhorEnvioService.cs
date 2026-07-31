using System.Collections.Generic;
using System.Threading.Tasks;

namespace MercadoBonsai.Web.Services;

public class CalculoFreteRequest
{
    public int ProdutoId { get; set; }
    public string CepOrigem { get; set; } = string.Empty;
    public string CepDestino { get; set; } = string.Empty;
    public decimal Altura { get; set; }
    public decimal Largura { get; set; }
    public decimal Comprimento { get; set; }
    public decimal Peso { get; set; }
    public decimal Preco { get; set; }
}

public class OpcaoFreteResult
{
    public int Id { get; set; }
    public string NomeServico { get; set; } = string.Empty;
    public string NomeTransportadora { get; set; } = string.Empty;
    public string LogoTransportadora { get; set; } = string.Empty;
    public decimal Preco { get; set; }
    public int PrazoDias { get; set; }
    public string? Erro { get; set; }
    public bool TemErro => !string.IsNullOrEmpty(Erro);
}

public interface IMelhorEnvioService
{
    Task<IEnumerable<OpcaoFreteResult>> CalcularFreteAsync(CalculoFreteRequest request);
}
