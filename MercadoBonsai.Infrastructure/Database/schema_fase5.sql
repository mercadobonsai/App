-- ============================================================
-- SCRIPT DE MIGRACAO FASE 5 - PRECISÃO DE PESO, LIMITE DE FOTOS E PLANOS
-- MercadoBonsai (PostgreSQL / Supabase)
-- ============================================================

-- 1. Ajustar coluna peso em produtos para 3 casas decimais
ALTER TABLE produtos ALTER COLUMN peso TYPE DECIMAL(10,3);

-- 2. Adicionar coluna limitefotos em planos
ALTER TABLE planos ADD COLUMN IF NOT EXISTS limitefotos INT NOT NULL DEFAULT 3;

-- 3. Remover planos excedentes/duplicados se existirem
DELETE FROM planos WHERE id NOT IN (1, 2, 3);

-- 4. Garantir que os 3 planos padrão estejam cadastrados com limites configuráveis
INSERT INTO planos (id, nome, valor, preco, percentualcomissao, limitelifas30dias, limiteleiloes30dias, limiteanuncios, limitefotos, destaqueshome)
OVERRIDING SYSTEM VALUE
VALUES 
    (1, 'Bronze', 0.00, 0.00, 10.00, 2, 2, 5, 3, FALSE),
    (2, 'Prata', 49.90, 49.90, 7.00, 6, 6, 25, 6, TRUE),
    (3, 'Ouro', 99.90, 99.90, 4.00, 15, 15, 100, 12, TRUE)
ON CONFLICT (id) DO UPDATE 
SET nome = EXCLUDED.nome, valor = EXCLUDED.valor, preco = EXCLUDED.preco, percentualcomissao = EXCLUDED.percentualcomissao,
    limitelifas30dias = EXCLUDED.limitelifas30dias, limiteleiloes30dias = EXCLUDED.limiteleiloes30dias, 
    limiteanuncios = EXCLUDED.limiteanuncios, limitefotos = EXCLUDED.limitefotos, destaqueshome = EXCLUDED.destaqueshome;
