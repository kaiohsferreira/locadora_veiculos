START TRANSACTION;
ALTER TABLE cliente ADD google_sub character varying(255);

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

CREATE INDEX idx_cliente_google_sub ON cliente (google_sub);

CREATE INDEX idx_redefinicao_token ON redefinicao_senha (token_hash);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261112050622_AdicionarRecuperacaoSenhaEVinculoGoogle', '10.0.14');

COMMIT;

