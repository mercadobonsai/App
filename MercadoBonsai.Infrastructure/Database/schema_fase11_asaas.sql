-- Migration Fase 11: Adição das colunas para suporte a Asaas e retenção com comissão personalizada
ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS asaas_subscription_id VARCHAR(100) NULL;
ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS percentualretencaopersonalizado NUMERIC(5,2) NULL;
ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS datanascimento DATE NULL;
ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS rendafaturamento NUMERIC(15,2) NULL;
