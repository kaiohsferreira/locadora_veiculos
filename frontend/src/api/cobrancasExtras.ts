import { api } from './client';
import { ErroApi } from '@/types/api';
import type { ApuracaoCobrancasExtras, ConfirmarApuracaoCobrancasExtras, SimulacaoCobrancasExtras } from '@/types/cobrancasExtras';

export async function simularCobrancasExtras(id: number): Promise<SimulacaoCobrancasExtras> {
  return (await api.post<SimulacaoCobrancasExtras>(`/api/locacoes/${id}/cobrancas-extras/simulacao`, {})).data;
}
export async function buscarApuracaoCobrancasExtras(id: number, signal?: AbortSignal): Promise<ApuracaoCobrancasExtras | null> {
  try { return (await api.get<ApuracaoCobrancasExtras>(`/api/locacoes/${id}/cobrancas-extras/apuracao`, { signal })).data; }
  catch (erro: unknown) { if (erro instanceof ErroApi && erro.status === 404) return null; throw erro; }
}
export async function registrarApuracaoCobrancasExtras(id: number, confirmacao: ConfirmarApuracaoCobrancasExtras): Promise<ApuracaoCobrancasExtras> {
  return (await api.post<ApuracaoCobrancasExtras>(`/api/locacoes/${id}/cobrancas-extras/apuracao`, confirmacao)).data;
}
