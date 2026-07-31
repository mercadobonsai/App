-- ============================================================
-- SCRIPT DE MIGRACAO FASE 8 LOCK - CONCORRENCIA NO PRONTUARIO
-- MercadoBonsai (PostgreSQL / Supabase)
-- ============================================================

ALTER TABLE prontuarioplantas ADD COLUMN IF NOT EXISTS lockusuarioid INT NULL;
ALTER TABLE prontuarioplantas ADD COLUMN IF NOT EXISTS lockusuarionome VARCHAR(150) NULL;
ALTER TABLE prontuarioplantas ADD COLUMN IF NOT EXISTS locktimestamp TIMESTAMP NULL;
