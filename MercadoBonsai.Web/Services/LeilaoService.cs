using System;
using System.Linq;
using System.Threading.Tasks;
using MercadoBonsai.Domain.Entities;
using MercadoBonsai.Domain.Enums;
using MercadoBonsai.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MercadoBonsai.Web.Services;

public class LeilaoService : ILeilaoService
{
    private readonly ILeilaoRepository _leilaoRepository;
    private readonly IPedidoRepository _pedidoRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPlanoRepository _planoRepository;
    private readonly IAsaasService _asaasService;
    private readonly IEvendasWebhookService _evendasWebhookService;
    private readonly ILogger<LeilaoService> _logger;

    public LeilaoService(
        ILeilaoRepository leilaoRepository,
        IPedidoRepository pedidoRepository,
        IUsuarioRepository usuarioRepository,
        IPlanoRepository planoRepository,
        IAsaasService asaasService,
        IEvendasWebhookService evendasWebhookService,
        ILogger<LeilaoService> logger)
    {
        _leilaoRepository = leilaoRepository;
        _pedidoRepository = pedidoRepository;
        _usuarioRepository = usuarioRepository;
        _planoRepository = planoRepository;
        _asaasService = asaasService;
        _evendasWebhookService = evendasWebhookService;
        _logger = logger;
    }

    public async Task ProcessarLeiloesEncerradosAsync()
    {
        try
        {
            var leiloesAtivos = await _leilaoRepository.ListarAtivosAsync();
            var agora = DateTime.UtcNow;
            var encerrar = leiloesAtivos.Where(l => l.Status == StatusLeilao.Iniciado && l.DataFinalizacao <= agora).ToList();

            foreach (var leilao in encerrar)
            {
                _logger.LogInformation("Encerrando leilão #{LeilaoId} '{Titulo}' (Término: {DataFinalizacao})...", leilao.Id, leilao.Titulo, leilao.DataFinalizacao);
                
                // 1. Atualizar status do leilão para Finalizado (4)
                leilao.Status = StatusLeilao.Finalizado;
                await _leilaoRepository.AtualizarAsync(leilao);

                // 2. Obter leilão completo com todos os lances
                var leilaoCompleto = await _leilaoRepository.ObterPorIdAsync(leilao.Id);
                if (leilaoCompleto == null || leilaoCompleto.Lances == null || !leilaoCompleto.Lances.Any())
                {
                    _logger.LogWarning("Leilão #{LeilaoId} finalizado sem nenhum lance válido.", leilao.Id);
                    continue;
                }

                // 3. Ordenar lances por Valor DESC e DataLance ASC (1º Colocado)
                var lancesOrdenados = leilaoCompleto.Lances
                    .OrderByDescending(l => l.Valor)
                    .ThenBy(l => l.DataLance)
                    .ToList();

                var vencedor = lancesOrdenados.First();
                _logger.LogInformation("Leilão #{LeilaoId}: 1º Colocado {UsuarioNome} (ID #{UsuarioId}) com lance de R$ {Valor:N2}", 
                    leilao.Id, vencedor.UsuarioNome, vencedor.UsuarioId, vencedor.Valor);

                // 4. Gerar Pedido Oficial para o 1º Colocado
                await GerarPedidoEIniciarCobrancaAsync(leilaoCompleto, vencedor, posicao: 1);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar leilões encerrados em segundo plano.");
        }
    }

    public async Task<bool> ChamarProximoColocadoAsync(int leilaoId, int posicaoAtual)
    {
        var leilao = await _leilaoRepository.ObterPorIdAsync(leilaoId);
        if (leilao == null || leilao.Lances == null || !leilao.Lances.Any())
        {
            _logger.LogWarning("Fallback Leilão #{LeilaoId}: Leilão não encontrado ou sem lances registrados.", leilaoId);
            return false;
        }

        // Ordenação estrita decrescente de lances válidos
        var lancesOrdenados = leilao.Lances
            .OrderByDescending(l => l.Valor)
            .ThenBy(l => l.DataLance)
            .ToList();

        // Posição zero-indexed para o próximo colocado (ex: se posicaoAtual == 1 (1º colocado), o próximo é o índice 1 (2º colocado))
        int proximoIndice = posicaoAtual;
        if (proximoIndice >= lancesOrdenados.Count)
        {
            _logger.LogWarning("Fallback Leilão #{LeilaoId}: Fila de lances esgotada. Não há {ProximaPosicao}º colocado disponível.", leilaoId, posicaoAtual + 1);
            return false;
        }

        var proximoVencedor = lancesOrdenados[proximoIndice];
        int novaPosicao = proximoIndice + 1;

        _logger.LogInformation("Fallback Leilão #{LeilaoId}: Ativando {Posicao}º Colocado {UsuarioNome} (ID #{UsuarioId}) com lance de R$ {Valor:N2}", 
            leilaoId, novaPosicao, proximoVencedor.UsuarioNome, proximoVencedor.UsuarioId, proximoVencedor.Valor);

        await GerarPedidoEIniciarCobrancaAsync(leilao, proximoVencedor, novaPosicao);
        return true;
    }

    private async Task GerarPedidoEIniciarCobrancaAsync(Leilao leilao, LanceLeilao arrematante, int posicao)
    {
        int compradorId = arrematante.UsuarioId ?? 0;
        int vendedorId = leilao.VendedorId ?? 0;

        var comprador = await _usuarioRepository.ObterPorIdAsync(compradorId);
        var vendedor = await _usuarioRepository.ObterPorIdAsync(vendedorId);

        int proximoNumero = await _pedidoRepository.ObterProximoNumeroPedidoAsync();

        string descricaoPosicao = posicao == 1 ? "1º Colocado (Vencedor)" : $"{posicao}º Colocado (Chamada Sequencial)";

        var novoPedido = new Pedido
        {
            Numero = proximoNumero,
            CompradorId = compradorId,
            VendedorId = vendedorId,
            ProdutoId = 0, // Leilão / Produto Virtual de Leilão
            StatusPedido = StatusPedido.AguardandoPagamento,
            TipoPagamento = "PIX / Asaas",
            DataPedido = DateTime.Now,
            ValorPedido = arrematante.Valor,
            ValorFrete = 0.00m,
            ValorSeguro = 0.00m,
            ValorTotal = arrematante.Valor,
            Observacao = $"Pedido gerado via Leilão #{leilao.Id} '{leilao.Titulo}' - Arrematante: {descricaoPosicao}",
            CompradorNome = comprador?.Nome ?? arrematante.UsuarioNome,
            CompradorEmail = comprador?.Email ?? string.Empty,
            CompradorTelefone = comprador?.Telefone ?? string.Empty,
            CompradorEndereco = comprador != null 
                ? $"{comprador.Logradouro}, {comprador.Numero} - {comprador.Bairro}, {comprador.Cidade}/{comprador.Estado} (CEP: {comprador.Cep})" 
                : string.Empty,
            CompradorAniversario = comprador?.DataNascimento,
            LeilaoId = leilao.Id,
            PosicaoVencedorLeilao = posicao
        };

        int pedidoId = await _pedidoRepository.CriarAsync(novoPedido);
        novoPedido.Id = pedidoId;

        // Tenta gerar a cobrança no Asaas com split de comissão
        if (vendedor != null)
        {
            var planoVendedor = await _planoRepository.ObterPorIdAsync(vendedor.PlanoId);
            decimal percentualComissao = vendedor.PercentualRetencaoPersonalizado ?? planoVendedor?.PercentualComissao ?? 10.00m;

            try
            {
                var cobrancaResult = await _asaasService.CriarCobrancaAsync(novoPedido, vendedor, percentualComissao);
                if (cobrancaResult.Sucesso)
                {
                    novoPedido.UrlCheckout = cobrancaResult.UrlCheckout;
                    novoPedido.AsaasPaymentId = cobrancaResult.AsaasPaymentId;
                    await _pedidoRepository.AtualizarAsync(novoPedido);
                    _logger.LogInformation("Cobrança Asaas gerada com sucesso para Pedido #{Numero} do Leilão #{LeilaoId}: {UrlCheckout}", 
                        novoPedido.Numero, leilao.Id, cobrancaResult.UrlCheckout);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar cobrança Asaas para Pedido de Leilão #{Numero}.", novoPedido.Numero);
            }
        }

        // Dispara o Webhook do e-vendas
        await _evendasWebhookService.NotificarMudancaStatusAsync(novoPedido);
    }
}
