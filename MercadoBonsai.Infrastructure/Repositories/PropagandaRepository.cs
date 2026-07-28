using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using MercadoBonsai.Domain.Entities;
using MercadoBonsai.Domain.Interfaces;
using MercadoBonsai.Infrastructure.Data;

namespace MercadoBonsai.Infrastructure.Repositories;

public class PropagandaRepository : IPropagandaRepository
{
    private readonly PostgresConnectionFactory _connectionFactory;

    public PropagandaRepository(PostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private const string SelectFields = @"
        id AS Id,
        usuarioid AS UsuarioId,
        usuarionome AS UsuarioNome,
        tipoespaco AS TipoEspaco,
        precomensal AS PrecoMensal,
        imagemurl AS ImagemUrl,
        linkdestino AS LinkDestino,
        status AS Status,
        datainicio AS DataInicio,
        dataexpiracao AS DataExpiracao,
        datacriacao AS DataCriacao";

    public async Task<Propaganda?> ObterPorIdAsync(int id)
    {
        var sql = $@"
            SELECT {SelectFields}
            FROM propagandas
            WHERE id = @Id;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Propaganda>(sql, new { Id = id });
    }

    public async Task<IEnumerable<Propaganda>> ListarTodasAsync()
    {
        var sql = $@"
            SELECT {SelectFields}
            FROM propagandas
            ORDER BY datacriacao DESC;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Propaganda>(sql);
    }

    public async Task<IEnumerable<Propaganda>> ListarPorUsuarioAsync(int usuarioId)
    {
        var sql = $@"
            SELECT {SelectFields}
            FROM propagandas
            WHERE usuarioid = @UsuarioId
            ORDER BY datacriacao DESC;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Propaganda>(sql, new { UsuarioId = usuarioId });
    }

    public async Task<IEnumerable<Propaganda>> ListarAtivasPorTipoAsync(string tipoEspaco)
    {
        var sql = $@"
            SELECT {SelectFields}
            FROM propagandas
            WHERE LOWER(tipoespaco) = LOWER(@TipoEspaco)
              AND status = 'Ativo'
              AND (dataexpiracao IS NULL OR dataexpiracao >= CURRENT_TIMESTAMP)
            ORDER BY datacriacao DESC;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Propaganda>(sql, new { TipoEspaco = tipoEspaco });
    }

    public async Task<int> InserirAsync(Propaganda propaganda)
    {
        const string sql = @"
            INSERT INTO propagandas (
                usuarioid, usuarionome, tipoespaco, precomensal, 
                imagemurl, linkdestino, status, datainicio, dataexpiracao, datacriacao
            )
            VALUES (
                @UsuarioId, @UsuarioNome, @TipoEspaco, @PrecoMensal, 
                @ImagemUrl, @LinkDestino, @Status, @DataInicio, @DataExpiracao, @DataCriacao
            )
            RETURNING id;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<int>(sql, propaganda);
    }

    public async Task AtualizarAsync(Propaganda propaganda)
    {
        const string sql = @"
            UPDATE propagandas
            SET 
                usuarioid = @UsuarioId,
                usuarionome = @UsuarioNome,
                tipoespaco = @TipoEspaco,
                precomensal = @PrecoMensal,
                imagemurl = @ImagemUrl,
                linkdestino = @LinkDestino,
                status = @Status,
                datainicio = @DataInicio,
                dataexpiracao = @DataExpiracao
            WHERE id = @Id;";

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, propaganda);
    }
}
