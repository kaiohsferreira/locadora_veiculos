-- Dado, nao esquema: validade do PIX em 900 segundos (decisao P14 do Kaio, 20/11/2026).
-- Nao e migration do EF: nao grava linha em __EFMigrationsHistory. Quem roda e o Kaio, no banco
-- de teste e no banco da equipe, depois de aplicar a migration 23 (AdicionarPixPagamento).
-- Se a linha nao existir, o codigo usa o padrao ConstantsPix.ExpiracaoPadraoSegundos (900).
START TRANSACTION;

UPDATE integration_setting
SET valor = '900',
    updated_at = CURRENT_TIMESTAMP
FROM integration
WHERE integration.id_integration = integration_setting.id_integration
  AND integration.codigo = 'PAGAMENTO'
  AND integration_setting.chave = 'pix_expiracao_segundos'
  AND integration_setting.disabled_at IS NULL;

COMMIT;
