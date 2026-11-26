import { formatarMoeda } from '@/lib/formatters';
import { obterMensagemErro } from '@/lib/erros';
import { MENSAGENS_DEVOLUCAO as M } from '@/lib/mensagensDevolucao';
import type { CargoFuncionario, StatusLocacao } from '@/types/enums';
import type { AvariaComparacao, ContextoDevolucao, EncerrarLocacao, ResultadoEncerramento, VistoriaDevolucao } from '@/types/devolucao';
import { ErroApi } from '@/types/api';
import { cargoPermitido } from '@/routes/autenticacao/usePermissoes';

/** Cargos que apuram e encerram a locação (decisão D6 do plano de 26/11/2026; contrato devolucao-encerramento, 403 para os demais). */
export const CARGOS_ENCERRAM_LOCACAO: readonly CargoFuncionario[] = ['ATENDENTE', 'ADMINISTRADOR'];

/** Só ATENDENTE e ADMINISTRADOR encerram. Sem cargo (cliente ou sessão vazia), nunca. Só afeta a tela: o servidor confere de novo. */
export function podeEncerrarLocacao(cargo: CargoFuncionario | null): boolean {
  return cargoPermitido(cargo, CARGOS_ENCERRAM_LOCACAO);
}

/** Tamanho da página da lista de devoluções pendentes. */
export const TAMANHO_PAGINA_DEVOLUCOES = 10;

/** Estado da tela 06, conforme o anexo (A a D): sem entrada, cancelada, só leitura, sem saída (formulário) e aberta (desenho). */
export type FaseDevolucao = 'SEM_ENTRADA' | 'CANCELADA' | 'FINALIZADA' | 'SEM_SAIDA' | 'ABERTA';

export function faseDevolucao(contexto: ContextoDevolucao): FaseDevolucao {
  if (contexto.entrada === null) return 'SEM_ENTRADA';
  if (contexto.locacao.status === 'CANCELADA') return 'CANCELADA';
  if (contexto.locacao.status === 'FINALIZADA') return 'FINALIZADA';
  if (contexto.comparacao === null) return 'SEM_SAIDA';
  return 'ABERTA';
}

/** Locação que ainda pode ser encerrada (em andamento ou atrasada). */
export function locacaoAberta(status: StatusLocacao): boolean {
  return status === 'EM_ANDAMENTO' || status === 'ATRASADA';
}

/**
 * Texto do selo de estado do cabeçalho (anexo, tela 06, "TROCAR"). O estado da fase continua; a situação Atrasada não o
 * substitui e aparece num selo à parte (ver estaAtrasadaDevolucao), junto com ele (decisão do orquestrador, 26/11/2026).
 */
export function textoEstadoDevolucao(fase: FaseDevolucao): string | null {
  if (fase === 'FINALIZADA') return M.estadoEncerrada;
  if (fase !== 'SEM_SAIDA' && fase !== 'ABERTA') return null;
  return fase === 'ABERTA' ? M.estadoAguardandoEncerramento : M.estadoAguardandoSaida;
}

/** Locação atrasada: o cabeçalho mostra o selo "Atrasada" junto com o estado da fase (anexo, tela 06, "ATRASADA, junto"). */
export function estaAtrasadaDevolucao(status: StatusLocacao): boolean {
  return status === 'ATRASADA';
}

/** O contexto traz tudo o que o encerramento precisa (política, versão e total). Sem isso, a caixa não abre. */
export function temDadosParaEncerrar(contexto: ContextoDevolucao): boolean {
  return contexto.idConfiguracao !== null && contexto.versaoEsperada !== null && contexto.valorTotalPrevisto !== null;
}

/** Número de uma nova avaria: continua a contagem depois das avarias da entrada (#4, #5; N10, item 5). */
export function numeroNovaAvaria(avariasEntrada: number, posicao: number): number {
  return avariasEntrada + posicao + 1;
}

/** Quilômetros rodados entre duas leituras do odômetro (só para mostrar; a apuração é do servidor). */
export function quilometrosRodados(kmEntrada: number, kmSaida: number): number {
  return Math.max(0, kmSaida - kmEntrada);
}

/** Quilômetros rodados entre a vistoria de entrada e a de saída. */
export function kmRodados(entrada: VistoriaDevolucao, saida: VistoriaDevolucao): number {
  return quilometrosRodados(entrada.kmRegistrado, saida.kmRegistrado);
}

/** Corpo do encerramento a partir do contexto, ou nulo quando falta política, versão ou total do servidor. */
export function dadosDeEncerramento(contexto: ContextoDevolucao): EncerrarLocacao | null {
  if (contexto.idConfiguracao === null || contexto.versaoEsperada === null || contexto.valorTotalPrevisto === null) return null;
  return { idConfiguracao: contexto.idConfiguracao, versaoEsperada: contexto.versaoEsperada, totalConfirmado: contexto.valorTotalPrevisto };
}

/** Texto da caução depois do encerramento, com os valores da resposta (anexo, NOVO aprovado). Nulo nos outros casos. */
export function textoCaucaoEncerrada(resultado: ResultadoEncerramento): string | null {
  const { caucao, cobrancas } = resultado;
  if (caucao.acao === 'BAIXADA') return cobrancas.totalExtras > 0 ? M.caucaoLiberadaComPix(cobrancas.totalExtrasStr) : M.caucaoLiberadaInteira;
  if (caucao.acao === 'RETIDA' || caucao.acao === 'RETIDA_PARCIAL') return M.caucaoRetidaLiberada(caucao.valorRetidoStr, caucao.valorLiberadoStr);
  return null;
}

/** Valor do campo ou zero quando o número não é finito ou é negativo. */
function valorNaoNegativo(valor: number): number {
  return Number.isFinite(valor) && valor > 0 ? valor : 0;
}

/**
 * Valor da caução que entra na previsão. Caução baixada (situação CANCELADO) não garante mais nada: a previsão trata como sem
 * caução, e a frase é de cobrança por PIX (o servidor responde BAIXADA com extras por PIX; caucao-devolucao).
 */
export function valorCaucaoPrevista(valorCaucao: number, situacao: string): number {
  return situacao === 'CANCELADO' ? 0 : valorCaucao;
}

/** Desfecho previsto da caução, só para a leitura da caixa de confirmação (o servidor decide de verdade). */
export type SeloTotalCaucao = 'LIQUIDADO_NA_CAUCAO' | 'A_COBRAR_POR_PIX' | 'SEM_COBRANCAS';

export interface DesfechoCaucaoPrevisto {
  temCaucao: boolean;
  retido: number;
  liberado: number;
  selo: SeloTotalCaucao;
  /** Frase do caso (anexo, NOVO aprovado); vazia quando não há o que dizer. */
  frase: string;
}

/**
 * Escolhe a frase e o selo pelos quatro casos da decisão D2: sem extras, extras até a caução (retém só as extras), extras
 * acima da caução (libera a caução e cobra por PIX) e sem caução. Valores vêm do servidor; a conta é só de exibição.
 */
export function desfechoCaucaoPrevisto(valorCaucao: number, totalExtras: number): DesfechoCaucaoPrevisto {
  const reservado = valorNaoNegativo(valorCaucao);
  const extras = valorNaoNegativo(totalExtras);
  if (reservado === 0) {
    return {
      temCaucao: false,
      retido: 0,
      liberado: 0,
      selo: extras === 0 ? 'SEM_COBRANCAS' : 'A_COBRAR_POR_PIX',
      frase: extras === 0 ? '' : M.semCaucao,
    };
  }
  if (extras === 0) {
    return { temCaucao: true, retido: 0, liberado: reservado, selo: 'SEM_COBRANCAS', frase: M.caucaoSemExtras(formatarMoeda(reservado)) };
  }
  if (extras <= reservado) {
    const liberado = Math.round((reservado - extras) * 100) / 100;
    return {
      temCaucao: true,
      retido: extras,
      liberado,
      selo: 'LIQUIDADO_NA_CAUCAO',
      frase: M.caucaoRetida(formatarMoeda(extras), formatarMoeda(liberado)),
    };
  }
  return {
    temCaucao: true,
    retido: 0,
    liberado: reservado,
    selo: 'A_COBRAR_POR_PIX',
    frase: M.caucaoAcimaDaCaucao(formatarMoeda(extras), formatarMoeda(reservado)),
  };
}

/** Rótulo do selo do total (anexo, NOVO aprovado). */
export function rotuloSeloTotal(selo: SeloTotalCaucao): string {
  if (selo === 'LIQUIDADO_NA_CAUCAO') return M.seloLiquidado;
  if (selo === 'A_COBRAR_POR_PIX') return M.seloAcobrarPix;
  return M.seloSemCobrancas;
}

/** Texto da franquia no padrão do ResultadoSimulacao (base TOTAL ou POR_DIARIA). */
export function textoFranquia(franquiaKm: number, baseTotal: boolean): string {
  return M.franquia(franquiaKm, baseTotal);
}

/** Erro do encerramento: texto do servidor e complemento da tela (anexo: 409 e 502 ou queda de conexão). */
export interface FalhaEncerramento {
  mensagem: string;
  complemento: string | null;
  tentarDeNovo: boolean;
  recarregar: boolean;
}

export function falhaDoEncerramento(erro: unknown): FalhaEncerramento {
  const mensagem = obterMensagemErro(erro);
  if (erro instanceof ErroApi) {
    if (erro.status === 409) return { mensagem, complemento: M.complemento409, tentarDeNovo: false, recarregar: true };
    if (erro.status === 502 || erro.status === 0) return { mensagem, complemento: M.complementoFalha, tentarDeNovo: true, recarregar: false };
  }
  return { mensagem, complemento: null, tentarDeNovo: false, recarregar: false };
}

/** Confirmação só com a caixa marcada, sem envio em curso e com o total do servidor presente. */
export function podeConfirmarEncerramento(estado: { confirmado: boolean; enviando: boolean; temDados: boolean }): boolean {
  return estado.confirmado && !estado.enviando && estado.temDados;
}

/** Envio do encerramento só com dados do servidor, sem envio em curso e sem consulta ocupada (bloqueia o envio duplo). */
export function podeIniciarEncerramento(estado: { temDados: boolean; enviando: boolean; ocupado: boolean }): boolean {
  return estado.temDados && !estado.enviando && !estado.ocupado;
}

/** Depois de um 409 a marcação é desfeita: a pessoa precisa conferir os dados atualizados antes de confirmar de novo. */
export function deveDesmarcarAposFalha(falha: FalhaEncerramento | null): boolean {
  return falha?.recarregar === true;
}

/** Avarias da entrada em ordem (a posição é o número exibido). */
export function avariasNumeradas(avarias: readonly AvariaComparacao[]): readonly { posicao: number; avaria: AvariaComparacao }[] {
  return avarias.map((avaria, indice) => ({ posicao: indice + 1, avaria }));
}
