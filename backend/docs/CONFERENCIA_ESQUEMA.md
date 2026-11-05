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
dotnet dotnet-ef migrations script 0 Inicial --project AutoVia.Infrastructure --startup-project AutoVia.Api -o docs/sql/01_Inicial.sql
dotnet dotnet-ef migrations script Inicial SeedIntegracoes --project AutoVia.Infrastructure --startup-project AutoVia.Api -o docs/sql/02_SeedIntegracoes.sql
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
dotnet dotnet-ef migrations script SeedIntegracoes AuditoriaSoftDelete --project AutoVia.Infrastructure --startup-project AutoVia.Api -o docs/sql/03_AuditoriaSoftDelete.sql
dotnet dotnet-ef dbcontext script --project AutoVia.Infrastructure --startup-project AutoVia.Api -o docs/sql/00_ModeloAtual.sql
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
