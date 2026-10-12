using System.Collections.Generic;
using System.Linq;
using MercadoBonsai.Domain.Entities;

namespace MercadoBonsai.Web.Models;

public class LeilaoIndexViewModel
{
    public IEnumerable<Leilao> LeiloesAtivos { get; set; } = new List<Leilao>();
    public IEnumerable<Leilao> LeiloesEncerrados { get; set; } = new List<Leilao>();
    public string AbaAtiva { get; set; } = "ativos"; // "ativos" ou "encerrados"
    public int PaginaAtual { get; set; } = 1;
    public int TotalPaginas { get; set; } = 1;
    public int TotalAtivos => LeiloesAtivos?.Count() ?? 0;
    public int TotalEncerrados => LeiloesEncerrados?.Count() ?? 0;
}
