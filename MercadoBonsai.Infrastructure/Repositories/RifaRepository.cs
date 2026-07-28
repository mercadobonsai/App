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

    public async Task<Rifa?> ObterRifaAtivaRecenteAsync()
    {
        const string sql = @"
            SELECT 
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
                datacriacao AS DataCriacao
            FROM rifas
            WHERE status = 1
            ORDER BY datacriacao DESC
            LIMIT 1;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Rifa>(sql);
    }

    public async Task<Rifa?> ObterPorIdAsync(int id)
    {
        const string sql = @"
            SELECT 
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
                datacriacao AS DataCriacao
            FROM rifas
            WHERE id = @Id;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Rifa>(sql, new { Id = id });
    }
}
