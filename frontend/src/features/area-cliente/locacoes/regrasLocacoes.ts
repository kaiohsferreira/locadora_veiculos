import { obterMensagemErro } from '@/lib/erros';
import { formatarData, formatarNumero, interpretarDataHoraServidor, VALOR_AUSENTE } from '@/lib/formatters';
import { MENSAGEM_LOCACOES_ERRO_LISTA } from '@/lib/mensagensLocacoesCliente';
import { mensagemReservasPeriodoDatas } from '@/lib/mensagensReservas';
import { ErroApi } from '@/types/api';
import type { LocacaoCliente } from '@/types/locacoesCliente';

/**
 * Regras puras da tela Minhas locações (tela 27). Sem React e sem API: o relógio entra como parâmetro (`agora`), para
 * a contagem regressiva ser testada nos limites. A data da devolução vem do servidor (devolucaoPrevistaEfetiva); a
 * tela não recalcula a regra de devolução.
 */

/** Cartões por página nas duas abas. */
export const TAMANHO_PAGINA_LOCACOES = 5;
/** Quantas locações do histórico aparecem em "Últimas locações do histórico". */
export const TAMANHO_ULTIMAS_LOCACOES = 3;

const SEGUNDOS_POR_MINUTO = 60;
const SEGUNDOS_POR_HORA = 3600;
const SEGUNDOS_POR_DIA = 86400;
const MILISSEGUNDOS_POR_SEGUNDO = 1000;
const PERCENTUAL_MAXIMO = 100;

/** Tempo que falta até a devolução. `vencida` = a devolução prevista já passou (os blocos ficam em zero). */
export interface ContagemRegressiva {
  dias: number;
  horas: number;
  minutos: number;
  segundos: number;
  vencida: boolean;
}

/**
 * Quanto falta entre `agora` e a devolução prevista efetiva (texto do servidor, "2026-11-18T10:00:00" ou dd/MM/yyyy
 * HH:mm). Devolve null quando a data não pode ser lida. Devolução igual a agora ainda conta como "no prazo", com zero.
 */
export function calcularContagemRegressiva(devolucaoPrevistaEfetiva: string, agora: Date): ContagemRegressiva | null {
  const instante = interpretarDataHoraServidor(devolucaoPrevistaEfetiva);
  if (instante === null) return null;

  const diferencaMs = instante.getTime() - agora.getTime();
  if (diferencaMs < 0) {
    return { dias: 0, horas: 0, minutos: 0, segundos: 0, vencida: true };
  }

  const totalSegundos = Math.floor(diferencaMs / MILISSEGUNDOS_POR_SEGUNDO);
  return {
    dias: Math.floor(totalSegundos / SEGUNDOS_POR_DIA),
    horas: Math.floor((totalSegundos % SEGUNDOS_POR_DIA) / SEGUNDOS_POR_HORA),
    minutos: Math.floor((totalSegundos % SEGUNDOS_POR_HORA) / SEGUNDOS_POR_MINUTO),
    segundos: totalSegundos % SEGUNDOS_POR_MINUTO,
    vencida: false,
  };
}

/** Locação atrasada: o servidor marcou (status ATRASADA ou devolução passada) ou a contagem local já venceu. */
export function locacaoEstaAtrasada(atrasadaServidor: boolean, contagem: ContagemRegressiva | null): boolean {
  return atrasadaServidor || (contagem?.vencida ?? false);
}

/** Dois dígitos para o cronômetro: 3 vira "03". */
export function doisDigitos(valor: number): string {
  return String(valor).padStart(2, '0');
}

/**
 * Percentual da locação já percorrido: da retirada até a devolução prevista efetiva. Fora do intervalo, fica entre 0 e
 * 100. Devolve null quando alguma das datas não pode ser lida.
 */
export function percentualProgresso(dataRetirada: string, devolucaoPrevistaEfetiva: string, agora: Date): number | null {
  const inicio = interpretarDataHoraServidor(dataRetirada);
  const fim = interpretarDataHoraServidor(devolucaoPrevistaEfetiva);
  if (inicio === null || fim === null) return null;

  const duracao = fim.getTime() - inicio.getTime();
  if (duracao <= 0) return PERCENTUAL_MAXIMO;

  const decorrido = agora.getTime() - inicio.getTime();
  const percentual = Math.round((decorrido / duracao) * PERCENTUAL_MAXIMO);
  return Math.min(PERCENTUAL_MAXIMO, Math.max(0, percentual));
}

/** Nível de combustível (0 a 1) como percentual para exibição: 0.5 → "50%". */
export function percentualCombustivel(nivel: number): string {
  return `${formatarNumero(Math.round(nivel * PERCENTUAL_MAXIMO))}%`;
}

/** Tanque cheio: o nível arredondado em percentual dá 100. */
export function combustivelCheio(nivel: number): boolean {
  return Math.round(nivel * PERCENTUAL_MAXIMO) === PERCENTUAL_MAXIMO;
}

/**
 * Período da locação só com as datas (dd/MM/aaaa a dd/MM/aaaa, anexo tela 27): a retirada e a devolução efetiva (ou a
 * prevista, quando a locação não foi encerrada). Sai dos campos ISO com o formatador do projeto, sem a hora do texto.
 */
export function periodoDaLocacao(
  locacao: Pick<LocacaoCliente, 'dataRetirada' | 'dataDevolucaoEfetiva' | 'dataDevolucaoPrevista'>,
): string {
  return mensagemReservasPeriodoDatas(
    formatarData(locacao.dataRetirada),
    formatarData(locacao.dataDevolucaoEfetiva ?? locacao.dataDevolucaoPrevista),
  );
}

/** Valor total pronto para a tela; locação sem valor (em andamento ou cancelada) mostra o travessão. */
export function valorTotalDaLocacao(locacao: Pick<LocacaoCliente, 'valorTotalStr'>): string {
  return locacao.valorTotalStr ?? VALOR_AUSENTE;
}

/**
 * Texto do erro da lista de locações. Sem resposta do servidor (status 0) ou com falha 5xx, vale o texto ERRO-1 aprovado
 * em 26/11/2026; nos demais casos (ex.: 401, 403, 400), a mensagem do servidor já tratada por obterMensagemErro.
 */
export function mensagemErroLista(erro: unknown): string {
  const status = erro instanceof ErroApi ? erro.status : 0;
  if (status === 0 || status >= 500) return MENSAGEM_LOCACOES_ERRO_LISTA;
  return obterMensagemErro(erro);
}

/** Última página com itens (mínimo 1), para a página voltar para a última quando a lista encolhe. */
export function ultimaPaginaLocacoes(total: number, tamanhoPagina: number): number {
  return Math.max(1, Math.ceil(total / tamanhoPagina));
}
