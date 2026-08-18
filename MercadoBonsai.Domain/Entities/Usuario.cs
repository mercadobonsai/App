using System;
using MercadoBonsai.Domain.Enums;

namespace MercadoBonsai.Domain.Entities;

public class Usuario
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public PerfilUsuario Perfil { get; set; } = PerfilUsuario.Comprador;
    public DateTime DataCadastro { get; set; }

    // Expansão: Dados Fiscais e Cadastrais
    public string? RazaoSocial { get; set; }
    public string? CpfCnpj { get; set; }
    public string? InscricaoEstadual { get; set; }

    // Expansão: Endereço Completo & Origem de Envio
    public string? Cep { get; set; }
    public string? Logradouro { get; set; }
    public string? Numero { get; set; }
    public string? Complemento { get; set; }
    public string? Bairro { get; set; }
    public string? Cidade { get; set; }
    public string? Estado { get; set; }

    // Expansão: Dados Financeiros para Repasses
    public string? ChavePix { get; set; }
    public string? Banco { get; set; }
    public string? Agencia { get; set; }
    public string? Conta { get; set; }

    // Expansão: Apresentação do Viveiro & Logotipo
    public string? DescricaoViveiro { get; set; }
    public string? LogotipoUrl { get; set; }

    // Expansão: Plano Pago & Reputação do Viveiro
    public int PlanoId { get; set; } = 1;
    public int Reputacao { get; set; } = 100;

    // Expansão Fase 8: Flag "Não cobrar" (Isento de Cobrança para parceiros selecionados)
    public bool IsentoCobranca { get; set; } = false;

    // Expansão: Integração Asaas
    public string? AsaasAccountId { get; set; }
    public string? AsaasCustomerId { get; set; }

    // Expansão: Auditoria de Alteração
    public DateTime? DataUltimaAlteracao { get; set; }
    public int? UsuarioAlteracaoId { get; set; }
    public string? UsuarioAlteracaoNome { get; set; }
}
