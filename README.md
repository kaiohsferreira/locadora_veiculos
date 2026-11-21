# locadora_veiculos — AutoVia

Essa é uma aplicação para o trabalho de TI para locadora de veiculos.

Plataforma web da **AutoVia**, locadora que aluga e vende veículos da própria frota.
Trabalho Interdisciplinar: Aplicações para Processos de Negócio — PUC Minas, 2026.

| Pasta | O que é | Como começar |
|---|---|---|
| [`backend/`](backend/README.md) | API REST em ASP.NET Core (.NET 10 LTS) + EF Core + PostgreSQL | passos 3 a 5 abaixo |
| [`frontend/`](frontend/README.md) | React 18 + TypeScript + Vite + Tailwind | passo 6 abaixo |

Convenções, mapa de pastas e regras de cada lado estão nos READMEs de `backend/` e `frontend/`.

Branches e commits citam o código da tarefa do plano (ex.: `feature/P4-FE-02-vistoria`, `T-BE-01: ...`).

---

## Passo a passo (do clone ao sistema rodando)

A ordem é: pré-requisitos, banco, configuração da API, tabelas (migrations), API, front, primeiro acesso e testes. Os comandos são para a pasta raiz do repositório, salvo quando indicado.

### 1. Pré-requisitos

- **.NET SDK 10**. O `backend/global.json` fixa a versão `10.0.100` com `rollForward: latestFeature`, então qualquer SDK 10.0.x serve. Confira com `dotnet --list-sdks`.
- **PostgreSQL**, com um usuário que possa criar banco. O repositório não fixa a versão do PostgreSQL.
- **Node.js 24.x** (campo `engines` do `frontend/package.json`) e npm.
- **Python 3**: só para a conferência do esquema (`backend/tools/compare_schema.py`). Não é preciso para subir o sistema.

### 2. Criar o banco

Crie um banco vazio. O nome `autovia` é o usado no exemplo de configuração:

```bash
psql -U postgres -c "CREATE DATABASE autovia;"
```

Não crie tabelas à mão: quem cria o esquema são as migrations (passo 4).

### 3. Configurar a API

Na pasta `backend/AutoVia.Api`, copie o exemplo para o arquivo local (ele não vai para o git):

```bash
cp appsettings.Local.example.json appsettings.Local.json
```

No Windows, use `copy` no lugar de `cp`. Depois, no `appsettings.Local.json`, troque:

- `ConnectionStrings:AutoViaConnection`: a string de conexão do seu PostgreSQL. O exemplo traz `Host=localhost;Port=5432;Database=autovia;Username=postgres;Password=SUA_SENHA`.
- `Jwt:Chave`: um segredo com 32 caracteres ou mais. Sem ele a API não sobe.

Também dá para usar variáveis de ambiente. Só os nomes, os valores ficam no seu ambiente:

- `ConnectionStrings__AutoViaConnection`, ou `DATABASE_URL` no formato `postgresql://usuario:senha@host:porta/banco` (é o que o Railway injeta no serviço publicado).
- `Jwt__Chave`.

> **Atenção: a `DATABASE_URL` do serviço publicado aponta para o banco publicado.** Nunca copie essa variável, nem a conexão do banco publicado, para a sua máquina. Com ela, o `database update` do passo 4 e o `dotnet run` do passo 5 passam a mexer no banco publicado, sem nenhum aviso. Na sua máquina, as migrations, a API e os testes manuais usam um banco separado, com nome próprio (como o `autovia` do passo 2).

Se nenhuma fonte de conexão existir, a API não sobe e a mensagem diz o que falta. Não commite senha nem segredo: `**/appsettings.Local.json` está no `.gitignore`.

As demais chaves são opcionais, exceto quando a coluna diz o contrário:

| Chave | Para que serve | Obrigatória? | Exemplo (sem segredo) |
|---|---|---|---|
| `Jwt:Chave` | Assina o token de login (HS256) | Sim: 32 caracteres ou mais. Sem ela a API não sobe | `<segredo com 32 caracteres ou mais>` |
| `Jwt:Emissor`, `Jwt:Audiencia`, `Jwt:ExpiracaoMinutos`, `Jwt:ExpiracaoLembrarDias` | Emissor e público do token; validade do login comum (120 min) e do "lembrar de mim" (30 dias) | Não (padrões no `appsettings.json`) | `AutoVia.Api`, `AutoVia.Web`, `120`, `30` |
| `Autenticacao:AdminInicial:Cpf`, `Nome`, `Matricula`, `Senha` | Cria o primeiro administrador ao subir a API (passo 7) | Não. Com `Cpf` vazio, nada é criado | CPF válido só com dígitos; senha com 12 caracteres ou mais; nome até 120; matrícula até 20 |
| `Armazenamento:Diretorio` | Pasta das fotos e dos documentos enviados (provedor `DISCO_LOCAL`) | No Windows fora de contêiner, não: o padrão é `%LOCALAPPDATA%\AutoVia\armazenamento`. Fora do Windows ou em contêiner, sim | Caminho absoluto de uma pasta fora do repositório git e que não seja raiz de disco |
| `Armazenamento:UrlPublicaApi` | Endereço da API usado nos links das fotos | Não quando a API é acessada em localhost. Em outro endereço, sim | `https://api.exemplo.com` |
| `Cors:Origens:0` (variável `Cors__Origens__0`) | Endereço do front liberado pela API | Não. Padrão `http://localhost:5173` | `https://site.exemplo.com`, sem barra no fim |
| `Rede:CabecalhoIpCliente` | Cabeçalho com o IP real do cliente, usado nos limites de requisição atrás de proxy | Não. Padrão `X-Real-IP`; vazio desliga a leitura | `X-Real-IP` |
| `FORWARDEDHEADERS_ENABLED` | Não pode ser `true`: com esse valor a API não sobe | Não. Deixe ausente ou `false` | `false` |
| `Autenticacao:Recuperacao:UrlPublicaFront` | Endereço do front no link do e-mail de redefinição (`/redefinir-senha#token=...`) | Não. Padrão `http://localhost:5173`. Fora de Development precisa ser `https` e não pode ser localhost | `https://site.exemplo.com` |
| `Autenticacao:Recuperacao:ValidadeMinutos`, `LimitePorHora`, `TempoMinimoRespostaMs` | Validade do link (30 min), pedidos por hora (3) e tempo mínimo de resposta (400 ms) | Não (padrões) | padrões do `appsettings.json` |
| `Autenticacao:LimitePublicoPorMinuto`, `Clientes:*`, `Veiculos:*`, `Armazenamento:LimiteLeituraPorMinuto` | Limites de requisição das rotas públicas | Não (padrões no `appsettings.json`) | padrões do `appsettings.json` |
| `Locadoras:CadastroPublicoHabilitado` | Liga o cadastro público de locadora. Com `false`, a rota responde 404 | Não. Padrão `false` | `false` |

**Integrações no banco (login com Google e provedores).** Essas configurações não ficam no `appsettings`. Elas moram nas tabelas `integration` e `integration_setting` do banco, e a API guarda cada uma em cache por 5 minutos. Os valores `DEFINIR_NO_BANCO` fazem a chamada falhar com a mensagem da integração que falta a chave. Para o login real com Google, faça as duas trocas no mesmo passo, na integração `GOOGLE`: a chave `provedor` de `FAKE` para `GOOGLE` e a chave `client_id` com o Client ID (ele é público, sem segredo). Depois confira os dois valores no banco. O front pega o Client ID pela própria API (`GET /api/auth/google/configuracao`).

> **Atenção (segurança): em ambiente publicado, nunca preencha o `client_id` deixando `provedor = FAKE`.** O provedor `FAKE` do Google não confere prova nenhuma: aceita uma credencial de teste como identidade Google com e-mail verificado. Ele passa a responder quando três condições valem juntas: a API roda com `ASPNETCORE_ENVIRONMENT=Development`, a integração `GOOGLE` está em `SANDBOX` (é como a migration a cria) e o `client_id` deixou de ser `DEFINIR_NO_BANCO`. Numa API publicada em `Development`, preencher o `client_id` sem trocar o `provedor` deixa qualquer pessoa forjar uma identidade Google. Em ambiente publicado, rode a API fora de `Development`; se isso não for possível, troque o `provedor` para `GOOGLE` antes de preencher o `client_id`. O Swagger (passo 5) e os provedores de teste de e-mail e de seguradora (seção 10) também dependem de `Development`.

### 4. Criar as tabelas (migrations)

O `database update` altera o banco que estiver configurado, seja ele qual for (veja o aviso do passo 3). Antes de aplicar, confira o alvo. O comando abaixo não mexe no banco e mostra o servidor e o nome do banco configurados:

```bash
cd backend
dotnet tool restore
dotnet dotnet-ef dbcontext info --project AutoVia.Infrastructure --startup-project AutoVia.Api
```

Se o banco mostrado for o esperado, aplique:

```bash
cd backend
dotnet tool restore
dotnet dotnet-ef database update --project AutoVia.Infrastructure --startup-project AutoVia.Api
```

`dotnet tool restore` instala o `dotnet-ef` local (versão fixada em `backend/dotnet-tools.json`). São 21 migrations, na pasta `backend/AutoVia.Infrastructure/Migrations`. Elas criam as tabelas, inserem as integrações (seed), criam a empresa operadora inicial e deixam o armazenamento no provedor de pasta (`DISCO_LOCAL`). Migration já aplicada não é editada; uma correção vira migration nova.

### 5. Subir a API

```bash
cd backend
dotnet run --project AutoVia.Api
```

- A URL da API aparece no terminal, na linha `Now listening on`. Anote-a: ela serve para o `VITE_API_URL` do front (passo 6).
- Conferência de saúde: `GET <url>/api/health` deve responder 200. Essa rota não consulta o banco.
- Swagger: `<url>/swagger`. Ele só existe com o ambiente em `Development` (`ASPNETCORE_ENVIRONMENT=Development`).

### 6. Configurar e subir o front

```bash
cd frontend
npm install
cp .env.example .env
npm run dev
```

No Windows, use `copy` no lugar de `cp`.

- `VITE_API_URL` deve apontar para a URL da API do passo 5. O `.env.example` traz `http://localhost:5000` como exemplo; troque pela porta real.
- O front sobe em `http://localhost:5173` (porta do `vite.config.ts`). A API libera essa origem por padrão. Se a 5173 estiver ocupada, o Vite troca de porta e a API passa a recusar o front por CORS; nesse caso, ajuste `Cors:Origens:0`.
- Mantenha `VITE_IGNORAR_AUTENTICACAO=false`. O valor `true` libera as rotas protegidas sem API e só vale no `npm run dev`.

### 7. Primeiro acesso

1. Antes de subir a API, preencha `Autenticacao:AdminInicial` (passo 3). Ao subir, a API cria o administrador com o CPF e a senha informados. Se o CPF já existir, mesmo desativado, nada é criado de novo. Uma configuração inválida aparece no log, e a API continua no ar.
2. Abra `/entrar` no front, na opção **Equipe**, e entre com o CPF e a senha do administrador. A API recebe `POST /api/auth/login` com `perfil: "FUNCIONARIO"`, `cpf` e `senha`.
3. Com a conta de administrador, cadastre a equipe em `/back-office/equipe` (só o perfil ADMINISTRADOR vê essa tela).
4. Depois do primeiro uso, apague o bloco `AdminInicial` do `appsettings.Local.json`, como pede o próprio exemplo.
5. Clientes se cadastram em `/cadastro`. O cadastro público de locadora fica desligado por padrão (`Locadoras:CadastroPublicoHabilitado`).

### 8. Rodar os testes

Backend. Os testes não precisam de banco: usam repositórios de mentira.

```bash
cd backend
dotnet build AutoVia.sln
dotnet test AutoVia.sln
```

Frontend:

```bash
cd frontend
npm test
npm run lint
npm run build
```

O `npm run build` local funciona sem `VITE_API_URL`. A exigência de uma URL `https` vale só no build do Vercel.

### 9. Conferência do esquema e documentação do banco

- [`backend/docs/CONFERENCIA_ESQUEMA.md`](backend/docs/CONFERENCIA_ESQUEMA.md): diferenças entre o esquema atual e os apêndices A e B da especificação, com a decisão de cada uma.
- [`backend/docs/especificacao/`](backend/docs/especificacao/): apêndices A e B (SQL) e `desvios_aprovados.json`.
- [`backend/tools/compare_schema.py`](backend/tools/compare_schema.py): o script da conferência. O uso está no início do arquivo.
- [`backend/docs/sql/`](backend/docs/sql/): o SQL de cada migration, para conferência.

### 10. O que é provedor de teste (FAKE) e o que depende de conta externa

| Integração | Provedor de teste (FAKE) | Conta externa para valer |
|---|---|---|
| PAGAMENTO (PIX e caução no cartão) | Padrão `FAKE` (simulação em memória, sem chamada à operadora) | Conta no Mercado Pago (`MERCADOPAGO`): `access_token` e `public_key`, preenchidos no banco |
| EMAIL (recuperação de senha e avisos) | É o único provedor do código. Só funciona com `ASPNETCORE_ENVIRONMENT=Development` e a integração em `SANDBOX`. Os e-mails viram arquivos `.txt` em `backend/AutoVia.Api/App_Data/email-fake/`. Fora de Development o provedor recusa o envio: nenhum e-mail é gravado e a falha fica só no log | Não há provedor de e-mail real no código |
| DOCUMENTO (OCR da CNH) | É o único provedor do código | Não há provedor real no código |
| GOOGLE (login com conta Google) | Padrão `FAKE`. Num banco novo o login com Google fica desligado, porque o `client_id` vem como `DEFINIR_NO_BANCO`. Com o `client_id` preenchido, o `FAKE` só funciona com `ASPNETCORE_ENVIRONMENT=Development` e a integração em `SANDBOX`. Não use o `FAKE` em ambiente publicado: veja o aviso de segurança do passo 3 | Client ID do Google (`client_id`) e `provedor = GOOGLE`, trocados no mesmo passo |
| SEGURADORA | É o único provedor do código. Só funciona com `ASPNETCORE_ENVIRONMENT=Development` e a integração em `SANDBOX`. Fora de Development o provedor recusa a chamada, com erro de integração | Não há provedor real no código |
| MULTAS | É o único provedor do código. Exige a integração em `SANDBOX` | Não há provedor real no código |
| ARMAZENAMENTO (fotos e documentos) | Provedor `DISCO_LOCAL`, a pasta da máquina (padrão do banco novo) | Só para o `S3_COMPATIVEL`: bucket e chaves de uma conta de armazenamento S3 |

Por isso, com o banco novo, sem contas externas e com a API em `Development`, o sistema roda com pagamento, e-mail, OCR, seguradora e multas de teste. Todo provedor `FAKE` exige a integração em `SANDBOX`, que é como as migrations criam todas. Os de EMAIL, GOOGLE e SEGURADORA exigem também a API em `Development` (`ASPNETCORE_ENVIRONMENT=Development`): fora dele, o e-mail e a seguradora de teste param de funcionar, e o Swagger deixa de existir (passo 5). Os valores reais dessas integrações ficam no banco, não no repositório.

---

## Estrutura de pastas

| Pasta | O que é |
|---|---|
| [`backend/`](backend/README.md) | API, banco (migrations), integrações e testes |
| [`frontend/`](frontend/README.md) | Site: telas, rotas, componentes e testes |
