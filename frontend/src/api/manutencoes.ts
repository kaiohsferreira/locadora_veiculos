/**
 * Chamadas da manutenção da frota (contrato manutencao.contract.json v2). As telas usam estas funções pelos hooks do
 * TanStack Query, nunca direto. Só GESTOR_FROTA e ADMINISTRADOR chegam às rotas: o servidor responde 403 aos demais.
 */
import { api } from './client';
import type { Pagina } from '@/types/api';
import type {
  AbrirManutencaoRequisicao,
  AlertasRevisaoRetorno,
  EncerrarManutencaoRequisicao,
  FiltroManutencao,
  ManutencaoListaRetorno,
  ManutencaoRetorno,
  ResumoManutencaoRetorno,
} from '@/types/manutencao';

const CAMINHO_MANUTENCOES = '/api/manutencoes';

/** Parâmetros da query de GET /api/manutencoes. Filtro ausente vira parâmetro ausente; busca vazia não é enviada. */
export type ParametrosListagemManutencoes = Record<string, string | number>;

/** Monta a query da listagem: busca aparada e só enviada quando sobra texto; tipo, situação e veículo só quando definidos. */
export function montarParametrosListagemManutencoes(filtro: FiltroManutencao): ParametrosListagemManutencoes {
  const parametros: ParametrosListagemManutencoes = {
    pagina: filtro.pagina,
    tamanhoPagina: filtro.tamanhoPagina,
  };
  const busca = filtro.busca?.trim();
  if (busca) parametros.busca = busca;
  if (filtro.tipo !== undefined) parametros.tipo = filtro.tipo;
  if (filtro.situacao !== undefined) parametros.situacao = filtro.situacao;
  if (filtro.idVeiculo !== undefined) parametros.idVeiculo = filtro.idVeiculo;
  return parametros;
}

/** Lista paginada de ordens, com a capa de cada veículo (200 PaginaVO<ReturnManutencaoListaVO>). */
export async function listarManutencoes(filtro: FiltroManutencao): Promise<Pagina<ManutencaoListaRetorno>> {
  const resposta = await api.get<Pagina<ManutencaoListaRetorno>>(CAMINHO_MANUTENCOES, {
    params: montarParametrosListagemManutencoes(filtro),
  });
  return resposta.data;
}

/** Números do cabeçalho: ordens abertas e custo do mês corrente (200). Sem parâmetros. */
export async function buscarResumoManutencao(): Promise<ResumoManutencaoRetorno> {
  const resposta = await api.get<ResumoManutencaoRetorno>(`${CAMINHO_MANUTENCOES}/resumo`);
  return resposta.data;
}

/** Veículos que pedem revisão, do mais atrasado para o menos (200 ReturnAlertasRevisaoVO). */
export async function listarAlertasRevisao(): Promise<AlertasRevisaoRetorno> {
  const resposta = await api.get<AlertasRevisaoRetorno>(`${CAMINHO_MANUTENCOES}/alertas`);
  return resposta.data;
}

/** Abre a ordem de manutenção (201). O veículo passa a MANUTENCAO, quando ainda não estiver. */
export async function abrirManutencao(requisicao: AbrirManutencaoRequisicao): Promise<ManutencaoRetorno> {
  const resposta = await api.post<ManutencaoRetorno>(CAMINHO_MANUTENCOES, requisicao);
  return resposta.data;
}

/** Encerra a ordem (200). O veículo volta a DISPONIVEL quando estava em MANUTENCAO. */
export async function encerrarManutencao(
  idManutencao: number,
  requisicao: EncerrarManutencaoRequisicao,
): Promise<ManutencaoRetorno> {
  const resposta = await api.patch<ManutencaoRetorno>(`${CAMINHO_MANUTENCOES}/${idManutencao}/encerrar`, requisicao);
  return resposta.data;
}
