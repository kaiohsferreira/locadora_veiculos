START TRANSACTION;
DROP INDEX uq_vistoria_locacao_tipo;

DROP INDEX venda_id_veiculo_key;

DROP INDEX venda_numero_nota_fiscal_key;

DROP INDEX veiculo_chassi_key;

DROP INDEX veiculo_placa_key;

DROP INDEX veiculo_renavam_key;

DROP INDEX seguro_numero_apolice_key;

DROP INDEX locacao_id_reserva_key;

DROP INDEX uq_setting_chave;

DROP INDEX integration_codigo_key;

DROP INDEX funcionario_cpf_key;

DROP INDEX funcionario_matricula_key;

DROP INDEX cliente_cnh_numero_key;

DROP INDEX cliente_cpf_key;

DROP INDEX categoria_nome_key;

ALTER TABLE vistoria ADD created_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP);

ALTER TABLE vistoria ADD disabled_at timestamp without time zone;

ALTER TABLE vistoria ADD updated_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP);

ALTER TABLE venda ADD created_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP);

ALTER TABLE venda ADD disabled_at timestamp without time zone;

ALTER TABLE venda ADD updated_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP);

ALTER TABLE veiculo ADD created_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP);

ALTER TABLE veiculo ADD disabled_at timestamp without time zone;

ALTER TABLE veiculo ADD updated_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP);

ALTER TABLE sinistro ADD created_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP);

ALTER TABLE sinistro ADD disabled_at timestamp without time zone;

ALTER TABLE sinistro ADD updated_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP);

ALTER TABLE seguro ADD created_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP);

ALTER TABLE seguro ADD disabled_at timestamp without time zone;

ALTER TABLE seguro ADD updated_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP);

ALTER TABLE reserva ADD created_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP);

ALTER TABLE reserva ADD disabled_at timestamp without time zone;

ALTER TABLE reserva ADD updated_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP);

ALTER TABLE pagamento ADD created_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP);

ALTER TABLE pagamento ADD disabled_at timestamp without time zone;

ALTER TABLE pagamento ADD updated_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP);

ALTER TABLE manutencao ADD created_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP);

ALTER TABLE manutencao ADD disabled_at timestamp without time zone;

ALTER TABLE manutencao ADD updated_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP);

ALTER TABLE locacao ADD created_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP);

ALTER TABLE locacao ADD disabled_at timestamp without time zone;

ALTER TABLE locacao ADD updated_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP);

ALTER TABLE integration_setting ADD disabled_at timestamp without time zone;

ALTER TABLE funcionario ADD created_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP);

ALTER TABLE funcionario ADD disabled_at timestamp without time zone;

ALTER TABLE funcionario ADD updated_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP);

ALTER TABLE cobranca_extra ADD created_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP);

ALTER TABLE cobranca_extra ADD disabled_at timestamp without time zone;

ALTER TABLE cobranca_extra ADD updated_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP);

ALTER TABLE cliente ADD created_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP);

ALTER TABLE cliente ADD disabled_at timestamp without time zone;

ALTER TABLE cliente ADD updated_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP);

ALTER TABLE categoria ADD created_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP);

ALTER TABLE categoria ADD disabled_at timestamp without time zone;

ALTER TABLE categoria ADD updated_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP);

ALTER TABLE avaria ADD created_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP);

ALTER TABLE avaria ADD disabled_at timestamp without time zone;

ALTER TABLE avaria ADD updated_at timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP);

CREATE INDEX idx_vistoria_locacao_tipo ON vistoria (id_locacao, tipo);

CREATE INDEX idx_venda_numero_nota_fiscal ON venda (numero_nota_fiscal);

CREATE INDEX idx_venda_veiculo ON venda (id_veiculo);

CREATE INDEX idx_veiculo_chassi ON veiculo (chassi);

CREATE INDEX idx_veiculo_placa ON veiculo (placa);

CREATE INDEX idx_veiculo_renavam ON veiculo (renavam);

CREATE INDEX idx_seguro_numero_apolice ON seguro (numero_apolice);

CREATE INDEX idx_locacao_reserva ON locacao (id_reserva);

CREATE INDEX idx_setting_chave ON integration_setting (id_integration, chave);

CREATE INDEX idx_integration_codigo ON integration (codigo);

CREATE INDEX idx_funcionario_cpf ON funcionario (cpf);

CREATE INDEX idx_funcionario_matricula ON funcionario (matricula);

CREATE INDEX idx_cliente_cnh_numero ON cliente (cnh_numero);

CREATE INDEX idx_cliente_cpf ON cliente (cpf);

CREATE INDEX idx_categoria_nome ON categoria (nome);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261104221220_AuditoriaSoftDelete', '10.0.14');

COMMIT;

