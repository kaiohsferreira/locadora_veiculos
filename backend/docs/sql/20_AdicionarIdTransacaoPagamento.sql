START TRANSACTION;
ALTER TABLE pagamento ADD id_transacao character varying(40);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261119162522_AdicionarIdTransacaoPagamento', '10.0.14');

COMMIT;
