using System;
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

    public async Task<Usuario?> ObterPorEmailAsync(string email)
    {
        const string sql = "SELECT * FROM Usuarios WHERE Email = @Email;";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Usuario>(sql, new { Email = email });
    }

    public async Task<Guid> InserirAsync(Usuario usuario)
    {
        const string sql = @"
            INSERT INTO Usuarios (Id, Nome, Email, SenhaHash, Telefone, Perfil, DataCadastro)
            VALUES (@Id, @Nome, @Email, @SenhaHash, @Telefone, @Perfil, @DataCadastro)
            RETURNING Id;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<Guid>(sql, usuario);
    }
}
