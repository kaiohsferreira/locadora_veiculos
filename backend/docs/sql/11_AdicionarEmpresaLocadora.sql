START TRANSACTION;
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

CREATE INDEX idx_empresa_cnpj ON empresa (cnpj);

CREATE INDEX idx_empresa_email ON empresa (email);

ALTER TABLE funcionario DROP CONSTRAINT ck_funcionario_cargo;

ALTER TABLE funcionario ADD CONSTRAINT ck_funcionario_cargo CHECK (cargo IN ('ATENDENTE', 'VENDEDOR', 'GESTOR_FROTA', 'FINANCEIRO', 'ADMINISTRATIVO', 'ADMINISTRADOR'));

ALTER TABLE funcionario ADD id_empresa integer;

ALTER TABLE veiculo ADD id_empresa integer;


INSERT INTO empresa (razao_social, nome_fantasia, cnpj, telefone, email, cep, logradouro, numero, complemento, bairro, cidade, uf, ativo, operadora)
SELECT 'AutoVia (empresa inicial)', 'AutoVia', '00000000000000', 'a definir', 'a definir', '00000000', 'a definir', 'S/N', NULL, 'a definir', 'a definir', 'XX', TRUE, TRUE
WHERE NOT EXISTS (SELECT 1 FROM empresa x WHERE x.cnpj = '00000000000000' AND x.disabled_at IS NULL);



UPDATE funcionario
SET id_empresa = (SELECT MIN(e.id_empresa) FROM empresa e WHERE e.cnpj = '00000000000000' AND e.disabled_at IS NULL)
WHERE id_empresa IS NULL;

UPDATE veiculo
SET id_empresa = (SELECT MIN(e.id_empresa) FROM empresa e WHERE e.cnpj = '00000000000000' AND e.disabled_at IS NULL)
WHERE id_empresa IS NULL;



DO $$
DECLARE
    total_funcionarios INTEGER;
    total_candidatos INTEGER;
BEGIN
    SELECT COUNT(*) INTO total_funcionarios FROM funcionario;

    IF total_funcionarios > 0 THEN
        SELECT COUNT(*) INTO total_candidatos FROM funcionario WHERE cargo = 'ADMINISTRATIVO' AND ativo = TRUE AND disabled_at IS NULL AND email IS NULL;

        IF total_candidatos = 1 THEN
            UPDATE funcionario SET cargo = 'ADMINISTRADOR' WHERE cargo = 'ADMINISTRATIVO' AND ativo = TRUE AND disabled_at IS NULL AND email IS NULL;
        ELSE
            RAISE EXCEPTION 'Migration AdicionarEmpresaLocadora, passo 6: encontrados % candidatos a administrador inicial (esperado: exatamente 1). Rode SELECT id_funcionario, nome, cpf, matricula, cargo, ativo, email FROM funcionario WHERE cargo = ''ADMINISTRATIVO'' AND ativo = TRUE AND disabled_at IS NULL AND email IS NULL; para conferir quem e. Corrija os dados e aplique a migration de novo.', total_candidatos;
        END IF;
    END IF;
END
$$;


ALTER TABLE funcionario ALTER COLUMN id_empresa SET NOT NULL;

ALTER TABLE veiculo ALTER COLUMN id_empresa SET NOT NULL;

ALTER TABLE funcionario ADD CONSTRAINT fk_funcionario_empresa FOREIGN KEY (id_empresa) REFERENCES empresa (id_empresa);

ALTER TABLE veiculo ADD CONSTRAINT fk_veiculo_empresa FOREIGN KEY (id_empresa) REFERENCES empresa (id_empresa);

CREATE INDEX idx_funcionario_empresa ON funcionario (id_empresa);

CREATE INDEX idx_veiculo_empresa ON veiculo (id_empresa);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261114021446_AdicionarEmpresaLocadora', '10.0.14');

COMMIT;

