using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MercadoBonsai.Domain.Entities;
using MercadoBonsai.Domain.DTOs;

namespace MercadoBonsai.Domain.Interfaces;

public interface IProdutoRepository
{
    Task<Guid> InserirAsync(Produto produto);
    Task<Produto?> ObterPorIdAsync(Guid id);
    Task<IEnumerable<ProdutoHomeDto>> ListarParaHomeAsync();
}
