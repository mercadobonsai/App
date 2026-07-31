using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using MercadoBonsai.Domain.Entities;
using MercadoBonsai.Domain.Interfaces;
using MercadoBonsai.Infrastructure.Data;

namespace MercadoBonsai.Infrastructure.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly PostgresConnectionFactory _connectionFactory;

    public UsuarioRepository(PostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private const string SelectFields = @"
        id AS Id, 
        nome AS Nome, 
        email AS Email, 
        senhahash AS SenhaHash, 
        telefone AS Telefone, 
        perfil AS Perfil, 
        datacadastro AS DataCadastro,
        razaosocial AS RazaoSocial,
        cpfcnpj AS CpfCnpj,
        inscricaoestadual AS InscricaoEstadual,
        chavepix AS ChavePix,
        banco AS Banco,
        agencia AS Agencia,
        conta AS Conta,
        descricaoviveiro AS DescricaoViveiro,
        logotipourl AS LogotipoUrl,
        planoid AS PlanoId,
        reputacao AS Reputacao,
        isentocobranca AS IsentoCobranca,
        dataultimaalteracao AS DataUltimaAlteracao,
        usuarioalteracaoid AS UsuarioAlteracaoId,
        usuarioalteracaonome AS UsuarioAlteracaoNome";

    public async Task<Usuario?> ObterPorIdAsync(int id)
    {
        var sql = $@"
            SELECT {SelectFields}
            FROM usuarios 
            WHERE id = @Id;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Usuario>(sql, new { Id = id });
    }

    public async Task<Usuario?> ObterPorEmailAsync(string email)
    {
        var sql = $@"
            SELECT {SelectFields}
            FROM usuarios 
            WHERE LOWER(TRIM(email)) = LOWER(TRIM(@Email));";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Usuario>(sql, new { Email = email });
    }

    public async Task<int> InserirAsync(Usuario usuario)
    {
        const string sql = @"
            INSERT INTO usuarios (
                nome, email, senhahash, telefone, perfil, datacadastro,
                razaosocial, cpfcnpj, inscricaoestadual, chavepix, banco, agencia, conta, descricaoviveiro, logotipourl, planoid, reputacao, isentocobranca
            )
            VALUES (
                @Nome, LOWER(TRIM(@Email)), @SenhaHash, @Telefone, @Perfil, @DataCadastro,
                @RazaoSocial, @CpfCnpj, @InscricaoEstadual, @ChavePix, @Banco, @Agencia, @Conta, @DescricaoViveiro, @LogotipoUrl, @PlanoId, @Reputacao, @IsentoCobranca
            )
            RETURNING id;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<int>(sql, usuario);
    }

    public async Task AtualizarAsync(Usuario usuario)
    {
        const string sql = @"
            UPDATE usuarios
            SET 
                nome = @Nome,
                email = LOWER(TRIM(@Email)),
                telefone = @Telefone,
                perfil = @Perfil,
                razaosocial = @RazaoSocial,
                cpfcnpj = @CpfCnpj,
                inscricaoestadual = @InscricaoEstadual,
                chavepix = @ChavePix,
                banco = @Banco,
                agencia = @Agencia,
                conta = @Conta,
                descricaoviveiro = @DescricaoViveiro,
                logotipourl = @LogotipoUrl,
                planoid = @PlanoId,
                reputacao = @Reputacao,
                isentocobranca = @IsentoCobranca,
                dataultimaalteracao = @DataUltimaAlteracao,
                usuarioalteracaoid = @UsuarioAlteracaoId,
                usuarioalteracaonome = @UsuarioAlteracaoNome
            WHERE id = @Id;";

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, usuario);
    }

    public async Task<IEnumerable<Usuario>> ListarTodosAsync(string? busca, int? perfil)
    {
        var sql = $@"
            SELECT {SelectFields}
            FROM usuarios
            WHERE 1=1";

        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            sql += " AND (LOWER(nome) LIKE @Busca OR LOWER(email) LIKE @Busca OR LOWER(razaosocial) LIKE @Busca OR LOWER(cpfcnpj) LIKE @Busca)";
            parameters.Add("Busca", $"%{busca.Trim().ToLower()}%");
        }

        if (perfil.HasValue && perfil.Value > 0)
        {
            sql += " AND perfil = @Perfil";
            parameters.Add("Perfil", perfil.Value);
        }

        sql += " ORDER BY id DESC;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Usuario>(sql, parameters);
    }

    public async Task<IEnumerable<Usuario>> ListarViveirosEmDestaqueAsync()
    {
        var sql = $@"
            SELECT {SelectFields}
            FROM usuarios
            WHERE (planoid > 1 OR logotipourl IS NOT NULL OR descricaoviveiro IS NOT NULL)
            ORDER BY reputacao DESC, id DESC
            LIMIT 5;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Usuario>(sql);
    }
}
