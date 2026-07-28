-- ============================================================
-- SCRIPT DE MIGRACAO FASE 7 - MODULO COMPLETO DE PROPAGANDAS E ANUNCIOS
-- MercadoBonsai (PostgreSQL / Supabase)
-- ============================================================

CREATE TABLE IF NOT EXISTS propagandas (
    id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    usuarioid INT NOT NULL REFERENCES usuarios(id),
    usuarionome VARCHAR(150) NOT NULL,
    tipoespaco VARCHAR(50) NOT NULL, -- 'Economico', 'Basico', 'Intermediario', 'Avancado'
    precomensal DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    imagemurl VARCHAR(500) NULL,
    linkdestino VARCHAR(500) NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'Pendente', -- 'Pendente', 'Ativo', 'Expirado', 'Rejeitado'
    datainicio TIMESTAMP NULL,
    dataexpiracao TIMESTAMP NULL,
    datacriacao TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);
