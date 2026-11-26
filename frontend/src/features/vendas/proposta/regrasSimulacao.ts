import type { ParametrosSimulacao } from '@/types/vendas';

/** Menor número de parcelas aceito pela API (definição da P6-BE-02). */
export const NUMERO_PARCELAS_MINIMO = 1;

/** Maior número de parcelas aceito pela API (definição da P6-BE-02). */
export const NUMERO_PARCELAS_MAXIMO = 60;

/** Centavos em um real: a entrada aceita no máximo 2 casas decimais. */
const CENTAVOS_POR_REAL = 100;

/** Folga da conta em ponto flutuante (0,29 × 100 dá 28,999…): abaixo disso, o valor conta como centavo inteiro. */
const TOLERANCIA_CENTAVOS = 1e-6;

/** Diz se o valor tem no máximo 2 casas decimais, sem cair no erro de arredondamento do ponto flutuante. */
export function temAteDuasCasas(valor: number): boolean {
  const emCentavos = valor * CENTAVOS_POR_REAL;
  return Math.abs(Math.round(emCentavos) - emCentavos) < TOLERANCIA_CENTAVOS;
}

/**
 * Diz se os parâmetros já podem ir para a simulação: entrada maior ou igual a zero, com até 2 casas, menor que o valor
 * de venda (quando ele é conhecido), e parcelas inteiras de 1 a 60. Enquanto não puderem, a consulta fica parada e a
 * tela mostra a regra no campo, em vez de chamar a API para receber um 400.
 */
export function podeSimular(parametros: ParametrosSimulacao | null, valorVenda?: number): parametros is ParametrosSimulacao {
  if (parametros === null) return false;
  const { valorEntrada, numeroParcelas } = parametros;
  const entradaValida =
    Number.isFinite(valorEntrada) &&
    valorEntrada >= 0 &&
    temAteDuasCasas(valorEntrada) &&
    (valorVenda === undefined || valorEntrada < valorVenda);
  const parcelasValidas =
    Number.isInteger(numeroParcelas) && numeroParcelas >= NUMERO_PARCELAS_MINIMO && numeroParcelas <= NUMERO_PARCELAS_MAXIMO;
  return entradaValida && parcelasValidas;
}

/** A observação da última parcela só aparece quando ela é diferente das demais. */
export function ultimaParcelaDiferente(valorParcela: number, valorUltimaParcela: number): boolean {
  return valorParcela !== valorUltimaParcela;
}

/** Cartões de parcelas do simulador (desenho 31): de 12x a 60x. */
export const OPCOES_PARCELAS: readonly number[] = [12, 24, 36, 48, 60];

/** Atalhos de entrada do simulador (desenho 31): porcentagens do valor do veículo. */
export const PERCENTUAIS_ENTRADA: readonly number[] = [10, 20, 30, 50];

/** Passo da barra deslizante da entrada, em reais. */
export const PASSO_BARRA_ENTRADA = 100;

/** Entrada que corresponde à porcentagem do valor do veículo, arredondada ao centavo. */
export function entradaDoPercentual(valorVenda: number, percentual: number): number {
  return Math.round(valorVenda * percentual) / CENTAVOS_POR_REAL;
}

/**
 * Maior entrada da barra deslizante: o maior múltiplo do passo que fica abaixo do valor de venda (a entrada precisa ser
 * menor que ele). Para carros abaixo de um passo, a barra vai só até zero.
 */
export function entradaMaximaDaBarra(valorVenda: number): number {
  const passos = Math.ceil(valorVenda / PASSO_BARRA_ENTRADA) - 1;
  return Math.max(0, passos * PASSO_BARRA_ENTRADA);
}

/** Porcentagem inteira da entrada sobre o valor de venda (para o texto da barra). */
export function percentualDaEntrada(valorEntrada: number, valorVenda: number): number {
  return valorVenda > 0 ? Math.round((valorEntrada / valorVenda) * 100) : 0;
}

/**
 * Diz se a simulação recebida é a dos valores que estão nos campos agora. Enquanto não for (a pessoa acabou de digitar
 * e a consulta nova ainda não voltou), a tela mostra "Calculando…" e não deixa enviar.
 */
export function simulacaoEhDosValores(
  simulacao: Pick<ParametrosSimulacao, 'valorEntrada' | 'numeroParcelas'> & { idVeiculo: number } | null | undefined,
  idVeiculo: number | null,
  valorEntrada: number | null,
  numeroParcelas: number | null,
): boolean {
  if (simulacao === null || simulacao === undefined || idVeiculo === null) return false;
  if (valorEntrada === null || numeroParcelas === null) return false;
  return (
    simulacao.idVeiculo === idVeiculo &&
    Math.round(simulacao.valorEntrada * CENTAVOS_POR_REAL) === Math.round(valorEntrada * CENTAVOS_POR_REAL) &&
    simulacao.numeroParcelas === numeroParcelas
  );
}
