using System;
using System.Threading;
using System.Threading.Tasks;
using MercadoBonsai.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MercadoBonsai.Web.Services;

public class LeilaoEncerradoBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LeilaoEncerradoBackgroundService> _logger;

    public LeilaoEncerradoBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<LeilaoEncerradoBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Serviço de Encerramento Automático e Fallback de Leilões iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var leilaoService = scope.ServiceProvider.GetRequiredService<ILeilaoService>();
                await leilaoService.ProcessarLeiloesEncerradosAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no ciclo do serviço de encerramento de leilões.");
            }

            // Executa a verificação a cada 60 segundos
            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
        }
    }
}
