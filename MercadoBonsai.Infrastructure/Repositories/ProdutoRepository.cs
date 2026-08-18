using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using MercadoBonsai.Domain.Entities;
using MercadoBonsai.Domain.Enums;
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

    private const string SelectFields = @"
        id AS Id, 
        vendedorid AS VendedorId, 
        nome AS Nome, 
        descricao AS Descricao, 
        preco AS Preco, 
        quantidadeestoque AS QuantidadeEstoque, 
        imagemurl AS ImagemUrl,
        status AS Status,
        altura AS Altura,
        largura AS Largura,
        comprimento AS Comprimento,
        peso AS Peso,
        formaenvio AS FormaEnvio,
        categoria AS Categoria,
        datacriacao AS DataCriacao";

    public async Task<int> InserirAsync(Produto produto)
    {
        const string sql = @"
            INSERT INTO produtos (
                vendedorid, nome, descricao, preco, quantidadeestoque, imagemurl, status, 
                altura, largura, comprimento, peso, formaenvio, categoria, datacriacao
            )
            VALUES (
                @VendedorId, @Nome, @Descricao, @Preco, @QuantidadeEstoque, @ImagemUrl, @Status, 
                @Altura, @Largura, @Comprimento, @Peso, @FormaEnvio, @Categoria, @DataCriacao
            )
            RETURNING id;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<int>(sql, produto);
    }

    public async Task<Produto?> ObterPorIdAsync(int id)
    {
        var sql = $@"
            SELECT {SelectFields}
            FROM produtos 
            WHERE id = @Id;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Produto>(sql, new { Id = id });
    }

    public async Task<IEnumerable<Produto>> ListarTodosAsync()
    {
        var sql = $@"
            SELECT {SelectFields}
            FROM produtos 
            WHERE status != 3
            ORDER BY datacriacao DESC;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Produto>(sql);
    }

    public async Task<IEnumerable<Produto>> ListarPorVendedorAsync(int vendedorId)
    {
        var sql = $@"
            SELECT {SelectFields}
            FROM produtos 
            WHERE vendedorid = @VendedorId 
            ORDER BY datacriacao DESC;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Produto>(sql, new { VendedorId = vendedorId });
    }

    public async Task<IEnumerable<Produto>> ListarPorCategoriasAsync(params string[] categorias)
    {
        if (categorias == null || !categorias.Any())
            return Enumerable.Empty<Produto>();

        var sql = $@"
            SELECT {SelectFields}
            FROM produtos 
            WHERE status != 3 AND LOWER(categoria) = ANY(@Categorias)
            ORDER BY datacriacao DESC;";

        var categoriasArray = categorias.Select(c => c.Trim().ToLower()).ToArray();

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Produto>(sql, new { Categorias = categoriasArray });
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
                status = @Status,
                altura = @Altura,
                largura = @Largura,
                comprimento = @Comprimento,
                peso = @Peso,
                formaenvio = @FormaEnvio,
                categoria = @Categoria
            WHERE id = @Id;";

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, produto);
    }

    public async Task<bool> AtualizarStatusDisponibilidadeAsync(int id, StatusProduto status, int quantidadeEstoque)
    {
        const string sql = @"
            UPDATE produtos
            SET 
                status = @status,
                quantidadeestoque = @quantidadeEstoque
            WHERE id = @id;";

        using var connection = _connectionFactory.CreateConnection();
        var rows = await connection.ExecuteAsync(sql, new { id, status = (int)status, quantidadeEstoque });
        return rows > 0;
    }
}
