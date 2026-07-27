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
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        return new NpgsqlConnection(connectionString);
    }
}
