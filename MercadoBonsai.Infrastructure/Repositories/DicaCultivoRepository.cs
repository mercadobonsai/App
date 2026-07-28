using System.Threading.Tasks;
using Dapper;
using MercadoBonsai.Domain.Entities;
using MercadoBonsai.Domain.Interfaces;
using MercadoBonsai.Infrastructure.Data;

namespace MercadoBonsai.Infrastructure.Repositories;

public class DicaCultivoRepository : IDicaCultivoRepository
{
    private readonly PostgresConnectionFactory _connectionFactory;

    public DicaCultivoRepository(PostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<DicaCultivo?> ObterDicaRecenteAsync()
    {
        const string sql = @"
            SELECT 
                id AS Id,
                titulo AS Titulo,
                subtitulo AS Subtitulo,
                conteudo AS Conteudo,
                categoria AS Categoria,
                isativa AS IsAtiva,
                datacriacao AS DataCriacao
            FROM dicascultivo
            WHERE isativa = TRUE
            ORDER BY datacriacao DESC
            LIMIT 1;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<DicaCultivo>(sql);
    }
}
