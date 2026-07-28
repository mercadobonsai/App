using System.Collections.Generic;
using System.Threading.Tasks;
using MercadoBonsai.Domain.Entities;

namespace MercadoBonsai.Domain.Interfaces;

public interface IProdutoRepository
{
    Task<int> InserirAsync(Produto produto);
    Task<Produto?> ObterPorIdAsync(int id);
    Task<IEnumerable<Produto>> ListarTodosAsync();
    Task<IEnumerable<Produto>> ListarPorVendedorAsync(int vendedorId);
    Task<IEnumerable<Produto>> ListarPorCategoriasAsync(params string[] categorias);
    Task AtualizarAsync(Produto produto);
}
