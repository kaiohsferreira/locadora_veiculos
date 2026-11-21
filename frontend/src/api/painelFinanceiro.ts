/**
 * Chamadas do painel financeiro (contrato painel-financeiro v1: GET /api/painel/financeiro e GET
 * /api/painel/financeiro/titulos). As telas usam estas funções pelos hooks do TanStack Query, nunca direto.
 * Sem período na chamada: o servidor usa o mês corrente, o mesmo para os indicadores e para a tabela.
 */
import { api } from './client';
import type { FiltroTitulosFinanceiros, RespostaIndicadoresFinanceiros, RespostaTitulosFinanceiros } from '@/types/painelFinanceiro';

const CAMINHO_INDICADORES_FINANCEIROS = '/api/painel/financeiro';
const CAMINHO_TITULOS_FINANCEIROS = '/api/painel/financeiro/titulos';

/** Parâmetros da query de GET /api/painel/financeiro/titulos. */
export type ParametrosTitulosFinanceiros = Record<string, string | number>;

/** Monta a query da tabela: aba, página e tamanho da página (sempre enviados). */
export function montarParametrosTitulosFinanceiros(filtro: FiltroTitulosFinanceiros): ParametrosTitulosFinanceiros {
  return {
    aba: filtro.aba,
    pagina: filtro.pagina,
    tamanhoPagina: filtro.tamanhoPagina,
  };
}

/** Recebido, a receber, em atraso e ticket médio do período do servidor (200). */
export async function buscarIndicadoresFinanceiros(): Promise<RespostaIndicadoresFinanceiros> {
  const resposta = await api.get<RespostaIndicadoresFinanceiros>(CAMINHO_INDICADORES_FINANCEIROS);
  return resposta.data;
}

/** Página da aba pedida, com os quatro contadores (200). Erros chegam como ErroApi e propagam. */
export async function listarTitulosFinanceiros(filtro: FiltroTitulosFinanceiros): Promise<RespostaTitulosFinanceiros> {
  const resposta = await api.get<RespostaTitulosFinanceiros>(CAMINHO_TITULOS_FINANCEIROS, {
    params: montarParametrosTitulosFinanceiros(filtro),
  });
  return resposta.data;
}
