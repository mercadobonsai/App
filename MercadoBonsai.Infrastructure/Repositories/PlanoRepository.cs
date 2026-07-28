using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using MercadoBonsai.Domain.Entities;
using MercadoBonsai.Domain.Interfaces;
using MercadoBonsai.Infrastructure.Data;

namespace MercadoBonsai.Infrastructure.Repositories;

public class PlanoRepository : IPlanoRepository
{
    private readonly PostgresConnectionFactory _connectionFactory;

    public PlanoRepository(PostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private const string SelectSql = @"
        SELECT 
            id AS Id, 
            nome AS Nome, 
            preco AS Preco, 
            percentualcomissao AS PercentualComissao, 
            limitelifas30dias AS LimiteRifas30Dias, 
            limiteleiloes30dias AS LimiteLeiloes30Dias, 
            limiteanuncios AS LimiteAnuncios, 
            destaqueshome AS DestaquesHome
        FROM planos";

    public async Task<Plano?> ObterPorIdAsync(int id)
    {
        var sql = $"{SelectSql} WHERE id = @Id;";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Plano>(sql, new { Id = id });
    }

    public async Task<IEnumerable<Plano>> ListarTodosAsync()
    {
        var sql = $"{SelectSql} ORDER BY id ASC;";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Plano>(sql);
    }

    public async Task AtualizarAsync(Plano plano)
    {
        const string sql = @"
            UPDATE planos
            SET 
                nome = @Nome,
                preco = @Preco,
                valor = @Preco,
                percentualcomissao = @PercentualComissao,
                limitelifas30dias = @LimiteRifas30Dias,
                limiteleiloes30dias = @LimiteLeiloes30Dias,
                limiteanuncios = @LimiteAnuncios,
                destaqueshome = @DestaquesHome
            WHERE id = @Id;";

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, plano);
    }
}
