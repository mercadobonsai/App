using System;
using System.Collections.Generic;
using MercadoBonsai.Domain.Enums;

namespace MercadoBonsai.Domain.Entities;

public class Produto
{
    public Guid Id { get; set; }
    public Guid VendedorId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal Preco { get; set; }
    public string Especie { get; set; } = string.Empty;
    public int IdadeAnos { get; set; }
    public StatusProduto Status { get; set; }
    public ModalidadeEntrega TipoModalidade { get; set; }
    public DateTime DataCadastro { get; set; }
    
    public ICollection<FotoProduto> Fotos { get; set; } = new List<FotoProduto>();
}
