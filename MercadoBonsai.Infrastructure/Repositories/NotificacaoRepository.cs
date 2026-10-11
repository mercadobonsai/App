using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using MercadoBonsai.Domain.Entities;
using MercadoBonsai.Domain.Interfaces;
using MercadoBonsai.Infrastructure.Data;

namespace MercadoBonsai.Infrastructure.Repositories;

public class NotificacaoRepository : INotificacaoRepository
{
    private readonly PostgresConnectionFactory _connectionFactory;

    public NotificacaoRepository(PostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> InserirAsync(Notificacao notificacao)
    {
        const string sql = @"
            INSERT INTO notificacoes (usuarioid, titulo, mensagem, tipo, linkurl, lida, datacriacao)
            VALUES (@UsuarioId, @Titulo, @Mensagem, @Tipo, @LinkUrl, FALSE, NOW())
            RETURNING id;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql, notificacao);
    }

    public async Task<IEnumerable<Notificacao>> ListarPorUsuarioAsync(int usuarioId, int limite = 10)
    {
        const string sql = @"
            SELECT 
                n.id AS Id,
                n.usuarioid AS UsuarioId,
                u.nome AS UsuarioNome,
                n.titulo AS Titulo,
                n.mensagem AS Mensagem,
                n.tipo AS Tipo,
                n.linkurl AS LinkUrl,
                CASE 
                    WHEN n.usuarioid IS NOT NULL THEN n.lida
                    ELSE (nl.usuarioid IS NOT NULL)
                END AS Lida,
                n.datacriacao AS DataCriacao
            FROM notificacoes n
            LEFT JOIN usuarios u ON u.id = n.usuarioid
            LEFT JOIN notificacao_leituras nl ON nl.notificacaoid = n.id AND nl.usuarioid = @UsuarioId
            WHERE (n.usuarioid = @UsuarioId OR n.usuarioid IS NULL)
            ORDER BY n.datacriacao DESC
            LIMIT @Limite;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Notificacao>(sql, new { UsuarioId = usuarioId, Limite = limite });
    }

    public async Task<int> ObterTotalNaoLidasAsync(int usuarioId)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM notificacoes n
            LEFT JOIN notificacao_leituras nl ON nl.notificacaoid = n.id AND nl.usuarioid = @UsuarioId
            WHERE (n.usuarioid = @UsuarioId OR n.usuarioid IS NULL)
              AND (
                  (n.usuarioid IS NOT NULL AND n.lida = false)
                  OR (n.usuarioid IS NULL AND nl.usuarioid IS NULL)
              );";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql, new { UsuarioId = usuarioId });
    }

    public async Task MarcarComoLidaAsync(int notificacaoId, int usuarioId)
    {
        const string sql = @"
            -- Marca lida caso seja notificação individual
            UPDATE notificacoes 
            SET lida = true 
            WHERE id = @NotificacaoId AND usuarioid = @UsuarioId;

            -- Registra leitura se for notificação global/broadcast
            INSERT INTO notificacao_leituras (notificacaoid, usuarioid, datalida)
            SELECT id, @UsuarioId, NOW()
            FROM notificacoes 
            WHERE id = @NotificacaoId AND usuarioid IS NULL
            ON CONFLICT (notificacaoid, usuarioid) DO NOTHING;";

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new { NotificacaoId = notificacaoId, UsuarioId = usuarioId });
    }

    public async Task MarcarTodasComoLidasAsync(int usuarioId)
    {
        const string sql = @"
            -- Marca lidas individuais do usuário
            UPDATE notificacoes 
            SET lida = true 
            WHERE usuarioid = @UsuarioId AND lida = false;

            -- Marca todas as globais ainda não lidas
            INSERT INTO notificacao_leituras (notificacaoid, usuarioid, datalida)
            SELECT n.id, @UsuarioId, NOW()
            FROM notificacoes n
            LEFT JOIN notificacao_leituras nl ON nl.notificacaoid = n.id AND nl.usuarioid = @UsuarioId
            WHERE n.usuarioid IS NULL AND nl.usuarioid IS NULL
            ON CONFLICT (notificacaoid, usuarioid) DO NOTHING;";

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new { UsuarioId = usuarioId });
    }

    public async Task<IEnumerable<Notificacao>> ListarTodasAdminAsync()
    {
        const string sql = @"
            SELECT 
                n.id AS Id,
                n.usuarioid AS UsuarioId,
                COALESCE(u.nome, 'Todos os Usuários (Geral)') AS UsuarioNome,
                n.titulo AS Titulo,
                n.mensagem AS Mensagem,
                n.tipo AS Tipo,
                n.linkurl AS LinkUrl,
                n.lida AS Lida,
                n.datacriacao AS DataCriacao
            FROM notificacoes n
            LEFT JOIN usuarios u ON u.id = n.usuarioid
            ORDER BY n.datacriacao DESC
            LIMIT 200;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Notificacao>(sql);
    }

    public async Task<bool> ExcluirAsync(int id)
    {
        const string sql = @"DELETE FROM notificacoes WHERE id = @Id;";
        using var connection = _connectionFactory.CreateConnection();
        var linhas = await connection.ExecuteAsync(sql, new { Id = id });
        return linhas > 0;
    }
}
