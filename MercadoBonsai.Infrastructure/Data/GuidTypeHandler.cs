using System;
using System.Data;
using Dapper;

namespace MercadoBonsai.Infrastructure.Data;

/// <summary>
/// TypeHandler necessário para que o Dapper converta corretamente
/// colunas UUID do PostgreSQL (via Npgsql) para System.Guid.
/// </summary>
public class GuidTypeHandler : SqlMapper.TypeHandler<Guid>
{
    public static readonly GuidTypeHandler Instance = new();

    public override void SetValue(IDbDataParameter parameter, Guid value)
    {
        parameter.Value = value;
        parameter.DbType = DbType.Guid;
    }

    public override Guid Parse(object value)
    {
        return value switch
        {
            Guid g => g,
            string s => Guid.Parse(s),
            _ => Guid.Parse(value.ToString()!)
        };
    }
}
