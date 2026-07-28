using System.Threading.Tasks;
using MercadoBonsai.Domain.Entities;

namespace MercadoBonsai.Domain.Interfaces;

public interface IDicaCultivoRepository
{
    Task<DicaCultivo?> ObterDicaRecenteAsync();
}
