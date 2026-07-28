using System.Collections.Generic;
using MercadoBonsai.Domain.Entities;

namespace MercadoBonsai.Web.Models;

public class HomeEngajamentoViewModel
{
    public IEnumerable<Produto> ProdutosDestaque { get; set; } = new List<Produto>();
    public IEnumerable<Usuario> ViveirosEmDestaque { get; set; } = new List<Usuario>();
    public Leilao? LeilaoAtivo { get; set; }
    public IEnumerable<Leilao>? LeiloesAtivos { get; set; } = new List<Leilao>();
    public Rifa? RifaAtiva { get; set; }
    public Patrocinio? PatrocinioDestaque { get; set; }
    public DicaCultivo? DicaCultivoSemana { get; set; }

    // Listas de Propagandas Ativas por Modalidade Visual
    public IEnumerable<Propaganda> PropagandasEconomico { get; set; } = new List<Propaganda>();
    public IEnumerable<Propaganda> PropagandasBasico { get; set; } = new List<Propaganda>();
    public IEnumerable<Propaganda> PropagandasIntermediario { get; set; } = new List<Propaganda>();
    public IEnumerable<Propaganda> PropagandasAvancado { get; set; } = new List<Propaganda>();
}
