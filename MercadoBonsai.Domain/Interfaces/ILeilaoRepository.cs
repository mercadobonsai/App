using System.Collections.Generic;
using System.Threading.Tasks;
using MercadoBonsai.Domain.Entities;

namespace MercadoBonsai.Domain.Interfaces;

public interface ILeilaoRepository
{
    Task<Leilao?> ObterPorIdAsync(int id);
    Task<Leilao?> ObterLeilaoAtivoRecenteAsync();
    Task<IEnumerable<Leilao>> ListarAtivosAsync();
    Task<IEnumerable<Leilao>> ListarPorVendedorAsync(int vendedorId);
    Task<IEnumerable<Leilao>> ListarEncerradosAsync();
    Task<int> InserirAsync(Leilao leilao);
    Task AtualizarAsync(Leilao leilao);
    Task InserirLanceAsync(LanceLeilao lance);
    Task<int> ContarPorVendedorNosUltimos30DiasAsync(int vendedorId);
}
