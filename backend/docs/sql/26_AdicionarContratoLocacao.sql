START TRANSACTION;
CREATE TABLE contrato_locacao (
    id_contrato_locacao serial NOT NULL,
    id_locacao integer NOT NULL,
    id_funcionario integer NOT NULL,
    documento_json text NOT NULL,
    hash_sha256 character varying(64) NOT NULL,
    nome_assinante character varying(100) NOT NULL,
    aceite_condicoes_gerais boolean NOT NULL,
    aceite_politica_combustivel boolean NOT NULL,
    assinatura_png_base64 text NOT NULL,
    assinado_em timestamp without time zone NOT NULL,
    created_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    updated_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    disabled_at timestamp without time zone,
    CONSTRAINT contrato_locacao_pkey PRIMARY KEY (id_contrato_locacao),
    CONSTRAINT fk_contrato_locacao_funcionario FOREIGN KEY (id_funcionario) REFERENCES funcionario (id_funcionario),
    CONSTRAINT fk_contrato_locacao_locacao FOREIGN KEY (id_locacao) REFERENCES locacao (id_locacao)
);

CREATE INDEX idx_contrato_locacao_locacao ON contrato_locacao (id_locacao);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261126225459_AdicionarContratoLocacao', '10.0.14');

COMMIT;

