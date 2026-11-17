START TRANSACTION;
ALTER TABLE veiculo ALTER COLUMN modelo TYPE character varying(60);
ALTER TABLE veiculo ADD fornecedor character varying(100);
ALTER TABLE veiculo ADD numero_nota_fiscal_compra character varying(20);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261117025802_AdicionarFornecedorNotaFiscalCompraVeiculo', '10.0.14');

COMMIT;
