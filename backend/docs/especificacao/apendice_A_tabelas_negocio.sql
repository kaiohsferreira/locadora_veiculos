-- =====================================================================
-- Trabalho Interdisciplinar: Aplicacoes para Processos de Negocio
-- Plataforma web para locadora de veiculos - locacao e venda facilitada
-- MODELO FISICO - PostgreSQL
-- =====================================================================

DROP TABLE IF EXISTS sinistro, seguro, manutencao, pagamento, venda,
    cobranca_extra, avaria, vistoria, locacao, reserva, veiculo,
    categoria, funcionario, cliente CASCADE;

-- =====================================================================
-- 1. CADASTROS BASICOS
-- =====================================================================

CREATE TABLE cliente (
    id_cliente        SERIAL        PRIMARY KEY,
    nome              VARCHAR(120)  NOT NULL,
    cpf               CHAR(11)      NOT NULL UNIQUE,
    data_nascimento   DATE          NOT NULL,
    email             VARCHAR(120)  NOT NULL,
    telefone          VARCHAR(20)   NOT NULL,
    cnh_numero        VARCHAR(20)   NOT NULL UNIQUE,
    cnh_categoria     CHAR(2)       NOT NULL,
    cnh_validade      DATE          NOT NULL,
    logradouro        VARCHAR(120),
    numero            VARCHAR(10),
    complemento       VARCHAR(60),
    bairro            VARCHAR(60),
    cidade            VARCHAR(60),
    uf                CHAR(2),
    cep               CHAR(8),
    data_cadastro     TIMESTAMP     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    status_cadastro   VARCHAR(15)   NOT NULL DEFAULT 'PENDENTE',
    CONSTRAINT ck_cliente_status
        CHECK (status_cadastro IN ('PENDENTE', 'APROVADO', 'RECUSADO')),
    CONSTRAINT ck_cliente_cnh_cat
        CHECK (cnh_categoria IN ('A', 'B', 'AB', 'C', 'D', 'E'))
);

CREATE TABLE funcionario (
    id_funcionario    SERIAL        PRIMARY KEY,
    nome              VARCHAR(120)  NOT NULL,
    cpf               CHAR(11)      NOT NULL UNIQUE,
    matricula         VARCHAR(20)   NOT NULL UNIQUE,
    cargo             VARCHAR(40)   NOT NULL,
    email             VARCHAR(120),
    data_admissao     DATE          NOT NULL,
    ativo             BOOLEAN       NOT NULL DEFAULT TRUE,
    CONSTRAINT ck_funcionario_cargo
        CHECK (cargo IN ('ATENDENTE', 'VENDEDOR', 'GESTOR_FROTA',
                         'FINANCEIRO', 'ADMINISTRATIVO'))
);

CREATE TABLE categoria (
    id_categoria      SERIAL        PRIMARY KEY,
    nome              VARCHAR(50)   NOT NULL UNIQUE,
    descricao         VARCHAR(200),
    valor_diaria_base NUMERIC(10,2) NOT NULL,
    CONSTRAINT ck_categoria_valor CHECK (valor_diaria_base > 0)
);

-- =====================================================================
-- 2. FROTA
-- =====================================================================

CREATE TABLE veiculo (
    id_veiculo        SERIAL        PRIMARY KEY,
    id_categoria      INTEGER       NOT NULL,
    placa             CHAR(7)       NOT NULL UNIQUE,
    chassi            VARCHAR(17)   NOT NULL UNIQUE,
    renavam           VARCHAR(11)   NOT NULL UNIQUE,
    marca             VARCHAR(40)   NOT NULL,
    modelo            VARCHAR(40)   NOT NULL,
    ano_fabricacao    SMALLINT      NOT NULL,
    ano_modelo        SMALLINT      NOT NULL,
    cor               VARCHAR(30),
    km_atual          INTEGER       NOT NULL DEFAULT 0,
    valor_diaria      NUMERIC(10,2) NOT NULL,
    valor_venda       NUMERIC(12,2),
    status            VARCHAR(20)   NOT NULL DEFAULT 'DISPONIVEL',
    data_aquisicao    DATE,
    CONSTRAINT fk_veiculo_categoria FOREIGN KEY (id_categoria)
        REFERENCES categoria (id_categoria),
    CONSTRAINT ck_veiculo_status
        CHECK (status IN ('DISPONIVEL', 'RESERVADO', 'LOCADO',
                          'MANUTENCAO', 'VENDIDO', 'BAIXADO')),
    CONSTRAINT ck_veiculo_km CHECK (km_atual >= 0),
    CONSTRAINT ck_veiculo_diaria CHECK (valor_diaria > 0),
    CONSTRAINT ck_veiculo_ano CHECK (ano_modelo >= ano_fabricacao)
);

-- =====================================================================
-- 3. RESERVA E LOCACAO
-- =====================================================================

CREATE TABLE reserva (
    id_reserva              SERIAL        PRIMARY KEY,
    id_cliente              INTEGER       NOT NULL,
    id_veiculo              INTEGER       NOT NULL,
    data_reserva            TIMESTAMP     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    data_retirada_prevista  TIMESTAMP     NOT NULL,
    data_devolucao_prevista TIMESTAMP     NOT NULL,
    valor_estimado          NUMERIC(10,2),
    status                  VARCHAR(20)   NOT NULL DEFAULT 'ATIVA',
    CONSTRAINT fk_reserva_cliente FOREIGN KEY (id_cliente)
        REFERENCES cliente (id_cliente),
    CONSTRAINT fk_reserva_veiculo FOREIGN KEY (id_veiculo)
        REFERENCES veiculo (id_veiculo),
    CONSTRAINT ck_reserva_status
        CHECK (status IN ('ATIVA', 'CONFIRMADA', 'CANCELADA', 'CONCLUIDA')),
    CONSTRAINT ck_reserva_periodo
        CHECK (data_devolucao_prevista > data_retirada_prevista)
);

CREATE TABLE locacao (
    id_locacao              SERIAL        PRIMARY KEY,
    id_reserva              INTEGER       UNIQUE,
    id_cliente              INTEGER       NOT NULL,
    id_veiculo              INTEGER       NOT NULL,
    id_funcionario          INTEGER       NOT NULL,
    data_retirada           TIMESTAMP     NOT NULL,
    data_devolucao_prevista TIMESTAMP     NOT NULL,
    data_devolucao_efetiva  TIMESTAMP,
    km_saida                INTEGER       NOT NULL,
    km_chegada              INTEGER,
    valor_diaria            NUMERIC(10,2) NOT NULL,
    valor_caucao            NUMERIC(10,2) NOT NULL DEFAULT 0,
    valor_total             NUMERIC(12,2),
    status                  VARCHAR(20)   NOT NULL DEFAULT 'EM_ANDAMENTO',
    CONSTRAINT fk_locacao_reserva FOREIGN KEY (id_reserva)
        REFERENCES reserva (id_reserva),
    CONSTRAINT fk_locacao_cliente FOREIGN KEY (id_cliente)
        REFERENCES cliente (id_cliente),
    CONSTRAINT fk_locacao_veiculo FOREIGN KEY (id_veiculo)
        REFERENCES veiculo (id_veiculo),
    CONSTRAINT fk_locacao_funcionario FOREIGN KEY (id_funcionario)
        REFERENCES funcionario (id_funcionario),
    CONSTRAINT ck_locacao_status
        CHECK (status IN ('EM_ANDAMENTO', 'FINALIZADA', 'ATRASADA', 'CANCELADA')),
    CONSTRAINT ck_locacao_km
        CHECK (km_chegada IS NULL OR km_chegada >= km_saida),
    CONSTRAINT ck_locacao_devolucao
        CHECK (data_devolucao_efetiva IS NULL
               OR data_devolucao_efetiva >= data_retirada)
);

CREATE TABLE vistoria (
    id_vistoria       SERIAL        PRIMARY KEY,
    id_locacao        INTEGER       NOT NULL,
    id_funcionario    INTEGER       NOT NULL,
    tipo              VARCHAR(10)   NOT NULL,
    data_hora         TIMESTAMP     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    km_registrado     INTEGER       NOT NULL,
    nivel_combustivel NUMERIC(4,2),
    observacoes       TEXT,
    CONSTRAINT fk_vistoria_locacao FOREIGN KEY (id_locacao)
        REFERENCES locacao (id_locacao) ON DELETE CASCADE,
    CONSTRAINT fk_vistoria_funcionario FOREIGN KEY (id_funcionario)
        REFERENCES funcionario (id_funcionario),
    CONSTRAINT ck_vistoria_tipo CHECK (tipo IN ('ENTRADA', 'SAIDA')),
    CONSTRAINT ck_vistoria_combustivel
        CHECK (nivel_combustivel BETWEEN 0 AND 1),
    CONSTRAINT uq_vistoria_locacao_tipo UNIQUE (id_locacao, tipo)
);

CREATE TABLE avaria (
    id_avaria         SERIAL        PRIMARY KEY,
    id_vistoria       INTEGER       NOT NULL,
    descricao         VARCHAR(200)  NOT NULL,
    gravidade         VARCHAR(15)   NOT NULL DEFAULT 'LEVE',
    valor_estimado    NUMERIC(10,2),
    CONSTRAINT fk_avaria_vistoria FOREIGN KEY (id_vistoria)
        REFERENCES vistoria (id_vistoria) ON DELETE CASCADE,
    CONSTRAINT ck_avaria_gravidade
        CHECK (gravidade IN ('LEVE', 'MEDIA', 'GRAVE'))
);

CREATE TABLE cobranca_extra (
    id_cobranca       SERIAL        PRIMARY KEY,
    id_locacao        INTEGER       NOT NULL,
    tipo              VARCHAR(20)   NOT NULL,
    descricao         VARCHAR(200),
    valor             NUMERIC(10,2) NOT NULL,
    data_lancamento   DATE          NOT NULL DEFAULT CURRENT_DATE,
    CONSTRAINT fk_cobranca_locacao FOREIGN KEY (id_locacao)
        REFERENCES locacao (id_locacao) ON DELETE CASCADE,
    CONSTRAINT ck_cobranca_tipo
        CHECK (tipo IN ('MULTA_TRANSITO', 'KM_EXCEDENTE', 'ATRASO',
                        'AVARIA', 'COMBUSTIVEL', 'LIMPEZA')),
    CONSTRAINT ck_cobranca_valor CHECK (valor >= 0)
);

-- =====================================================================
-- 4. VENDA E PAGAMENTOS
-- =====================================================================

CREATE TABLE venda (
    id_venda            SERIAL        PRIMARY KEY,
    id_veiculo          INTEGER       NOT NULL UNIQUE,
    id_cliente          INTEGER       NOT NULL,
    id_funcionario      INTEGER       NOT NULL,
    data_venda          DATE          NOT NULL DEFAULT CURRENT_DATE,
    valor_venda         NUMERIC(12,2) NOT NULL,
    forma_pagamento     VARCHAR(20)   NOT NULL,
    numero_nota_fiscal  VARCHAR(20)   UNIQUE,
    status_documentacao VARCHAR(20)   NOT NULL DEFAULT 'PENDENTE',
    CONSTRAINT fk_venda_veiculo FOREIGN KEY (id_veiculo)
        REFERENCES veiculo (id_veiculo),
    CONSTRAINT fk_venda_cliente FOREIGN KEY (id_cliente)
        REFERENCES cliente (id_cliente),
    CONSTRAINT fk_venda_funcionario FOREIGN KEY (id_funcionario)
        REFERENCES funcionario (id_funcionario),
    CONSTRAINT ck_venda_valor CHECK (valor_venda > 0),
    CONSTRAINT ck_venda_forma
        CHECK (forma_pagamento IN ('A_VISTA', 'FINANCIAMENTO', 'CONSORCIO')),
    CONSTRAINT ck_venda_doc
        CHECK (status_documentacao IN ('PENDENTE', 'EM_TRANSFERENCIA', 'CONCLUIDA'))
);

CREATE TABLE pagamento (
    id_pagamento      SERIAL        PRIMARY KEY,
    id_locacao        INTEGER,
    id_venda          INTEGER,
    tipo              VARCHAR(20)   NOT NULL,
    valor             NUMERIC(12,2) NOT NULL,
    data_vencimento   DATE          NOT NULL,
    data_pagamento    DATE,
    forma             VARCHAR(20),
    status            VARCHAR(15)   NOT NULL DEFAULT 'PENDENTE',
    CONSTRAINT fk_pagamento_locacao FOREIGN KEY (id_locacao)
        REFERENCES locacao (id_locacao),
    CONSTRAINT fk_pagamento_venda FOREIGN KEY (id_venda)
        REFERENCES venda (id_venda),
    CONSTRAINT ck_pagamento_tipo
        CHECK (tipo IN ('CAUCAO', 'DIARIA', 'COBRANCA_EXTRA', 'VENDA')),
    CONSTRAINT ck_pagamento_status
        CHECK (status IN ('PENDENTE', 'PAGO', 'ATRASADO', 'CANCELADO')),
    CONSTRAINT ck_pagamento_valor CHECK (valor > 0),
    -- um pagamento refere-se a uma locacao OU a uma venda, nunca a ambos
    CONSTRAINT ck_pagamento_origem CHECK (
        (id_locacao IS NOT NULL AND id_venda IS NULL) OR
        (id_locacao IS NULL     AND id_venda IS NOT NULL)
    )
);

-- =====================================================================
-- 5. MANUTENCAO, SEGUROS E SINISTROS
-- =====================================================================

CREATE TABLE manutencao (
    id_manutencao     SERIAL        PRIMARY KEY,
    id_veiculo        INTEGER       NOT NULL,
    tipo              VARCHAR(15)   NOT NULL,
    data_entrada      DATE          NOT NULL,
    data_saida        DATE,
    km_registrado     INTEGER       NOT NULL,
    oficina           VARCHAR(100),
    descricao         VARCHAR(200),
    custo             NUMERIC(10,2),
    CONSTRAINT fk_manutencao_veiculo FOREIGN KEY (id_veiculo)
        REFERENCES veiculo (id_veiculo),
    CONSTRAINT ck_manutencao_tipo
        CHECK (tipo IN ('PREVENTIVA', 'CORRETIVA')),
    CONSTRAINT ck_manutencao_periodo
        CHECK (data_saida IS NULL OR data_saida >= data_entrada)
);

CREATE TABLE seguro (
    id_seguro         SERIAL        PRIMARY KEY,
    id_veiculo        INTEGER       NOT NULL,
    seguradora        VARCHAR(100)  NOT NULL,
    numero_apolice    VARCHAR(30)   NOT NULL UNIQUE,
    data_inicio       DATE          NOT NULL,
    data_fim          DATE          NOT NULL,
    valor_premio      NUMERIC(10,2) NOT NULL,
    cobertura         VARCHAR(200),
    CONSTRAINT fk_seguro_veiculo FOREIGN KEY (id_veiculo)
        REFERENCES veiculo (id_veiculo),
    CONSTRAINT ck_seguro_vigencia CHECK (data_fim > data_inicio)
);

CREATE TABLE sinistro (
    id_sinistro       SERIAL        PRIMARY KEY,
    id_veiculo        INTEGER       NOT NULL,
    id_seguro         INTEGER,
    id_locacao        INTEGER,
    data_ocorrencia   DATE          NOT NULL,
    tipo              VARCHAR(20)   NOT NULL,
    descricao         TEXT,
    valor_prejuizo    NUMERIC(12,2),
    status            VARCHAR(20)   NOT NULL DEFAULT 'ABERTO',
    CONSTRAINT fk_sinistro_veiculo FOREIGN KEY (id_veiculo)
        REFERENCES veiculo (id_veiculo),
    CONSTRAINT fk_sinistro_seguro FOREIGN KEY (id_seguro)
        REFERENCES seguro (id_seguro),
    CONSTRAINT fk_sinistro_locacao FOREIGN KEY (id_locacao)
        REFERENCES locacao (id_locacao),
    CONSTRAINT ck_sinistro_tipo
        CHECK (tipo IN ('COLISAO', 'ROUBO', 'FURTO', 'INCENDIO',
                        'FENOMENO_NATURAL', 'TERCEIROS')),
    CONSTRAINT ck_sinistro_status
        CHECK (status IN ('ABERTO', 'EM_ANALISE', 'APROVADO',
                          'NEGADO', 'ENCERRADO'))
);

-- =====================================================================
-- 6. INDICES DE APOIO AS CONSULTAS MAIS FREQUENTES
-- =====================================================================

CREATE INDEX idx_veiculo_status      ON veiculo (status);
CREATE INDEX idx_veiculo_categoria   ON veiculo (id_categoria);
CREATE INDEX idx_reserva_periodo     ON reserva (data_retirada_prevista,
                                                 data_devolucao_prevista);
CREATE INDEX idx_reserva_cliente     ON reserva (id_cliente);
CREATE INDEX idx_locacao_cliente     ON locacao (id_cliente);
CREATE INDEX idx_locacao_veiculo     ON locacao (id_veiculo);
CREATE INDEX idx_locacao_status      ON locacao (status);
CREATE INDEX idx_pagamento_status    ON pagamento (status, data_vencimento);
CREATE INDEX idx_manutencao_veiculo  ON manutencao (id_veiculo, data_entrada);
CREATE INDEX idx_sinistro_veiculo    ON sinistro (id_veiculo, data_ocorrencia);
