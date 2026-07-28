using System.Threading.Tasks;
using MercadoBonsai.Domain.Entities;

namespace MercadoBonsai.Domain.Interfaces;

public interface IRifaRepository
{
    Task<Rifa?> ObterRifaAtivaRecenteAsync();
    Task<Rifa?> ObterPorIdAsync(int id);
}
