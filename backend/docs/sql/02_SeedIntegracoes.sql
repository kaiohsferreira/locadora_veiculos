START TRANSACTION;

INSERT INTO integration (codigo, nome, descricao, ambiente, base_url, timeout_ms, ativo) VALUES
    ('PAGAMENTO',     'Gateway de pagamento',                 'Cobrança por PIX, cartão de crédito (com pré-autorização) e boleto', 'SANDBOX', 'https://sandbox.pagamento.exemplo.com.br',  30000, TRUE),
    ('EMAIL',         'Envio de e-mails transacionais',       'Confirmação de reserva, voucher, recibo e aviso de cobrança',        'SANDBOX', 'https://sandbox.email.exemplo.com.br',      30000, TRUE),
    ('DOCUMENTO',     'Leitura e validação de CNH',           'OCR da CNH enviada no cadastro do cliente',                          'SANDBOX', 'https://sandbox.documento.exemplo.com.br',  30000, TRUE),
    ('ARMAZENAMENTO', 'Armazenamento de arquivos',            'Fotos das vistorias e documentos enviados pelos clientes',           'SANDBOX', 'https://sandbox.storage.exemplo.com.br',    30000, TRUE),
    ('SEGURADORA',    'Seguradora',                           'Vigência de apólice, abertura e acompanhamento de sinistro',         'SANDBOX', 'https://sandbox.seguradora.exemplo.com.br', 30000, TRUE),
    ('MULTAS',        'Consulta de multas de trânsito',       'Infrações por placa no período da locação',                          'SANDBOX', 'https://sandbox.multas.exemplo.com.br',     30000, TRUE);



INSERT INTO integration_setting (id_integration, chave, valor, protegido)
SELECT i.id_integration, s.chave, s.valor, s.protegido
FROM integration i
JOIN (VALUES
    ('PAGAMENTO',     'client_id',                    'DEFINIR_NO_BANCO',            FALSE),
    ('PAGAMENTO',     'client_secret',                'DEFINIR_NO_BANCO',            TRUE),
    ('PAGAMENTO',     'merchant_id',                  'DEFINIR_NO_BANCO',            FALSE),
    ('PAGAMENTO',     'chave_pix',                    'financeiro@autovia.com.br',   FALSE),
    ('PAGAMENTO',     'webhook_secret',               'DEFINIR_NO_BANCO',            TRUE),

    ('EMAIL',         'api_key',                      'DEFINIR_NO_BANCO',            TRUE),
    ('EMAIL',         'remetente_email',              'nao-responda@autovia.com.br', FALSE),
    ('EMAIL',         'remetente_nome',               'AutoVia',                     FALSE),
    ('EMAIL',         'template_confirmacao_reserva', 'confirmacao-reserva',         FALSE),
    ('EMAIL',         'template_cobranca',            'aviso-cobranca',              FALSE),

    ('DOCUMENTO',     'api_key',                      'DEFINIR_NO_BANCO',            TRUE),
    ('DOCUMENTO',     'modelo_ocr',                   'cnh-v1',                      FALSE),
    ('DOCUMENTO',     'confianca_minima',             '0.85',                        FALSE),

    ('ARMAZENAMENTO', 'access_key',                   'DEFINIR_NO_BANCO',            TRUE),
    ('ARMAZENAMENTO', 'secret_key',                   'DEFINIR_NO_BANCO',            TRUE),
    ('ARMAZENAMENTO', 'bucket',                       'autovia-arquivos',            FALSE),
    ('ARMAZENAMENTO', 'regiao',                       'sa-east-1',                   FALSE),
    ('ARMAZENAMENTO', 'url_expiracao_segundos',       '900',                         FALSE),

    ('SEGURADORA',    'usuario',                      'DEFINIR_NO_BANCO',            FALSE),
    ('SEGURADORA',    'senha',                        'DEFINIR_NO_BANCO',            TRUE),
    ('SEGURADORA',    'codigo_corretora',             'DEFINIR_NO_BANCO',            FALSE),
    ('SEGURADORA',    'apolice_master',               'DEFINIR_NO_BANCO',            FALSE),

    ('MULTAS',        'token',                        'DEFINIR_NO_BANCO',            TRUE),
    ('MULTAS',        'orgao',                        'DETRAN-MG',                   FALSE),
    ('MULTAS',        'uf_padrao',                    'MG',                          FALSE)
) AS s (codigo, chave, valor, protegido) ON s.codigo = i.codigo;


INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261104080647_SeedIntegracoes', '10.0.14');

COMMIT;

