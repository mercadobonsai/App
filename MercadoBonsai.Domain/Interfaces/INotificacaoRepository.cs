using System.Collections.Generic;
using System.Threading.Tasks;
using MercadoBonsai.Domain.Entities;

namespace MercadoBonsai.Domain.Interfaces;

public interface INotificacaoRepository
{
    Task<int> InserirAsync(Notificacao notificacao);
    Task<IEnumerable<Notificacao>> ListarPorUsuarioAsync(int usuarioId, int limite = 10);
    Task<int> ObterTotalNaoLidasAsync(int usuarioId);
    Task MarcarComoLidaAsync(int notificacaoId, int usuarioId);
    Task MarcarTodasComoLidasAsync(int usuarioId);
    Task<IEnumerable<Notificacao>> ListarTodasAdminAsync();
    Task<bool> ExcluirAsync(int id);
}
