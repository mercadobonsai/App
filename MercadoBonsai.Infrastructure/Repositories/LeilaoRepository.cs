using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using MercadoBonsai.Domain.Entities;
using MercadoBonsai.Domain.Interfaces;
using MercadoBonsai.Infrastructure.Data;

namespace MercadoBonsai.Infrastructure.Repositories;

public class LeilaoRepository : ILeilaoRepository
{
    private readonly PostgresConnectionFactory _connectionFactory;

    public LeilaoRepository(PostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Leilao?> ObterLeilaoAtivoRecenteAsync()
    {
        const string sqlLeilao = @"
            SELECT 
                id AS Id,
                titulo AS Titulo,
                subtitulo AS Subtitulo,
                descricao AS Descricao,
                fotoprincipalurl AS FotoPrincipalUrl,
                fotodetalheurl AS FotoDetalheUrl,
                badge AS Badge,
                lanceatual AS LanceAtual,
                proximolanceminimo AS ProximoLanceMinimo,
                incrementominimo AS IncrementoMinimo,
                vendedorid AS VendedorId,
                vendedornome AS VendedorNome,
                datafinalizacao AS DataFinalizacao,
                status AS Status,
                datacriacao AS DataCriacao
            FROM leiloes
            WHERE status = 1
            ORDER BY datacriacao DESC
            LIMIT 1;";

        using var connection = _connectionFactory.CreateConnection();
        var leilao = await connection.QuerySingleOrDefaultAsync<Leilao>(sqlLeilao);

        if (leilao != null)
        {
            var lances = await ListarLancesPorLeilaoIdAsync(leilao.Id);
            leilao.Lances = lances.ToList();
        }

        return leilao;
    }

    public async Task<Leilao?> ObterPorIdAsync(int id)
    {
        const string sql = @"
            SELECT 
                id AS Id,
                titulo AS Titulo,
                subtitulo AS Subtitulo,
                descricao AS Descricao,
                fotoprincipalurl AS FotoPrincipalUrl,
                fotodetalheurl AS FotoDetalheUrl,
                badge AS Badge,
                lanceatual AS LanceAtual,
                proximolanceminimo AS ProximoLanceMinimo,
                incrementominimo AS IncrementoMinimo,
                vendedorid AS VendedorId,
                vendedornome AS VendedorNome,
                datafinalizacao AS DataFinalizacao,
                status AS Status,
                datacriacao AS DataCriacao
            FROM leiloes
            WHERE id = @Id;";

        using var connection = _connectionFactory.CreateConnection();
        var leilao = await connection.QuerySingleOrDefaultAsync<Leilao>(sql, new { Id = id });

        if (leilao != null)
        {
            var lances = await ListarLancesPorLeilaoIdAsync(leilao.Id);
            leilao.Lances = lances.ToList();
        }

        return leilao;
    }

    public async Task<IEnumerable<LanceLeilao>> ListarLancesPorLeilaoIdAsync(int leilaoId)
    {
        const string sql = @"
            SELECT 
                id AS Id,
                leilaoid AS LeilaoId,
                usuarioid AS UsuarioId,
                usuarionome AS UsuarioNome,
                valor AS Valor,
                datalance AS DataLance
            FROM lancesleilao
            WHERE leilaoid = @LeilaoId
            ORDER BY valor DESC, datalance DESC;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<LanceLeilao>(sql, new { LeilaoId = leilaoId });
    }

    public async Task InserirLanceAsync(LanceLeilao lance)
    {
        const string sqlLance = @"
            INSERT INTO lancesleilao (leilaoid, usuarioid, usuarionome, valor, datalance)
            VALUES (@LeilaoId, @UsuarioId, @UsuarioNome, @Valor, @DataLance);";

        const string sqlUpdateLeilao = @"
            UPDATE leiloes
            SET lanceatual = @Valor,
                proximolanceminimo = @Valor + incrementominimo
            WHERE id = @LeilaoId;";

        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        await connection.ExecuteAsync(sqlLance, lance, transaction);
        await connection.ExecuteAsync(sqlUpdateLeilao, new { LeilaoId = lance.LeilaoId, Valor = lance.Valor }, transaction);

        transaction.Commit();
    }
}
