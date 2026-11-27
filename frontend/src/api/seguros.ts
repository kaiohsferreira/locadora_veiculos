/**
 * Chamadas da gestão de seguros (P9-BE-01, /api/seguros, só funcionário). As telas usam estas funções pelos hooks do
 * TanStack Query, nunca direto. Erros chegam como ErroApi (400 com campos, 404, 409).
 */
import { api } from './client';
import type { Apolice, ApoliceRequisicao, FiltroApolices, RespostaApolices } from '@/types/seguros';

const CAMINHO_SEGUROS = '/api/seguros';

/** Parâmetros da query da listagem. */
export type ParametrosApolices = Record<string, string | number | boolean>;

/** Monta a query: página e tamanho sempre; veículo só quando escolhido; "apenasAVencer" só quando ligado. */
export function montarParametrosApolices(filtro: FiltroApolices): ParametrosApolices {
  const parametros: ParametrosApolices = { pagina: filtro.pagina, tamanhoPagina: filtro.tamanhoPagina };
  if (filtro.idVeiculo !== undefined) parametros.idVeiculo = filtro.idVeiculo;
  if (filtro.apenasAVencer) parametros.apenasAVencer = true;
  return parametros;
}

/** Resposta vazia ou sem lista vira página vazia, para a tela não quebrar. */
function normalizarApolices(corpo: unknown, filtro: FiltroApolices): RespostaApolices {
  const objeto = corpo as Partial<RespostaApolices> | null | undefined;
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

/** Lista paginada das apólices (200). */
export async function listarApolices(filtro: FiltroApolices): Promise<RespostaApolices> {
  const resposta = await api.get<RespostaApolices>(CAMINHO_SEGUROS, { params: montarParametrosApolices(filtro) });
  return normalizarApolices(resposta.data, filtro);
}

/** Uma apólice pelo id (200; 404 quando não existe). */
export async function buscarApolice(idSeguro: number): Promise<Apolice> {
  const resposta = await api.get<Apolice>(`${CAMINHO_SEGUROS}/${idSeguro}`);
  return resposta.data;
}

/** Cadastra a apólice (201). 404 veículo inexistente; 409 número já usado por apólice ativa. */
export async function cadastrarApolice(requisicao: ApoliceRequisicao): Promise<Apolice> {
  const resposta = await api.post<Apolice>(CAMINHO_SEGUROS, requisicao);
  return resposta.data;
}

/** Edita a apólice (204). Mesmas recusas do cadastro, e 404 quando a apólice não existe mais. */
export async function editarApolice(idSeguro: number, requisicao: ApoliceRequisicao): Promise<void> {
  await api.put(`${CAMINHO_SEGUROS}/${idSeguro}`, requisicao);
}
