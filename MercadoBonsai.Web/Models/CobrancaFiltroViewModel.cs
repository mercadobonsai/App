using System;
using System.Collections.Generic;

namespace MercadoBonsai.Web.Models;

public class CobrancaFiltroViewModel
{
    public string? Status { get; set; }
    public string? BillingType { get; set; }
    public DateTime? DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
    public string? ExternalReference { get; set; }
    public string? Customer { get; set; }

    // Paginação
    public int Offset { get; set; } = 0;
    public int Limit { get; set; } = 10;
    public int TotalCount { get; set; }
    public bool HasMore { get; set; }

    // Lista de Itens Retornados
    public List<AsaasCobrancaItemDto> Cobrancas { get; set; } = new();

    // Métricas para os Cards de Topo
    public decimal TotalCobrado { get; set; }
    public decimal TotalRecebido { get; set; }
    public decimal TotalPendente { get; set; }
}

public class AsaasCobrancaItemDto
{
    public string Id { get; set; } = string.Empty;
    public DateTime DateCreated { get; set; }
    public string? Customer { get; set; }
    public decimal Value { get; set; }
    public decimal? NetValue { get; set; }
    public string BillingType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? InvoiceUrl { get; set; }
    public string? ExternalReference { get; set; }
    public string? Description { get; set; }
}

public class AsaasCobrancasPaginadasResult
{
    public bool Sucesso { get; set; }
    public string? MensagemErro { get; set; }
    public int TotalCount { get; set; }
    public int Offset { get; set; }
    public int Limit { get; set; }
    public bool HasMore { get; set; }
    public List<AsaasCobrancaItemDto> Data { get; set; } = new();
}
