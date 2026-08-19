using Microsoft.AspNetCore.Authentication.Cookies;
using Dapper;
using MercadoBonsai.Domain.Interfaces;
using MercadoBonsai.Infrastructure.Data;
using MercadoBonsai.Infrastructure.Repositories;
using MercadoBonsai.Web.Services;

// Registra TypeHandler para Dapper + Npgsql mapear UUID -> Guid corretamente
SqlMapper.AddTypeHandler(GuidTypeHandler.Instance);

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
