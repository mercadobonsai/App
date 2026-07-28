using System.Collections.Generic;
using MercadoBonsai.Domain.Entities;

namespace MercadoBonsai.Web.Models;

public class HomeEngajamentoViewModel
{
    public IEnumerable<Produto> ProdutosDestaque { get; set; } = new List<Produto>();
    public IEnumerable<Usuario> ViveirosEmDestaque { get; set; } = new List<Usuario>();
    public Leilao? LeilaoAtivo { get; set; }
    public Rifa? RifaAtiva { get; set; }
    public Patrocinio? PatrocinioDestaque { get; set; }
    public DicaCultivo? DicaCultivoSemana { get; set; }
}
