START TRANSACTION;
CREATE TABLE proposta_compra (
    id_proposta_compra serial NOT NULL,
    id_veiculo integer NOT NULL,
    id_cliente integer NOT NULL,
    valor_venda numeric(12,2) NOT NULL,
    valor_entrada numeric(12,2) NOT NULL,
    numero_parcelas integer NOT NULL,
    valor_parcela numeric(12,2) NOT NULL,
    valor_ultima_parcela numeric(12,2) NOT NULL,
    status character varying(20) NOT NULL DEFAULT 'ENVIADA',
    created_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    updated_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    disabled_at timestamp without time zone,
    CONSTRAINT proposta_compra_pkey PRIMARY KEY (id_proposta_compra),
    CONSTRAINT ck_proposta_compra_entrada CHECK (valor_entrada >= 0 AND valor_entrada < valor_venda),
    CONSTRAINT ck_proposta_compra_parcela CHECK (valor_parcela > 0 AND valor_ultima_parcela >= valor_parcela),
    CONSTRAINT ck_proposta_compra_parcelas CHECK (numero_parcelas BETWEEN 1 AND 60),
    CONSTRAINT ck_proposta_compra_status CHECK (status IN ('ENVIADA', 'ACEITA', 'RECUSADA', 'CANCELADA')),
    CONSTRAINT ck_proposta_compra_valor CHECK (valor_venda > 0),
    CONSTRAINT fk_proposta_compra_cliente FOREIGN KEY (id_cliente) REFERENCES cliente (id_cliente),
    CONSTRAINT fk_proposta_compra_veiculo FOREIGN KEY (id_veiculo) REFERENCES veiculo (id_veiculo)
);

CREATE INDEX idx_proposta_compra_cliente ON proposta_compra (id_cliente);

CREATE INDEX idx_proposta_compra_veiculo ON proposta_compra (id_veiculo);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261121151200_AdicionarPropostaCompra', '10.0.14');

COMMIT;

