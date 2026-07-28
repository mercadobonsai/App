using System.Collections.Generic;
using System.Threading.Tasks;
using MercadoBonsai.Domain.Entities;

namespace MercadoBonsai.Domain.Interfaces;

public interface IUsuarioRepository
{
    Task<Usuario?> ObterPorIdAsync(int id);
    Task<Usuario?> ObterPorEmailAsync(string email);
    Task<int> InserirAsync(Usuario usuario);
    Task AtualizarAsync(Usuario usuario);
    Task<IEnumerable<Usuario>> ListarTodosAsync(string? busca, int? perfil);
    Task<IEnumerable<Usuario>> ListarViveirosEmDestaqueAsync();
}
