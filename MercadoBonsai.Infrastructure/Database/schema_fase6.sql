-- ============================================================
-- SCRIPT DE MIGRACAO FASE 6 - PEDIDOS E RESERVAS DE COTAS DE ACOES ENTRE AMIGOS
-- MercadoBonsai (PostgreSQL / Supabase)
-- ============================================================

CREATE TABLE IF NOT EXISTS pedidosrifa (
    id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    rifaid INT NOT NULL REFERENCES rifas(id) ON DELETE CASCADE,
    usuarioid INT NOT NULL REFERENCES usuarios(id),
    usuarionome VARCHAR(150) NOT NULL,
    quantidadecotas INT NOT NULL,
    valortotal DECIMAL(10,2) NOT NULL,
    chavepix VARCHAR(500) NOT NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'Pendente',
    datareserva TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);
