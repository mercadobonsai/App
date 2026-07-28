-- ============================================================
-- SCRIPT DE EXPANSÃO DA TABELA DE USUÁRIOS/CLIENTES
-- MercadoBonsai (PostgreSQL / Supabase)
-- ============================================================

ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS razaosocial VARCHAR(200) NULL;
ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS cpfcnpj VARCHAR(20) NULL;
ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS inscricaoestadual VARCHAR(30) NULL;
ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS chavepix VARCHAR(100) NULL;
ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS banco VARCHAR(100) NULL;
ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS agencia VARCHAR(20) NULL;
ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS conta VARCHAR(30) NULL;
ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS descricaoviveiro TEXT NULL;
ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS logotipourl VARCHAR(500) NULL;
ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS dataultimaalteracao TIMESTAMP NULL;
ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS usuarioalteracaoid INT NULL;
ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS usuarioalteracaonome VARCHAR(150) NULL;

-- Atualizar Usuário Admin/Vendedor com dados iniciais de demonstração
UPDATE usuarios
SET razaosocial = 'Viveiro Shokunin Ltda',
    cpfcnpj = '12.345.678/0001-90',
    chavepix = 'shokunin@mercadobonsai.com.br',
    banco = 'Banco do Brasil (001)',
    agencia = '1234-5',
    conta = '98765-4',
    descricaoviveiro = 'Especialistas em pré-bonsais importados do Japão e ferramentas tradicionais de alta precisão.',
    logotipourl = '/starter-kit/assets/img/shimpaku_leilao.png'
WHERE email = 'admin@mercadobonsai.com.br';
