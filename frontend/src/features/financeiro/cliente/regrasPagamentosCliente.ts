import type { OpcaoSelect } from '@/components/ui';
import {
  mensagemLocacaoNumero,
  mensagemPagoVia,
  mensagemPeriodo,
  mensagemVencidaHa,
  mensagemVencimento,
  mensagemVendaNumero,
  mensagemFaixaPendencias,
} from '@/lib/mensagens';
import {
  FORMA_PAGAMENTO,
  ROTULO_FORMA_PAGAMENTO,
  type FormaPagamento,
  type StatusPagamento,
  type TipoPagamento,
} from '@/types/enums';
import { ErroApi } from '@/types/api';
import {
  PERIODOS_PAGAMENTOS_CLIENTE,
  type AbaPagamentosCliente,
  type FiltroMeusPagamentos,
  type MeuPagamento,
  type PeriodoPagamentosCliente,
} from '@/types/pagamentos';

/** Itens por página da tabela (mesmo tamanho da aba Histórico de reservas). */
export const TAMANHO_PAGINA_PAGAMENTOS_CLIENTE = 10;

/** Filtro ao abrir a tela: aba Todos e período de 12 meses (as primeiras opções do desenho). */
export const FILTRO_INICIAL_PAGAMENTOS_CLIENTE: FiltroMeusPagamentos = {
  aba: 'TODOS',
  periodo: 'DOZE_MESES',
  pagina: 1,
  tamanhoPagina: TAMANHO_PAGINA_PAGAMENTOS_CLIENTE,
};

/** Ids das abas, para o foco e o aria-controls. */
export const ID_ABA_PAGAMENTOS_TODOS = 'pagamentos-aba-todos';
export const ID_ABA_PAGAMENTOS_PENDENTES = 'pagamentos-aba-pendentes';
export const ID_ABA_PAGAMENTOS_PAGOS = 'pagamentos-aba-pagos';
export const ID_PAINEL_PAGAMENTOS = 'pagamentos-painel';

/** Mudança de filtro vinda da tela: trocar aba ou período volta para a página 1. */
export type MudancaFiltroPagamentos =
  | { tipo: 'aba'; aba: AbaPagamentosCliente }
  | { tipo: 'periodo'; periodo: PeriodoPagamentosCliente }
  | { tipo: 'pagina'; pagina: number };

/** Aplica uma mudança de filtro. Só a mudança de página mantém a página pedida. */
export function aplicarMudancaFiltro(
  filtro: FiltroMeusPagamentos,
  mudanca: MudancaFiltroPagamentos,
): FiltroMeusPagamentos {
  if (mudanca.tipo === 'pagina') return { ...filtro, pagina: mudanca.pagina };
  if (mudanca.tipo === 'aba') return { ...filtro, aba: mudanca.aba, pagina: 1 };
  return { ...filtro, periodo: mudanca.periodo, pagina: 1 };
}

/** Última página existente para o total informado (sempre pelo menos 1). */
export function ultimaPaginaPagamentos(total: number, tamanhoPagina: number): number {
  return Math.max(1, Math.ceil(total / Math.max(1, tamanhoPagina)));
}

/** Texto da faixa de pendência; nulo quando não há pendente (a faixa some). */
export function textoFaixaPendencias(quantidadePendentes: number): string | null {
  if (quantidadePendentes <= 0) return null;
  return mensagemFaixaPendencias(quantidadePendentes);
}

/**
 * Cobrança que pode ser paga por PIX agora (contrato pagamento-pix, tela 30): pendente ou atrasada, e nunca caução.
 * Paga, cancelada e caução não têm botão de pagar.
 */
export function podePagarCobranca(situacao: StatusPagamento, tipo: TipoPagamento): boolean {
  const pendente = situacao === 'PENDENTE' || situacao === 'ATRASADO';
  return pendente && tipo !== 'CAUCAO';
}

/** Pagar agora aparece só em cobrança que pode ser paga (ver podePagarCobranca). */
export function podePagarNaLinha(pagamento: MeuPagamento): boolean {
  return podePagarCobranca(pagamento.situacao, pagamento.tipo);
}

/**
 * Destino dos botões "Pagar Fatura" (faixa) e "Regularizar" (cartão), pela quantidade de pendentes do resumo. O resumo já
 * não conta caução. Uma cobrança abre a gaveta dela (é preciso buscá-la); mais de uma leva à aba Pendentes; nenhuma não
 * tem botão.
 */
export type DestinoBotaoPagarResumo = 'NENHUM' | 'COBRANCA_UNICA' | 'ABA_PENDENTES';

export function destinoBotaoPagarResumo(quantidadePendentes: number): DestinoBotaoPagarResumo {
  if (quantidadePendentes <= 0) return 'NENHUM';
  if (quantidadePendentes === 1) return 'COBRANCA_UNICA';
  return 'ABA_PENDENTES';
}

/** Contagem regressiva do PIX em mm:ss, com zeros à esquerda. Zero, negativo ou inválido vira 00:00. */
export function formatarContagemPix(segundos: number): string {
  const total = Number.isFinite(segundos) ? Math.max(0, Math.floor(segundos)) : 0;
  const minutos = Math.floor(total / 60);
  const restoSegundos = total % 60;
  return `${String(minutos).padStart(2, '0')}:${String(restoSegundos).padStart(2, '0')}`;
}

/** Um segundo a menos na contagem, sem passar de zero. Valor inválido conta como zero. */
export function proximaContagemPix(segundos: number): number {
  if (!Number.isFinite(segundos)) return 0;
  return Math.max(0, segundos - 1);
}

/** O PIX expirou quando a contagem chega a zero. Valor inválido também conta como expirado. */
export function pixExpirou(segundos: number): boolean {
  return !(segundos > 0);
}

/** Estado do conteúdo da gaveta de PIX: erro da geração, gerando (espera ou sem cobrança ainda) ou pronto. */
export type EstadoGavetaPix = 'ERRO' | 'GERANDO' | 'PRONTO';

/**
 * Escolhe o estado da gaveta a partir da geração. Erro vence; enquanto espera, ou sem cobrança, mostra gerando; só com
 * a cobrança devolvida vira pronto.
 */
export function estadoGavetaPix(temErro: boolean, pendente: boolean, temCobranca: boolean): EstadoGavetaPix {
  if (temErro) return 'ERRO';
  if (pendente || !temCobranca) return 'GERANDO';
  return 'PRONTO';
}

/**
 * A gaveta pede o PIX uma única vez por abertura (o modo de desenvolvimento do React monta o componente duas vezes).
 * Devolve verdadeiro só na primeira vez.
 */
export function devePedirPixAoAbrir(jaPediu: boolean): boolean {
  return !jaPediu;
}

/**
 * Depois de um erro ao gerar o PIX, recarrega as cobranças só quando o servidor disse 409: a situação da cobrança mudou
 * (paga, cancelada ou sem PIX possível). Outros erros (502, 429, 404, 401, falha de rede ou qualquer valor que não seja
 * erro da API) não mudam a lista, então não recarregam.
 */
export function deveRecarregarCobrancasAposErroPix(erro: unknown): boolean {
  return erro instanceof ErroApi && erro.status === 409;
}

/**
 * Chave da gaveta de PIX para cada geração pedida. Cada geração tem um número diferente (o instante do envio), então a
 * contagem recomeça do valor novo do servidor, mesmo quando o PIX volta com a mesma validade.
 */
export function chaveDaGeracaoPix(instanteEnvio: number): string {
  return `geracao-${instanteEnvio}`;
}

/**
 * Cobrança que a gaveta pode abrir a partir da busca da cobrança única (Pagar Fatura ou Regularizar). Nula quando a
 * busca voltou vazia (a cobrança foi paga no meio do caminho) ou quando a cobrança encontrada não pode ser paga: a tela
 * então recarrega o resumo em vez de abrir a gaveta.
 */
export function cobrancaParaGaveta(unica: MeuPagamento | null): MeuPagamento | null {
  if (unica === null) return null;
  return podePagarNaLinha(unica) ? unica : null;
}

/** Rótulo da forma usada na baixa; nulo quando a forma não é uma da lista fechada. */
export function rotuloForma(forma: string | null): string | null {
  if (forma === null) return null;
  if (!(FORMA_PAGAMENTO as readonly string[]).includes(forma)) return null;
  return ROTULO_FORMA_PAGAMENTO[forma as FormaPagamento];
}

/**
 * Subtexto embaixo do vencimento: pendente (vence hoje, amanhã ou em N dias), atrasado (vencida há N dias), paga (com
 * a forma usada). Cancelada não tem subtexto. Caução é bloqueio no cartão, não dívida: só mostra "Pago via" quando paga
 * com forma; em qualquer outra situação não tem subtexto de vencimento. Os dias vêm do servidor; a tela não recalcula
 * com o relógio do navegador.
 */
export function subtextoVencimento(pagamento: MeuPagamento): string | null {
  if (pagamento.situacao === 'PAGO') {
    const forma = rotuloForma(pagamento.forma);
    return forma === null ? null : mensagemPagoVia(forma);
  }
  if (pagamento.tipo === 'CAUCAO') return null;
  if (pagamento.situacao === 'PENDENTE') {
    return pagamento.diasParaVencer >= 0 ? mensagemVencimento(pagamento.diasParaVencer) : null;
  }
  if (pagamento.situacao === 'ATRASADO') {
    return pagamento.diasParaVencer < 0 ? mensagemVencidaHa(-pagamento.diasParaVencer) : null;
  }
  return null;
}

/** Linha da origem: "Locação nº 123" ou "Venda nº 45"; nulo quando não há origem. */
export function rotuloOrigem(pagamento: MeuPagamento): string | null {
  if (pagamento.idLocacao !== null) return mensagemLocacaoNumero(pagamento.idLocacao);
  if (pagamento.idVenda !== null) return mensagemVendaNumero(pagamento.idVenda);
  return null;
}

/** Opções do seletor de período, na ordem do desenho. O ano vem do servidor quando já chegou. */
export function opcoesPeriodo(ano: number): OpcaoSelect[] {
  return PERIODOS_PAGAMENTOS_CLIENTE.map((periodo) => ({
    valor: periodo,
    rotulo: mensagemPeriodo(periodo, ano),
  }));
}

/** Ícone do tipo da cobrança na linha da tabela (novo para cobrança extra e caução). */
export function iconeDoTipo(tipo: MeuPagamento['tipo']): string {
  switch (tipo) {
    case 'DIARIA':
    case 'VENDA':
      return 'directions_car';
    case 'COBRANCA_EXTRA':
      return 'receipt_long';
    case 'CAUCAO':
      return 'verified_user';
  }
}

/** Cobrança pendente, para o estilo do ícone e da data (PENDENTE ou ATRASADO). */
export function ehPendente(pagamento: MeuPagamento): boolean {
  return pagamento.situacao === 'PENDENTE' || pagamento.situacao === 'ATRASADO';
}

/** Há filtro quando a aba ou o período saíram do padrão (decide o texto do vazio). */
export function temFiltroAplicado(filtro: FiltroMeusPagamentos): boolean {
  return filtro.aba !== FILTRO_INICIAL_PAGAMENTOS_CLIENTE.aba || filtro.periodo !== FILTRO_INICIAL_PAGAMENTOS_CLIENTE.periodo;
}
