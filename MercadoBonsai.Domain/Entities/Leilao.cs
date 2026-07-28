using System;
using System.Collections.Generic;
using MercadoBonsai.Domain.Enums;

namespace MercadoBonsai.Domain.Entities;

public class Leilao
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Subtitulo { get; set; }
    public string? Descricao { get; set; }
    public string FotoPrincipalUrl { get; set; } = string.Empty;
    public string? FotoDetalheUrl { get; set; }
    public string? Badge { get; set; }
    public decimal LanceAtual { get; set; }
    public decimal ProximoLanceMinimo { get; set; }
    public decimal IncrementoMinimo { get; set; } = 50.00m;
    public int? VendedorId { get; set; }
    public string? VendedorNome { get; set; }
    public DateTime DataFinalizacao { get; set; }
    public StatusLeilao Status { get; set; } = StatusLeilao.Criado; // 1=Criado, 2=Iniciado, 3=Suspenso, 4=Finalizado
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    public List<LanceLeilao> Lances { get; set; } = new();

    public bool TemLances => Lances != null && Lances.Count > 0;
    public bool PodeEditarDadosGerais => Status == StatusLeilao.Criado || (Status == StatusLeilao.Iniciado && !TemLances);
}
