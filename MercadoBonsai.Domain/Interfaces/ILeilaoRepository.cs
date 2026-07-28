using System.Collections.Generic;
using System.Threading.Tasks;
using MercadoBonsai.Domain.Entities;

namespace MercadoBonsai.Domain.Interfaces;

public interface ILeilaoRepository
{
    Task<Leilao?> ObterLeilaoAtivoRecenteAsync();
    Task<Leilao?> ObterPorIdAsync(int id);
    Task<IEnumerable<LanceLeilao>> ListarLancesPorLeilaoIdAsync(int leilaoId);
    Task InserirLanceAsync(LanceLeilao lance);
}
