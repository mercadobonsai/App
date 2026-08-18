using System.Collections.Generic;
using System.Threading.Tasks;
using MercadoBonsai.Domain.Entities;

namespace MercadoBonsai.Domain.Interfaces;

public interface IPedidoRepository
{
    Task<Pedido?> ObterPorIdAsync(int id);
    Task<Pedido?> ObterPorNumeroAsync(int numero);
    Task<Pedido?> ObterPorAsaasPaymentIdAsync(string asaasPaymentId);
    Task<IEnumerable<Pedido>> ObterPorCompradorAsync(int compradorId);
    Task<IEnumerable<Pedido>> ObterPorVendedorAsync(int vendedorId);
    Task<int> CriarAsync(Pedido pedido);
    Task<bool> AtualizarAsync(Pedido pedido);
    Task<bool> AtualizarStatusAsync(int id, string novoStatus, string? observacao = null);
    Task<bool> AtualizarFreteECheckoutAsync(int id, decimal valorFrete, decimal valorTotal, string? urlCheckout, string? asaasPaymentId);
    Task<bool> AtualizarRastreioAsync(int id, string codigoRastreio, string? urlRastreio);
    Task<int> ObterProximoNumeroPedidoAsync();
}
