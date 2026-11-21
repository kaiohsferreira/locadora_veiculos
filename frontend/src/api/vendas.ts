/**
 * Chamadas da venda de seminovos (contrato proposta-compra v1.2, P6-BE-02): detalhe público do veículo, simulação do
 * parcelamento (públicas) e envio da proposta (cliente autenticado). As telas usam estas funções pelos hooks do
 * TanStack Query, nunca direto.
 */
import { api } from './client';
import type {
  ParametrosSimulacao,
  PropostaCompra,
  PropostaCompraRequisicao,
  SimulacaoParcelamento,
  VeiculoAVenda,
} from '@/types/vendas';

const CAMINHO_VEICULOS = '/api/veiculos';
const CAMINHO_PROPOSTAS = '/api/propostas-compra';

/** Caminho do detalhe público do seminovo. */
export function caminhoVeiculoAVenda(idVeiculo: number): string {
  return `${CAMINHO_VEICULOS}/a-venda/${idVeiculo}`;
}

/** Caminho da simulação do parcelamento de um veículo. */
export function caminhoSimulacao(idVeiculo: number): string {
  return `${CAMINHO_VEICULOS}/${idVeiculo}/simulacao`;
}

/** Detalhe público do seminovo (200). Veículo fora do catálogo responde 404 (ErroApi). */
export async function buscarVeiculoAVenda(idVeiculo: number): Promise<VeiculoAVenda> {
  const resposta = await api.get<VeiculoAVenda>(caminhoVeiculoAVenda(idVeiculo));
  return resposta.data;
}

/** Simula o parcelamento (200), sem gravar nada. Entrada ou parcelas inválidas respondem 400 (ErroApi). */
export async function simularParcelamento(idVeiculo: number, parametros: ParametrosSimulacao): Promise<SimulacaoParcelamento> {
  const resposta = await api.get<SimulacaoParcelamento>(caminhoSimulacao(idVeiculo), {
    params: { valorEntrada: parametros.valorEntrada, numeroParcelas: parametros.numeroParcelas },
  });
  return resposta.data;
}

/** Envia a proposta do cliente autenticado (201). 401 sem login; 409 com cadastro não aprovado na locadora. */
export async function enviarPropostaCompra(requisicao: PropostaCompraRequisicao): Promise<PropostaCompra> {
  const resposta = await api.post<PropostaCompra>(CAMINHO_PROPOSTAS, requisicao);
  return resposta.data;
}
