START TRANSACTION;
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

CREATE INDEX idx_cliente_locadora_cliente ON cliente_locadora (id_cliente);

CREATE INDEX idx_cliente_locadora_empresa_cliente ON cliente_locadora (id_empresa, id_cliente);


INSERT INTO cliente_locadora (id_cliente, id_empresa, status)
SELECT c.id_cliente, v.id_empresa, c.status_cadastro
FROM cliente c
JOIN (SELECT id_cliente, id_veiculo FROM reserva WHERE disabled_at IS NULL
      UNION SELECT id_cliente, id_veiculo FROM locacao WHERE disabled_at IS NULL
      UNION SELECT id_cliente, id_veiculo FROM venda WHERE disabled_at IS NULL) n ON n.id_cliente = c.id_cliente
JOIN veiculo v ON v.id_veiculo = n.id_veiculo
WHERE c.disabled_at IS NULL
  AND c.status_cadastro IN ('APROVADO', 'RECUSADO')
GROUP BY c.id_cliente, v.id_empresa, c.status_cadastro;


INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261114202316_AdicionarClienteLocadora', '10.0.14');

COMMIT;

