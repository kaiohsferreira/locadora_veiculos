START TRANSACTION;
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

CREATE INDEX idx_veiculo_foto_veiculo ON veiculo_foto (id_veiculo, ordem);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261116233935_AdicionarVeiculoFoto', '10.0.14');

COMMIT;

