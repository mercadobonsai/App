using System;
using System.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace MercadoBonsai.Infrastructure.Data;

public class PostgresConnectionFactory
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PostgresConnectionFactory> _logger;

    public PostgresConnectionFactory(IConfiguration configuration, ILogger<PostgresConnectionFactory> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public IDbConnection CreateConnection()
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection") ?? string.Empty;

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = "Host=167.233.55.149;Port=5432;Database=mercado_bonsai;Username=postgres;Password=sua_senha_segura_aqui";
        }

        var builder = new NpgsqlConnectionStringBuilder(connectionString);

        // Se o Host for 'db', 'localhost' ou '127.0.0.1', direciona diretamente para o IP do PostgreSQL no Hetzner
        if (string.IsNullOrWhiteSpace(builder.Host) || 
            string.Equals(builder.Host, "db", StringComparison.OrdinalIgnoreCase) || 
            string.Equals(builder.Host, "localhost", StringComparison.OrdinalIgnoreCase) || 
            string.Equals(builder.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase))
        {
            builder.Host = "167.233.55.149";
        }

        builder.Timeout = 15;
        builder.CommandTimeout = 30;

        _logger.LogInformation("PostgresConnectionFactory conectando em Host={Host}, Database={Database}", builder.Host, builder.Database);

        return new NpgsqlConnection(builder.ConnectionString);
    }
}
