using System.Threading.Tasks;
using MercadoBonsai.Domain.Entities;

namespace MercadoBonsai.Web.Services;

public class AsaasClienteResult
{
    public bool Sucesso { get; set; }
    public string? AsaasCustomerId { get; set; }
    public string? MensagemErro { get; set; }
}

public class AsaasSubcontaResult
{
    public bool Sucesso { get; set; }
    public string? AsaasAccountId { get; set; }
    public string? MensagemErro { get; set; }
}

public class AsaasCobrancaResult
{
    public bool Sucesso { get; set; }
    public string? AsaasPaymentId { get; set; }
    public string? UrlCheckout { get; set; }
    public string? MensagemErro { get; set; }
}

public interface IAsaasService
{
    Task<AsaasClienteResult> CriarClienteAsync(Usuario usuario);
    Task<AsaasSubcontaResult> CriarSubcontaVendedorAsync(Usuario vendedor);
    Task<AsaasCobrancaResult> CriarCobrancaAsync(Pedido pedido, Usuario vendedor);
}
