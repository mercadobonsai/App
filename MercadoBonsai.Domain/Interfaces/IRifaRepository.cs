using System.Collections.Generic;
using System.Threading.Tasks;
using MercadoBonsai.Domain.Entities;

namespace MercadoBonsai.Domain.Interfaces;

public interface IRifaRepository
{
    Task<Rifa?> ObterPorIdAsync(int id);
    Task<Rifa?> ObterRifaAtivaRecenteAsync();
    Task<IEnumerable<Rifa>> ListarPorVendedorAsync(int vendedorId);
    Task<int> InserirAsync(Rifa rifa);
    Task AtualizarAsync(Rifa rifa);
    Task<int> ContarPorVendedorNosUltimos30DiasAsync(int vendedorId);
}
