START TRANSACTION;
ALTER TABLE pagamento ADD pix_copia_e_cola character varying(512);

ALTER TABLE pagamento ADD pix_expira_em timestamp without time zone;

ALTER TABLE pagamento ADD qr_code_base64 text;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261121000527_AdicionarPixPagamento', '10.0.14');

COMMIT;

