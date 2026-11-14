/**
 * Formatadores da visão geral que não existem em `@/lib/formatters`: percentuais, variação com sinal e plural.
 * Funções puras. Valores em reais continuam com `formatarMoeda` e números com `formatarNumero`.
 */
import { formatarNumero, VALOR_AUSENTE } from '@/lib/formatters';

/** 68 → "68%"; com casas decimais: (63.6, 1) → "63,6%". Nulo ou não numérico vira o valor ausente. */
export function formatarPercentual(valor: number | null | undefined, casasDecimais = 0): string {
  if (valor === null || valor === undefined || !Number.isFinite(valor)) return VALOR_AUSENTE;
  return `${formatarNumero(valor, casasDecimais)}%`;
}

/** Sentido da variação: alta, queda ou neutra (zero, ou valor que arredonda para zero). */
export type SentidoVariacao = 'alta' | 'queda' | 'neutra';

/** Até uma casa decimal, sem zeros à direita: 12 → "12", 3,5 → "3,5", 0,3 → "0,3". */
const FORMATO_VARIACAO = new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 1 });

/**
 * Sentido de uma variação percentual já arredondada como ela aparece na tela. Valor que arredonda para zero
 * (ex.: 0,04 ou -0,04) é neutro, para nunca mostrar "+0%" ou "-0%". Nulo ou não numérico devolve null.
 */
export function sentidoVariacao(valor: number | null | undefined): SentidoVariacao | null {
  if (valor === null || valor === undefined || !Number.isFinite(valor)) return null;
  const magnitude = Number(FORMATO_VARIACAO.format(Math.abs(valor)).replace(',', '.'));
  if (magnitude === 0) return 'neutra';
  return valor > 0 ? 'alta' : 'queda';
}

/**
 * Variação com sinal e uma casa decimal: 12 → "+12%", -3,5 → "-3,5%", 0,3 → "+0,3%", 0 → "0%".
 * Zero e valor que arredonda para zero saem sem sinal. Nulo ou não numérico devolve null (a linha não aparece).
 */
export function formatarVariacaoPercentual(valor: number | null | undefined): string | null {
  const sentido = sentidoVariacao(valor);
  if (sentido === null || valor === null || valor === undefined) return null;
  const texto = FORMATO_VARIACAO.format(Math.abs(valor));
  if (sentido === 'neutra') return `${texto}%`;
  return `${sentido === 'alta' ? '+' : '-'}${texto}%`;
}

/** Quantidade com o rótulo certo no singular ou no plural: (1, "cobrança", "cobranças") → "1 cobrança". */
export function pluralizar(quantidade: number, singular: string, plural: string): string {
  return `${formatarNumero(quantidade)} ${quantidade === 1 ? singular : plural}`;
}
