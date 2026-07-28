using System.Collections.Generic;
using System.Threading.Tasks;
using MercadoBonsai.Domain.Entities;

namespace MercadoBonsai.Domain.Interfaces;

public interface IPatrocinioRepository
{
    Task<Patrocinio?> ObterPatrocinioDestaqueAsync();
    Task<IEnumerable<Patrocinio>> ListarAtivosAsync();
}
