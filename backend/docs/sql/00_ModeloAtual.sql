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
    google_sub character varying(255),
    created_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    updated_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    disabled_at timestamp without time zone,
    CONSTRAINT cliente_pkey PRIMARY KEY (id_cliente),
    CONSTRAINT ck_cliente_cnh_cat CHECK (cnh_categoria IN ('A', 'B', 'AB', 'C', 'D', 'E')),
    CONSTRAINT ck_cliente_status CHECK (status_cadastro IN ('PENDENTE', 'APROVADO', 'RECUSADO'))
);


CREATE TABLE empresa (
    id_empresa serial NOT NULL,
    razao_social character varying(150) NOT NULL,
    nome_fantasia character varying(100) NOT NULL,
    cnpj character(14) NOT NULL,
    telefone character varying(20) NOT NULL,
    email character varying(120) NOT NULL,
    cep character(8) NOT NULL,
    logradouro character varying(120) NOT NULL,
    numero character varying(10) NOT NULL,
    complemento character varying(60),
    bairro character varying(60) NOT NULL,
    cidade character varying(60) NOT NULL,
    uf character(2) NOT NULL,
    ativo boolean NOT NULL DEFAULT TRUE,
    operadora boolean NOT NULL DEFAULT FALSE,
    created_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    updated_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    disabled_at timestamp without time zone,
    CONSTRAINT empresa_pkey PRIMARY KEY (id_empresa),
    CONSTRAINT ck_empresa_cnpj CHECK (cnpj ~ '^[0-9]{14}$')
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
    id_empresa integer NOT NULL,
    created_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    updated_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    disabled_at timestamp without time zone,
    CONSTRAINT funcionario_pkey PRIMARY KEY (id_funcionario),
    CONSTRAINT ck_funcionario_cargo CHECK (cargo IN ('ATENDENTE', 'VENDEDOR', 'GESTOR_FROTA', 'FINANCEIRO', 'ADMINISTRATIVO', 'ADMINISTRADOR')),
    CONSTRAINT fk_funcionario_empresa FOREIGN KEY (id_empresa) REFERENCES empresa (id_empresa)
);


CREATE TABLE veiculo (
    id_veiculo serial NOT NULL,
    id_categoria integer NOT NULL,
    placa character(7) NOT NULL,
    chassi character varying(17) NOT NULL,
    renavam character varying(11) NOT NULL,
    marca character varying(40) NOT NULL,
    modelo character varying(60) NOT NULL,
    ano_fabricacao smallint NOT NULL,
    ano_modelo smallint NOT NULL,
    cor character varying(30),
    km_atual integer NOT NULL DEFAULT 0,
    valor_diaria numeric(10,2) NOT NULL,
    valor_venda numeric(12,2),
    status character varying(20) NOT NULL DEFAULT 'DISPONIVEL',
    data_aquisicao date,
    fornecedor character varying(100),
    numero_nota_fiscal_compra character varying(20),
    id_empresa integer NOT NULL,
    created_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    updated_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    disabled_at timestamp without time zone,
    CONSTRAINT veiculo_pkey PRIMARY KEY (id_veiculo),
    CONSTRAINT ck_veiculo_ano CHECK (ano_modelo >= ano_fabricacao),
    CONSTRAINT ck_veiculo_diaria CHECK (valor_diaria > 0),
    CONSTRAINT ck_veiculo_km CHECK (km_atual >= 0),
    CONSTRAINT ck_veiculo_status CHECK (status IN ('DISPONIVEL', 'RESERVADO', 'LOCADO', 'MANUTENCAO', 'VENDIDO', 'BAIXADO')),
    CONSTRAINT fk_veiculo_categoria FOREIGN KEY (id_categoria) REFERENCES categoria (id_categoria),
    CONSTRAINT fk_veiculo_empresa FOREIGN KEY (id_empresa) REFERENCES empresa (id_empresa)
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


CREATE TABLE cliente_locadora (
    id_cliente_locadora serial NOT NULL,
    id_cliente integer NOT NULL,
    id_empresa integer NOT NULL,
    status character varying(15) NOT NULL DEFAULT 'PENDENTE',
    justificativa_recusa character varying(500),
    id_funcionario_verificador integer,
    data_solicitacao timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    data_verificacao timestamp without time zone,
    created_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    updated_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    disabled_at timestamp without time zone,
    CONSTRAINT cliente_locadora_pkey PRIMARY KEY (id_cliente_locadora),
    CONSTRAINT ck_cliente_locadora_status CHECK (status IN ('PENDENTE', 'APROVADO', 'RECUSADO')),
    CONSTRAINT fk_cliente_locadora_cliente FOREIGN KEY (id_cliente) REFERENCES cliente (id_cliente),
    CONSTRAINT fk_cliente_locadora_empresa FOREIGN KEY (id_empresa) REFERENCES empresa (id_empresa),
    CONSTRAINT fk_cliente_locadora_funcionario FOREIGN KEY (id_funcionario_verificador) REFERENCES funcionario (id_funcionario)
);


CREATE TABLE redefinicao_senha (
    id_redefinicao_senha serial NOT NULL,
    id_cliente integer,
    id_funcionario integer,
    token_hash character(64) NOT NULL,
    expira_em timestamp without time zone NOT NULL,
    created_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    updated_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    disabled_at timestamp without time zone,
    CONSTRAINT redefinicao_senha_pkey PRIMARY KEY (id_redefinicao_senha),
    CONSTRAINT ck_redefinicao_titular CHECK ((id_cliente IS NOT NULL AND id_funcionario IS NULL) OR (id_cliente IS NULL AND id_funcionario IS NOT NULL)),
    CONSTRAINT fk_redefinicao_cliente FOREIGN KEY (id_cliente) REFERENCES cliente (id_cliente),
    CONSTRAINT fk_redefinicao_funcionario FOREIGN KEY (id_funcionario) REFERENCES funcionario (id_funcionario)
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


CREATE TABLE cliente_documento (
    id_cliente_documento serial NOT NULL,
    id_cliente integer NOT NULL,
    tipo character varying(30) NOT NULL,
    chave_objeto character varying(200) NOT NULL,
    nome_arquivo character varying(120) NOT NULL,
    tipo_conteudo character varying(50) NOT NULL,
    tamanho_bytes integer NOT NULL,
    created_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    updated_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    disabled_at timestamp without time zone,
    CONSTRAINT cliente_documento_pkey PRIMARY KEY (id_cliente_documento),
    CONSTRAINT ck_cliente_documento_tipo CHECK (tipo IN ('CNH', 'COMPROVANTE_RESIDENCIA')),
    CONSTRAINT fk_cliente_documento_cliente FOREIGN KEY (id_cliente) REFERENCES cliente (id_cliente)
);


CREATE TABLE veiculo_foto (
    id_veiculo_foto serial NOT NULL,
    id_veiculo integer NOT NULL,
    ordem integer NOT NULL,
    chave_objeto character varying(200) NOT NULL,
    chave_miniatura character varying(200),
    id_funcionario_envio integer NOT NULL,
    id_funcionario_remocao integer,
    created_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    updated_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    disabled_at timestamp without time zone,
    CONSTRAINT veiculo_foto_pkey PRIMARY KEY (id_veiculo_foto),
    CONSTRAINT ck_veiculo_foto_ordem CHECK (ordem >= 1),
    CONSTRAINT fk_veiculo_foto_funcionario_envio FOREIGN KEY (id_funcionario_envio) REFERENCES funcionario (id_funcionario),
    CONSTRAINT fk_veiculo_foto_funcionario_remocao FOREIGN KEY (id_funcionario_remocao) REFERENCES funcionario (id_funcionario),
    CONSTRAINT fk_veiculo_foto_veiculo FOREIGN KEY (id_veiculo) REFERENCES veiculo (id_veiculo)
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
    id_transacao character varying(40),
    qr_code_base64 text,
    pix_copia_e_cola character varying(512),
    pix_expira_em timestamp without time zone,
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
    protocolo_seguradora character varying(40),
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
    comparacao_realizada boolean NOT NULL DEFAULT FALSE,
    created_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    updated_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    disabled_at timestamp without time zone,
    CONSTRAINT vistoria_pkey PRIMARY KEY (id_vistoria),
    CONSTRAINT ck_vistoria_combustivel CHECK (nivel_combustivel BETWEEN 0 AND 1),
    CONSTRAINT ck_vistoria_tipo CHECK (tipo IN ('ENTRADA', 'SAIDA')),
    CONSTRAINT fk_vistoria_funcionario FOREIGN KEY (id_funcionario) REFERENCES funcionario (id_funcionario),
    CONSTRAINT fk_vistoria_locacao FOREIGN KEY (id_locacao) REFERENCES locacao (id_locacao) ON DELETE CASCADE
);


CREATE TABLE sinistro_historico (
    id_sinistro_historico serial NOT NULL,
    id_sinistro integer NOT NULL,
    status_anterior character varying(20),
    status_novo character varying(20) NOT NULL,
    origem character varying(20) NOT NULL,
    id_funcionario integer,
    observacao character varying(500),
    data_alteracao timestamp without time zone NOT NULL,
    created_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    updated_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    disabled_at timestamp without time zone,
    CONSTRAINT sinistro_historico_pkey PRIMARY KEY (id_sinistro_historico),
    CONSTRAINT ck_sinistro_historico_funcionario CHECK (origem <> 'FUNCIONARIO' OR id_funcionario IS NOT NULL),
    CONSTRAINT ck_sinistro_historico_origem CHECK (origem IN ('FUNCIONARIO', 'SEGURADORA')),
    CONSTRAINT ck_sinistro_historico_status_anterior CHECK (status_anterior IS NULL OR status_anterior IN ('ABERTO', 'EM_ANALISE', 'APROVADO', 'NEGADO', 'ENCERRADO')),
    CONSTRAINT ck_sinistro_historico_status_novo CHECK (status_novo IN ('ABERTO', 'EM_ANALISE', 'APROVADO', 'NEGADO', 'ENCERRADO')),
    CONSTRAINT fk_sinistro_historico_funcionario FOREIGN KEY (id_funcionario) REFERENCES funcionario (id_funcionario),
    CONSTRAINT fk_sinistro_historico_sinistro FOREIGN KEY (id_sinistro) REFERENCES sinistro (id_sinistro)
);


CREATE TABLE avaria (
    id_avaria serial NOT NULL,
    id_vistoria integer NOT NULL,
    descricao character varying(200) NOT NULL,
    gravidade character varying(15) NOT NULL DEFAULT 'LEVE',
    valor_estimado numeric(10,2),
    angulo character varying(20),
    pos_x numeric(5,2),
    pos_y numeric(5,2),
    id_avaria_entrada integer,
    created_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    updated_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    disabled_at timestamp without time zone,
    CONSTRAINT avaria_pkey PRIMARY KEY (id_avaria),
    CONSTRAINT ck_avaria_angulo CHECK (angulo IS NULL OR angulo IN ('FRENTE', 'TRASEIRA', 'LATERAL_ESQUERDA', 'LATERAL_DIREITA')),
    CONSTRAINT ck_avaria_gravidade CHECK (gravidade IN ('LEVE', 'MEDIA', 'GRAVE')),
    CONSTRAINT ck_avaria_pos_x CHECK (pos_x IS NULL OR (pos_x >= 0 AND pos_x <= 100)),
    CONSTRAINT ck_avaria_pos_y CHECK (pos_y IS NULL OR (pos_y >= 0 AND pos_y <= 100)),
    CONSTRAINT fk_avaria_entrada FOREIGN KEY (id_avaria_entrada) REFERENCES avaria (id_avaria),
    CONSTRAINT fk_avaria_vistoria FOREIGN KEY (id_vistoria) REFERENCES vistoria (id_vistoria) ON DELETE CASCADE
);


CREATE TABLE configuracao_cobranca_extra (
    id_configuracao serial NOT NULL,
    id_empresa integer NOT NULL,
    id_categoria integer NOT NULL,
    versao integer NOT NULL,
    base_franquia character varying(20) NOT NULL,
    franquia_km integer NOT NULL,
    valor_km_excedente numeric(10,2) NOT NULL,
    valor_tanque_completo numeric(10,2) NOT NULL,
    tolerancia_atraso_minutos integer NOT NULL,
    unidade_atraso character varying(10) NOT NULL,
    fracionamento_atraso character varying(20) NOT NULL,
    valor_unidade_atraso numeric(10,2) NOT NULL,
    base_avarias character varying(30) NOT NULL,
    created_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    updated_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    disabled_at timestamp without time zone,
    CONSTRAINT configuracao_cobranca_extra_pkey PRIMARY KEY (id_configuracao),
    CONSTRAINT ck_configuracao_extra_atraso CHECK (tolerancia_atraso_minutos >= 0 AND unidade_atraso IN ('HORA', 'DIARIA') AND fracionamento_atraso IN ('PROPORCIONAL', 'INICIADA')),
    CONSTRAINT ck_configuracao_extra_avarias CHECK (base_avarias = 'ESTIMATIVA_VISTORIA'),
    CONSTRAINT ck_configuracao_extra_franquia CHECK (franquia_km >= 0 AND base_franquia IN ('TOTAL', 'POR_DIARIA')),
    CONSTRAINT ck_configuracao_extra_tarifas CHECK (valor_km_excedente > 0 AND valor_tanque_completo > 0 AND valor_unidade_atraso > 0),
    CONSTRAINT ck_configuracao_extra_versao CHECK (versao > 0),
    CONSTRAINT fk_configuracao_extra_categoria FOREIGN KEY (id_categoria) REFERENCES categoria (id_categoria),
    CONSTRAINT fk_configuracao_extra_empresa FOREIGN KEY (id_empresa) REFERENCES empresa (id_empresa)
);


CREATE TABLE apuracao_cobranca_extra (
    id_apuracao serial NOT NULL,
    id_locacao integer NOT NULL,
    id_configuracao integer NOT NULL,
    versao_configuracao integer NOT NULL,
    id_vistoria_entrada integer NOT NULL,
    id_vistoria_saida integer NOT NULL,
    id_funcionario integer NOT NULL,
    data_hora timestamp without time zone NOT NULL,
    versao_snapshot integer NOT NULL,
    snapshot_json jsonb NOT NULL,
    total numeric(12,2) NOT NULL,
    created_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    updated_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    disabled_at timestamp without time zone,
    CONSTRAINT apuracao_cobranca_extra_pkey PRIMARY KEY (id_apuracao),
    CONSTRAINT ck_apuracao_extra_total CHECK (total >= 0),
    CONSTRAINT ck_apuracao_extra_versoes CHECK (versao_configuracao > 0 AND versao_snapshot > 0),
    CONSTRAINT fk_apuracao_extra_configuracao FOREIGN KEY (id_configuracao) REFERENCES configuracao_cobranca_extra (id_configuracao),
    CONSTRAINT fk_apuracao_extra_entrada FOREIGN KEY (id_vistoria_entrada) REFERENCES vistoria (id_vistoria),
    CONSTRAINT fk_apuracao_extra_funcionario FOREIGN KEY (id_funcionario) REFERENCES funcionario (id_funcionario),
    CONSTRAINT fk_apuracao_extra_locacao FOREIGN KEY (id_locacao) REFERENCES locacao (id_locacao),
    CONSTRAINT fk_apuracao_extra_saida FOREIGN KEY (id_vistoria_saida) REFERENCES vistoria (id_vistoria)
);


CREATE TABLE cobranca_extra (
    id_cobranca serial NOT NULL,
    id_locacao integer NOT NULL,
    tipo character varying(20) NOT NULL,
    descricao character varying(200),
    valor numeric(10,2) NOT NULL,
    data_lancamento date NOT NULL DEFAULT (CURRENT_DATE),
    id_apuracao integer,
    created_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    updated_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    disabled_at timestamp without time zone,
    CONSTRAINT cobranca_extra_pkey PRIMARY KEY (id_cobranca),
    CONSTRAINT ck_cobranca_tipo CHECK (tipo IN ('MULTA_TRANSITO', 'KM_EXCEDENTE', 'ATRASO', 'AVARIA', 'COMBUSTIVEL', 'LIMPEZA')),
    CONSTRAINT ck_cobranca_valor CHECK (valor >= 0),
    CONSTRAINT fk_cobranca_apuracao FOREIGN KEY (id_apuracao) REFERENCES apuracao_cobranca_extra (id_apuracao),
    CONSTRAINT fk_cobranca_locacao FOREIGN KEY (id_locacao) REFERENCES locacao (id_locacao) ON DELETE CASCADE
);


CREATE INDEX idx_apuracao_extra_locacao ON apuracao_cobranca_extra (id_locacao);


CREATE INDEX idx_avaria_entrada ON avaria (id_avaria_entrada);


CREATE INDEX idx_categoria_nome ON categoria (nome);


CREATE INDEX idx_cliente_cnh_numero ON cliente (cnh_numero);


CREATE INDEX idx_cliente_cpf ON cliente (cpf);


CREATE INDEX idx_cliente_documento_cliente_tipo ON cliente_documento (id_cliente, tipo);


CREATE INDEX idx_cliente_google_sub ON cliente (google_sub);


CREATE INDEX idx_cliente_locadora_cliente ON cliente_locadora (id_cliente);


CREATE INDEX idx_cliente_locadora_empresa_cliente ON cliente_locadora (id_empresa, id_cliente);


CREATE INDEX idx_cobranca_apuracao ON cobranca_extra (id_apuracao);


CREATE INDEX idx_configuracao_extra_empresa_categoria ON configuracao_cobranca_extra (id_empresa, id_categoria);


CREATE INDEX idx_empresa_cnpj ON empresa (cnpj);


CREATE INDEX idx_empresa_email ON empresa (email);


CREATE INDEX idx_funcionario_cpf ON funcionario (cpf);


CREATE INDEX idx_funcionario_empresa ON funcionario (id_empresa);


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


CREATE INDEX idx_redefinicao_token ON redefinicao_senha (token_hash);


CREATE INDEX idx_reserva_cliente ON reserva (id_cliente);


CREATE INDEX idx_reserva_periodo ON reserva (data_retirada_prevista, data_devolucao_prevista);


CREATE INDEX idx_seguro_numero_apolice ON seguro (numero_apolice);


CREATE INDEX idx_sinistro_veiculo ON sinistro (id_veiculo, data_ocorrencia);


CREATE INDEX idx_sinistro_historico_sinistro ON sinistro_historico (id_sinistro, data_alteracao);


CREATE INDEX idx_veiculo_categoria ON veiculo (id_categoria);


CREATE INDEX idx_veiculo_chassi ON veiculo (chassi);


CREATE INDEX idx_veiculo_empresa ON veiculo (id_empresa);


CREATE INDEX idx_veiculo_placa ON veiculo (placa);


CREATE INDEX idx_veiculo_renavam ON veiculo (renavam);


CREATE INDEX idx_veiculo_status ON veiculo (status);


CREATE INDEX idx_veiculo_foto_veiculo ON veiculo_foto (id_veiculo, ordem);


CREATE INDEX idx_venda_numero_nota_fiscal ON venda (numero_nota_fiscal);


CREATE INDEX idx_venda_veiculo ON venda (id_veiculo);


CREATE INDEX idx_vistoria_locacao_tipo ON vistoria (id_locacao, tipo);


