START TRANSACTION;

UPDATE integration_setting s
SET valor = 'DISCO_LOCAL',
    updated_at = CURRENT_TIMESTAMP
FROM integration i
WHERE i.id_integration = s.id_integration
  AND i.codigo = 'ARMAZENAMENTO'
  AND s.chave = 'provedor'
  AND s.valor <> 'DISCO_LOCAL';

INSERT INTO integration_setting (id_integration, chave, valor, protegido)
SELECT i.id_integration, 'provedor', 'DISCO_LOCAL', FALSE
FROM integration i
WHERE i.codigo = 'ARMAZENAMENTO'
  AND i.disabled_at IS NULL
  AND NOT EXISTS (SELECT 1 FROM integration_setting x
                  WHERE x.id_integration = i.id_integration AND x.chave = 'provedor');


INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261118180936_ArmazenamentoDiscoLocal', '10.0.14');

COMMIT;

