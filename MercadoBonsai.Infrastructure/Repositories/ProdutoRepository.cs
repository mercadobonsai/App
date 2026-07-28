using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using MercadoBonsai.Domain.Entities;
using MercadoBonsai.Domain.Interfaces;
using MercadoBonsai.Infrastructure.Data;

namespace MercadoBonsai.Infrastructure.Repositories;

public class ProdutoRepository : IProdutoRepository
{
    private readonly PostgresConnectionFactory _connectionFactory;

    public ProdutoRepository(PostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> InserirAsync(Produto produto)
    {
        const string sql = @"
            INSERT INTO produtos (vendedorid, nome, descricao, preco, quantidadeestoque, imagemurl, status, datacriacao)
            VALUES (@VendedorId, @Nome, @Descricao, @Preco, @QuantidadeEstoque, @ImagemUrl, @Status, @DataCriacao)
            RETURNING id;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<int>(sql, produto);
    }

    public async Task<Produto?> ObterPorIdAsync(int id)
    {
        const string sql = @"
            SELECT 
                id AS Id, 
                vendedorid AS VendedorId, 
                nome AS Nome, 
                descricao AS Descricao, 
                preco AS Preco, 
                quantidadeestoque AS QuantidadeEstoque, 
                imagemurl AS ImagemUrl,
                status AS Status,
                datacriacao AS DataCriacao 
            FROM produtos 
            WHERE id = @Id;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Produto>(sql, new { Id = id });
    }

    public async Task<IEnumerable<Produto>> ListarTodosAsync()
    {
        const string sql = @"
            SELECT 
                id AS Id, 
                vendedorid AS VendedorId, 
                nome AS Nome, 
                descricao AS Descricao, 
                preco AS Preco, 
                quantidadeestoque AS QuantidadeEstoque, 
                imagemurl AS ImagemUrl,
                status AS Status,
                datacriacao AS DataCriacao 
            FROM produtos 
            WHERE status != 3
            ORDER BY datacriacao DESC;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Produto>(sql);
    }

    public async Task<IEnumerable<Produto>> ListarPorVendedorAsync(int vendedorId)
    {
        const string sql = @"
            SELECT 
                id AS Id, 
                vendedorid AS VendedorId, 
                nome AS Nome, 
                descricao AS Descricao, 
                preco AS Preco, 
                quantidadeestoque AS QuantidadeEstoque, 
                imagemurl AS ImagemUrl,
                status AS Status,
                datacriacao AS DataCriacao 
            FROM produtos 
            WHERE vendedorid = @VendedorId 
            ORDER BY datacriacao DESC;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Produto>(sql, new { VendedorId = vendedorId });
    }

    public async Task AtualizarAsync(Produto produto)
    {
        const string sql = @"
            UPDATE produtos
            SET 
                nome = @Nome,
                descricao = @Descricao,
                preco = @Preco,
                quantidadeestoque = @QuantidadeEstoque,
                imagemurl = @ImagemUrl,
                status = @Status
            WHERE id = @Id;";

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, produto);
    }
}
