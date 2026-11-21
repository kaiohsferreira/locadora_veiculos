/**
 * Chamadas da venda de seminovos: catálogo (P6-BE-01, GET /api/veiculos/a-venda), detalhe público do veículo e simulação
 * do parcelamento (públicas, contrato proposta-compra v1.2) e envio da proposta (cliente autenticado). As telas usam estas funções pelos hooks do
 * TanStack Query, nunca direto.
 */
import { api } from './client';
import type {
  FiltroCatalogo,
  ParametrosSimulacao,
  PropostaCompra,
  PropostaCompraRequisicao,
  RespostaCatalogo,
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

/** Parâmetros da query do catálogo. */
export type ParametrosCatalogo = Record<string, string | number>;

/** Monta a query do catálogo: página e tamanho sempre; cada filtro só quando informado. A marca sai aparada. */
export function montarParametrosCatalogo(filtro: FiltroCatalogo): ParametrosCatalogo {
  const parametros: ParametrosCatalogo = { pagina: filtro.pagina, tamanhoPagina: filtro.tamanhoPagina };
  const marca = filtro.marca?.trim();
  if (marca) parametros.marca = marca;
  if (filtro.precoMinimo !== undefined) parametros.precoMinimo = filtro.precoMinimo;
  if (filtro.precoMaximo !== undefined) parametros.precoMaximo = filtro.precoMaximo;
  if (filtro.anoMinimo !== undefined) parametros.anoMinimo = filtro.anoMinimo;
  if (filtro.anoMaximo !== undefined) parametros.anoMaximo = filtro.anoMaximo;
  if (filtro.kmMaximo !== undefined) parametros.kmMaximo = filtro.kmMaximo;
  return parametros;
}

/** Resposta vazia ou sem lista vira página vazia, para a tela não quebrar. */
function normalizarCatalogo(corpo: unknown, filtro: FiltroCatalogo): RespostaCatalogo {
  const objeto = corpo as Partial<RespostaCatalogo> | null | undefined;
  if (objeto === null || typeof objeto !== 'object' || !Array.isArray(objeto.itens)) {
    return { itens: [], total: 0, pagina: filtro.pagina, tamanhoPagina: filtro.tamanhoPagina };
  }
  return {
    itens: objeto.itens,
    total: typeof objeto.total === 'number' ? objeto.total : objeto.itens.length,
    pagina: typeof objeto.pagina === 'number' ? objeto.pagina : filtro.pagina,
    tamanhoPagina: typeof objeto.tamanhoPagina === 'number' ? objeto.tamanhoPagina : filtro.tamanhoPagina,
  };
}

/** Página do catálogo de seminovos (200). Filtro inválido responde 400 (ErroApi). */
export async function listarCatalogo(filtro: FiltroCatalogo): Promise<RespostaCatalogo> {
  const resposta = await api.get<RespostaCatalogo>(`${CAMINHO_VEICULOS}/a-venda`, {
    params: montarParametrosCatalogo(filtro),
  });
  return normalizarCatalogo(resposta.data, filtro);
}
