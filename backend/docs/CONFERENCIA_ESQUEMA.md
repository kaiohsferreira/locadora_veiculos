# Conferência do esquema: migration `Inicial` x especificação (T-BE-02)

Data: 04/11/2026 · Responsável: Kaio Henrique dos Santos Ferreira

## Arquivos

| Arquivo | O que é |
|---|---|
| `especificacao/apendice_A_tabelas_negocio.sql` | Especificação das 14 tabelas de negócio (cópia do Prompt 1, sem alteração) |
| `especificacao/apendice_B_tabelas_integracoes.sql` | Especificação das 2 tabelas da biblioteca de integrações |
| `sql/01_Inicial.sql` | SQL gerado pela migration `Inicial` |
| `sql/02_SeedIntegracoes.sql` | SQL gerado pela migration `SeedIntegracoes` |
| `../tools/compare_schema.py` | Script que compara a especificação com o SQL gerado |

## Como gerar e conferir de novo

Na pasta `backend/`:

```bash
dotnet tool restore
dotnet ef migrations script 0 Inicial --project AutoVia.Infrastructure --startup-project AutoVia.Api -o docs/sql/01_Inicial.sql
dotnet ef migrations script Inicial SeedIntegracoes --project AutoVia.Infrastructure --startup-project AutoVia.Api -o docs/sql/02_SeedIntegracoes.sql
python tools/compare_schema.py docs/especificacao/apendice_A_tabelas_negocio.sql docs/especificacao/apendice_B_tabelas_integracoes.sql docs/sql/01_Inicial.sql
```

O script compara tabela por tabela: nome, tipo, nulidade e DEFAULT de cada coluna; nome e expressão de cada CHECK; nome, coluna, tabela de destino e ação de exclusão de cada FK; nome e colunas de cada UNIQUE e de cada índice.

## Resultado

```text
Tabelas:  especificacao=16 gerado=16
Colunas:  especificacao=148 gerado=148
CHECK:    especificacao=32 gerado=32
FK:       especificacao=22 gerado=22
UNIQUE:   especificacao=15 gerado=15
Indices:  especificacao=11 gerado=11
Diferencas: 0
```

Na primeira geração, apareceu uma única diferença: `locacao.valor_caucao` com `DEFAULT 0.0` em vez de `DEFAULT 0`. O mapeamento passou a usar `HasDefaultValueSql("0")` e a migration foi gerada de novo. Ela nunca tinha sido aplicada.

## Desvios aprovados em 04/11/2026 (migration `AuditoriaSoftDelete`)

Decisão do Kaio, depois da conferência acima:
- **Todas as entidades herdam `BaseEntities`.** As 16 tabelas passam a ter `created_at` e `updated_at` (`NOT NULL`, `DEFAULT CURRENT_TIMESTAMP`) e `disabled_at` (anulável). A `integration_setting`, que só tinha as duas primeiras, ganhou `disabled_at`.
- **Exclusão lógica.** Toda busca filtra `disabled_at` nulo.
- **Nenhum índice único no banco.** Os 15 UNIQUE da especificação viraram índices comuns (`idx_*`), e a unicidade passou a ser validada no serviço, só entre os registros ativos.
- **Relações Reserva→Locação e Veículo→Venda passam a ser 1:N no modelo.** O EF exige índice único na FK de uma relação 1:1. A regra "uma ativa por vez" fica no serviço.

Os apêndices A e B **não foram alterados**. Os desvios ficam descritos em `especificacao/desvios_aprovados.json`, e o script aplica esses desvios antes de comparar:

```bash
dotnet ef migrations script SeedIntegracoes AuditoriaSoftDelete --project AutoVia.Infrastructure --startup-project AutoVia.Api -o docs/sql/03_AuditoriaSoftDelete.sql
dotnet ef dbcontext script --project AutoVia.Infrastructure --startup-project AutoVia.Api -o docs/sql/00_ModeloAtual.sql
python tools/compare_schema.py docs/especificacao/apendice_A_tabelas_negocio.sql docs/especificacao/apendice_B_tabelas_integracoes.sql docs/sql/00_ModeloAtual.sql docs/especificacao/desvios_aprovados.json
```

Resultado:

```text
Desvios aprovados aplicados a especificacao: 58   (43 colunas de auditoria + 15 unique -> indice)
Tabelas:  especificacao=16 gerado=16
Colunas:  especificacao=191 gerado=191
CHECK:    especificacao=32 gerado=32
FK:       especificacao=22 gerado=22
UNIQUE:   especificacao=0 gerado=0
Indices:  especificacao=26 gerado=26
Diferencas: 0
```

Sem o arquivo de desvios, a comparação literal contra os apêndices mostra exatamente esses 58 pontos (43 colunas a mais, 15 UNIQUE a menos e 15 índices a mais), e nada além disso.

A migration `AuditoriaSoftDelete` foi **gerada, não aplicada**. Num banco que já tenha a `Inicial`, ela acrescenta as colunas com DEFAULT (as linhas existentes recebem a data atual) e troca os índices.

## Migrations 04 e 05 e desvio `colunas_acesso` (05/11/2026)

- **Migration `AdicionarSenhaHashAcesso` (04, `sql/04_AdicionarSenhaHashAcesso.sql`):** acrescenta `senha_hash VARCHAR(255) NULL` em `cliente` e `funcionario`, guardando somente o hash. O desvio está no bloco `colunas_acesso` de `desvios_aprovados.json` (decisão D1, aprovada pelo Kaio) e o script o aplica como faz com as colunas de auditoria.
- **Migration `AjustarIntegracoesProvedores` (05, `sql/05_AjustarIntegracoesProvedores.sql`):** migration só de dados, sem mudança de esquema. Acrescenta `provedor`, `access_token` (`DEFINIR_NO_BANCO`) e `pix_expiracao_segundos` em `integration_setting`, por isso ela não aparece na conferência.
- **Resultado atual** (com `colunas_acesso`): `Desvios aprovados aplicados a especificacao: 60`, `Colunas: especificacao=193 gerado=193`, `Diferencas: 0`. Sem esse bloco, o script mostra 2 diferenças (`senha_hash` em `cliente` e `funcionario`).

## Desvio de 12/11/2026: recuperação de senha e login Google (RF-49 e RF-51)

Aprovado pelo Kaio em 12/11/2026 (bloco `recuperacao_senha_e_login_google` em `especificacao/desvios_aprovados.json`):
- Tabela nova `redefinicao_senha`: guarda só o hash SHA-256 do token do link de recuperação, a validade e o titular (cliente OU funcionario, pelo CHECK `ck_redefinicao_titular`). Tem as colunas de auditoria do `BaseEntities`.
- Coluna nova `cliente.google_sub VARCHAR(255) NULL`: vínculo com a conta Google.
- Índices comuns, sem UNIQUE: `idx_redefinicao_token` e `idx_cliente_google_sub`. A unicidade fica no serviço.
- Migrations geradas, não aplicadas, nesta ordem depois da `AdicionarChavesCaucao` (06):
  - `20261112050622_AdicionarRecuperacaoSenhaEVinculoGoogle` (esquema), script `sql/07_AdicionarRecuperacaoSenhaEVinculoGoogle.sql`;
  - `20261112050640_AdicionarIntegracaoGoogleEChavesEmail` (dados: integration `GOOGLE`, settings `GOOGLE.client_id`, `GOOGLE.provedor` e `EMAIL.provedor`), script `sql/08_AdicionarIntegracaoGoogleEChavesEmail.sql`.

Contagens depois do desvio (conferidas pelo `SchemaModelTests`):

```text
Tabelas:  16 -> 17 (14 de negocio + redefinicao_senha + integration + integration_setting)
Colunas:  +9 (redefinicao_senha: 8; cliente.google_sub: 1)
CHECK:    32 -> 33 (ck_redefinicao_titular)
FK:       +2 (fk_redefinicao_cliente, fk_redefinicao_funcionario)
UNIQUE:   0 (inalterado)
Indices:  +2 (idx_redefinicao_token, idx_cliente_google_sub)
```

## Diferenças de forma, que não mudam o esquema

Estes pontos não aparecem como diferença porque são equivalentes. Vale registrar mesmo assim, para ninguém se surpreender ao abrir o banco:

1. **Tabela `__EFMigrationsHistory`.** O EF Core a cria para registrar quais migrations já rodaram. Ela não faz parte do domínio.
2. **UNIQUE vira índice único.** Na especificação, `cpf ... UNIQUE` cria uma *constraint* UNIQUE. O EF cria um *índice* único com o mesmo nome (`cliente_cpf_key`) e o mesmo efeito: o banco recusa duplicados. A única diferença prática: `ON CONFLICT ON CONSTRAINT <nome>` exige constraint. Com índice, use `ON CONFLICT (coluna)`.
3. **Nomes iguais aos do PostgreSQL.** As chaves primárias foram nomeadas `<tabela>_pkey`, e os UNIQUE escritos na própria linha da coluna, `<tabela>_<coluna>_key`. São exatamente os nomes que o PostgreSQL daria rodando o SQL da especificação. Os nomes declarados na especificação (`fk_*`, `ck_*`, `uq_*`, `idx_*`) foram mantidos.
4. **`SERIAL`.** O mapeamento usa `UseSerialColumns()`. Sem isso, o Npgsql geraria `GENERATED BY DEFAULT AS IDENTITY`. Por isso a migration não tem nenhuma anotação `Npgsql:IdentitySequenceOptions` para limpar.
5. **Ação de exclusão.** O `ON DELETE CASCADE` ficou só onde a especificação pede (`fk_vistoria_locacao`, `fk_avaria_vistoria`, `fk_cobranca_locacao` e `fk_setting_integration`). O restante ficou sem cláusula, ou seja, `NO ACTION`, que é o padrão do PostgreSQL e o mesmo comportamento do SQL da especificação.
6. **Nenhum índice automático de FK.** Por convenção, o EF cria um índice para cada chave estrangeira. Essa convenção foi desligada no `AutoViaDbContext`, então existem só os 11 índices da especificação mais os UNIQUE.

## Pontos da especificação que merecem atenção (não alterados)

O Prompt 1 diz para apontar, e não corrigir por conta própria:

- ~~**`integration_setting` não tem `disabled_at`**~~ Resolvido em 04/11/2026: a coluna entrou na migration `AuditoriaSoftDelete` (ver "Desvios aprovados").
- **`pagamento.forma` não tem CHECK** e continua `string`. Se a equipe quiser restringir a PIX, CARTAO e BOLETO, isso vira uma migration nova.
- **`locacao.id_reserva` é anulável**, o que permite uma locação sem reserva de origem. A regra "a reserva de origem passa para CONCLUIDA" vale só quando existe reserva.
- **Não há colunas para autenticação** (senha ou hash) em `cliente` nem em `funcionario`. A tarefa P2-BE-01 (login e token) vai precisar de uma decisão: tabela nova ou ASP.NET Identity. Nos dois casos, é uma migration nova.

## Desvio de 13/11/2026: multi-locadora (T-BE-03a)

Aprovado pelo Kaio em 13/11/2026 (bloco `empresa_locadora` em `especificacao/desvios_aprovados.json`). Cada locadora se cadastra na plataforma, tem equipe própria e só vê os seus dados.

- Tabela nova `empresa` (18 colunas, incluindo as de auditoria do `BaseEntities`), com o CHECK `ck_empresa_cnpj` (CNPJ com exatamente 14 dígitos) e os índices comuns `idx_empresa_cnpj` e `idx_empresa_email`. Nenhum é único: unicidade de CNPJ e e-mail é validada no serviço.
- Colunas novas `funcionario.id_empresa` e `veiculo.id_empresa`, `INT NOT NULL`, sem DEFAULT, com `fk_funcionario_empresa` e `fk_veiculo_empresa` (NO ACTION) e os índices comuns `idx_funcionario_empresa` e `idx_veiculo_empresa`.
- `ck_funcionario_cargo` passa a aceitar `ADMINISTRADOR` (seis valores). ADMINISTRADOR é o último membro do enum `CargoFuncionario`.
- Migration `AdicionarEmpresaLocadora` (`20261114021446_AdicionarEmpresaLocadora`), script `sql/11_AdicionarEmpresaLocadora.sql` (os números 09 e 10 ficaram com a branch de outro desenvolvedor). Ordem do `Up`: cria `empresa`; troca o CHECK de cargo; cria as colunas nulas; cria a empresa inicial; aponta todo funcionário e veículo existentes (inclusive desativados) para ela; promove o administrador inicial a ADMINISTRADOR somente quando há exatamente 1 candidato (ADMINISTRATIVO, ativo, sem e-mail, não desativado) — tabela vazia não faz nada, e 0 ou mais de 1 candidato dispara `RAISE EXCEPTION` com a contagem e o SELECT de conferência, abortando a migration sem promover ninguém (A1, revisão de segurança); torna `id_empresa` NOT NULL; cria as FKs e os índices. O `Down` volta ADMINISTRADOR para ADMINISTRATIVO antes de restaurar o CHECK antigo.
- **Dados da empresa inicial são PROVISÓRIOS** (razão social "AutoVia (empresa inicial)", CNPJ `00000000000000`, demais campos "a definir"/valores válidos). Estão em constantes no topo da migration e devem ser trocados pelos reais antes de aplicar.
- **`docs/sql/00_ModeloAtual.sql` foi regenerado.** A versão anterior estava defasada: não tinha `redefinicao_senha`, `cliente.google_sub` nem os índices `idx_redefinicao_token` e `idx_cliente_google_sub` (desvio de 12/11). A nova versão traz tudo isso mais a empresa.
- Resultado do `compare_schema.py` depois do desvio de 13/11: `Tabelas: especificacao=16 gerado=18`, `CHECK: 32 -> 34`, `FK: 22 -> 26`, `Indices: 26 -> 32`, `Diferencas: 21`. As 21 diferenças são só os itens dos desvios de 12/11 (`redefinicao_senha`, `cliente.google_sub` e seus índices e FKs) e de 13/11 (`empresa`, `id_empresa`, FKs, índices e CHECK de cargo). O script ainda não lê as entradas `tabelas_novas` e `colunas_novas` dos blocos de desvio, por isso lista esses itens como sobra. Ajustar o script é um passo à parte.

## Desvio de 14/11/2026: aprovação do cadastro por locadora (T-BE-05)

Aprovado pelo Kaio em 14/11/2026 (bloco `cliente_locadora` em `especificacao/desvios_aprovados.json`):
- Tabela nova `cliente_locadora`: um vínculo por par cliente x locadora, com `status` (PENDENTE, APROVADO ou RECUSADO; enum gravado como texto, CHECK `ck_cliente_locadora_status`), `justificativa_recusa` (motivo interno da recusa), `id_funcionario_verificador`, `data_solicitacao` e `data_verificacao`. Colunas de auditoria do `BaseEntities`.
- FKs `fk_cliente_locadora_cliente`, `fk_cliente_locadora_empresa` e `fk_cliente_locadora_funcionario`, todas NO ACTION.
- Índices comuns, sem UNIQUE: `idx_cliente_locadora_empresa_cliente` e `idx_cliente_locadora_cliente`. A unicidade do vínculo ativo de menor id fica no serviço (T-BE-06).
- `cliente.status_cadastro` fica congelada: o cadastro continua gravando PENDENTE, nenhum código novo a lê ou atualiza, e não há migração destrutiva.
- Migration `20261114202316_AdicionarClienteLocadora`: gerada, não aplicada. Depois da `AdicionarEmpresaLocadora` (11). Preenche um vínculo por par (cliente ativo com status APROVADO ou RECUSADO, locadora) com reserva, locação ou venda ativa em veículo dela, com o mesmo status global do cliente. Cliente PENDENTE não ganha vínculo (RF-83, decisão do QA de 14/11); verificador e justificativa ficam nulos. Script `sql/12_AdicionarClienteLocadora.sql`.

Contagens que o `SchemaModelTests` passa a conferir: tabelas 18 -> 19, CHECK 34 -> 35, +3 FKs, +2 índices comuns, +11 colunas (8 de negócio e 3 de auditoria), UNIQUE 0 (inalterado).

## Desvio de 16/11/2026: sinistro_acompanhamento (P9-INT-01, RF-43)

Aprovado pelo Kaio em 16/11/2026. O registro original foi escrito pela Maria em 13/11/2026, na branch dela, e a redação abaixo segue esse registro. O desvio entra no merge S1 de 16/11/2026.

- Coluna nova `sinistro.protocolo_seguradora VARCHAR(40) NULL`: protocolo devolvido pela seguradora na abertura do sinistro.
- Tabela nova `sinistro_historico` (11 colunas: 8 de negócio e 3 de auditoria do `BaseEntities`): linha do tempo do sinistro, com status anterior e novo, origem (FUNCIONARIO ou SEGURADORA), funcionário (obrigatório quando a origem é FUNCIONARIO), observação e data da alteração.
- CHECK: `ck_sinistro_historico_status_anterior` (anulável), `ck_sinistro_historico_status_novo`, `ck_sinistro_historico_origem` e `ck_sinistro_historico_funcionario`.
- FKs NO ACTION: `fk_sinistro_historico_sinistro` e `fk_sinistro_historico_funcionario`.
- Índice comum `idx_sinistro_historico_sinistro (id_sinistro, data_alteracao)`. Nenhum índice único.
- Migration `20261116204240_SinistroHistorico`: gerada, não aplicada. Só muda o esquema. Script `sql/13_SinistroHistorico.sql`.
- Migrations de dados que vêm depois, sem DDL e sem mudança de esquema:
  - `20261116204317_SeguradoraProvedor` (script `sql/14_SeguradoraProvedor.sql`): `SEGURADORA.provedor = FAKE`;
  - `20261116204428_MultasProvedor` (script `sql/15_MultasProvedor.sql`): `MULTAS.provedor = FAKE` e `ARMAZENAMENTO.provedor = S3_COMPATIVEL`.
  - Ambas inserem com `WHERE NOT EXISTS` (idempotentes). O `Down` faz exclusão lógica (`disabled_at`), nunca DELETE. Ressalva: a chave desativada continua sendo lida pelo repositório de integrações, que não filtra `disabled_at` (pendência conhecida, item L13 da síntese do Eduardo).
- Os números 09 e 10 ficaram sem uso: eram as migrations de 13/11 da Maria, removidas no merge. A numeração segue 13, 14 e 15.

Contagens depois do desvio (`sql/00_ModeloAtual.sql` regenerado em 16/11/2026):

```text
Tabelas:  19 -> 20 (+ sinistro_historico)
Colunas:  +12 (sinistro.protocolo_seguradora: 1; sinistro_historico: 11)
CHECK:    35 -> 39 (+4, todos em sinistro_historico)
FK:       29 -> 31 (+2)
UNIQUE:   0 (inalterado)
Indices:  34 -> 35 (+1: idx_sinistro_historico_sinistro)
```

Notas sobre a conferência:
- `tools/compare_schema.py` não lê as entradas `tabelas_novas` nem `colunas_novas` dos blocos de desvio. Por isso "Diferencas: 0" não é critério para este desvio. A conferência é feita pelas contagens acima e pelo diff do `00_ModeloAtual.sql`.
- O diff do `00_ModeloAtual.sql` traz só as adições de sinistro. O EF também reordena as tabelas do arquivo por dependência: nesta regeneração, o bloco `cliente_locadora` mudou de posição, sem mudança de conteúdo.
- O bloco do desvio (`sinistro_acompanhamento`) está em `especificacao/desvios_aprovados.json` desde o merge da branch da Maria: aprovado por Kaio em 16/11/2026, com o registro original de 13/11/2026.

## Desvio de 16/11/2026: fotos do veículo (RF-94 a RF-101 e RF-105)

Aprovado pelo Kaio em 16/11/2026 (bloco `veiculo_foto` em `especificacao/desvios_aprovados.json`). O sistema passa a guardar as fotos do veículo, até 15 ativas por veículo. O arquivo fica no armazenamento externo; o banco guarda só a chave do objeto, a ordem, quem enviou e quem removeu.
- Tabela nova `veiculo_foto` (10 colunas: 7 de negócio e 3 de auditoria do `BaseEntities`): `id_veiculo_foto` (SERIAL, PK `veiculo_foto_pkey`), `id_veiculo`, `ordem`, `chave_objeto` VARCHAR(200), `chave_miniatura` VARCHAR(200) anulável (reservada, nula nesta entrega), `id_funcionario_envio` (obrigatório) e `id_funcionario_remocao` (anulável).
- CHECK `ck_veiculo_foto_ordem` (`ordem >= 1`).
- FKs NO ACTION: `fk_veiculo_foto_veiculo`, `fk_veiculo_foto_funcionario_envio` e `fk_veiculo_foto_funcionario_remocao`.
- Índice comum `idx_veiculo_foto_veiculo (id_veiculo, ordem)`. Nenhum índice único: o teto de 15 fotos ativas fica no serviço.
- Migration `20261116233935_AdicionarVeiculoFoto`: gerada, não aplicada. Só cria a tabela, o CHECK, as FKs e o índice, e a tabela nasce vazia. Script `sql/16_AdicionarVeiculoFoto.sql`.

Contagens depois do desvio (`sql/00_ModeloAtual.sql` regenerado em 16/11/2026):

```text
Tabelas:  20 -> 21 (+ veiculo_foto)
Colunas:  +10 (veiculo_foto: 7 de negócio e 3 de auditoria)
CHECK:    39 -> 40 (+1, ck_veiculo_foto_ordem)
FK:       31 -> 34 (+3)
UNIQUE:   0 (inalterado)
Indices:  35 -> 36 (+1: idx_veiculo_foto_veiculo)
```

Como o `compare_schema.py` não lê as entradas `tabelas_novas`, a conferência é feita pelas contagens acima e pelo diff do `00_ModeloAtual.sql`, que traz só a tabela e o índice novos.


## Desvio de 16/11/2026: fornecedor e nota fiscal de compra do veículo (BE-05)

Aprovado pelo Kaio em 16/11/2026 (bloco `veiculo_fornecedor_nota_fiscal_compra` em `especificacao/desvios_aprovados.json`). O cadastro do veículo passa a guardar o fornecedor e o número da nota fiscal de compra (dados internos da locadora, nunca em retorno público nem de reserva), e o modelo passa a aceitar 60 caracteres.
- Coluna nova `veiculo.fornecedor` VARCHAR(100) anulável, sem índice.
- Coluna nova `veiculo.numero_nota_fiscal_compra` VARCHAR(20) anulável, sem índice. O nome diferencia da `venda.numero_nota_fiscal`, que já existe.
- Alargamento de `veiculo.modelo` de VARCHAR(40) para VARCHAR(60). Os dados existentes continuam válidos.
- Migration `20261117025802_AdicionarFornecedorNotaFiscalCompraVeiculo`: gerada, não aplicada. Script `sql/17_AdicionarFornecedorNotaFiscalCompraVeiculo.sql`.

Contagens depois do desvio (`sql/00_ModeloAtual.sql` ajustado à mão em 16/11/2026, conferido contra o modelo do EF):

```text
Tabelas:  21 (inalterado)
Colunas:  +2 (veiculo.fornecedor e veiculo.numero_nota_fiscal_compra); veiculo.modelo VARCHAR(40) -> VARCHAR(60)
CHECK:    40 (inalterado)
FK:       34 (inalterado)
UNIQUE:   0 (inalterado)
Indices:  36 (inalterado)
```

Como o `compare_schema.py` não lê as entradas `colunas_novas` nem `alteracao_de_tamanho` dos blocos de desvio, ele lista as duas colunas e o tamanho do modelo como diferença. A conferência é feita pelo diff do `00_ModeloAtual.sql` e por `dotnet ef migrations has-pending-model-changes`, que responde "No changes have been made to the model since the last migration".

## Desvio de 19/11/2026: documentos do cliente (RF-08, P2-BE-03)

Aprovado pelo Kaio em 19/11/2026 (bloco `cliente_documento` em `especificacao/desvios_aprovados.json`). O cliente envia a CNH e o comprovante de residência; o arquivo fica no armazenamento externo e o banco guarda só a chave do objeto, o nome limpo para a tela, o tipo do conteúdo e o tamanho.
- Tabela nova `cliente_documento` (10 colunas: 7 de negócio e 3 de auditoria do `BaseEntities`): `id_cliente_documento` (SERIAL, PK `cliente_documento_pkey`), `id_cliente`, `tipo` VARCHAR(30) com CHECK, `chave_objeto` VARCHAR(200), `nome_arquivo` VARCHAR(120), `tipo_conteudo` VARCHAR(50) e `tamanho_bytes`.
- CHECK `ck_cliente_documento_tipo` (`tipo IN ('CNH', 'COMPROVANTE_RESIDENCIA')`).
- FK NO ACTION: `fk_cliente_documento_cliente`.
- Índice comum `idx_cliente_documento_cliente_tipo (id_cliente, tipo)`. Nenhum índice único: um documento ativo por cliente e tipo fica no serviço.
- Migration `20261119142116_AdicionarClienteDocumento`: só gerada, não aplicada. Script `sql/19_AdicionarClienteDocumento.sql`, conferido linha a linha contra a migration gerada em 19/11/2026 (tabela, colunas, CHECK, chave estrangeira e índice iguais).

Contagens depois do desvio (`sql/00_ModeloAtual.sql` ajustado à mão em 19/11/2026):

```text
Tabelas:  21 -> 22 (+ cliente_documento)
Colunas:  +10 (cliente_documento: 7 de negócio e 3 de auditoria)
CHECK:    40 -> 41 (+1, ck_cliente_documento_tipo)
FK:       34 -> 35 (+1, fk_cliente_documento_cliente)
UNIQUE:   0 (inalterado)
Indices:  36 -> 37 (+1: idx_cliente_documento_cliente_tipo)
```

Como o `compare_schema.py` não lê as entradas `tabelas_novas`, a conferência é feita pelas contagens acima, pelo diff do `00_ModeloAtual.sql` e por `dotnet ef migrations has-pending-model-changes` depois de gerada a migration.

## Desvio de 19/11/2026: id da transação no pagamento (P7-INT-01, RF-20)

Aprovado pelo Kaio em 19/11/2026 (bloco `pagamento_id_transacao` em `especificacao/desvios_aprovados.json`). Sem o id da transação no provedor, a caucao não pode ser capturada nem cancelada depois. A coluna guarda esse identificador; nesta entrega nada a grava.
- Coluna nova `pagamento.id_transacao` VARCHAR(40) NULL, sem CHECK e sem índice.
- Migration `20261119162522_AdicionarIdTransacaoPagamento`: só gerada, não aplicada. Script `sql/20_AdicionarIdTransacaoPagamento.sql`, conferido contra a migration gerada em 19/11/2026 (uma coluna nova, `id_transacao`, texto de 40, anulável; sem índice e sem CHECK).

Contagens depois do desvio (`sql/00_ModeloAtual.sql` ajustado à mão em 19/11/2026):

```text
Tabelas:  inalterado
Colunas:  +1 (pagamento.id_transacao)
CHECK:    inalterado
FK:       inalterado
UNIQUE:   0 (inalterado)
Indices:  inalterado
```
