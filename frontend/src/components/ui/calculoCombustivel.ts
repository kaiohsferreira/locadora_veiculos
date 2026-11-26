/** Número de segmentos do medidor (oitavos). */
export const SEGMENTOS_COMBUSTIVEL = 8;

/** Separador padrão entre o oitavo e o nome do ponto (tela 19). A tela 06 usa ' - '. */
export const SEPARADOR_COMBUSTIVEL_PADRAO = ' • ';

/** Nomes dos pontos conhecidos, pelo número de oitavos. Os demais pontos mostram só a fração. */
const NOME_POR_OITAVO: Readonly<Record<number, string>> = {
  8: 'Tanque Cheio',
  6: '3/4 de Tanque',
  4: '1/2 Tanque',
  2: '1/4 de Tanque',
  0: 'Tanque Vazio',
};

/** Leitura do nível de combustível para o medidor e para o texto ao lado dele. */
export interface DescricaoCombustivel {
  /** Oitavo mais próximo do nível (0 a 8). Só visual: o valor gravado continua exato. */
  oitavos: number;
  /** Percentual do valor exato (0 a 100, inteiro). */
  percentual: number;
  /** Nome do ponto conhecido, ou undefined quando o oitavo não tem nome. */
  nome: string | undefined;
  /** Texto pronto: "{k}/8 • Nome (100%)" ou "{k}/8 (50%)" quando não há nome. */
  texto: string;
}

/**
 * Limita o nível ao intervalo de 0 a 1. Valor que não é número finito (NaN, infinito) conta como vazio.
 */
export function limitarNivelCombustivel(nivel: number): number {
  if (!Number.isFinite(nivel)) return 0;
  return Math.min(1, Math.max(0, nivel));
}

/** Oitavo mais próximo do nível (0 a 8). 0,5 vira 4/8 e 0,33 vira 3/8. */
export function oitavosDoNivel(nivel: number): number {
  return Math.round(limitarNivelCombustivel(nivel) * SEGMENTOS_COMBUSTIVEL);
}

/**
 * Descreve o nível: o oitavo mais próximo, o nome do ponto conhecido (só nos pontos 0, 2, 4, 6 e 8) e o percentual
 * do valor exato. Aprovado pelo Kaio em 26/11/2026 (N1 e N10, item 8).
 */
export function descreverNivelCombustivel(
  nivel: number,
  separador: string = SEPARADOR_COMBUSTIVEL_PADRAO,
): DescricaoCombustivel {
  const valor = limitarNivelCombustivel(nivel);
  const oitavos = oitavosDoNivel(valor);
  const percentual = Math.round(valor * 100);
  const nome = NOME_POR_OITAVO[oitavos];
  const fracao = `${oitavos}/${SEGMENTOS_COMBUSTIVEL}`;
  const texto = nome ? `${fracao}${separador}${nome} (${percentual}%)` : `${fracao} (${percentual}%)`;
  return { oitavos, percentual, nome, texto };
}
