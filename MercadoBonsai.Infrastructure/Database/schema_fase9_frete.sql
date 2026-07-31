-- ============================================================
-- SCRIPT DE MIGRACAO FASE 9 - INTEGRACAO FRETE MELHOR ENVIO
-- MercadoBonsai (PostgreSQL / Supabase)
-- ============================================================

ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS cep VARCHAR(20) NULL;
ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS endereco VARCHAR(250) NULL;
ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS cidade VARCHAR(100) NULL;
ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS estado VARCHAR(50) NULL;

ALTER TABLE vendas ADD COLUMN IF NOT EXISTS valor_frete DECIMAL(10,2) NULL;
ALTER TABLE vendas ADD COLUMN IF NOT EXISTS valor_seguro DECIMAL(10,2) NULL;
