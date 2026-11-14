/** Opções do seletor de período (tela 39) e conversão entre o valor do contrato e o rótulo. */
import { ROTULO_PERIODO_VISAO_GERAL } from '@/lib/mensagens';
import { PERIODOS_VISAO_GERAL, type PeriodoVisaoGeral } from '@/types/painel';

export interface OpcaoPeriodo {
  valor: PeriodoVisaoGeral;
  rotulo: string;
}

/** Quatro opções, na ordem do desenho: Hoje, 7 dias, 30 dias, Este mês. */
export const OPCOES_PERIODO: readonly OpcaoPeriodo[] = PERIODOS_VISAO_GERAL.map((valor) => ({
  valor,
  rotulo: ROTULO_PERIODO_VISAO_GERAL[valor],
}));

/** Rótulo visível de um período do contrato. */
export function rotuloPeriodo(periodo: PeriodoVisaoGeral): string {
  return ROTULO_PERIODO_VISAO_GERAL[periodo];
}
