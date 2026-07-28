using System.Collections.Generic;
using System.Threading.Tasks;
using MercadoBonsai.Domain.Entities;

namespace MercadoBonsai.Domain.Interfaces;

public interface IPlanoRepository
{
    Task<Plano?> ObterPorIdAsync(int id);
    Task<IEnumerable<Plano>> ListarTodosAsync();
    Task AtualizarAsync(Plano plano);
}
