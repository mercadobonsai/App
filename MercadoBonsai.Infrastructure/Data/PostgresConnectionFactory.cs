using System;
using System.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace MercadoBonsai.Infrastructure.Data;

public class PostgresConnectionFactory
{
    private readonly IConfiguration _configuration;

    public PostgresConnectionFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IDbConnection CreateConnection()
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection") ?? string.Empty;

        // Garante timeouts configurados para evitar estouro/congelamento em conexões remotas
        if (!string.IsNullOrWhiteSpace(connectionString) && !connectionString.Contains("Timeout=", StringComparison.OrdinalIgnoreCase))
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString)
            {
                Timeout = 15,
                CommandTimeout = 30
            };
            connectionString = builder.ConnectionString;
        }

        return new NpgsqlConnection(connectionString);
    }
}
