import { api } from './client';
import type {
  CobrancasLocacao,
  ComparacaoSaida,
  ContextoDevolucao,
  EncerrarLocacao,
  RegistrarSaida,
  ResultadoEncerramento,
} from '@/types/devolucao';

export async function buscarContextoDevolucao(id: number, signal?: AbortSignal): Promise<ContextoDevolucao> {
  return (await api.get<ContextoDevolucao>(`/api/locacoes/${id}/devolucao`, { signal })).data;
}
export async function buscarComparacaoSaida(id: number): Promise<ComparacaoSaida> {
  return (await api.get<ComparacaoSaida>(`/api/locacoes/${id}/vistorias/saida`)).data;
}
export async function registrarSaida(id: number, dados: RegistrarSaida): Promise<ComparacaoSaida> {
  return (await api.post<ComparacaoSaida>(`/api/locacoes/${id}/vistorias/saida`, dados)).data;
}

/**
 * Encerra a locação (devolucao-encerramento.contract.json v4, POST). O corpo leva só a política, a versão e o total que a
 * caixa de confirmação mostrou: nada de valor de caução, de diária, de km ou de cartão.
 */
export async function encerrarLocacao(id: number, dados: EncerrarLocacao): Promise<ResultadoEncerramento> {
  const corpo: EncerrarLocacao = {
    idConfiguracao: dados.idConfiguracao,
    versaoEsperada: dados.versaoEsperada,
    totalConfirmado: dados.totalConfirmado,
  };
  return (await api.post<ResultadoEncerramento>(`/api/locacoes/${id}/devolucao`, corpo)).data;
}

/** Cobranças extras da locação (GET, sem gravar e sem chamar o provedor). */
export async function buscarCobrancasLocacao(id: number, signal?: AbortSignal): Promise<CobrancasLocacao> {
  return (await api.get<CobrancasLocacao>(`/api/locacoes/${id}/cobrancas`, { signal })).data;
}
