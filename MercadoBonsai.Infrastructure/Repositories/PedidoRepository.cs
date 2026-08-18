using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using MercadoBonsai.Domain.Entities;
using MercadoBonsai.Domain.Interfaces;
using MercadoBonsai.Infrastructure.Data;

namespace MercadoBonsai.Infrastructure.Repositories;

public class PedidoRepository : IPedidoRepository
{
    private readonly PostgresConnectionFactory _connectionFactory;

    public PedidoRepository(PostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private const string SelectBaseSql = @"
        SELECT 
            p.id,
            p.numero,
            p.comprador_id AS CompradorId,
            p.vendedor_id AS VendedorId,
            p.produto_id AS ProdutoId,
            p.statuspedido AS StatusPedido,
            p.tipopagamento AS TipoPagamento,
            p.datapedido AS DataPedido,
            p.datapagamento AS DataPagamento,
            p.valorpedido AS ValorPedido,
            p.valor_frete AS ValorFrete,
            p.valor_seguro AS ValorSeguro,
            p.valor_total AS ValorTotal,
            p.urlcheckout AS UrlCheckout,
            p.observacao AS Observacao,
            p.codigorastreio AS CodigoRastreio,
            p.urlrastreio AS UrlRastreio,
            p.compradornome AS CompradorNome,
            p.compradoremail AS CompradorEmail,
            p.compradortelefone AS CompradorTelefone,
            p.compradorendereco AS CompradorEndereco,
            p.compradoraniversario AS CompradorAniversario,
            p.urlavaliacao AS UrlAvaliacao,
            p.asaas_payment_id AS AsaasPaymentId,
            prod.nome AS ProdutoNome,
            prod.imagemurl AS ProdutoImagemUrl,
            vend.nome AS VendedorNome,
            vend.telefone AS VendedorTelefone
        FROM pedidos p
        INNER JOIN produtos prod ON prod.id = p.produto_id
        INNER JOIN usuarios vend ON vend.id = p.vendedor_id
    ";

    public async Task<Pedido?> ObterPorIdAsync(int id)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = $"{SelectBaseSql} WHERE p.id = @id";
        return await conn.QueryFirstOrDefaultAsync<Pedido>(sql, new { id });
    }

    public async Task<Pedido?> ObterPorNumeroAsync(int numero)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = $"{SelectBaseSql} WHERE p.numero = @numero";
        return await conn.QueryFirstOrDefaultAsync<Pedido>(sql, new { numero });
    }

    public async Task<Pedido?> ObterPorAsaasPaymentIdAsync(string asaasPaymentId)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = $"{SelectBaseSql} WHERE p.asaas_payment_id = @asaasPaymentId";
        return await conn.QueryFirstOrDefaultAsync<Pedido>(sql, new { asaasPaymentId });
    }

    public async Task<IEnumerable<Pedido>> ObterPorCompradorAsync(int compradorId)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = $"{SelectBaseSql} WHERE p.comprador_id = @compradorId ORDER BY p.id DESC";
        return await conn.QueryAsync<Pedido>(sql, new { compradorId });
    }

    public async Task<IEnumerable<Pedido>> ObterPorVendedorAsync(int vendedorId)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = $"{SelectBaseSql} WHERE p.vendedor_id = @vendedorId ORDER BY p.id DESC";
        return await conn.QueryAsync<Pedido>(sql, new { vendedorId });
    }

    public async Task<int> CriarAsync(Pedido pedido)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"
            INSERT INTO pedidos (
                numero, comprador_id, vendedor_id, produto_id, statuspedido, tipopagamento,
                datapedido, datapagamento, valorpedido, valor_frete, valor_seguro, valor_total,
                urlcheckout, observacao, codigorastreio, urlrastreio, compradornome, compradoremail,
                compradortelefone, compradorendereco, compradoraniversario, urlavaliacao, asaas_payment_id
            ) VALUES (
                @Numero, @CompradorId, @VendedorId, @ProdutoId, @StatusPedido, @TipoPagamento,
                @DataPedido, @DataPagamento, @ValorPedido, @ValorFrete, @ValorSeguro, @ValorTotal,
                @UrlCheckout, @Observacao, @CodigoRastreio, @UrlRastreio, @CompradorNome, @CompradorEmail,
                @CompradorTelefone, @CompradorEndereco, @CompradorAniversario, @UrlAvaliacao, @AsaasPaymentId
            )
            RETURNING id;
        ";
        return await conn.ExecuteScalarAsync<int>(sql, pedido);
    }

    public async Task<bool> AtualizarAsync(Pedido pedido)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"
            UPDATE pedidos SET
                statuspedido = @StatusPedido,
                tipopagamento = @TipoPagamento,
                datapagamento = @DataPagamento,
                valor_frete = @ValorFrete,
                valor_seguro = @ValorSeguro,
                valor_total = @ValorTotal,
                urlcheckout = @UrlCheckout,
                observacao = @Observacao,
                codigorastreio = @CodigoRastreio,
                urlrastreio = @UrlRastreio,
                compradornome = @CompradorNome,
                compradoremail = @CompradorEmail,
                compradortelefone = @CompradorTelefone,
                compradorendereco = @CompradorEndereco,
                compradoraniversario = @CompradorAniversario,
                urlavaliacao = @UrlAvaliacao,
                asaas_payment_id = @AsaasPaymentId
            WHERE id = @Id;
        ";
        var rows = await conn.ExecuteAsync(sql, pedido);
        return rows > 0;
    }

    public async Task<bool> AtualizarStatusAsync(int id, string novoStatus, string? observacao = null)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"
            UPDATE pedidos SET
                statuspedido = @novoStatus,
                observacao = COALESCE(@observacao, observacao),
                datapagamento = CASE WHEN @novoStatus = 'Pago' THEN NOW() ELSE datapagamento END
            WHERE id = @id;
        ";
        var rows = await conn.ExecuteAsync(sql, new { id, novoStatus, observacao });
        return rows > 0;
    }

    public async Task<bool> AtualizarFreteECheckoutAsync(int id, decimal valorFrete, decimal valorTotal, string? urlCheckout, string? asaasPaymentId)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"
            UPDATE pedidos SET
                valor_frete = @valorFrete,
                valor_total = @valorTotal,
                urlcheckout = COALESCE(@urlCheckout, urlcheckout),
                asaas_payment_id = COALESCE(@asaasPaymentId, asaas_payment_id),
                statuspedido = 'Aguardando Pagamento'
            WHERE id = @id;
        ";
        var rows = await conn.ExecuteAsync(sql, new { id, valorFrete, valorTotal, urlCheckout, asaasPaymentId });
        return rows > 0;
    }

    public async Task<bool> AtualizarRastreioAsync(int id, string codigoRastreio, string? urlRastreio)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"
            UPDATE pedidos SET
                codigorastreio = @codigoRastreio,
                urlrastreio = COALESCE(@urlRastreio, urlrastreio),
                statuspedido = 'Em Transito'
            WHERE id = @id;
        ";
        var rows = await conn.ExecuteAsync(sql, new { id, codigoRastreio, urlRastreio });
        return rows > 0;
    }

    public async Task<int> ObterProximoNumeroPedidoAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = "SELECT COALESCE(MAX(numero), 1000) + 1 FROM pedidos;";
        return await conn.ExecuteScalarAsync<int>(sql);
    }
}
