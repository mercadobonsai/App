using System;
using System.Data;
using System.Net;
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

        // Se o Host for 'db', 'localhost' ou qualquer nome não resolvível no DNS do container, usa fallback automático para o IP do Hetzner
        if (!string.IsNullOrWhiteSpace(builder.Host))
        {
            try
            {
                Dns.GetHostAddresses(builder.Host);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Host '{Host}' não pôde ser resolvido via DNS no container. Utilizando fallback para Hetzner IP (167.233.55.149).", builder.Host);
                builder.Host = "167.233.55.149";
            }
        }
        else
        {
            builder.Host = "167.233.55.149";
        }

        builder.Timeout = 15;
        builder.CommandTimeout = 30;

        _logger.LogInformation("PostgresConnectionFactory conectando em Host={Host}, Database={Database}", builder.Host, builder.Database);

        return new NpgsqlConnection(builder.ConnectionString);
    }
}
