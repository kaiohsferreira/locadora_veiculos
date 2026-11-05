CREATE TABLE categoria (
    id_categoria serial NOT NULL,
    nome character varying(50) NOT NULL,
    descricao character varying(200),
    valor_diaria_base numeric(10,2) NOT NULL,
    created_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    updated_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    disabled_at timestamp without time zone,
    CONSTRAINT categoria_pkey PRIMARY KEY (id_categoria),
    CONSTRAINT ck_categoria_valor CHECK (valor_diaria_base > 0)
);


CREATE TABLE cliente (
    id_cliente serial NOT NULL,
    nome character varying(120) NOT NULL,
    cpf character(11) NOT NULL,
    data_nascimento date NOT NULL,
    email character varying(120) NOT NULL,
    telefone character varying(20) NOT NULL,
    cnh_numero character varying(20) NOT NULL,
    cnh_categoria character(2) NOT NULL,
    cnh_validade date NOT NULL,
    logradouro character varying(120),
    numero character varying(10),
    complemento character varying(60),
    bairro character varying(60),
    cidade character varying(60),
    uf character(2),
    cep character(8),
    data_cadastro timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    status_cadastro character varying(15) NOT NULL DEFAULT 'PENDENTE',
    senha_hash character varying(255),
    created_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    updated_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    disabled_at timestamp without time zone,
    CONSTRAINT cliente_pkey PRIMARY KEY (id_cliente),
    CONSTRAINT ck_cliente_cnh_cat CHECK (cnh_categoria IN ('A', 'B', 'AB', 'C', 'D', 'E')),
    CONSTRAINT ck_cliente_status CHECK (status_cadastro IN ('PENDENTE', 'APROVADO', 'RECUSADO'))
);


CREATE TABLE funcionario (
    id_funcionario serial NOT NULL,
    nome character varying(120) NOT NULL,
    cpf character(11) NOT NULL,
    matricula character varying(20) NOT NULL,
    cargo character varying(40) NOT NULL,
    email character varying(120),
    data_admissao date NOT NULL,
    ativo boolean NOT NULL DEFAULT TRUE,
    senha_hash character varying(255),
    created_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    updated_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    disabled_at timestamp without time zone,
    CONSTRAINT funcionario_pkey PRIMARY KEY (id_funcionario),
    CONSTRAINT ck_funcionario_cargo CHECK (cargo IN ('ATENDENTE', 'VENDEDOR', 'GESTOR_FROTA', 'FINANCEIRO', 'ADMINISTRATIVO'))
);


CREATE TABLE integration (
    id_integration serial NOT NULL,
    codigo character varying(40) NOT NULL,
    nome character varying(120) NOT NULL,
    descricao character varying(200),
    ambiente character varying(15) NOT NULL DEFAULT 'PRODUCAO',
    base_url character varying(255) NOT NULL,
    timeout_ms integer NOT NULL DEFAULT 30000,
    ativo boolean NOT NULL DEFAULT TRUE,
    created_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    updated_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    disabled_at timestamp without time zone,
    CONSTRAINT integration_pkey PRIMARY KEY (id_integration),
    CONSTRAINT ck_integration_ambiente CHECK (ambiente IN ('SANDBOX', 'HOMOLOGACAO', 'PRODUCAO')),
    CONSTRAINT ck_integration_timeout CHECK (timeout_ms > 0)
);


CREATE TABLE veiculo (
    id_veiculo serial NOT NULL,
    id_categoria integer NOT NULL,
    placa character(7) NOT NULL,
    chassi character varying(17) NOT NULL,
    renavam character varying(11) NOT NULL,
    marca character varying(40) NOT NULL,
    modelo character varying(40) NOT NULL,
    ano_fabricacao smallint NOT NULL,
    ano_modelo smallint NOT NULL,
    cor character varying(30),
    km_atual integer NOT NULL DEFAULT 0,
    valor_diaria numeric(10,2) NOT NULL,
    valor_venda numeric(12,2),
    status character varying(20) NOT NULL DEFAULT 'DISPONIVEL',
    data_aquisicao date,
    created_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    updated_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    disabled_at timestamp without time zone,
    CONSTRAINT veiculo_pkey PRIMARY KEY (id_veiculo),
    CONSTRAINT ck_veiculo_ano CHECK (ano_modelo >= ano_fabricacao),
    CONSTRAINT ck_veiculo_diaria CHECK (valor_diaria > 0),
    CONSTRAINT ck_veiculo_km CHECK (km_atual >= 0),
    CONSTRAINT ck_veiculo_status CHECK (status IN ('DISPONIVEL', 'RESERVADO', 'LOCADO', 'MANUTENCAO', 'VENDIDO', 'BAIXADO')),
    CONSTRAINT fk_veiculo_categoria FOREIGN KEY (id_categoria) REFERENCES categoria (id_categoria)
);


CREATE TABLE integration_setting (
    id_setting serial NOT NULL,
    id_integration integer NOT NULL,
    chave character varying(60) NOT NULL,
    valor text NOT NULL,
    protegido boolean NOT NULL DEFAULT FALSE,
    created_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    updated_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    disabled_at timestamp without time zone,
    CONSTRAINT integration_setting_pkey PRIMARY KEY (id_setting),
    CONSTRAINT fk_setting_integration FOREIGN KEY (id_integration) REFERENCES integration (id_integration) ON DELETE CASCADE
);


CREATE TABLE manutencao (
    id_manutencao serial NOT NULL,
    id_veiculo integer NOT NULL,
    tipo character varying(15) NOT NULL,
    data_entrada date NOT NULL,
    data_saida date,
    km_registrado integer NOT NULL,
    oficina character varying(100),
    descricao character varying(200),
    custo numeric(10,2),
    created_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    updated_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    disabled_at timestamp without time zone,
    CONSTRAINT manutencao_pkey PRIMARY KEY (id_manutencao),
    CONSTRAINT ck_manutencao_periodo CHECK (data_saida IS NULL OR data_saida >= data_entrada),
    CONSTRAINT ck_manutencao_tipo CHECK (tipo IN ('PREVENTIVA', 'CORRETIVA')),
    CONSTRAINT fk_manutencao_veiculo FOREIGN KEY (id_veiculo) REFERENCES veiculo (id_veiculo)
);


CREATE TABLE reserva (
    id_reserva serial NOT NULL,
    id_cliente integer NOT NULL,
    id_veiculo integer NOT NULL,
    data_reserva timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    data_retirada_prevista timestamp without time zone NOT NULL,
    data_devolucao_prevista timestamp without time zone NOT NULL,
    valor_estimado numeric(10,2),
    status character varying(20) NOT NULL DEFAULT 'ATIVA',
    created_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    updated_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    disabled_at timestamp without time zone,
    CONSTRAINT reserva_pkey PRIMARY KEY (id_reserva),
    CONSTRAINT ck_reserva_periodo CHECK (data_devolucao_prevista > data_retirada_prevista),
    CONSTRAINT ck_reserva_status CHECK (status IN ('ATIVA', 'CONFIRMADA', 'CANCELADA', 'CONCLUIDA')),
    CONSTRAINT fk_reserva_cliente FOREIGN KEY (id_cliente) REFERENCES cliente (id_cliente),
    CONSTRAINT fk_reserva_veiculo FOREIGN KEY (id_veiculo) REFERENCES veiculo (id_veiculo)
);


CREATE TABLE seguro (
    id_seguro serial NOT NULL,
    id_veiculo integer NOT NULL,
    seguradora character varying(100) NOT NULL,
    numero_apolice character varying(30) NOT NULL,
    data_inicio date NOT NULL,
    data_fim date NOT NULL,
    valor_premio numeric(10,2) NOT NULL,
    cobertura character varying(200),
    created_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    updated_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    disabled_at timestamp without time zone,
    CONSTRAINT seguro_pkey PRIMARY KEY (id_seguro),
    CONSTRAINT ck_seguro_vigencia CHECK (data_fim > data_inicio),
    CONSTRAINT fk_seguro_veiculo FOREIGN KEY (id_veiculo) REFERENCES veiculo (id_veiculo)
);


CREATE TABLE venda (
    id_venda serial NOT NULL,
    id_veiculo integer NOT NULL,
    id_cliente integer NOT NULL,
    id_funcionario integer NOT NULL,
    data_venda date NOT NULL DEFAULT (CURRENT_DATE),
    valor_venda numeric(12,2) NOT NULL,
    forma_pagamento character varying(20) NOT NULL,
    numero_nota_fiscal character varying(20),
    status_documentacao character varying(20) NOT NULL DEFAULT 'PENDENTE',
    created_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    updated_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    disabled_at timestamp without time zone,
    CONSTRAINT venda_pkey PRIMARY KEY (id_venda),
    CONSTRAINT ck_venda_doc CHECK (status_documentacao IN ('PENDENTE', 'EM_TRANSFERENCIA', 'CONCLUIDA')),
    CONSTRAINT ck_venda_forma CHECK (forma_pagamento IN ('A_VISTA', 'FINANCIAMENTO', 'CONSORCIO')),
    CONSTRAINT ck_venda_valor CHECK (valor_venda > 0),
    CONSTRAINT fk_venda_cliente FOREIGN KEY (id_cliente) REFERENCES cliente (id_cliente),
    CONSTRAINT fk_venda_funcionario FOREIGN KEY (id_funcionario) REFERENCES funcionario (id_funcionario),
    CONSTRAINT fk_venda_veiculo FOREIGN KEY (id_veiculo) REFERENCES veiculo (id_veiculo)
);


CREATE TABLE locacao (
    id_locacao serial NOT NULL,
    id_reserva integer,
    id_cliente integer NOT NULL,
    id_veiculo integer NOT NULL,
    id_funcionario integer NOT NULL,
    data_retirada timestamp without time zone NOT NULL,
    data_devolucao_prevista timestamp without time zone NOT NULL,
    data_devolucao_efetiva timestamp without time zone,
    km_saida integer NOT NULL,
    km_chegada integer,
    valor_diaria numeric(10,2) NOT NULL,
    valor_caucao numeric(10,2) NOT NULL DEFAULT (0),
    valor_total numeric(12,2),
    status character varying(20) NOT NULL DEFAULT 'EM_ANDAMENTO',
    created_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    updated_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    disabled_at timestamp without time zone,
    CONSTRAINT locacao_pkey PRIMARY KEY (id_locacao),
    CONSTRAINT ck_locacao_devolucao CHECK (data_devolucao_efetiva IS NULL OR data_devolucao_efetiva >= data_retirada),
    CONSTRAINT ck_locacao_km CHECK (km_chegada IS NULL OR km_chegada >= km_saida),
    CONSTRAINT ck_locacao_status CHECK (status IN ('EM_ANDAMENTO', 'FINALIZADA', 'ATRASADA', 'CANCELADA')),
    CONSTRAINT fk_locacao_cliente FOREIGN KEY (id_cliente) REFERENCES cliente (id_cliente),
    CONSTRAINT fk_locacao_funcionario FOREIGN KEY (id_funcionario) REFERENCES funcionario (id_funcionario),
    CONSTRAINT fk_locacao_reserva FOREIGN KEY (id_reserva) REFERENCES reserva (id_reserva),
    CONSTRAINT fk_locacao_veiculo FOREIGN KEY (id_veiculo) REFERENCES veiculo (id_veiculo)
);


CREATE TABLE cobranca_extra (
    id_cobranca serial NOT NULL,
    id_locacao integer NOT NULL,
    tipo character varying(20) NOT NULL,
    descricao character varying(200),
    valor numeric(10,2) NOT NULL,
    data_lancamento date NOT NULL DEFAULT (CURRENT_DATE),
    created_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    updated_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    disabled_at timestamp without time zone,
    CONSTRAINT cobranca_extra_pkey PRIMARY KEY (id_cobranca),
    CONSTRAINT ck_cobranca_tipo CHECK (tipo IN ('MULTA_TRANSITO', 'KM_EXCEDENTE', 'ATRASO', 'AVARIA', 'COMBUSTIVEL', 'LIMPEZA')),
    CONSTRAINT ck_cobranca_valor CHECK (valor >= 0),
    CONSTRAINT fk_cobranca_locacao FOREIGN KEY (id_locacao) REFERENCES locacao (id_locacao) ON DELETE CASCADE
);


CREATE TABLE pagamento (
    id_pagamento serial NOT NULL,
    id_locacao integer,
    id_venda integer,
    tipo character varying(20) NOT NULL,
    valor numeric(12,2) NOT NULL,
    data_vencimento date NOT NULL,
    data_pagamento date,
    forma character varying(20),
    status character varying(15) NOT NULL DEFAULT 'PENDENTE',
    created_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    updated_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    disabled_at timestamp without time zone,
    CONSTRAINT pagamento_pkey PRIMARY KEY (id_pagamento),
    CONSTRAINT ck_pagamento_origem CHECK ((id_locacao IS NOT NULL AND id_venda IS NULL) OR (id_locacao IS NULL AND id_venda IS NOT NULL)),
    CONSTRAINT ck_pagamento_status CHECK (status IN ('PENDENTE', 'PAGO', 'ATRASADO', 'CANCELADO')),
    CONSTRAINT ck_pagamento_tipo CHECK (tipo IN ('CAUCAO', 'DIARIA', 'COBRANCA_EXTRA', 'VENDA')),
    CONSTRAINT ck_pagamento_valor CHECK (valor > 0),
    CONSTRAINT fk_pagamento_locacao FOREIGN KEY (id_locacao) REFERENCES locacao (id_locacao),
    CONSTRAINT fk_pagamento_venda FOREIGN KEY (id_venda) REFERENCES venda (id_venda)
);


CREATE TABLE sinistro (
    id_sinistro serial NOT NULL,
    id_veiculo integer NOT NULL,
    id_seguro integer,
    id_locacao integer,
    data_ocorrencia date NOT NULL,
    tipo character varying(20) NOT NULL,
    descricao text,
    valor_prejuizo numeric(12,2),
    status character varying(20) NOT NULL DEFAULT 'ABERTO',
    created_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    updated_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    disabled_at timestamp without time zone,
    CONSTRAINT sinistro_pkey PRIMARY KEY (id_sinistro),
    CONSTRAINT ck_sinistro_status CHECK (status IN ('ABERTO', 'EM_ANALISE', 'APROVADO', 'NEGADO', 'ENCERRADO')),
    CONSTRAINT ck_sinistro_tipo CHECK (tipo IN ('COLISAO', 'ROUBO', 'FURTO', 'INCENDIO', 'FENOMENO_NATURAL', 'TERCEIROS')),
    CONSTRAINT fk_sinistro_locacao FOREIGN KEY (id_locacao) REFERENCES locacao (id_locacao),
    CONSTRAINT fk_sinistro_seguro FOREIGN KEY (id_seguro) REFERENCES seguro (id_seguro),
    CONSTRAINT fk_sinistro_veiculo FOREIGN KEY (id_veiculo) REFERENCES veiculo (id_veiculo)
);


CREATE TABLE vistoria (
    id_vistoria serial NOT NULL,
    id_locacao integer NOT NULL,
    id_funcionario integer NOT NULL,
    tipo character varying(10) NOT NULL,
    data_hora timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    km_registrado integer NOT NULL,
    nivel_combustivel numeric(4,2),
    observacoes text,
    created_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    updated_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    disabled_at timestamp without time zone,
    CONSTRAINT vistoria_pkey PRIMARY KEY (id_vistoria),
    CONSTRAINT ck_vistoria_combustivel CHECK (nivel_combustivel BETWEEN 0 AND 1),
    CONSTRAINT ck_vistoria_tipo CHECK (tipo IN ('ENTRADA', 'SAIDA')),
    CONSTRAINT fk_vistoria_funcionario FOREIGN KEY (id_funcionario) REFERENCES funcionario (id_funcionario),
    CONSTRAINT fk_vistoria_locacao FOREIGN KEY (id_locacao) REFERENCES locacao (id_locacao) ON DELETE CASCADE
);


CREATE TABLE avaria (
    id_avaria serial NOT NULL,
    id_vistoria integer NOT NULL,
    descricao character varying(200) NOT NULL,
    gravidade character varying(15) NOT NULL DEFAULT 'LEVE',
    valor_estimado numeric(10,2),
    created_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    updated_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    disabled_at timestamp without time zone,
    CONSTRAINT avaria_pkey PRIMARY KEY (id_avaria),
    CONSTRAINT ck_avaria_gravidade CHECK (gravidade IN ('LEVE', 'MEDIA', 'GRAVE')),
    CONSTRAINT fk_avaria_vistoria FOREIGN KEY (id_vistoria) REFERENCES vistoria (id_vistoria) ON DELETE CASCADE
);


CREATE INDEX idx_categoria_nome ON categoria (nome);


CREATE INDEX idx_cliente_cnh_numero ON cliente (cnh_numero);


CREATE INDEX idx_cliente_cpf ON cliente (cpf);


CREATE INDEX idx_funcionario_cpf ON funcionario (cpf);


CREATE INDEX idx_funcionario_matricula ON funcionario (matricula);


CREATE INDEX idx_integration_codigo ON integration (codigo);


CREATE INDEX idx_setting_chave ON integration_setting (id_integration, chave);


CREATE INDEX idx_setting_integration ON integration_setting (id_integration);


CREATE INDEX idx_locacao_cliente ON locacao (id_cliente);


CREATE INDEX idx_locacao_reserva ON locacao (id_reserva);


CREATE INDEX idx_locacao_status ON locacao (status);


CREATE INDEX idx_locacao_veiculo ON locacao (id_veiculo);


CREATE INDEX idx_manutencao_veiculo ON manutencao (id_veiculo, data_entrada);


CREATE INDEX idx_pagamento_status ON pagamento (status, data_vencimento);


CREATE INDEX idx_reserva_cliente ON reserva (id_cliente);


CREATE INDEX idx_reserva_periodo ON reserva (data_retirada_prevista, data_devolucao_prevista);


CREATE INDEX idx_seguro_numero_apolice ON seguro (numero_apolice);


CREATE INDEX idx_sinistro_veiculo ON sinistro (id_veiculo, data_ocorrencia);


CREATE INDEX idx_veiculo_categoria ON veiculo (id_categoria);


CREATE INDEX idx_veiculo_chassi ON veiculo (chassi);


CREATE INDEX idx_veiculo_placa ON veiculo (placa);


CREATE INDEX idx_veiculo_renavam ON veiculo (renavam);


CREATE INDEX idx_veiculo_status ON veiculo (status);


CREATE INDEX idx_venda_numero_nota_fiscal ON venda (numero_nota_fiscal);


CREATE INDEX idx_venda_veiculo ON venda (id_veiculo);


CREATE INDEX idx_vistoria_locacao_tipo ON vistoria (id_locacao, tipo);


