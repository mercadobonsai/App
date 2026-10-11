using System;

namespace MercadoBonsai.Domain.Entities;

public class Notificacao
{
    public int Id { get; set; }
    public int? UsuarioId { get; set; } // null = Todos os Usuários (Broadcast Geral)
    public string? UsuarioNome { get; set; } // Nome do destinatário para auditoria e relatórios
    public string Titulo { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
    public string Tipo { get; set; } = "Info"; // Info, Sucesso, Alerta, Urgente
    public string? LinkUrl { get; set; }
    public bool Lida { get; set; } = false;
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
}
