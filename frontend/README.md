# AutoVia: frontend

Fundação do frontend da AutoVia (locadora que aluga e vende veículos da própria frota):
design system, componentes de base, layouts e rotas. As telas ainda são
**esqueletos**: cada rota mostra o título, o código da tarefa e o protótipo de
referência. As telas reais entram módulo a módulo, a partir dos protótipos e dos
contratos do backend.

## Stack

React 18 + TypeScript (strict) · Vite · React Router v6 · TanStack Query v5 ·
Axios · React Hook Form + Zod · Tailwind CSS v3.4. Sem biblioteca de componentes
pronta: os componentes de base são próprios, em `src/components/ui/`.
Fonte Inter e ícones Material Symbols (os mesmos dos protótipos), via Google Fonts.

## Como rodar

Pré-requisito: Node 20.19+ (testado com Node 24 e npm 11).

```bash
cd frontend
npm install
cp .env.example .env      # no Windows: copy .env.example .env
npm run dev               # http://localhost:5173
```

| Script            | O que faz                                        |
| ----------------- | ------------------------------------------------ |
| `npm run dev`     | Servidor de desenvolvimento                      |
| `npm run build`   | Checagem de tipos (`tsc -b`) + build de produção |
| `npm run lint`    | Lint (oxlint, o padrão do template do Vite)      |
| `npm run preview` | Serve o build de produção localmente             |
| `npm test`        | Testes unitários (Vitest, `*.test.ts` ao lado do arquivo) |

### Variáveis de ambiente (`.env`)

| Variável                    | Exemplo                 | Uso                                                                                                                                  |
| --------------------------- | ----------------------- | ------------------------------------------------------------------------------------------------------------------------------------ |
| `VITE_API_URL`              | `http://localhost:5000` | URL base da API (ASP.NET Core)                                                                                                       |
| `VITE_IGNORAR_AUTENTICACAO` | `false`                 | `true` libera as rotas protegidas sem passar pela API de login, só para navegar no `npm run dev` com o backend parado. **Só vale no `npm run dev`**: no build a proteção sempre é aplicada. |

## Estrutura de pastas

```
frontend/
├── index.html               fontes (Inter e Material Symbols) e ponto de entrada
├── tailwind.config.js       design system (tokens de cor, tipografia, raio, sombra)
├── vite.config.ts           atalho "@/" → src/
└── src/
    ├── main.tsx             QueryClientProvider → AvisoProvider → AutenticacaoProvider → RouterProvider
    ├── index.css            Tailwind + estilos base
    ├── api/
    │   ├── client.ts        instância única do Axios + interceptadores (token e ProblemDetails → ErroApi)
    │   ├── autenticacao.ts  login, recuperação de senha e login com Google (contratos auth*)
    │   ├── clientes.ts      cadastro de clientes
    │   └── README.md        como cada módulo ganha seu arquivo quando o contrato sair
    ├── components/
    │   ├── ui/              componentes de base (ver lista abaixo) + index.ts
    │   │   └── aviso/       notificações: AvisoProvider e useAviso()
    │   ├── PaginaEsqueleto.tsx, CarregandoPagina.tsx, Logotipo.tsx, AvisoModoDesenvolvimento.tsx
    ├── features/            uma pasta por processo; cada uma com pages/ (e hooks/ quando houver contrato)
    │   ├── frota/ reservas/ locacoes/ devolucao/
    │   ├── vendas/ financeiro/ manutencao/ seguros/
    │   ├── area-cliente/    telas "Minhas ..." do cliente (tarefas T-FE)
    │   └── clientes/        acesso/ (login, P2-FE-01), cadastro/ (cadastro, P2-FE-02), pages/
    │       ├── google/      login com Google (N3): botão, bloco, alerta, decisão e vínculo (tela /entrar/vincular-google)
    │       ├── recuperacao/ recuperação de senha (N1): cartões, alertas, validação e estado de navegação
    │       └── hooks/       useEntrar (login: abre a sessão pelo contexto), useCadastrarCliente
    ├── hooks/               useDebounce, useTituloDocumento
    ├── layouts/             LayoutPublico, LayoutBackOffice
    ├── lib/                 formatters, mensagens (+ mensagensAcesso, mensagensCadastro, mensagensRecuperacao, mensagensGoogle), sessao, erros,
    │                        validadores, conversaoLogin, ambiente, queryClient,
    │                        credencialGoogle (memória do login com Google), googleIdentity (carregador do script do Google),
    │                        preenchimentoGoogle (nome e e-mail do Google para o cadastro, em memória), tokenRecuperacao (token do link)
    ├── routes/              rotas.tsx, caminhos.ts, paginas.ts (lazy), RotaProtegida, 404, erro e acesso negado
    │   └── autenticacao/    contexto mínimo de autenticação (useAutenticacao)
    └── types/               api.ts (ProblemDetails, ErroApi, Pagina<T>), enums.ts, autenticacao.ts (sessão e login), clientes.ts (cadastro), google.ts (script do Google)
```

## Regras do projeto

- **Cores só por token** (`bg-navy`, `text-texto-suave`, `bg-status-sucesso-fundo`...). A paleta
  padrão do Tailwind foi substituída: só existem os tokens do `tailwind.config.js`.
- **Espaçamento em grade de 8px**: prefira as classes pares (`2`, `4`, `6`, `8`...).
- **Formatação só por `@/lib/formatters`**: moeda, número, km, data, data e hora, CPF, placa
  (Mercosul e antiga), telefone e CEP. Nunca formate dentro do componente.
- **Mensagens fixas em `@/lib/mensagens`**, incluindo `MENSAGEM_ERRO_API_PADRAO`.
  Nunca mostre a mensagem técnica do backend.
- **API**: nenhum endpoint inventado. Cada módulo ganha `src/api/<modulo>.ts` quando o contrato
  sair; as telas acessam a API **somente por hooks do TanStack Query**, nunca pelo Axios direto.
- **Erros da API** sempre chegam como `ErroApi { status, mensagem, erros? }`. A mensagem vem de
  `detail`, senão do primeiro item de `errors`, senão de `title`, senão de uma mensagem padrão
  (mensagens técnicas ou em inglês são descartadas; erro 500 sempre usa a genérica). Sem resposta =
  mensagem de falha de conexão. Exiba com `obterMensagemErro(erro)` (`@/lib/erros`).
- **Tipos**: toda prop, payload, estado e resposta tem `interface`/`type`. Proibido `any`
  (o lint acusa erro).
- **Textos** visíveis sempre em português do Brasil.

## Rotas

Versões mobile usam a **mesma rota** da versão desktop (layout responsivo).

| Caminho                                         | Página                        | Tarefa                | Acesso      |
| ----------------------------------------------- | ----------------------------- | --------------------- | ----------- |
| `/`                                             | InicioPagina                  | P3-FE-01              | pública     |
| `/entrar`                                       | AcessoPortalPagina            | P2-FE-01              | pública     |
| `/entrar/vincular-google`                       | VincularGooglePagina          | N3 (RF-51)            | pública     |
| `/recuperar-senha`                              | RecuperarSenhaPagina          | N1 (RF-49)            | pública     |
| `/redefinir-senha`                              | RedefinirSenhaPagina          | N1 (RF-49)            | pública     |
| `/cadastro`                                     | redireciona para `/cadastro/dados-pessoais` | –       | pública     |
| `/cadastro/dados-pessoais`                      | CadastroDadosPessoaisPagina   | P2-FE-02              | pública     |
| `/cadastro/documentos`                          | CadastroDocumentosPagina      | P2-FE-03              | pública     |
| `/veiculos`                                     | ResultadosBuscaPagina         | P3-FE-02 (+ P3-FE-07) | pública     |
| `/veiculos/:veiculoId`                          | DetalheVeiculoPagina          | P3-FE-03              | pública     |
| `/seminovos`                                    | CatalogoSeminovosPagina       | P6-FE-01 (+ P6-FE-04) | pública     |
| `/seminovos/:veiculoId/proposta`                | PropostaCompraPagina          | P6-FE-02              | pública     |
| `/reservas/checkout/:veiculoId`                 | CheckoutReservaPagina         | P3-FE-04 (+ P3-FE-08) | cliente     |
| `/reservas/:reservaId/confirmacao`              | ConfirmacaoReservaPagina      | P3-FE-05              | cliente     |
| `/minhas-reservas`                              | MinhasReservasPagina          | T-FE-03 (+ T-FE-04)   | cliente     |
| `/minhas-locacoes`                              | MinhasLocacoesPagina          | T-FE-02               | cliente     |
| `/meus-pagamentos`                              | PagamentosFaturasPagina       | P7-FE-02              | cliente     |
| `/back-office`                                  | redireciona para `/back-office/frota` | –             | funcionário |
| `/back-office/frota`                            | PainelFrotaPagina             | P1-FE-01              | funcionário |
| `/back-office/frota/veiculos`                   | ListagemFrotaPagina           | P1-FE-02              | funcionário |
| `/back-office/frota/veiculos/novo`              | CadastroVeiculoPagina         | P1-FE-03              | funcionário |
| `/back-office/clientes`                         | redireciona para `/back-office/clientes/verificacao` | – | funcionário |
| `/back-office/clientes/verificacao`             | VerificacaoCadastrosPagina    | P2-FE-04              | funcionário |
| `/back-office/reservas`                         | PainelReservasPagina          | P3-FE-06              | funcionário |
| `/back-office/locacoes`                         | PainelReservasPagina          | P3-FE-06              | funcionário |
| `/back-office/locacoes/retirada/:reservaId`     | RetiradaVeiculoPagina         | P4-FE-01              | funcionário |
| `/back-office/locacoes/:locacaoId/vistoria`     | VistoriaAvariasPagina         | P4-FE-02              | funcionário |
| `/back-office/locacoes/:locacaoId/contrato`     | ContratoDigitalPagina         | P4-FE-03              | funcionário |
| `/back-office/locacoes/:locacaoId/devolucao`    | CheckoutDevolucaoPagina       | P5-FE-01              | funcionário |
| `/back-office/vendas`                           | GestaoVendasPagina            | P6-FE-03              | funcionário |
| `/back-office/financeiro`                       | PainelFinanceiroPagina        | P7-FE-01              | funcionário |
| `/back-office/manutencao`                       | ManutencaoFrotaPagina         | P8-FE-01              | funcionário |
| `/back-office/manutencao/custos-e-resultado`    | CustoResultadoFrotaPagina     | P8-FE-02              | funcionário |
| `/back-office/seguros`                          | GestaoSegurosPagina           | P9-FE-01              | funcionário |
| `/back-office/seguros/sinistros/:sinistroId`    | DetalheSinistroPagina         | P9-FE-02              | funcionário |
| qualquer outro                                  | PaginaNaoEncontrada (404)     | –                     | –           |

Os caminhos ficam em `src/routes/caminhos.ts` (`CAMINHOS`). Para montar um link com parâmetro:
`generatePath(CAMINHOS.detalheVeiculo, { veiculoId: String(id) })`.

## Componentes de base

Importe de `@/components/ui`. Todos aceitam `className` quando indicado.

| Componente | Props |
| --- | --- |
| **Botao** | `variante?: 'primario' \| 'secundario' \| 'contorno' \| 'fantasma' \| 'perigo'` (padrão `primario`), `tamanho?: 'pequeno' \| 'medio' \| 'grande'`, `carregando?`, `larguraTotal?`, `icone?` e `iconeDireita?` (nome Material Symbols), e todas as props de `<button>` (`type` padrão `button`). `forwardRef`. |
| **BotaoLink** | Mesmas props visuais do Botao + props de `Link` do React Router (`to`, `replace`, `state`...). |
| **Campo** | `rotulo: string`, `erro?`, `dica?`, `obrigatorio?` (mostra `*` e `aria-required`), `ocultarRotulo?`, `mascara?: (valor) => string` (ex.: `formatarCpf`), e todas as props de `<input>`. `forwardRef`, compatível com `register()`. |
| **CampoSenha** | Como o Campo, sem `type` nem `mascara`: botão mostrar/ocultar a senha (`aria-pressed`, não envia o formulário). Use `autoComplete="current-password"` (login) ou `"new-password"` (cadastro). `acaoRotulo?` (ReactNode, opcional): ação na linha do rótulo, à direita, antes do campo no Tab (ex.: "Esqueci minha senha"); sem ela, o componente não muda. `forwardRef`, compatível com `register()`. |
| **Select** | `rotulo: string`, `opcoes: OpcaoSelect[]` (`{ valor, rotulo, desabilitada? }`), `erro?`, `dica?`, `obrigatorio?`, `ocultarRotulo?`, `opcaoVazia?: string \| false` (padrão "Selecione"), e todas as props de `<select>`. `forwardRef`, compatível com `register()`. |
| **Tabela\<T\>** | `colunas: ColunaTabela<T>[]` (`{ chave, titulo, renderizar(item), alinhamento?, classe?, ocultarTitulo? }`), `dados: T[]`, `obterChave(item)`, `legenda: string` (para leitores de tela), `carregando?` (linhas de esqueleto), `linhasEsqueleto?` (padrão 5), `vazio?: ReactNode` (padrão `<EstadoVazio />`). |
| **Etiqueta** | `status: StatusDominio` (define texto e tom), `rotulo?`, `tom?`; ou etiqueta livre com `rotulo` + `tom: 'sucesso' \| 'pendente' \| 'perigo' \| 'neutro'`. Sempre exibe texto. |
| **Modal** | `aberto`, `aoFechar()`, `titulo`, `descricao?`, `children`, `rodape?`, `tamanho?: 'pequeno' \| 'medio' \| 'grande'`, `fecharAoClicarFora?` (padrão `true`). `<dialog>` nativo: foco preso, Esc fecha, foco volta ao gatilho. |
| **Paginacao** | `pagina`, `tamanhoPagina`, `total` (do `Pagina<T>`), `aoMudarPagina(pagina)`, `desabilitada?`, `rotuloItens?` (padrão "registros"). Página começa em 1. |
| **EstadoVazio** | `titulo?` (padrão "Nenhum registro encontrado."), `descricao?`, `icone?`, `acao?: ReactNode`. |
| **EstadoErro** | `titulo?`, `mensagem?`, `aoTentarNovamente?()` (mostra "Tentar novamente"), `tentando?`. |
| **Esqueleto** | `className?` (tamanho e forma, ex.: `"h-4 w-32"`). |
| **Cartao** | `titulo?`, `descricao?`, `acoes?: ReactNode`, `children`, `semEspacamento?`, `className?`. |
| **Icone** | `nome` (Material Symbols), `tamanho?` (px, padrão 20), `preenchido?`, `rotulo?` (sem rótulo é decorativo), `className?`. |
| **AvisoProvider / useAviso()** | `useAviso()` devolve `sucesso(mensagem)` e `erro(mensagem)`. Erros com `role="alert"`; somem sozinhos (5 s sucesso, 8 s erro). |

Também disponíveis: `useAutenticacao()` (`usuario`, `autenticado`, `autenticacaoIgnorada`,
`entrar(token, usuario)`, `sair()`, `possuiPerfil(perfis)`), `useDebounce(valor, atrasoMs?)`
e `useTituloDocumento(titulo)`.

### Padrão para as telas (Prompt 5)

```tsx
const aviso = useAviso();
const consulta = useAlgo();                       // hook do TanStack Query em features/<processo>/hooks
if (consulta.isPending) return <Tabela carregando ... />;
if (consulta.isError) return <EstadoErro mensagem={obterMensagemErro(consulta.error)} aoTentarNovamente={() => consulta.refetch()} />;
// mutação: onSuccess → aviso.sucesso(...) + invalidateQueries; onError → aviso.erro(obterMensagemErro(erro))
```
