START TRANSACTION;

INSERT INTO integration (codigo, nome, descricao, ambiente, base_url, timeout_ms, ativo)
SELECT 'GOOGLE', 'Login com Google', 'Confirmacao da identidade do cliente pela conta Google (RF-51)', 'SANDBOX', 'https://www.googleapis.com', 15000, TRUE
WHERE NOT EXISTS (SELECT 1 FROM integration x WHERE x.codigo = 'GOOGLE' AND x.disabled_at IS NULL);

INSERT INTO integration_setting (id_integration, chave, valor, protegido)
SELECT i.id_integration, s.chave, s.valor, s.protegido
FROM integration i
JOIN (VALUES
    ('GOOGLE', 'client_id', 'DEFINIR_NO_BANCO', FALSE),
    ('GOOGLE', 'provedor',  'FAKE',             FALSE),
    ('EMAIL',  'provedor',  'FAKE',             FALSE)
) AS s (codigo, chave, valor, protegido) ON s.codigo = i.codigo
WHERE i.disabled_at IS NULL
  AND NOT EXISTS (SELECT 1 FROM integration_setting x
                  WHERE x.id_integration = i.id_integration AND x.chave = s.chave AND x.disabled_at IS NULL);


INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261112050640_AdicionarIntegracaoGoogleEChavesEmail', '10.0.14');

COMMIT;

