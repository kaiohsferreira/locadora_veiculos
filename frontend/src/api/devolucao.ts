import { api } from './client';
import type { ComparacaoSaida, ContextoDevolucao, RegistrarSaida } from '@/types/devolucao';

export async function buscarContextoDevolucao(id: number, signal?: AbortSignal): Promise<ContextoDevolucao> {
  return (await api.get<ContextoDevolucao>(`/api/locacoes/${id}/devolucao`, { signal })).data;
}
export async function buscarComparacaoSaida(id: number): Promise<ComparacaoSaida> {
  return (await api.get<ComparacaoSaida>(`/api/locacoes/${id}/vistorias/saida`)).data;
}
export async function registrarSaida(id: number, dados: RegistrarSaida): Promise<ComparacaoSaida> {
  return (await api.post<ComparacaoSaida>(`/api/locacoes/${id}/vistorias/saida`, dados)).data;
}
