using System;
using System.Threading.Tasks;
using MercadoBonsai.Domain.Entities;

namespace MercadoBonsai.Domain.Interfaces;

public interface IUsuarioRepository
{
    Task<Usuario?> ObterPorEmailAsync(string email);
    Task<Guid> InserirAsync(Usuario usuario);
}
