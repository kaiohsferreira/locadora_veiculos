START TRANSACTION;
ALTER TABLE funcionario ADD senha_hash character varying(255);

ALTER TABLE cliente ADD senha_hash character varying(255);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261105094843_AdicionarSenhaHashAcesso', '10.0.14');

COMMIT;

