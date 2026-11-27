/**
 * Chamadas da frota de veículos (contrato veiculo.contract.json v3, P1-BE-02). As telas usam estas funções pelos
 * hooks do TanStack Query. A disponibilidade é pública; as demais chamadas são do back office.
 */
import { api } from './client';
import type { Pagina } from '@/types/api';
import type {
  DestinoManualStatusVeiculo,
  FiltroListagemVeiculos,
  FiltroVeiculosDisponiveis,
  RequisicaoStatusVeiculo,
  VeiculoDisponivel,
  VeiculoRequisicao,
  VeiculoRetorno,
} from '@/types/veiculos';

const CAMINHO_VEICULOS = '/api/veiculos';
const CAMINHO_VEICULOS_DISPONIVEIS = `${CAMINHO_VEICULOS}/disponiveis`;

/** Caminho de um veículo pelo id. */
function caminhoVeiculo(idVeiculo: number): string {
  return `${CAMINHO_VEICULOS}/${idVeiculo}`;
}

/** Parâmetros da query de GET /api/veiculos. Filtro ausente vira parâmetro ausente; busca vazia não é enviada. */
export type ParametrosListagemVeiculos = Record<string, string | number>;

/** Monta a query da listagem: busca aparada e só enviada quando sobra texto; status e categoria só quando definidos. */
export function montarParametrosListagemVeiculos(filtro: FiltroListagemVeiculos): ParametrosListagemVeiculos {
  const parametros: ParametrosListagemVeiculos = {
    pagina: filtro.pagina,
    tamanhoPagina: filtro.tamanhoPagina,
  };
  if (filtro.status !== undefined) parametros.status = filtro.status;
  if (filtro.idCategoria !== undefined) parametros.idCategoria = filtro.idCategoria;
  const busca = filtro.busca?.trim();
  if (busca) parametros.busca = busca;
  return parametros;
}

/** Lista paginada de veículos da locadora, com a capa de cada item (200 PaginaVO<ReturnVeiculoVO>). */
export async function listarVeiculos(filtro: FiltroListagemVeiculos): Promise<Pagina<VeiculoRetorno>> {
  const resposta = await api.get<Pagina<VeiculoRetorno>>(CAMINHO_VEICULOS, {
    params: montarParametrosListagemVeiculos(filtro),
  });
  return resposta.data;
}

/** Consulta um veículo pelo id (200). Usada para carregar o formulário de edição. */
export async function buscarVeiculo(idVeiculo: number): Promise<VeiculoRetorno> {
  const resposta = await api.get<VeiculoRetorno>(caminhoVeiculo(idVeiculo));
  return resposta.data;
}

/** Cadastra um veículo (201). O status nasce DISPONIVEL no servidor; o corpo não o envia. */
export async function cadastrarVeiculo(requisicao: VeiculoRequisicao): Promise<VeiculoRetorno> {
  const resposta = await api.post<VeiculoRetorno>(CAMINHO_VEICULOS, requisicao);
  return resposta.data;
}

/** Altera os 13 campos do veículo (204, sem corpo). O status não muda por esta rota. */
export async function editarVeiculo(idVeiculo: number, requisicao: VeiculoRequisicao): Promise<void> {
  await api.put(caminhoVeiculo(idVeiculo), requisicao);
}

/**
 * Troca manual de status (PATCH /api/veiculos/{id}/status, 204 sem corpo). Só GESTOR_FROTA e ADMINISTRADOR: o servidor
 * responde 403 para os demais, 404 para veículo de outra locadora ou desativado e 409 quando o fluxo não permite.
 */
export async function alterarStatusVeiculo(idVeiculo: number, status: DestinoManualStatusVeiculo): Promise<void> {
  const requisicao: RequisicaoStatusVeiculo = { status };
  await api.patch(`${caminhoVeiculo(idVeiculo)}/status`, requisicao);
}

/** Consulta pública de veículos disponíveis por período, cidade/UF, categoria e faixa de diária (P3-BE-01). */
export async function buscarVeiculosDisponiveis(
  filtro: FiltroVeiculosDisponiveis,
  signal?: AbortSignal,
): Promise<Pagina<VeiculoDisponivel>> {
  const resposta = await api.get<Pagina<VeiculoDisponivel>>(CAMINHO_VEICULOS_DISPONIVEIS, {
    params: filtro,
    signal,
  });
  return resposta.data;
}

/** Quantos veículos a busca por placa mostra de uma vez. */
export const LIMITE_BUSCA_PLACA = 8;

/**
 * Busca veículos ativos pela placa (GET /api/veiculos?placa=, parte da placa; o servidor normaliza). Usada pela busca de
 * veículo da gestão de seguros. Lista ordenada por placa, no máximo LIMITE_BUSCA_PLACA itens.
 */
export async function buscarVeiculosPorPlaca(placa: string): Promise<VeiculoRetorno[]> {
  const resposta = await api.get<Pagina<VeiculoRetorno>>(CAMINHO_VEICULOS, {
    params: { placa: placa.trim(), pagina: 1, tamanhoPagina: LIMITE_BUSCA_PLACA },
  });
  return Array.isArray(resposta.data?.itens) ? resposta.data.itens : [];
}
