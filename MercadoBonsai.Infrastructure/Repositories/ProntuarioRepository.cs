using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using MercadoBonsai.Domain.Entities;
using MercadoBonsai.Domain.Interfaces;
using MercadoBonsai.Infrastructure.Data;

namespace MercadoBonsai.Infrastructure.Repositories;

public class ProntuarioRepository : IProntuarioRepository
{
    private readonly PostgresConnectionFactory _connectionFactory;

    public ProntuarioRepository(PostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private const string PlantaSelectFields = @"
        id AS Id,
        usuarioid AS UsuarioId,
        nomepopular AS NomePopular,
        nomecientifico AS NomeCientifico,
        especie AS Especie,
        altura AS Altura,
        largura AS Largura,
        comprimento AS Comprimento,
        peso AS Peso,
        descricaolivre AS DescricaoLivre,
        fotoprincipalurl AS FotoPrincipalUrl,
        datainicial AS DataInicial,
        dataultimamanutencao AS DataUltimaManutencao,
        dataproximamanutencao AS DataProximaManutencao,
        dataultimaadubacao AS DataUltimaAdubacao,
        dataproximaadubacao AS DataProximaAdubacao,
        datacriacao AS DataCriacao";

    private const string EventoSelectFields = @"
        id AS Id,
        plantaid AS PlantaId,
        titulo AS Titulo,
        descricao AS Descricao,
        dataevento AS DataEvento,
        fotourl AS FotoUrl,
        nomeadubo AS NomeAdubo,
        nomeremedio AS NomeRemedio,
        datacriacao AS DataCriacao";

    public async Task<int> InserirPlantaAsync(ProntuarioPlanta planta)
    {
        const string sql = @"
            INSERT INTO prontuarioplantas (
                usuarioid, nomepopular, nomecientifico, especie, altura, largura, comprimento, peso,
                descricaolivre, fotoprincipalurl, datainicial, dataultimamanutencao, dataproximamanutencao,
                dataultimaadubacao, dataproximaadubacao, datacriacao
            )
            VALUES (
                @UsuarioId, @NomePopular, @NomeCientifico, @Especie, @Altura, @Largura, @Comprimento, @Peso,
                @DescricaoLivre, @FotoPrincipalUrl, @DataInicial, @DataUltimaManutencao, @DataProximaManutencao,
                @DataUltimaAdubacao, @DataProximaAdubacao, @DataCriacao
            )
            RETURNING id;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<int>(sql, planta);
    }

    public async Task<ProntuarioPlanta?> ObterPlantaPorIdAsync(int id)
    {
        var sql = $@"
            SELECT {PlantaSelectFields}
            FROM prontuarioplantas
            WHERE id = @Id;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<ProntuarioPlanta>(sql, new { Id = id });
    }

    public async Task<IEnumerable<ProntuarioPlanta>> ListarPlantasPorUsuarioAsync(int usuarioId)
    {
        var sql = $@"
            SELECT {PlantaSelectFields}
            FROM prontuarioplantas
            WHERE usuarioid = @UsuarioId
            ORDER BY id DESC;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<ProntuarioPlanta>(sql, new { UsuarioId = usuarioId });
    }

    public async Task AtualizarPlantaAsync(ProntuarioPlanta planta)
    {
        const string sql = @"
            UPDATE prontuarioplantas
            SET
                nomepopular = @NomePopular,
                nomecientifico = @NomeCientifico,
                especie = @Especie,
                altura = @Altura,
                largura = @Largura,
                comprimento = @Comprimento,
                peso = @Peso,
                descricaolivre = @DescricaoLivre,
                fotoprincipalurl = @FotoPrincipalUrl,
                datainicial = @DataInicial,
                dataultimamanutencao = @DataUltimaManutencao,
                dataproximamanutencao = @DataProximaManutencao,
                dataultimaadubacao = @DataUltimaAdubacao,
                dataproximaadubacao = @DataProximaAdubacao
            WHERE id = @Id;";

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, planta);
    }

    public async Task DeletarPlantaAsync(int id)
    {
        const string sql = "DELETE FROM prontuarioplantas WHERE id = @Id;";
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task<int> InserirEventoAsync(ProntuarioEvento evento)
    {
        const string sql = @"
            INSERT INTO prontuarioeventos (
                plantaid, titulo, descricao, dataevento, fotourl, nomeadubo, nomeremedio, datacriacao
            )
            VALUES (
                @PlantaId, @Titulo, @Descricao, @DataEvento, @FotoUrl, @NomeAdubo, @NomeRemedio, @DataCriacao
            )
            RETURNING id;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<int>(sql, evento);
    }

    public async Task<IEnumerable<ProntuarioEvento>> ListarEventosPorPlantaAsync(int plantaId)
    {
        var sql = $@"
            SELECT {EventoSelectFields}
            FROM prontuarioeventos
            WHERE plantaid = @PlantaId
            ORDER BY dataevento DESC, id DESC;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<ProntuarioEvento>(sql, new { PlantaId = plantaId });
    }
}
