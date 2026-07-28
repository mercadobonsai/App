using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using MercadoBonsai.Domain.Entities;
using MercadoBonsai.Domain.Interfaces;
using MercadoBonsai.Infrastructure.Data;

namespace MercadoBonsai.Infrastructure.Repositories;

public class PatrocinioRepository : IPatrocinioRepository
{
    private readonly PostgresConnectionFactory _connectionFactory;

    public PatrocinioRepository(PostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Patrocinio?> ObterPatrocinioDestaqueAsync()
    {
        const string sql = @"
            SELECT 
                id AS Id,
                nomelojaviveiro AS NomeLojaViveiro,
                descricao AS Descricao,
                imagemurl AS ImagemUrl,
                linkdestino AS LinkDestino,
                badge AS Badge,
                posicao AS Posicao,
                isativo AS IsAtivo,
                datacriacao AS DataCriacao
            FROM patrocinios
            WHERE isativo = TRUE AND posicao = 1
            ORDER BY datacriacao DESC
            LIMIT 1;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Patrocinio>(sql);
    }

    public async Task<IEnumerable<Patrocinio>> ListarAtivosAsync()
    {
        const string sql = @"
            SELECT 
                id AS Id,
                nomelojaviveiro AS NomeLojaViveiro,
                descricao AS Descricao,
                imagemurl AS ImagemUrl,
                linkdestino AS LinkDestino,
                badge AS Badge,
                posicao AS Posicao,
                isativo AS IsAtivo,
                datacriacao AS DataCriacao
            FROM patrocinios
            WHERE isativo = TRUE
            ORDER BY posicao ASC, datacriacao DESC;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Patrocinio>(sql);
    }
}
