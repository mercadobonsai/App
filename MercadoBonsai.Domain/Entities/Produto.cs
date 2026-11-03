using System;
using System.Collections.Generic;
using MercadoBonsai.Domain.Enums;

namespace MercadoBonsai.Domain.Entities;

public class Produto
{
    public int Id { get; set; }
    public int VendedorId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal Preco { get; set; }
    public decimal? PrecoCusto { get; set; }
    public int QuantidadeEstoque { get; set; }
    public string ImagemUrl { get; set; } = string.Empty;
    public string? ImagensAdicionais { get; set; }
    public StatusProduto Status { get; set; } = StatusProduto.Disponivel;

    public List<string> TodasImagens
    {
        get
        {
            var lista = new List<string>();
            if (!string.IsNullOrWhiteSpace(ImagemUrl))
            {
                lista.Add(ImagemUrl.Trim());
            }
            if (!string.IsNullOrWhiteSpace(ImagensAdicionais))
            {
                foreach (var img in ImagensAdicionais.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (!lista.Contains(img))
                    {
                        lista.Add(img);
                    }
                }
            }
            return lista;
        }
    }

    // Dimensões Físicas & Peso
    public decimal Altura { get; set; }
    public decimal Largura { get; set; }
    public decimal Comprimento { get; set; }
    public decimal Peso { get; set; }

    // Envio e Categorização
    public string FormaEnvio { get; set; } = "A combinar"; // Frete incluso, Frete por conta comprador, A combinar
    public string Categoria { get; set; } = "Bonsai"; // Pre-bonsai, Bonsai, Insumo, Ferramenta, Vaso

    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
}
