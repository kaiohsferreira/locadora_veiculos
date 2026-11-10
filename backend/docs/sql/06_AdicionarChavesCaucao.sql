START TRANSACTION;

INSERT INTO integration_setting (id_integration, chave, valor, protegido)
SELECT i.id_integration, s.chave, s.valor, s.protegido
FROM integration i
JOIN (VALUES
    ('PAGAMENTO', 'public_key',            'DEFINIR_NO_BANCO', FALSE),
    ('PAGAMENTO', 'caucao_validade_dias',  '5',                FALSE)
) AS s (codigo, chave, valor, protegido) ON s.codigo = i.codigo
WHERE i.disabled_at IS NULL
  AND NOT EXISTS (SELECT 1 FROM integration_setting x
                  WHERE x.id_integration = i.id_integration AND x.chave = s.chave AND x.disabled_at IS NULL);


INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261110134348_AdicionarChavesCaucao', '10.0.14');

COMMIT;

