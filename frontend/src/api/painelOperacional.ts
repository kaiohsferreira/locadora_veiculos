/**
 * Painel operacional de hoje (contrato painel-operacional v2, GET /api/painel-operacional/hoje). A página de entrada da
 * retirada lê a lista `retiradas`. As telas usam esta função pelo hook do TanStack Query, nunca direto.
 */
import { api } from './client';
import type { RespostaPainelOperacionalHoje } from '@/types/painelOperacional';

const CAMINHO_PAINEL_HOJE = '/api/painel-operacional/hoje';

/** Retiradas e devoluções previstas para hoje da locadora do funcionário (200 ReturnPainelDiarioVO). */
export async function buscarRetiradasDeHoje(): Promise<RespostaPainelOperacionalHoje> {
  const resposta = await api.get<RespostaPainelOperacionalHoje>(CAMINHO_PAINEL_HOJE);
  return resposta.data;
}
