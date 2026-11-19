/**
 * Chamadas da verificação de cadastros da locadora (contrato cliente-fila-verificacao v1, tela 18). As telas usam estas
 * funções pelos hooks do TanStack Query, nunca direto. Só ATENDENTE e ADMINISTRADOR chegam aqui (o servidor responde 403
 * para os demais).
 */
import { api } from './client';
import type { Pagina } from '@/types/api';
import type { DocumentoVerificacao, ClienteFila, ClienteVerificacao, FiltroFilaVerificacao, RequisicaoDecisaoVerificacao } from '@/types/verificacaoCadastros';

const CAMINHO_FILA = '/api/clientes/verificacoes';

/** Caminho do cadastro para a verificação (detalhe e decisão). */
function caminhoVerificacao(idCliente: number): string {
  return `/api/clientes/${idCliente}/verificacao`;
}

/** Parâmetros da query da fila. Busca vazia ou só com espaços não é enviada; status ausente = todos. */
export type ParametrosFilaVerificacao = Record<string, string | number>;

/** Monta a query da fila: busca aparada e só enviada quando sobra texto; status só quando escolhido. */
export function montarParametrosFilaVerificacao(filtro: FiltroFilaVerificacao): ParametrosFilaVerificacao {
  const parametros: ParametrosFilaVerificacao = {
    pagina: filtro.pagina,
    tamanhoPagina: filtro.tamanhoPagina,
  };
  if (filtro.status !== undefined) parametros.status = filtro.status;
  const busca = filtro.busca?.trim();
  if (busca) parametros.busca = busca;
  return parametros;
}

/** Fila paginada de cadastros da locadora (200 PaginaVO<ReturnClienteFilaVO>). */
export async function listarFilaVerificacao(filtro: FiltroFilaVerificacao): Promise<Pagina<ClienteFila>> {
  const resposta = await api.get<Pagina<ClienteFila>>(CAMINHO_FILA, {
    params: montarParametrosFilaVerificacao(filtro),
  });
  return resposta.data;
}

/** Dados do cadastro para o painel (200). 404 quando o cliente não tem pedido nesta locadora. */
export async function buscarVerificacaoCliente(idCliente: number): Promise<ClienteVerificacao> {
  const resposta = await api.get<ClienteVerificacao>(caminhoVerificacao(idCliente));
  return resposta.data;
}

/**
 * Documentos ativos do cliente, com o link temporário de leitura de cada um (200, lista). Resposta que não é lista vira
 * lista vazia. 404 quando o cliente não tem pedido nesta locadora; 502 quando o armazenamento falha.
 */
export async function listarDocumentosVerificacao(idCliente: number): Promise<DocumentoVerificacao[]> {
  const resposta = await api.get<unknown>(`${caminhoVerificacao(idCliente)}/documentos`);
  return Array.isArray(resposta.data) ? (resposta.data as DocumentoVerificacao[]) : [];
}

/**
 * Decide o pedido (PATCH, 204 sem corpo). Recusa exige justificativa (o servidor responde 400 sem ela); aprovação não
 * leva justificativa. 409 quando o cadastro já está aprovado; 404 quando o cliente não tem pedido nesta locadora.
 */
export async function decidirVerificacaoCliente(
  idCliente: number,
  requisicao: RequisicaoDecisaoVerificacao,
): Promise<void> {
  await api.patch(caminhoVerificacao(idCliente), requisicao);
}
