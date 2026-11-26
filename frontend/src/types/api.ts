/**
 * Corpo de erro devolvido pelo backend, no formato ProblemDetails (RFC 7807).
 * As mensagens já vêm em português. Pela RFC, todos os campos são opcionais
 * (ex.: erros de validação trazem "errors" e não trazem "detail").
 */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  /** Erros de validação por campo: { [campo]: [mensagens] }. */
  errors?: Record<string, string[]>;
  /** Campo extra do 502 de foto de avaria (contrato avaria): id da avaria gravada sem foto. */
  idAvaria?: number;
  /** Campo extra do 502 de foto de avaria (contrato avaria): caminho relativo do PUT que repete só a foto. */
  retryUploadUrl?: string;
}

/** Dados usados para montar um ErroApi. */
export interface DadosErroApi {
  /** Status HTTP. 0 quando não houve resposta (falha de rede, tempo esgotado ou cancelamento). */
  status: number;
  /** Mensagem amigável, pronta para exibir ao usuário. */
  mensagem: string;
  /** Erros de validação por campo, já sem mensagens técnicas. */
  erros?: Record<string, string[]>;
  /** Corpo do ProblemDetails já lido (campos com tipo válido). Ausente quando não houve corpo. */
  problema?: ProblemDetails;
}

/**
 * Erro padronizado de toda chamada à API. O interceptador de resposta do
 * `src/api/client.ts` sempre rejeita com uma instância desta classe.
 */
export class ErroApi extends Error {
  readonly status: number;
  readonly mensagem: string;
  readonly erros?: Record<string, string[]>;
  readonly problema?: ProblemDetails;

  constructor(dados: DadosErroApi) {
    super(dados.mensagem);
    this.name = 'ErroApi';
    this.status = dados.status;
    this.mensagem = dados.mensagem;
    this.erros = dados.erros;
    this.problema = dados.problema;
  }
}

/** Formato de listagem paginada do backend. */
export interface Pagina<T> {
  itens: T[];
  total: number;
  /** Número da página atual (começa em 1). */
  pagina: number;
  tamanhoPagina: number;
}
