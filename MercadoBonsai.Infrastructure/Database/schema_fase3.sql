-- ============================================================
-- SCRIPT DE STATUS DE PRODUTOS, PLANOS E REPUTAÇÃO DE VIVEIROS
-- MercadoBonsai (PostgreSQL / Supabase)
-- ============================================================

ALTER TABLE produtos ADD COLUMN IF NOT EXISTS status INT NOT NULL DEFAULT 1; -- 1=Disponível, 2=Indisponível, 3=Vendido

ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS planoid INT NOT NULL DEFAULT 1;
ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS reputacao INT NOT NULL DEFAULT 100;

-- Atualizar Usuário Admin/Vendedor para Plano Pago (Prata=3) com Alta Reputação (980 pts)
UPDATE usuarios
SET planoid = 3,
    reputacao = 980
WHERE email = 'admin@mercadobonsai.com.br';
