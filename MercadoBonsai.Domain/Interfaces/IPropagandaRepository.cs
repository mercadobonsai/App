using System.Collections.Generic;
using System.Threading.Tasks;
using MercadoBonsai.Domain.Entities;

namespace MercadoBonsai.Domain.Interfaces;

public interface IPropagandaRepository
{
    Task<Propaganda?> ObterPorIdAsync(int id);
    Task<IEnumerable<Propaganda>> ListarTodasAsync();
    Task<IEnumerable<Propaganda>> ListarPorUsuarioAsync(int usuarioId);
    Task<IEnumerable<Propaganda>> ListarAtivasPorTipoAsync(string tipoEspaco);
    Task<int> InserirAsync(Propaganda propaganda);
    Task AtualizarAsync(Propaganda propaganda);
}
