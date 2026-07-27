using System;
using MercadoBonsai.Domain.Enums;

namespace MercadoBonsai.Domain.Entities;

public class Usuario
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public PerfilUsuario Perfil { get; set; } = PerfilUsuario.Comprador;
    public DateTime DataCadastro { get; set; }
}
