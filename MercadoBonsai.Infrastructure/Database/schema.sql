CREATE TABLE Planos (
    Id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    Nome VARCHAR(100) NOT NULL,
    Valor DECIMAL(18,2) NOT NULL,
    LimiteAnuncios INT NOT NULL
);

CREATE TABLE Usuarios (
    Id UUID PRIMARY KEY,
    Nome VARCHAR(150) NOT NULL,
    Email VARCHAR(150) NOT NULL UNIQUE,
    SenhaHash VARCHAR(255) NOT NULL,
    Telefone VARCHAR(20) NULL,
    Perfil INT NOT NULL DEFAULT 1, -- 1=Comprador, 2=Vendedor, 3=Administrador
    DataCadastro TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE Produtos (
    Id UUID PRIMARY KEY,
    VendedorId UUID NOT NULL,
    Nome VARCHAR(150) NOT NULL,
    Descricao TEXT NULL,
    Preco DECIMAL(18,2) NOT NULL,
    Especie VARCHAR(100) NULL,
    IdadeAnos INT NULL,
    Status INT NOT NULL, -- Enum StatusProduto
    TipoModalidade INT NOT NULL, -- Enum ModalidadeEntrega
    DataCadastro TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT FK_Produtos_Usuarios FOREIGN KEY (VendedorId) REFERENCES Usuarios(Id)
);

CREATE TABLE FotosProduto (
    Id UUID PRIMARY KEY,
    ProdutoId UUID NOT NULL,
    Url VARCHAR(500) NOT NULL,
    IsPrincipal BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT FK_FotosProduto_Produtos FOREIGN KEY (ProdutoId) REFERENCES Produtos(Id) ON DELETE CASCADE
);

CREATE TABLE Vendas (
    Id UUID PRIMARY KEY,
    CompradorId UUID NOT NULL,
    ProdutoId UUID NOT NULL,
    ValorTotal DECIMAL(18,2) NOT NULL,
    DataVenda TIMESTAMP NOT NULL DEFAULT NOW(),
    Status INT NOT NULL, -- Enum StatusVenda
    ModalidadePagamento INT NOT NULL, -- Enum ModalidadePagamento
    ModalidadeEntrega INT NOT NULL, -- Enum ModalidadeEntrega
    CONSTRAINT FK_Vendas_Usuarios FOREIGN KEY (CompradorId) REFERENCES Usuarios(Id),
    CONSTRAINT FK_Vendas_Produtos FOREIGN KEY (ProdutoId) REFERENCES Produtos(Id)
);

-- Índices adicionais
CREATE INDEX IX_Produtos_Status ON Produtos(Status);
CREATE INDEX IX_FotosProduto_ProdutoId ON FotosProduto(ProdutoId);
CREATE INDEX IX_Vendas_CompradorId ON Vendas(CompradorId);
CREATE INDEX IX_Vendas_ProdutoId ON Vendas(ProdutoId);

-- Script de SEED inicial
INSERT INTO Planos (Nome, Valor, LimiteAnuncios) VALUES 
('Free', 0.00, 3),
('Bronze', 19.90, 10),
('Prata', 49.90, 30),
('Ouro', 99.90, 100);

-- Seed: Usuário Administrador padrão (senha: Admin123!)
INSERT INTO Usuarios (Id, Nome, Email, SenhaHash, Telefone, Perfil, DataCadastro) VALUES
(gen_random_uuid(), 'Administrador', 'admin@mercadobonsai.com.br', '$2a$11$YLAl2IVYFQjhyhqOGsaPBe3mst9.LiFr1sc1UX47sV.ChAH585ogm', NULL, 3, NOW());
