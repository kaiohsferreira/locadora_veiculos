START TRANSACTION;
ALTER TABLE cobranca_extra ADD id_apuracao integer;

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

CREATE INDEX idx_cobranca_apuracao ON cobranca_extra (id_apuracao);

CREATE INDEX idx_apuracao_extra_locacao ON apuracao_cobranca_extra (id_locacao);

CREATE INDEX idx_configuracao_extra_empresa_categoria ON configuracao_cobranca_extra (id_empresa, id_categoria);

ALTER TABLE cobranca_extra ADD CONSTRAINT fk_cobranca_apuracao FOREIGN KEY (id_apuracao) REFERENCES apuracao_cobranca_extra (id_apuracao);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261119023757_CadastrarRegrasEApurarCobrancasExtras', '10.0.14');

COMMIT;
