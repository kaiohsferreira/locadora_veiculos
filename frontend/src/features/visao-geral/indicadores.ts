/**
 * Cartões de indicador da visão geral (telas 39 e 40): formata cada número e decide o que aparece em cada cartão.
 * Função pura, sem React. O corte de valores em reais é do servidor: aqui só se respeita `valoresVisiveis`.
 */
import { formatarMoeda, formatarNumero } from '@/lib/formatters';
import { formatarPercentual, formatarVariacaoPercentual, sentidoVariacao, type SentidoVariacao } from '@/lib/formatadoresPainel';
import {
  MENSAGEM_PAINEL_INDICADOR_A_RECEBER,
  MENSAGEM_PAINEL_INDICADOR_EM_ATRASO,
  MENSAGEM_PAINEL_INDICADOR_LOCACOES,
  MENSAGEM_PAINEL_INDICADOR_OCUPACAO,
  MENSAGEM_PAINEL_INDICADOR_RECEITA,
  MENSAGEM_PAINEL_SEM_DADOS,
  MENSAGEM_PAINEL_SEM_PERMISSAO_VALORES,
  mensagemPainelCobrancasEmAberto,
  mensagemPainelCobrancasVencidas,
  mensagemPainelDevolucoesPrevistas,
  mensagemPainelOcupacao,
  mensagemPainelVariacao,
} from '@/lib/mensagens';
import type { RespostaVisaoGeral } from '@/types/painel';

/** Chave estável de cada indicador (usada pelo React e pelos testes). */
export type ChaveIndicador = 'receita' | 'aReceber' | 'emAtraso' | 'ocupacao' | 'locacoes';

/**
 * Tamanho do número. Quantidades e percentuais usam o maior; valores em reais diminuem conforme a quantidade de dígitos,
 * para caber no cartão (ver `classificarTamanhoValor`).
 */
export type TamanhoValor = 'grande' | 'medio' | 'pequeno' | 'minimo';

/** Tom do texto de apoio: neutro, destaque (devoluções) ou perigo (atraso). */
export type TomDetalhe = 'neutro' | 'destaque' | 'perigo';

export interface CartaoIndicador {
  chave: ChaveIndicador;
  titulo: string;
  icone: string;
  /** Número exibido; nulo quando o cartão não mostra número (sem permissão de valores). */
  valor: string | null;
  /** Texto que ocupa o lugar do número quando `valor` é nulo. */
  semValor: string | null;
  tamanhoValor: TamanhoValor;
  /** Número em vermelho (em atraso). */
  destacarValor: boolean;
  /** Linha de variação; nula quando o campo não veio preenchido. */
  variacao: string | null;
  /** Sentido da variação (seta e cor); nulo quando não há linha de variação. */
  sentidoVariacao: SentidoVariacao | null;
  /** Texto de apoio abaixo do número. */
  detalhe: string | null;
  tomDetalhe: TomDetalhe;
  /** Cartão de valor em reais: pode ser ocultado pelo servidor. */
  monetario: boolean;
}

/** Opções de montagem. `primeiroAcesso` zera a leitura: cada cartão diz "Sem dados ainda" e não mostra variação. */
export interface OpcoesCartoes {
  primeiroAcesso?: boolean;
}

/**
 * Tamanho do número pelo total de dígitos (inteiros e centavos). Até 8 dígitos (ex.: R$ 9.870,00) cabe em 20 px; até 10
 * (ex.: R$ 123.456.789,00 com 9 dígitos antes da vírgula, 11 no total) reduz para 16 px e 14 px no máximo.
 * Valores de até 9 dígitos antes da vírgula cabem em 1280 px e em 375 px sem quebrar o cartão.
 */
export function classificarTamanhoValor(textoMonetario: string): TamanhoValor {
  const digitos = textoMonetario.replace(/\D/g, '').length;
  if (digitos <= 8) return 'medio';
  if (digitos <= 10) return 'pequeno';
  return 'minimo';
}

/** Valor em reais respeitando `valoresVisiveis`. Sem permissão, não há número nem é calculado. */
function valorMonetario(
  valor: number | null,
  valoresVisiveis: boolean,
): { valor: string | null; semValor: string | null } {
  if (!valoresVisiveis) return { valor: null, semValor: MENSAGEM_PAINEL_SEM_PERMISSAO_VALORES };
  return { valor: formatarMoeda(valor), semValor: null };
}

/** Os cinco cartões na ordem do desenho: receita, a receber, em atraso, ocupação e locações ativas. */
export function montarCartoesIndicadores(
  resposta: RespostaVisaoGeral,
  opcoes: OpcoesCartoes = {},
): CartaoIndicador[] {
  const zerado = opcoes.primeiroAcesso === true;
  const detalheZerado = zerado ? MENSAGEM_PAINEL_SEM_DADOS : null;

  const receita = valorMonetario(resposta.receitaPeriodo, resposta.valoresVisiveis);
  const variacaoReceita = zerado ? null : resposta.variacaoReceitaPercentual;
  const variacao = formatarVariacaoPercentual(variacaoReceita);

  const aReceber = valorMonetario(resposta.aReceber, resposta.valoresVisiveis);
  const emAtraso = valorMonetario(resposta.emAtraso, resposta.valoresVisiveis);

  return [
    {
      chave: 'receita',
      titulo: MENSAGEM_PAINEL_INDICADOR_RECEITA,
      icone: 'payments',
      valor: receita.valor,
      semValor: receita.semValor,
      tamanhoValor: receita.valor === null ? 'medio' : classificarTamanhoValor(receita.valor),
      destacarValor: false,
      variacao: variacao === null ? null : mensagemPainelVariacao(variacao),
      sentidoVariacao: variacao === null ? null : sentidoVariacao(variacaoReceita),
      detalhe: detalheZerado,
      tomDetalhe: 'neutro',
      monetario: true,
    },
    {
      chave: 'aReceber',
      titulo: MENSAGEM_PAINEL_INDICADOR_A_RECEBER,
      icone: 'receipt_long',
      valor: aReceber.valor,
      semValor: aReceber.semValor,
      tamanhoValor: aReceber.valor === null ? 'medio' : classificarTamanhoValor(aReceber.valor),
      destacarValor: false,
      variacao: null,
      sentidoVariacao: null,
      detalhe: detalheZerado ?? mensagemPainelCobrancasEmAberto(resposta.quantidadeCobrancasEmAberto),
      tomDetalhe: 'neutro',
      monetario: true,
    },
    {
      chave: 'emAtraso',
      titulo: MENSAGEM_PAINEL_INDICADOR_EM_ATRASO,
      icone: 'warning',
      valor: emAtraso.valor,
      semValor: emAtraso.semValor,
      tamanhoValor: emAtraso.valor === null ? 'medio' : classificarTamanhoValor(emAtraso.valor),
      destacarValor: true,
      variacao: null,
      sentidoVariacao: null,
      detalhe: detalheZerado ?? mensagemPainelCobrancasVencidas(resposta.quantidadeCobrancasVencidas),
      tomDetalhe: 'perigo',
      monetario: true,
    },
    {
      chave: 'ocupacao',
      titulo: MENSAGEM_PAINEL_INDICADOR_OCUPACAO,
      icone: 'speed',
      valor: formatarPercentual(resposta.taxaOcupacaoPercentual),
      semValor: null,
      tamanhoValor: 'grande',
      destacarValor: false,
      variacao: null,
      sentidoVariacao: null,
      detalhe: detalheZerado ?? mensagemPainelOcupacao(resposta.veiculosLocados, resposta.totalVeiculos),
      tomDetalhe: 'neutro',
      monetario: false,
    },
    {
      chave: 'locacoes',
      titulo: MENSAGEM_PAINEL_INDICADOR_LOCACOES,
      icone: 'key',
      valor: formatarNumero(resposta.locacoesAtivas),
      semValor: null,
      tamanhoValor: 'grande',
      destacarValor: false,
      variacao: null,
      sentidoVariacao: null,
      detalhe:
        detalheZerado ?? mensagemPainelDevolucoesPrevistas(resposta.devolucoesPrevistasHoje),
      tomDetalhe: 'destaque',
      monetario: false,
    },
  ];
}
