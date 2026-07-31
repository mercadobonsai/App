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

    // Controle de Concorrência (Lock de Edição Simultânea)
    Task AdquirirOuRenovarLockAsync(int plantaId, int usuarioId, string usuarioNome);
    Task LiberarLockAsync(int plantaId, int usuarioId);

    Task<int> InserirEventoAsync(ProntuarioEvento evento);
    Task<IEnumerable<ProntuarioEvento>> ListarEventosPorPlantaAsync(int plantaId);
}
