CREATE DATABASE MercadoBonsai;
GO

USE MercadoBonsai;
GO

CREATE TABLE Planos (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nome VARCHAR(100) NOT NULL,
    Valor DECIMAL(18,2) NOT NULL,
    LimiteAnuncios INT NOT NULL
);
GO

CREATE TABLE Usuarios (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Nome VARCHAR(150) NOT NULL,
    Email VARCHAR(150) NOT NULL UNIQUE,
    SenhaHash VARCHAR(255) NOT NULL,
    Telefone VARCHAR(20) NULL,
    DataCadastro DATETIME NOT NULL DEFAULT GETDATE()
);
GO

CREATE TABLE Produtos (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    VendedorId UNIQUEIDENTIFIER NOT NULL,
    Nome VARCHAR(150) NOT NULL,
    Descricao TEXT NULL,
    Preco DECIMAL(18,2) NOT NULL,
    Especie VARCHAR(100) NULL,
    IdadeAnos INT NULL,
    Status INT NOT NULL, -- Enum StatusProduto
    TipoModalidade INT NOT NULL, -- Enum ModalidadeEntrega
    DataCadastro DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Produtos_Usuarios FOREIGN KEY (VendedorId) REFERENCES Usuarios(Id)
);
GO

CREATE TABLE FotosProduto (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    ProdutoId UNIQUEIDENTIFIER NOT NULL,
    Url VARCHAR(500) NOT NULL,
    IsPrincipal BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_FotosProduto_Produtos FOREIGN KEY (ProdutoId) REFERENCES Produtos(Id) ON DELETE CASCADE
);
GO

CREATE TABLE Vendas (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    CompradorId UNIQUEIDENTIFIER NOT NULL,
    ProdutoId UNIQUEIDENTIFIER NOT NULL,
    ValorTotal DECIMAL(18,2) NOT NULL,
    DataVenda DATETIME NOT NULL DEFAULT GETDATE(),
    Status INT NOT NULL, -- Enum StatusVenda
    ModalidadePagamento INT NOT NULL, -- Enum ModalidadePagamento
    ModalidadeEntrega INT NOT NULL, -- Enum ModalidadeEntrega
    CONSTRAINT FK_Vendas_Usuarios FOREIGN KEY (CompradorId) REFERENCES Usuarios(Id),
    CONSTRAINT FK_Vendas_Produtos FOREIGN KEY (ProdutoId) REFERENCES Produtos(Id)
);
GO

-- Índices adicionais
CREATE NONCLUSTERED INDEX IX_Produtos_Status ON Produtos(Status);
CREATE NONCLUSTERED INDEX IX_FotosProduto_ProdutoId ON FotosProduto(ProdutoId);
CREATE NONCLUSTERED INDEX IX_Vendas_CompradorId ON Vendas(CompradorId);
CREATE NONCLUSTERED INDEX IX_Vendas_ProdutoId ON Vendas(ProdutoId);
GO

-- Script de SEED inicial
INSERT INTO Planos (Nome, Valor, LimiteAnuncios) VALUES 
('Free', 0.00, 3),
('Bronze', 19.90, 10),
('Prata', 49.90, 30),
('Ouro', 99.90, 100);
GO
