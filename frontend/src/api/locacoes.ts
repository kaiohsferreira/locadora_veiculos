import { api } from './client';
import type { AbrirLocacao, ContextoRetirada, LocacaoAberta } from '@/types/retirada';
export async function buscarContextoRetirada(idReserva: number): Promise<ContextoRetirada> {
  const { data } = await api.get<ContextoRetirada>(`/api/locacoes/retirada/${idReserva}`);
  return data;
}
export async function abrirLocacao(dados: AbrirLocacao): Promise<LocacaoAberta> {
  const { data } = await api.post<LocacaoAberta>('/api/locacoes', { idReserva: dados.idReserva });
  return data;
}
