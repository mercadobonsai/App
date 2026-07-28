-- ============================================================
-- SCRIPT FASE 2 - Leilões, Rifas, Patrocínios e Dicas
-- MercadoBonsai (PostgreSQL / Supabase)
-- ============================================================

CREATE TABLE IF NOT EXISTS leiloes (
    id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    titulo VARCHAR(200) NOT NULL,
    subtitulo VARCHAR(300) NULL,
    descricao TEXT NULL,
    fotoprincipalurl VARCHAR(500) NOT NULL,
    fotodetalheurl VARCHAR(500) NULL,
    badge VARCHAR(100) NULL,
    lanceatual DECIMAL(18,2) NOT NULL,
    proximolanceminimo DECIMAL(18,2) NOT NULL,
    incrementominimo DECIMAL(18,2) NOT NULL DEFAULT 50.00,
    vendedorid INT NULL,
    vendedornome VARCHAR(150) NULL,
    datafinalizacao TIMESTAMP NOT NULL,
    status INT NOT NULL DEFAULT 1, -- 1=Ativo, 2=Finalizado, 3=Cancelado
    datacriacao TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS lancesleilao (
    id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    leilaoid INT NOT NULL,
    usuarioid INT NULL,
    usuarionome VARCHAR(100) NOT NULL,
    valor DECIMAL(18,2) NOT NULL,
    datalance TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_lances_leilao FOREIGN KEY (leilaoid) REFERENCES leiloes(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS rifas (
    id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    titulo VARCHAR(200) NOT NULL,
    subtitulo VARCHAR(300) NULL,
    descricao TEXT NULL,
    fotoprincipalurl VARCHAR(500) NOT NULL,
    fotodetalheurl VARCHAR(500) NULL,
    valorcota DECIMAL(18,2) NOT NULL,
    totalcotas INT NOT NULL,
    cotasvendidas INT NOT NULL DEFAULT 0,
    vendedorid INT NULL,
    vendedornome VARCHAR(150) NULL,
    datasorteio TIMESTAMP NOT NULL,
    status INT NOT NULL DEFAULT 1, -- 1=Ativa, 2=Sorteada, 3=Cancelada
    datacriacao TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS patrocinios (
    id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    nomelojaviveiro VARCHAR(150) NOT NULL,
    descricao TEXT NULL,
    imagemurl VARCHAR(500) NULL,
    linkdestino VARCHAR(500) NULL,
    badge VARCHAR(50) NULL DEFAULT 'Patrocinado',
    posicao INT NOT NULL DEFAULT 1, -- 1=LateralTopo, 2=LateralRodape, 3=HomeBanner
    isativo BOOLEAN NOT NULL DEFAULT TRUE,
    datacriacao TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS dicascultivo (
    id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    titulo VARCHAR(200) NOT NULL,
    subtitulo VARCHAR(200) NULL,
    conteudo TEXT NOT NULL,
    categoria VARCHAR(100) NULL,
    isativa BOOLEAN NOT NULL DEFAULT TRUE,
    datacriacao TIMESTAMP NOT NULL DEFAULT NOW()
);

-- SEED DATA
INSERT INTO leiloes (titulo, subtitulo, descricao, fotoprincipalurl, fotodetalheurl, badge, lanceatual, proximolanceminimo, incrementominimo, vendedornome, datafinalizacao, status)
VALUES (
    'Shimpaku Itoigawa (35 anos)',
    'Exemplar raro de coleção com trabalho esculpido em madeira morta (Jin e Shari).',
    'Este bonsai Shimpaku Itoigawa de 35 anos é uma verdadeira obra de arte viva. Cultivado em vaso de cerâmica japonesa importada de Yixing, possui ramificação densa, agulhas compactas e um Jin imponente esculpido manualmente por mestre bonsai.',
    '/starter-kit/assets/img/shimpaku_leilao.png',
    '/starter-kit/assets/img/shimpaku_detalhe.png',
    'Ao Vivo • Encerra em 02h 45m 12s',
    1450.00,
    1500.00,
    50.00,
    'Viveiro Shokunin (Certificado Mercado Bonsai)',
    NOW() + INTERVAL '1 DAY',
    1
);

INSERT INTO rifas (titulo, subtitulo, descricao, fotoprincipalurl, fotodetalheurl, valorcota, totalcotas, cotasvendidas, vendedornome, datasorteio, status)
VALUES (
    'Pinus Parviflora (15 anos)',
    'Pinheiro Branco Japonês de 15 anos com excelente estruturação dos galhos.',
    'Participe da Rifa Oficial deste belíssimo Pinus Parviflora. Cada cota custa apenas R$ 15,00. O sorteio será realizado com base na extração da Loteria Federal assim que 100% das cotas forem preenchidas.',
    '/starter-kit/assets/img/pinus_rifa.png',
    '/starter-kit/assets/img/pinus_detalhe.png',
    15.00,
    100,
    72,
    'Mercado Bonsai Oficial',
    NOW() + INTERVAL '7 DAYS',
    1
);

INSERT INTO patrocinios (nomelojaviveiro, descricao, imagemurl, linkdestino, badge, posicao, isativo)
VALUES (
    'Viveiro Shokunin',
    'Especialistas em pré-bonsais importados e ferramentas tradicionais de precisão.',
    '/starter-kit/assets/img/shimpaku_leilao.png',
    '#',
    'Patrocinado',
    1,
    TRUE
);

INSERT INTO dicascultivo (titulo, subtitulo, conteudo, categoria, isativa)
VALUES (
    'Dica de Cultivo da Semana',
    'Cuidados Essenciais com a Rega',
    'Sempre regue seu bonsai abundantemente até que a água escorra de forma uniforme pelos furos de drenagem do vaso. Evite regas nos horários de sol a pino para proteger as raízes do calor excessivo!',
    'Rega & Nutrição',
    TRUE
);
