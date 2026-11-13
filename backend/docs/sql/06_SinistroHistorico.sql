START TRANSACTION;
ALTER TABLE sinistro ADD protocolo_seguradora character varying(40);

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

CREATE INDEX idx_sinistro_historico_sinistro ON sinistro_historico (id_sinistro, data_alteracao);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261113164956_SinistroHistorico', '10.0.14');

COMMIT;

