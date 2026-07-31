-- ============================================================
-- SCRIPT DE MIGRACAO FASE 8 - ISENCAO DE COBRANCA E PRONTUARIO DO BONSAI
-- MercadoBonsai (PostgreSQL / Supabase)
-- ============================================================

-- 1. Flag de Isenção de Cobrança ("Não cobrar") em Usuários
ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS isentocobranca BOOLEAN NOT NULL DEFAULT FALSE;

-- 2. Tabela de Cadastro e Saúde das Plantas (Prontuário do Bonsai)
CREATE TABLE IF NOT EXISTS prontuarioplantas (
    id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    usuarioid INT NOT NULL REFERENCES usuarios(id) ON DELETE CASCADE,
    nomepopular VARCHAR(150) NOT NULL,
    nomecientifico VARCHAR(150) NULL,
    especie VARCHAR(100) NOT NULL,
    altura DECIMAL(10,2) NULL DEFAULT 0.00,
    largura DECIMAL(10,2) NULL DEFAULT 0.00,
    comprimento DECIMAL(10,2) NULL DEFAULT 0.00,
    peso DECIMAL(10,3) NULL DEFAULT 0.000,
    descricaolivre TEXT NULL,
    fotoprincipalurl VARCHAR(500) NULL,
    datainicial TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    dataultimamanutencao TIMESTAMP NULL,
    dataproximamanutencao TIMESTAMP NULL,
    dataultimaadubacao TIMESTAMP NULL,
    dataproximaadubacao TIMESTAMP NULL,
    datacriacao TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 3. Tabela de Eventos, Manutenções e Linha do Tempo (Feed da Planta)
CREATE TABLE IF NOT EXISTS prontuarioeventos (
    id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    plantaid INT NOT NULL REFERENCES prontuarioplantas(id) ON DELETE CASCADE,
    titulo VARCHAR(200) NOT NULL,
    descricao TEXT NOT NULL,
    dataevento TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    fotourl VARCHAR(500) NULL,
    nomeadubo VARCHAR(150) NULL,
    nomeremedio VARCHAR(150) NULL,
    datacriacao TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS IX_prontuarioplantas_usuarioid ON prontuarioplantas(usuarioid);
CREATE INDEX IF NOT EXISTS IX_prontuarioeventos_plantaid ON prontuarioeventos(plantaid);
