-- ============================================================
-- SCRIPT DE MIGRACAO FASE 4 - DIMENSOES, CATEGORIAS, PLANOS, LEILOES E RIFAS
-- MercadoBonsai (PostgreSQL / Supabase)
-- ============================================================

-- 1. Novos campos em produtos
ALTER TABLE produtos ADD COLUMN IF NOT EXISTS altura DECIMAL(10,2) NULL DEFAULT 0.00;
ALTER TABLE produtos ADD COLUMN IF NOT EXISTS largura DECIMAL(10,2) NULL DEFAULT 0.00;
ALTER TABLE produtos ADD COLUMN IF NOT EXISTS comprimento DECIMAL(10,2) NULL DEFAULT 0.00;
ALTER TABLE produtos ADD COLUMN IF NOT EXISTS peso DECIMAL(10,2) NULL DEFAULT 0.00;
ALTER TABLE produtos ADD COLUMN IF NOT EXISTS formaenvio VARCHAR(50) NULL DEFAULT 'A combinar';
ALTER TABLE produtos ADD COLUMN IF NOT EXISTS categoria VARCHAR(50) NULL DEFAULT 'Bonsai';

-- 2. Tabela de Planos de Assinatura
CREATE TABLE IF NOT EXISTS planos (
    id INT PRIMARY KEY,
    nome VARCHAR(50) NOT NULL,
    valor DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    preco DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    percentualcomissao DECIMAL(5,2) NOT NULL DEFAULT 10.00,
    limitelifas30dias INT NOT NULL DEFAULT 2,
    limiteleiloes30dias INT NOT NULL DEFAULT 2,
    limiteanuncios INT NOT NULL DEFAULT 10,
    destaqueshome BOOLEAN NOT NULL DEFAULT FALSE
);

ALTER TABLE planos ADD COLUMN IF NOT EXISTS valor DECIMAL(10,2) NULL DEFAULT 0.00;
ALTER TABLE planos ADD COLUMN IF NOT EXISTS preco DECIMAL(10,2) NULL DEFAULT 0.00;
ALTER TABLE planos ADD COLUMN IF NOT EXISTS percentualcomissao DECIMAL(5,2) NOT NULL DEFAULT 10.00;
ALTER TABLE planos ADD COLUMN IF NOT EXISTS limitelifas30dias INT NOT NULL DEFAULT 2;
ALTER TABLE planos ADD COLUMN IF NOT EXISTS limiteleiloes30dias INT NOT NULL DEFAULT 2;
ALTER TABLE planos ADD COLUMN IF NOT EXISTS limiteanuncios INT NOT NULL DEFAULT 10;
ALTER TABLE planos ADD COLUMN IF NOT EXISTS destaqueshome BOOLEAN NOT NULL DEFAULT FALSE;

-- Inserir/Atualizar planos padrao
INSERT INTO planos (id, nome, valor, preco, percentualcomissao, limitelifas30dias, limiteleiloes30dias, limiteanuncios, destaqueshome)
OVERRIDING SYSTEM VALUE
VALUES 
    (1, 'Bronze', 0.00, 0.00, 10.00, 2, 2, 5, FALSE),
    (2, 'Prata', 49.90, 49.90, 7.00, 6, 6, 25, TRUE),
    (3, 'Ouro', 99.90, 99.90, 4.00, 15, 15, 100, TRUE)
ON CONFLICT (id) DO UPDATE 
SET nome = EXCLUDED.nome, valor = EXCLUDED.valor, preco = EXCLUDED.preco, percentualcomissao = EXCLUDED.percentualcomissao,
    limitelifas30dias = EXCLUDED.limitelifas30dias, limiteleiloes30dias = EXCLUDED.limiteleiloes30dias, limiteanuncios = EXCLUDED.limiteanuncios, destaqueshome = EXCLUDED.destaqueshome;

-- 3. Atualizar/Garantir estrutura da tabela leiloes
CREATE TABLE IF NOT EXISTS leiloes (
    id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    titulo VARCHAR(200) NOT NULL,
    subtitulo VARCHAR(300) NULL,
    descricao TEXT NULL,
    fotoprincipalurl VARCHAR(500) NOT NULL,
    fotodetalheurl VARCHAR(500) NULL,
    badge VARCHAR(50) NULL,
    lanceatual DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    proximolanceminimo DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    incrementominimo DECIMAL(10,2) NOT NULL DEFAULT 50.00,
    vendedorid INT NULL,
    vendedornome VARCHAR(150) NULL,
    datafinalizacao TIMESTAMP NOT NULL,
    status INT NOT NULL DEFAULT 1, -- 1=Criado, 2=Iniciado, 3=Suspenso, 4=Finalizado
    datacriacao TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 4. Atualizar/Garantir estrutura da tabela rifas
CREATE TABLE IF NOT EXISTS rifas (
    id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    titulo VARCHAR(200) NOT NULL,
    subtitulo VARCHAR(300) NULL,
    descricao TEXT NULL,
    fotoprincipalurl VARCHAR(500) NOT NULL,
    fotodetalheurl VARCHAR(500) NULL,
    valorcota DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    totalcotas INT NOT NULL DEFAULT 100,
    cotasvendidas INT NOT NULL DEFAULT 0,
    vendedorid INT NULL,
    vendedornome VARCHAR(150) NULL,
    datasorteio TIMESTAMP NOT NULL,
    status INT NOT NULL DEFAULT 1, -- 1=Ativa, 2=Sorteada, 3=Cancelada
    datacriacao TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);
