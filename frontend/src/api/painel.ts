/**
 * Chamada da visão geral da locadora (contrato painel.contract.json, P11-BE-01/02). Qualquer cargo de funcionário acessa.
 * As telas usam esta função pelo hook do TanStack Query, nunca direto.
 */
import { api } from './client';
import type { PeriodoVisaoGeral, RespostaVisaoGeral } from '@/types/painel';

const CAMINHO_VISAO_GERAL = '/api/painel/visao-geral';

/** Parâmetros da query de GET /api/painel/visao-geral. */
export interface ParametrosVisaoGeral {
  periodo: PeriodoVisaoGeral;
}

/** Monta a query: o período sempre vai explícito, para a resposta bater com o botão escolhido. */
export function montarParametrosVisaoGeral(periodo: PeriodoVisaoGeral): ParametrosVisaoGeral {
  return { periodo };
}

/** Indicadores e situação da frota da locadora do funcionário, no período pedido (200 ReturnVisaoGeralVO). */
export async function buscarVisaoGeral(periodo: PeriodoVisaoGeral): Promise<RespostaVisaoGeral> {
  const resposta = await api.get<RespostaVisaoGeral>(CAMINHO_VISAO_GERAL, {
    params: montarParametrosVisaoGeral(periodo),
  });
  return resposta.data;
}
