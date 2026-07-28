using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using MercadoBonsai.Domain.Entities;
using MercadoBonsai.Domain.Interfaces;
using MercadoBonsai.Infrastructure.Data;

namespace MercadoBonsai.Infrastructure.Repositories;

public class RifaRepository : IRifaRepository
{
    private readonly PostgresConnectionFactory _connectionFactory;

    public RifaRepository(PostgresConnectionFactory connectionFactory)
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
        valorcota AS ValorCota,
        totalcotas AS TotalCotas,
        cotasvendidas AS CotasVendidas,
        vendedorid AS VendedorId,
        vendedornome AS VendedorNome,
        datasorteio AS DataSorteio,
        status AS Status,
        datacriacao AS DataCriacao";

    public async Task<Rifa?> ObterPorIdAsync(int id)
    {
        var sql = $@"
            SELECT {SelectFields}
            FROM rifas
            WHERE id = @Id;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Rifa>(sql, new { Id = id });
    }

    public async Task<Rifa?> ObterRifaAtivaRecenteAsync()
    {
        var sql = $@"
            SELECT {SelectFields}
            FROM rifas
            WHERE status = 1
            ORDER BY datacriacao DESC
            LIMIT 1;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Rifa>(sql);
    }

    public async Task<IEnumerable<Rifa>> ListarPorVendedorAsync(int vendedorId)
    {
        var sql = $@"
            SELECT {SelectFields}
            FROM rifas
            WHERE vendedorid = @VendedorId
            ORDER BY datacriacao DESC;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Rifa>(sql, new { VendedorId = vendedorId });
    }

    public async Task<int> InserirAsync(Rifa rifa)
    {
        const string sql = @"
            INSERT INTO rifas (
                titulo, subtitulo, descricao, fotoprincipalurl, fotodetalheurl, 
                valorcota, totalcotas, cotasvendidas, vendedorid, vendedornome, 
                datasorteio, status, datacriacao
            )
            VALUES (
                @Titulo, @Subtitulo, @Descricao, @FotoPrincipalUrl, @FotoDetalheUrl, 
                @ValorCota, @TotalCotas, @CotasVendidas, @VendedorId, @VendedorNome, 
                @DataSorteio, @Status, @DataCriacao
            )
            RETURNING id;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<int>(sql, rifa);
    }

    public async Task AtualizarAsync(Rifa rifa)
    {
        const string sql = @"
            UPDATE rifas
            SET 
                titulo = @Titulo,
                subtitulo = @Subtitulo,
                descricao = @Descricao,
                fotoprincipalurl = @FotoPrincipalUrl,
                fotodetalheurl = @FotoDetalheUrl,
                valorcota = @ValorCota,
                totalcotas = @TotalCotas,
                cotasvendidas = @CotasVendidas,
                datasorteio = @DataSorteio,
                status = @Status
            WHERE id = @Id;";

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, rifa);
    }

    public async Task<int> ContarPorVendedorNosUltimos30DiasAsync(int vendedorId)
    {
        const string sql = @"
            SELECT COUNT(1) 
            FROM rifas 
            WHERE vendedorid = @VendedorId AND datacriacao >= CURRENT_TIMESTAMP - INTERVAL '30 days';";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql, new { VendedorId = vendedorId });
    }
}
