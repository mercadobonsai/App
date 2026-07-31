using System.Collections.Generic;
using System.Threading.Tasks;
using MercadoBonsai.Domain.Entities;

namespace MercadoBonsai.Domain.Interfaces;

public interface IProntuarioRepository
{
    Task<int> InserirPlantaAsync(ProntuarioPlanta planta);
    Task<ProntuarioPlanta?> ObterPlantaPorIdAsync(int id);
    Task<IEnumerable<ProntuarioPlanta>> ListarPlantasPorUsuarioAsync(int usuarioId);
    Task AtualizarPlantaAsync(ProntuarioPlanta planta);
    Task DeletarPlantaAsync(int id);

    Task<int> InserirEventoAsync(ProntuarioEvento evento);
    Task<IEnumerable<ProntuarioEvento>> ListarEventosPorPlantaAsync(int plantaId);
}
