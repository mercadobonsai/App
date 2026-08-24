using System.Threading.Tasks;

namespace MercadoBonsai.Domain.Interfaces;

public interface ILeilaoService
{
    /// <summary>
    /// Verifica leilões ativos expirados, marca como Finalizados e converte o 1º colocado em pedido com cobrança Asaas.
    /// </summary>
    Task ProcessarLeiloesEncerradosAsync();

    /// <summary>
    /// Ativa em cascata o próximo arrematante da fila (2º, 3º... colocados) se o pedido do atual for recusado/cancelado.
    /// </summary>
    Task<bool> ChamarProximoColocadoAsync(int leilaoId, int posicaoAtual);
}
