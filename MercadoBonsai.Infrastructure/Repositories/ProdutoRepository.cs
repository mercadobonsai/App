using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using MercadoBonsai.Domain.DTOs;
using MercadoBonsai.Domain.Entities;
using MercadoBonsai.Domain.Enums;
using MercadoBonsai.Domain.Interfaces;
using MercadoBonsai.Infrastructure.Data;

namespace MercadoBonsai.Infrastructure.Repositories;

public class ProdutoRepository : IProdutoRepository
{
    private readonly SqlServerConnectionFactory _connectionFactory;

    public ProdutoRepository(SqlServerConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Guid> InserirAsync(Produto produto)
    {
        const string sql = @"
            INSERT INTO Produtos (Id, VendedorId, Nome, Descricao, Preco, Especie, IdadeAnos, Status, TipoModalidade, DataCadastro)
            OUTPUT INSERTED.Id
            VALUES (@Id, @VendedorId, @Nome, @Descricao, @Preco, @Especie, @IdadeAnos, @Status, @TipoModalidade, @DataCadastro);";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<Guid>(sql, produto);
    }

    public async Task<Produto?> ObterPorIdAsync(Guid id)
    {
        const string sqlProduto = "SELECT * FROM Produtos WHERE Id = @Id;";
        const string sqlFotos = "SELECT * FROM FotosProduto WHERE ProdutoId = @Id;";

        using var connection = _connectionFactory.CreateConnection();
        var produto = await connection.QuerySingleOrDefaultAsync<Produto>(sqlProduto, new { Id = id });

        if (produto != null)
        {
            var fotos = await connection.QueryAsync<FotoProduto>(sqlFotos, new { Id = id });
            produto.Fotos = fotos.AsList();
        }

        return produto;
    }

    public async Task<IEnumerable<ProdutoHomeDto>> ListarParaHomeAsync()
    {
        const string sql = @"
            SELECT 
                p.Id,
                p.Nome AS Titulo,
                p.Preco AS ValorVenda,
                p.TipoModalidade,
                p.Especie,
                f.Url AS FotoCapaUrl
            FROM Produtos p
            LEFT JOIN FotosProduto f ON p.Id = f.ProdutoId AND f.IsPrincipal = 1
            WHERE p.Status = @Status
            ORDER BY p.DataCadastro DESC;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<ProdutoHomeDto>(sql, new { Status = StatusProduto.Disponivel });
    }
}
