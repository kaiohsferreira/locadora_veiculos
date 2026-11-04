# AutoVia — Backend

API REST em **ASP.NET Core (.NET 10 LTS)**, **EF Core 10 + Npgsql (PostgreSQL)**, AutoMapper, FluentValidation, RestSharp, Swashbuckle e xUnit.

Fundação gerada a partir do Prompt 1 do guia da equipe (tarefa **T-BE-01**). Os controllers de negócio chegam com o Prompt 3 de cada módulo.

> **Mudança de stack:** o Prompt 1 pedia .NET 8. O suporte do .NET 8 termina em 10/11/2026, antes da entrega (30/11), por isso a solução usa o **.NET 10 (LTS, com suporte até novembro de 2028)**. Todos precisam do **SDK do .NET 10** instalado (`dotnet --list-sdks`). O `global.json` fixa a versão 10.

## Pré-requisitos

- SDK do .NET 10
- PostgreSQL 14 ou superior
- Python 3, apenas para o script de conferência do esquema (opcional)

## Como rodar

```bash
cd backend
dotnet tool restore          # instala o dotnet-ef local (dotnet-tools.json)
dotnet build AutoVia.sln
dotnet test AutoVia.sln
dotnet run --project AutoVia.Api --launch-profile http
```

- Swagger: <http://localhost:5000/swagger>
- Saúde da API: <http://localhost:5000/api/health>

### Conexão com o banco

O `appsettings.json` traz uma conexão local padrão (`Host=localhost;Database=autovia;Username=postgres;Password=postgres`). Para usar a **sua** senha sem alterar arquivo versionado:

1. Copie `AutoVia.Api/appsettings.Local.example.json` para `AutoVia.Api/appsettings.Local.json`. Esse arquivo está no `.gitignore`.
2. Ajuste a `AutoViaConnection`.

Também funcionam a variável `ConnectionStrings__AutoViaConnection` e a `DATABASE_URL` (`postgresql://usuario:senha@host:5432/banco`).

### Banco: criado pelas migrations

O banco **não** é criado rodando SQL à mão. Quem cria o esquema são as migrations:

| Migration | O que faz |
|---|---|
| `Inicial` | Cria as 16 tabelas (14 de negócio + `integration` e `integration_setting`), com todas as restrições e índices da especificação |
| `SeedIntegracoes` | Insere as 6 integrações e as chaves de cada uma, via **SQL explícito** (`migrationBuilder.Sql`). Não usa `HasData`, porque id fixo em `SERIAL` não avança a sequência |

```bash
# aplicar no banco local (cada um no seu ambiente)
dotnet dotnet-ef database update --project AutoVia.Infrastructure --startup-project AutoVia.Api

# gerar uma migration nova depois de alterar entidade/configuração
dotnet dotnet-ef migrations add NomeDaMudanca --project AutoVia.Infrastructure --startup-project AutoVia.Api --output-dir Migrations

# exportar o SQL para conferência
dotnet dotnet-ef migrations script 0 Inicial --project AutoVia.Infrastructure --startup-project AutoVia.Api -o docs/sql/01_Inicial.sql
```

Regras:
- Migration já aplicada **nunca** é editada. Uma correção vira migration nova.
- Depois de gerar, procure e remova qualquer `.Annotation("Npgsql:IdentitySequenceOptions", ...)`. Com `UseSerialColumns()`, normalmente não aparece nenhuma.
- A conferência contra a especificação está em [`docs/CONFERENCIA_ESQUEMA.md`](docs/CONFERENCIA_ESQUEMA.md). Resultado atual: **0 diferenças**.

## Estrutura

```text
backend/
├─ AutoVia.sln · Directory.Build.props · global.json · dotnet-tools.json
├─ AutoVia.Helpers/          constantes, mensagens em PT-BR, StaticMethods (data, moeda, CPF, placa)
├─ AutoVia.Communication/    VOs por módulo, validadores FluentValidation, exceções de negócio
│  ├─ ViewObjects/<Modulo>/  VOs de entrada ({X}VO) e de saída (Return{X}VO)
│  ├─ Validators/<Modulo>/   um AbstractValidator por VO de entrada
│  └─ CustomExceptions/      BusinessException (409), NotFoundException (404), IntegrationException (502)
├─ AutoVia.Core/             centro da solução, SEM EF Core
│  ├─ Entities/PgSql/        as 14 entidades de negócio (+ BaseEntities, só para integrações)
│  ├─ Enums/                 valores fixos (status, tipo...), gravados como texto
│  ├─ RepositoriesInterface/ contratos dos repositórios
│  ├─ ServicesInterface/     contratos dos serviços (+ ILogService)
│  └─ Profiles/              profiles do AutoMapper
├─ AutoVia.Integrations/     biblioteca ÚNICA de integrações externas
│  ├─ Entities/              Integration, IntegrationSetting
│  ├─ Communication/Base/    RequestInfo, Headers, RequestResponse<T>, ConfiguracaoIntegracao
│  ├─ Communication/<Prov>/  VOs de cada provedor (Prompt 2)
│  ├─ Interfaces/            IIntegrationResolver, IComunicacaoService, IIntegrationRepository
│  ├─ Requests/              cliente HTTP de cada provedor (Prompt 2)
│  ├─ Services/              IntegrationResolver (cache), ComunicacaoService (RestSharp)
│  ├─ Helpers/               códigos, serialização, máscara, HMAC
│  └─ Extensions/            AddIntegrationsStartup.cs (registro de TODOS os provedores)
├─ AutoVia.Infrastructure/   acesso a dados
│  ├─ Persistence/           AutoViaDbContext + Configurations/ (uma por entidade)
│  ├─ Repositories/          implementação dos repositórios
│  ├─ Services/              implementação dos serviços
│  └─ Migrations/            Inicial, SeedIntegracoes
├─ AutoVia.Api/              controllers, Extensions/Add*Startup.cs, Filters/, Middleware/
├─ AutoVia.Tests/            xUnit (sem PostgreSQL real)
├─ docs/                     especificação, SQL exportado e conferência
└─ tools/compare_schema.py   conferência automática do esquema
```

Dependências (sempre para dentro): `Api → Infrastructure → Integrations → Core → Communication → Helpers`. O Core nunca referencia o EF Core, e nenhum controller acessa o DbContext.

> O Prompt 1 dizia "Communication depende apenas de Core". Isso criaria uma referência circular, porque o Core guarda os Profiles e as interfaces de serviço, que usam os VOs. Por isso a seta é `Core → Communication`.

## Para quem vai usar o Prompt 3 (módulos)

O Prompt 3 cita um projeto `AutoVia.Application`, que **não existe** nesta solução. Ao colar o prompt, indique à IA onde cada coisa fica:

| O Prompt 3 diz | Nesta solução |
|---|---|
| `AutoVia.Application/DTOs/<Modulo>` | `AutoVia.Communication/ViewObjects/<Modulo>` (`{X}VO` para entrada, `Return{X}VO` para saída) |
| Validadores FluentValidation | `AutoVia.Communication/Validators/<Modulo>` (modelo: `Validators/Utils/FiltroPaginacaoVOValidator.cs`) |
| `AutoVia.Application/Interfaces` (repositório) | `AutoVia.Core/RepositoriesInterface` |
| `AutoVia.Application/Interfaces` (serviço) | `AutoVia.Core/ServicesInterface` |
| `AutoVia.Application/Services` | `AutoVia.Infrastructure/Services` |
| `AutoVia.Infrastructure/Repositories` | igual |
| Registro de DI | `AutoVia.Api/Extensions/AddRepositoriesStartup.cs` e `AddServicesStartup.cs` |
| Profiles do AutoMapper | `AutoVia.Core/Profiles` |

Para o Prompt 2 (provedores), o registro fica em `AutoVia.Integrations/Extensions/AddIntegrationsStartup.cs`.

## Convenções obrigatórias

- **Tipagem explícita, nunca `var`.**
- **`try/catch` com log** (`_logService.Write(...)`) em repositório, serviço e controller.
- **Nenhuma string mágica:** toda mensagem vem de `AutoVia.Helpers/ConstantsMessage*.cs`.
- **Controller nunca devolve entidade:** devolve sempre um VO de saída.
- **Leitura com `AsNoTracking()`.** Operação que altera mais de uma tabela roda em transação.
- **Formato de erro:** ProblemDetails (RFC 7807) em português, montado pelo `ErrorHandlingMiddleware`:
  - `NotFoundException` → 404
  - `BusinessException` → 409
  - erro de validação → 400
  - `IntegrationException` → 502
  - qualquer outro → 500, sem detalhe técnico
- **Códigos HTTP:** 200 leitura · 201 + Location criação · 204 atualização · 400 · 404 · 409.
- **Listagem paginada:** `?pagina=1&tamanhoPagina=20` (máximo 100). Herde `FiltroPaginacaoVO` e devolva `PaginaVO<T>` (`itens`, `total`, `pagina`, `tamanhoPagina`).
- **Enums** trafegam como texto (`"DISPONIVEL"`), igual ao banco. Não reordene os membros: o primeiro é o DEFAULT da coluna.
- **Datas:** colunas `TIMESTAMP` usam `DateTime.Now` (o Npgsql recusa `DateTime` UTC nessas colunas); colunas `DATE` usam `DateOnly`.
- **Swagger:** todo endpoint tem `/// <summary>`.

## Integrações: configuração no banco

URL, timeout e credenciais **nunca** vêm de appsettings, de variável de ambiente ou de constante. Vêm das tabelas `integration` e `integration_setting`, lidas pelo `IIntegrationResolver`, que guarda a configuração em cache por 5 minutos.

- O seed cria as 6 integrações em `SANDBOX`, com as chaves protegidas valendo `DEFINIR_NO_BANCO`. Preencha os valores reais **direto no banco** de cada ambiente.
- Enquanto uma chave estiver com `DEFINIR_NO_BANCO`, `GetRequired("chave")` lança uma `IntegrationException` dizendo qual integração e qual chave falta.
- Valor com `protegido = true` nunca vai para log nem para resposta da API. Use `ConfiguracaoIntegracao.ProtectedValues` em `RequestInfo.SensitiveValues`.
- Toda chamada externa passa pelo `IComunicacaoService`. Ele aplica o timeout da tabela e só tenta de novo em falha de rede ou erro 5xx (no máximo 3 vezes, com espera progressiva). POST e PATCH só são repetidos com `IdempotencyKey`.

## Observações

- **AutoMapper 16:** a partir da versão 15, o AutoMapper exige uma chave de licença; sem ela, só registra um aviso no log e continua funcionando. A licença *Community* é gratuita para uso educacional (luckypennysoftware.com). Ficamos na 16 porque a 14, a última gratuita sem chave, tem uma vulnerabilidade alta (GHSA-rvv3-g6hj-g44x) sem correção.
- **Autenticação** (JWT, perfis de cliente e de funcionário) entra na **P2-BE-01**. O `Program.cs` já tem o lugar marcado.
