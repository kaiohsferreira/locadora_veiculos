-- =====================================================================
-- Tabelas de apoio da biblioteca de integrações
-- Guardam a configuração de cada integração no banco, em vez de em
-- variáveis de ambiente ou constantes compiladas no código.
-- =====================================================================

CREATE TABLE integration (
    id_integration  SERIAL        PRIMARY KEY,
    codigo          VARCHAR(40)   NOT NULL UNIQUE,
    nome            VARCHAR(120)  NOT NULL,
    descricao       VARCHAR(200),
    ambiente        VARCHAR(15)   NOT NULL DEFAULT 'PRODUCAO',
    base_url        VARCHAR(255)  NOT NULL,
    timeout_ms      INTEGER       NOT NULL DEFAULT 30000,
    ativo           BOOLEAN       NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMP     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      TIMESTAMP     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    disabled_at     TIMESTAMP,
    CONSTRAINT ck_integration_ambiente
        CHECK (ambiente IN ('SANDBOX', 'HOMOLOGACAO', 'PRODUCAO')),
    CONSTRAINT ck_integration_timeout CHECK (timeout_ms > 0)
);

CREATE TABLE integration_setting (
    id_setting      SERIAL        PRIMARY KEY,
    id_integration  INTEGER       NOT NULL,
    chave           VARCHAR(60)   NOT NULL,
    valor           TEXT          NOT NULL,
    protegido       BOOLEAN       NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMP     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      TIMESTAMP     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_setting_integration FOREIGN KEY (id_integration)
        REFERENCES integration (id_integration) ON DELETE CASCADE,
    CONSTRAINT uq_setting_chave UNIQUE (id_integration, chave)
);

CREATE INDEX idx_setting_integration ON integration_setting (id_integration);
