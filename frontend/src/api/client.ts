/**
 * Instância única do Axios da aplicação.
 *
 * - Requisição: põe `Authorization: Bearer <token>` quando há sessão.
 * - Resposta: converte qualquer erro em `ErroApi` com mensagem legível em
 *   português, a partir do ProblemDetails (RFC 7807) do backend.
 *
 * Regra: componentes NUNCA importam este arquivo. Cada módulo ganha seu
 * arquivo em `src/api/` (funções tipadas) quando o contrato do backend sair,
 * e as telas acessam a API somente por hooks do TanStack Query.
 */
import axios, { AxiosError, type AxiosInstance } from 'axios';
import { URL_API } from '@/lib/ambiente';
import {
  MENSAGEM_ACESSO_NEGADO,
  MENSAGEM_CONFLITO,
  MENSAGEM_DADOS_INVALIDOS,
  MENSAGEM_ERRO_API_PADRAO,
  MENSAGEM_ERRO_SERVIDOR,
  MENSAGEM_FALHA_CONEXAO,
  MENSAGEM_OPERACAO_CANCELADA,
  MENSAGEM_REGISTRO_NAO_ENCONTRADO,
  MENSAGEM_SESSAO_EXPIRADA,
  MENSAGEM_TEMPO_ESGOTADO,
} from '@/lib/mensagens';
import { limparSessao, obterToken } from '@/lib/sessao';
import { ErroApi, type ProblemDetails } from '@/types/api';

const TEMPO_LIMITE_MS = 30_000;
const TAMANHO_MAXIMO_MENSAGEM = 300;

export const api: AxiosInstance = axios.create({
  baseURL: URL_API,
  timeout: TEMPO_LIMITE_MS,
  headers: { Accept: 'application/json' },
});

// Interceptador de requisição: token da sessão --------------------------------

api.interceptors.request.use((config) => {
  const token = obterToken();
  if (token) {
    config.headers.set('Authorization', `Bearer ${token}`);
  }
  return config;
});

// Interceptador de resposta: erro legível -----------------------------------

api.interceptors.response.use(
  (resposta) => resposta,
  (erro: unknown) => {
    const erroApi = converterParaErroApi(erro);
    if (erroApi.status === 401) {
      // Token ausente, inválido ou expirado: encerra a sessão local.
      limparSessao();
    }
    return Promise.reject(erroApi);
  },
);

// Conversão -----------------------------------------------------------------

/** Trechos que denunciam mensagem técnica (stack trace, serialização, padrão do framework). */
const PADROES_MENSAGEM_TECNICA: readonly RegExp[] = [
  /json/i,
  /exception/i,
  /\bSystem\./,
  /\bMicrosoft\./,
  /\bNpgsql\b/i,
  /stack\s?trace/i,
  /\bat\s+[\w$.<>`]+\(/,
  /LineNumber|BytePosition|Path:\s*\$/i,
  /object reference not set/i,
  /one or more validation errors occurred/i,
  /an error occurred while processing your request/i,
  /^the .+ field is required\.?$/i,
  /<\/?[a-z][^>]*>/i,
];

/** Títulos padrão do HTTP/ASP.NET em inglês, que não servem para o usuário. */
const TITULOS_PADRAO_HTTP = new Set([
  'bad request',
  'unauthorized',
  'forbidden',
  'not found',
  'method not allowed',
  'conflict',
  'unsupported media type',
  'unprocessable entity',
  'too many requests',
  'internal server error',
  'service unavailable',
]);

function ehMensagemTecnica(texto: string): boolean {
  const limpo = texto.trim();
  if (limpo.length === 0 || limpo.length > TAMANHO_MAXIMO_MENSAGEM) return true;
  if (TITULOS_PADRAO_HTTP.has(limpo.toLowerCase())) return true;
  return PADROES_MENSAGEM_TECNICA.some((padrao) => padrao.test(limpo));
}

function mensagemAproveitavel(texto: string | undefined): string | undefined {
  if (texto === undefined || ehMensagemTecnica(texto)) return undefined;
  return texto.trim();
}

function ehListaDeTextos(valor: unknown): valor is string[] {
  return Array.isArray(valor) && valor.every((item) => typeof item === 'string');
}

/** Lê o corpo da resposta como ProblemDetails, descartando campos com tipo inesperado. */
function lerProblemDetails(dados: unknown): ProblemDetails | null {
  if (typeof dados !== 'object' || dados === null || Array.isArray(dados)) return null;
  const corpo = dados as Record<string, unknown>;
  const problema: ProblemDetails = {};

  if (typeof corpo.type === 'string') problema.type = corpo.type;
  if (typeof corpo.title === 'string') problema.title = corpo.title;
  if (typeof corpo.status === 'number') problema.status = corpo.status;
  if (typeof corpo.detail === 'string') problema.detail = corpo.detail;

  if (typeof corpo.errors === 'object' && corpo.errors !== null && !Array.isArray(corpo.errors)) {
    const erros: Record<string, string[]> = {};
    for (const [campo, mensagens] of Object.entries(corpo.errors as Record<string, unknown>)) {
      if (ehListaDeTextos(mensagens)) erros[campo] = mensagens;
    }
    problema.errors = erros;
  }

  return problema;
}

/** Mantém só as mensagens de campo aproveitáveis; remove campos que ficarem vazios. */
function filtrarErrosDeCampo(
  erros: Record<string, string[]> | undefined,
): Record<string, string[]> | undefined {
  if (!erros) return undefined;
  const filtrados: Record<string, string[]> = {};
  for (const [campo, mensagens] of Object.entries(erros)) {
    const aproveitaveis = mensagens
      .map((mensagem) => mensagemAproveitavel(mensagem))
      .filter((mensagem): mensagem is string => mensagem !== undefined);
    if (aproveitaveis.length > 0) filtrados[campo] = aproveitaveis;
  }
  return Object.keys(filtrados).length > 0 ? filtrados : undefined;
}

function primeiroErroDeCampo(erros: Record<string, string[]> | undefined): string | undefined {
  if (!erros) return undefined;
  for (const mensagens of Object.values(erros)) {
    if (mensagens.length > 0) return mensagens[0];
  }
  return undefined;
}

function mensagemPadraoPorStatus(status: number): string {
  switch (status) {
    case 400:
    case 422:
      return MENSAGEM_DADOS_INVALIDOS;
    case 401:
      return MENSAGEM_SESSAO_EXPIRADA;
    case 403:
      return MENSAGEM_ACESSO_NEGADO;
    case 404:
      return MENSAGEM_REGISTRO_NAO_ENCONTRADO;
    case 409:
      return MENSAGEM_CONFLITO;
    default:
      return status >= 500 ? MENSAGEM_ERRO_SERVIDOR : MENSAGEM_ERRO_API_PADRAO;
  }
}

/**
 * Ordem de escolha da mensagem: `detail`, senão o primeiro item de `errors`,
 * senão `title`, senão a mensagem padrão do status. Mensagens técnicas são
 * ignoradas em todas as etapas. Erro 500 sempre usa a mensagem genérica.
 */
function converterParaErroApi(erro: unknown): ErroApi {
  if (erro instanceof ErroApi) return erro;

  if (axios.isCancel(erro)) {
    return new ErroApi({ status: 0, mensagem: MENSAGEM_OPERACAO_CANCELADA });
  }

  if (!axios.isAxiosError(erro)) {
    return new ErroApi({ status: 0, mensagem: MENSAGEM_ERRO_API_PADRAO });
  }

  if (!erro.response) {
    const tempoEsgotado =
      erro.code === AxiosError.ECONNABORTED || erro.code === AxiosError.ETIMEDOUT;
    return new ErroApi({
      status: 0,
      mensagem: tempoEsgotado ? MENSAGEM_TEMPO_ESGOTADO : MENSAGEM_FALHA_CONEXAO,
    });
  }

  const { status, data } = erro.response;

  if (status === 500) {
    return new ErroApi({ status, mensagem: MENSAGEM_ERRO_SERVIDOR });
  }

  const problema = lerProblemDetails(data);
  const erros = filtrarErrosDeCampo(problema?.errors);
  const textoSimples = typeof data === 'string' ? data : undefined;

  const mensagem =
    mensagemAproveitavel(problema?.detail) ??
    primeiroErroDeCampo(erros) ??
    mensagemAproveitavel(problema?.title) ??
    mensagemAproveitavel(textoSimples) ??
    mensagemPadraoPorStatus(status);

  return new ErroApi({ status, mensagem, erros });
}
