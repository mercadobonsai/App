using System;
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

    private const string SelectFields = @"
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
        datacriacao AS DataCriacao";

    public async Task<Leilao?> ObterPorIdAsync(int id)
    {
        var sqlLeilao = $@"
            SELECT {SelectFields}
            FROM leiloes
            WHERE id = @Id;";

        var sqlLances = @"
            SELECT 
                id AS Id,
                leilaoid AS LeilaoId,
                usuarioid AS UsuarioId,
                usuarionome AS UsuarioNome,
                valor AS Valor,
                datalance AS DataLance
            FROM lancesleilao
            WHERE leilaoid = @Id
            ORDER BY datalance DESC;";

        using var connection = _connectionFactory.CreateConnection();
        var leilao = await connection.QuerySingleOrDefaultAsync<Leilao>(sqlLeilao, new { Id = id });

        if (leilao != null)
        {
            var lances = await connection.QueryAsync<LanceLeilao>(sqlLances, new { Id = id });
            leilao.Lances = lances.ToList();
        }

        return leilao;
    }

    public async Task<Leilao?> ObterLeilaoAtivoRecenteAsync()
    {
        var sqlLeilao = $@"
            SELECT {SelectFields}
            FROM leiloes
            WHERE status IN (1, 2)
            ORDER BY datacriacao DESC
            LIMIT 1;";

        using var connection = _connectionFactory.CreateConnection();
        var leilao = await connection.QuerySingleOrDefaultAsync<Leilao>(sqlLeilao);

        if (leilao != null)
        {
            var sqlLances = @"
                SELECT 
                    id AS Id,
                    leilaoid AS LeilaoId,
                    usuarioid AS UsuarioId,
                    usuarionome AS UsuarioNome,
                    valor AS Valor,
                    datalance AS DataLance
                FROM lancesleilao
                WHERE leilaoid = @Id
                ORDER BY datalance DESC
                LIMIT 5;";

            var lances = await connection.QueryAsync<LanceLeilao>(sqlLances, new { Id = leilao.Id });
            leilao.Lances = lances.ToList();
        }

        return leilao;
    }

    public async Task<IEnumerable<Leilao>> ListarPorVendedorAsync(int vendedorId)
    {
        var sql = $@"
            SELECT {SelectFields}
            FROM leiloes
            WHERE vendedorid = @VendedorId
            ORDER BY datacriacao DESC;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Leilao>(sql, new { VendedorId = vendedorId });
    }

    public async Task<IEnumerable<Leilao>> ListarEncerradosAsync()
    {
        var sql = $@"
            SELECT {SelectFields}
            FROM leiloes
            WHERE status = 4 OR datafinalizacao < CURRENT_TIMESTAMP
            ORDER BY datafinalizacao DESC;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Leilao>(sql);
    }

    public async Task<int> InserirAsync(Leilao leilao)
    {
        const string sql = @"
            INSERT INTO leiloes (
                titulo, subtitulo, descricao, fotoprincipalurl, fotodetalheurl, badge, 
                lanceatual, proximolanceminimo, incrementominimo, vendedorid, vendedornome, 
                datafinalizacao, status, datacriacao
            )
            VALUES (
                @Titulo, @Subtitulo, @Descricao, @FotoPrincipalUrl, @FotoDetalheUrl, @Badge, 
                @LanceAtual, @ProximoLanceMinimo, @IncrementoMinimo, @VendedorId, @VendedorNome, 
                @DataFinalizacao, @Status, @DataCriacao
            )
            RETURNING id;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<int>(sql, leilao);
    }

    public async Task AtualizarAsync(Leilao leilao)
    {
        const string sql = @"
            UPDATE leiloes
            SET 
                titulo = @Titulo,
                subtitulo = @Subtitulo,
                descricao = @Descricao,
                fotoprincipalurl = @FotoPrincipalUrl,
                fotodetalheurl = @FotoDetalheUrl,
                badge = @Badge,
                lanceatual = @LanceAtual,
                proximolanceminimo = @ProximoLanceMinimo,
                incrementominimo = @IncrementoMinimo,
                datafinalizacao = @DataFinalizacao,
                status = @Status
            WHERE id = @Id;";

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, leilao);
    }

    public async Task<int> ContarPorVendedorNosUltimos30DiasAsync(int vendedorId)
    {
        const string sql = @"
            SELECT COUNT(1) 
            FROM leiloes 
            WHERE vendedorid = @VendedorId AND datacriacao >= CURRENT_TIMESTAMP - INTERVAL '30 days';";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql, new { VendedorId = vendedorId });
    }
}
