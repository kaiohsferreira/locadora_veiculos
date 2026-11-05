START TRANSACTION;

INSERT INTO integration_setting (id_integration, chave, valor, protegido)
SELECT i.id_integration, s.chave, s.valor, s.protegido
FROM integration i
JOIN (VALUES
    ('PAGAMENTO', 'provedor',               'FAKE',             FALSE),
    ('PAGAMENTO', 'access_token',           'DEFINIR_NO_BANCO', TRUE),
    ('PAGAMENTO', 'pix_expiracao_segundos', '3600',             FALSE),
    ('DOCUMENTO', 'provedor',               'FAKE',             FALSE)
) AS s (codigo, chave, valor, protegido) ON s.codigo = i.codigo
WHERE i.disabled_at IS NULL
  AND NOT EXISTS (SELECT 1 FROM integration_setting x
                  WHERE x.id_integration = i.id_integration AND x.chave = s.chave AND x.disabled_at IS NULL);


INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261105103740_AjustarIntegracoesProvedores', '10.0.14');

COMMIT;

