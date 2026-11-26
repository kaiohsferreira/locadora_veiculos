START TRANSACTION;
ALTER TABLE avaria ADD angulo character varying(20);

ALTER TABLE avaria ADD pos_x numeric(5,2);

ALTER TABLE avaria ADD pos_y numeric(5,2);

ALTER TABLE avaria ADD CONSTRAINT ck_avaria_angulo CHECK (angulo IS NULL OR angulo IN ('FRENTE', 'TRASEIRA', 'LATERAL_ESQUERDA', 'LATERAL_DIREITA'));

ALTER TABLE avaria ADD CONSTRAINT ck_avaria_pos_x CHECK (pos_x IS NULL OR (pos_x >= 0 AND pos_x <= 100));

ALTER TABLE avaria ADD CONSTRAINT ck_avaria_pos_y CHECK (pos_y IS NULL OR (pos_y >= 0 AND pos_y <= 100));

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261126222013_AdicionarAnguloPosicaoAvaria', '10.0.14');

COMMIT;

