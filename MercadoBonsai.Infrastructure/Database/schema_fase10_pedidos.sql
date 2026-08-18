-- ============================================================
-- SCRIPT DE MIGRACAO FASE 10 - ESTEIRA DE VENDAS E ESTRUTURA DE PEDIDOS
-- MercadoBonsai (PostgreSQL / Supabase)
-- ============================================================

-- 1. Tabela de Pedidos
CREATE TABLE IF NOT EXISTS pedidos (
    id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    numero INT NOT NULL,
    comprador_id INT NOT NULL,
    vendedor_id INT NOT NULL,
    produto_id INT NOT NULL,
    statuspedido VARCHAR(100) NOT NULL DEFAULT 'Criado',
    tipopagamento VARCHAR(100) NOT NULL DEFAULT 'PIX / Asaas',
    datapedido TIMESTAMP NOT NULL DEFAULT NOW(),
    datapagamento TIMESTAMP NULL,
    valorpedido DECIMAL(10,2) NOT NULL,
    valor_frete DECIMAL(10,2) NULL DEFAULT 0.00,
    valor_seguro DECIMAL(10,2) NULL DEFAULT 0.00,
    valor_total DECIMAL(10,2) NOT NULL,
    urlcheckout VARCHAR(1000) NULL,
    observacao TEXT NULL,
    codigorastreio VARCHAR(50) NULL,
    urlrastreio VARCHAR(500) NULL,
    compradornome VARCHAR(255) NULL,
    compradoremail VARCHAR(255) NULL,
    compradortelefone VARCHAR(25) NULL,
    compradorendereco VARCHAR(1000) NULL,
    compradoraniversario TIMESTAMP NULL,
    urlavaliacao VARCHAR(1000) NULL,
    asaas_payment_id VARCHAR(100) NULL,
    CONSTRAINT fk_pedidos_comprador FOREIGN KEY (comprador_id) REFERENCES usuarios(id),
    CONSTRAINT fk_pedidos_vendedor FOREIGN KEY (vendedor_id) REFERENCES usuarios(id),
    CONSTRAINT fk_pedidos_produto FOREIGN KEY (produto_id) REFERENCES produtos(id)
);

-- Índices para buscas performáticas
CREATE INDEX IF NOT EXISTS IX_pedidos_comprador_id ON pedidos(comprador_id);
CREATE INDEX IF NOT EXISTS IX_pedidos_vendedor_id ON pedidos(vendedor_id);
CREATE INDEX IF NOT EXISTS IX_pedidos_statuspedido ON pedidos(statuspedido);
CREATE INDEX IF NOT EXISTS IX_pedidos_asaas_payment_id ON pedidos(asaas_payment_id);

-- 2. Expansão na Tabela de Usuários para Subconta e Cliente Asaas
ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS asaas_account_id VARCHAR(100) NULL;
ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS asaas_customer_id VARCHAR(100) NULL;
