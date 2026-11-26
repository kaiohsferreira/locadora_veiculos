import { api } from './client';
import type { AssinarContratoLocacao, ContratoLocacao } from '@/types/contratoLocacao';

/**
 * Documento do contrato da locação (GET). Antes de assinar, o servidor monta o documento na hora; depois de assinar,
 * devolve a cópia guardada (contrato-locacao.contract.json, rota CONTRATO-GET).
 */
export async function buscarContratoLocacao(id: number, signal?: AbortSignal): Promise<ContratoLocacao> {
  return (await api.get<ContratoLocacao>(`/api/locacoes/${id}/contrato`, { signal })).data;
}

/**
 * Coleta a assinatura (POST, 201). O corpo leva só o nome, os aceites e o PNG da assinatura; o servidor define a data,
 * o funcionário e o hash.
 */
export async function assinarContratoLocacao(id: number, dados: AssinarContratoLocacao): Promise<ContratoLocacao> {
  const corpo: AssinarContratoLocacao = {
    nomeAssinante: dados.nomeAssinante,
    aceiteCondicoesGerais: dados.aceiteCondicoesGerais,
    aceitePoliticaCombustivel: dados.aceitePoliticaCombustivel,
    assinaturaPngBase64: dados.assinaturaPngBase64,
  };
  return (await api.post<ContratoLocacao>(`/api/locacoes/${id}/contrato/assinatura`, corpo)).data;
}
