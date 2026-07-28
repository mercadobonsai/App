using Microsoft.AspNetCore.Authentication.Cookies;
using Dapper;
using MercadoBonsai.Domain.Interfaces;
using MercadoBonsai.Infrastructure.Data;
using MercadoBonsai.Infrastructure.Repositories;

// Registra TypeHandler para Dapper + Npgsql mapear UUID -> Guid corretamente
SqlMapper.AddTypeHandler(GuidTypeHandler.Instance);

var options = new WebApplicationOptions
{
    Args = args,
    ContentRootPath = Directory.GetCurrentDirectory()
};

var builder = WebApplication.CreateBuilder(options);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Infraestrutura de Dados
builder.Services.AddSingleton<PostgresConnectionFactory>();
builder.Services.AddScoped<IProdutoRepository, ProdutoRepository>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<ILeilaoRepository, LeilaoRepository>();
builder.Services.AddScoped<IRifaRepository, RifaRepository>();
builder.Services.AddScoped<IPatrocinioRepository, PatrocinioRepository>();
builder.Services.AddScoped<IDicaCultivoRepository, DicaCultivoRepository>();
builder.Services.AddScoped<IPlanoRepository, PlanoRepository>();
builder.Services.AddScoped<IPropagandaRepository, PropagandaRepository>();

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
