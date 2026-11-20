START TRANSACTION;
ALTER TABLE vistoria ADD comparacao_realizada boolean NOT NULL DEFAULT FALSE;

ALTER TABLE avaria ADD id_avaria_entrada integer;

CREATE INDEX idx_avaria_entrada ON avaria (id_avaria_entrada);

ALTER TABLE avaria ADD CONSTRAINT fk_avaria_entrada FOREIGN KEY (id_avaria_entrada) REFERENCES avaria (id_avaria);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261118163147_VincularAvariasVistoriaSaida', '10.0.14');

COMMIT;
