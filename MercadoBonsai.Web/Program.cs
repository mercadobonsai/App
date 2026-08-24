using Microsoft.AspNetCore.Authentication.Cookies;
using Dapper;
using MercadoBonsai.Domain.Interfaces;
using MercadoBonsai.Infrastructure.Data;
using MercadoBonsai.Infrastructure.Repositories;
using MercadoBonsai.Web.Services;

// Registra TypeHandler para Dapper + Npgsql mapear UUID -> Guid e DateOnly -> DateTime corretamente
SqlMapper.AddTypeHandler(GuidTypeHandler.Instance);
SqlMapper.AddTypeHandler(new NullableDateTimeTypeHandler());
SqlMapper.AddTypeHandler(new DateTimeTypeHandler());

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Infraestrutura de Dados e Serviços
builder.Services.AddSingleton<PostgresConnectionFactory>();
builder.Services.AddSingleton<VendedorTokenService>();
builder.Services.AddScoped<IProdutoRepository, ProdutoRepository>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<ILeilaoRepository, LeilaoRepository>();
builder.Services.AddScoped<IRifaRepository, RifaRepository>();
builder.Services.AddScoped<IPatrocinioRepository, PatrocinioRepository>();
builder.Services.AddScoped<IDicaCultivoRepository, DicaCultivoRepository>();
builder.Services.AddScoped<IPlanoRepository, PlanoRepository>();
builder.Services.AddScoped<IPropagandaRepository, PropagandaRepository>();
builder.Services.AddScoped<IProntuarioRepository, ProntuarioRepository>();
builder.Services.AddScoped<IPedidoRepository, PedidoRepository>();
builder.Services.AddHttpClient<IMelhorEnvioService, MelhorEnvioService>();
builder.Services.AddHttpClient<IEvendasWebhookService, EvendasWebhookService>();
builder.Services.AddHttpClient<IAsaasService, AsaasService>();
builder.Services.AddScoped<ILeilaoService, LeilaoService>();
builder.Services.AddHostedService<LeilaoEncerradoBackgroundService>();

// Autenticação por Cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Conta/Login";
        options.LogoutPath = "/Conta/Logout";
        options.AccessDeniedPath = "/Conta/Login";
        options.Cookie.Name = "MercadoBonsai.Auth";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

var app = builder.Build();

// Executa migrações de schema automáticas no PostgreSQL
using (var scope = app.Services.CreateScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<PostgresConnectionFactory>();
    try
    {
        using var conn = factory.CreateConnection();
        conn.Execute(@"
            ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS asaas_account_id VARCHAR(100) NULL;
            ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS asaas_customer_id VARCHAR(100) NULL;
            ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS asaas_subscription_id VARCHAR(100) NULL;
            ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS percentualretencaopersonalizado NUMERIC(5,2) NULL;
            ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS datanascimento DATE NULL;
            ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS rendafaturamento NUMERIC(15,2) NULL;
            ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS webhook_url VARCHAR(500) NULL;
            ALTER TABLE pedidos ADD COLUMN IF NOT EXISTS leilaoid INT NULL;
            ALTER TABLE pedidos ADD COLUMN IF NOT EXISTS posicao_vencedor_leilao INT NULL DEFAULT 1;

            UPDATE produtos SET status = 1 WHERE quantidadeestoque > 0 AND status = 2;
            UPDATE produtos SET status = 2 WHERE quantidadeestoque = 0 AND status = 1;

            INSERT INTO planos (id, nome, valor, preco, percentualcomissao, limitelifas30dias, limiteleiloes30dias, limiteanuncios, limitefotos, destaqueshome)
            OVERRIDING SYSTEM VALUE
            VALUES (4, 'Diamante', 99.90, 99.90, 4.00, 15, 15, 100, 12, TRUE)
            ON CONFLICT (id) DO UPDATE 
            SET nome = EXCLUDED.nome, valor = EXCLUDED.valor, preco = EXCLUDED.preco, percentualcomissao = EXCLUDED.percentualcomissao,
                limitelifas30dias = EXCLUDED.limitelifas30dias, limiteleiloes30dias = EXCLUDED.limiteleiloes30dias, 
                limiteanuncios = EXCLUDED.limiteanuncios, limitefotos = EXCLUDED.limitefotos, destaqueshome = EXCLUDED.destaqueshome;
        ");
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Erro ao executar verificação/migração automática de schema no PostgreSQL.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

public class NullableDateTimeTypeHandler : SqlMapper.TypeHandler<DateTime?>
{
    public override void SetValue(System.Data.IDbDataParameter parameter, DateTime? value)
    {
        parameter.Value = value.HasValue ? (object)value.Value : DBNull.Value;
    }

    public override DateTime? Parse(object value)
    {
        if (value == null || value is DBNull) return null;
        if (value is DateTime dt) return dt;
        if (value is DateOnly d) return d.ToDateTime(TimeOnly.MinValue);
        if (DateTime.TryParse(value.ToString(), out var parsed)) return parsed;
        return null;
    }
}

public class DateTimeTypeHandler : SqlMapper.TypeHandler<DateTime>
{
    public override void SetValue(System.Data.IDbDataParameter parameter, DateTime value)
    {
        parameter.Value = value;
    }

    public override DateTime Parse(object value)
    {
        if (value is DateTime dt) return dt;
        if (value is DateOnly d) return d.ToDateTime(TimeOnly.MinValue);
        if (DateTime.TryParse(value.ToString(), out var parsed)) return parsed;
        return DateTime.MinValue;
    }
}
