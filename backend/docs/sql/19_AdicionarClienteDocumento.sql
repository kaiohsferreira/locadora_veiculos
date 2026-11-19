START TRANSACTION;
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

CREATE INDEX idx_cliente_documento_cliente_tipo ON cliente_documento (id_cliente, tipo);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261119142116_AdicionarClienteDocumento', '10.0.14');

COMMIT;
