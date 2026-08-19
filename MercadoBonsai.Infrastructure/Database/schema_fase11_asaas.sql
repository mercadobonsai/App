-- Migration Fase 11: Adição da coluna asaas_subscription_id para suporte a assinaturas no Asaas
ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS asaas_subscription_id VARCHAR(100) NULL;
