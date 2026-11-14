/**
 * Chamadas da equipe da locadora (contrato equipe.contract.json, P10-BE-03). Só ADMINISTRADOR chega aqui: o servidor
 * responde 403 para os demais. As telas usam estas funções pelos hooks do TanStack Query, nunca direto.
 */
import { api } from './client';
import type {
  FiltroEquipe,
  FuncionarioEquipe,
  RequisicaoCadastroFuncionario,
  RequisicaoEdicaoFuncionario,
  RequisicaoSituacaoFuncionario,
  RespostaEquipe,
} from '@/types/equipe';

const CAMINHO_EQUIPE = '/api/equipe';

/** Parâmetros da query de GET /api/equipe. Filtro vazio vira parâmetro ausente; `ativo=false` é enviado. */
export type ParametrosListagemEquipe = Record<string, string | number | boolean>;

/**
 * Monta a query da lista. Busca vazia ou só com espaços não é enviada; cargo e situação só saem quando definidos
 * (inclusive `ativo=false`, que filtra os inativos).
 */
export function montarParametrosListagemEquipe(filtro: FiltroEquipe): ParametrosListagemEquipe {
  const parametros: ParametrosListagemEquipe = {
    pagina: filtro.pagina,
    tamanhoPagina: filtro.tamanhoPagina,
  };
  const busca = filtro.busca?.trim();
  if (busca) parametros.busca = busca;
  if (filtro.cargo !== undefined) parametros.cargo = filtro.cargo;
  if (filtro.ativo !== undefined) parametros.ativo = filtro.ativo;
  return parametros;
}

/** Lista paginada da equipe, com os totais da empresa (200 ReturnEquipeVO). */
export async function listarEquipe(filtro: FiltroEquipe): Promise<RespostaEquipe> {
  const resposta = await api.get<RespostaEquipe>(CAMINHO_EQUIPE, {
    params: montarParametrosListagemEquipe(filtro),
  });
  return resposta.data;
}

/** Consulta um funcionário pelo id (200). Usada para carregar o formulário de edição. */
export async function buscarFuncionarioEquipe(idFuncionario: number): Promise<FuncionarioEquipe> {
  const resposta = await api.get<FuncionarioEquipe>(`${CAMINHO_EQUIPE}/${idFuncionario}`);
  return resposta.data;
}

/** Adiciona um funcionário (201). A senha inicial vai só no corpo desta chamada. */
export async function cadastrarFuncionarioEquipe(
  requisicao: RequisicaoCadastroFuncionario,
): Promise<FuncionarioEquipe> {
  const resposta = await api.post<FuncionarioEquipe>(CAMINHO_EQUIPE, requisicao);
  return resposta.data;
}

/** Altera nome, matrícula, e-mail, admissão e cargo (204). O CPF nunca é enviado. */
export async function editarFuncionarioEquipe(
  idFuncionario: number,
  requisicao: RequisicaoEdicaoFuncionario,
): Promise<void> {
  await api.put(`${CAMINHO_EQUIPE}/${idFuncionario}`, requisicao);
}

/** Desativa (ativo = false) ou reativa (ativo = true) o acesso (204). Não existe exclusão. */
export async function alterarSituacaoFuncionarioEquipe(
  idFuncionario: number,
  requisicao: RequisicaoSituacaoFuncionario,
): Promise<void> {
  await api.patch(`${CAMINHO_EQUIPE}/${idFuncionario}/situacao`, requisicao);
}
