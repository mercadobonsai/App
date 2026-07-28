-- ============================================================
-- SCRIPT DE SCHEMA COMPLETO DO BANCO - MercadoBonsai (PostgreSQL / Supabase)
-- Execute este script no SQL Editor do Supabase.
-- ============================================================

DROP TABLE IF EXISTS vendas CASCADE;
DROP TABLE IF EXISTS fotosproduto CASCADE;
DROP TABLE IF EXISTS produtos CASCADE;
DROP TABLE IF EXISTS usuarios CASCADE;
DROP TABLE IF EXISTS planos CASCADE;

CREATE TABLE planos (
    id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    nome VARCHAR(100) NOT NULL,
    valor DECIMAL(18,2) NOT NULL,
    limiteanuncios INT NOT NULL
);

CREATE TABLE usuarios (
    id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    nome VARCHAR(150) NOT NULL,
    email VARCHAR(150) NOT NULL UNIQUE,
    senhahash VARCHAR(255) NOT NULL,
    telefone VARCHAR(20) NULL,
    perfil INT NOT NULL DEFAULT 1, -- 1=Comprador, 2=Vendedor, 3=Administrador
    datacadastro TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE produtos (
    id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    vendedorid INT NOT NULL,
    nome VARCHAR(150) NOT NULL,
    descricao TEXT NULL,
    preco DECIMAL(18,2) NOT NULL,
    quantidadeestoque INT NOT NULL DEFAULT 1,
    imagemurl VARCHAR(500) NULL,
    datacriacao TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_produtos_usuarios FOREIGN KEY (vendedorid) REFERENCES usuarios(id) ON DELETE CASCADE
);

CREATE TABLE fotosproduto (
    id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    produtoid INT NOT NULL,
    url VARCHAR(500) NOT NULL,
    isprincipal BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT fk_fotosproduto_produtos FOREIGN KEY (produtoid) REFERENCES produtos(id) ON DELETE CASCADE
);

CREATE TABLE vendas (
    id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    compradorid INT NOT NULL,
    produtoid INT NOT NULL,
    valortotal DECIMAL(18,2) NOT NULL,
    datavenda TIMESTAMP NOT NULL DEFAULT NOW(),
    status INT NOT NULL,
    modalidadepagamento INT NOT NULL,
    modalidadeentrega INT NOT NULL,
    CONSTRAINT fk_vendas_usuarios FOREIGN KEY (compradorid) REFERENCES usuarios(id),
    CONSTRAINT fk_vendas_produtos FOREIGN KEY (produtoid) REFERENCES produtos(id)
);

-- Índices
CREATE INDEX IX_produtos_vendedorid ON produtos(vendedorid);
CREATE INDEX IX_vendas_compradorid ON vendas(compradorid);
CREATE INDEX IX_vendas_produtoid ON vendas(produtoid);

-- SEED: Planos
INSERT INTO planos (nome, valor, limiteanuncios) VALUES
('Free', 0.00, 3),
('Bronze', 19.90, 10),
('Prata', 49.90, 30),
('Ouro', 99.90, 100);

-- SEED: Administrador padrão (senha: Admin123!)
INSERT INTO usuarios (nome, email, senhahash, telefone, perfil, datacadastro) VALUES
('Administrador', 'admin@mercadobonsai.com.br', '$2a$11$YLAl2IVYFQjhyhqOGsaPBe3mst9.LiFr1sc1UX47sV.ChAH585ogm', NULL, 3, NOW());
