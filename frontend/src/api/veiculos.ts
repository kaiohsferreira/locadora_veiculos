/**
 * Chamadas da frota de veículos (contrato veiculo.contract.json v3, P1-BE-02). As telas usam estas funções pelos
 * hooks do TanStack Query. A disponibilidade é pública; as demais chamadas são do back office.
 */
import { api } from './client';
import type { Pagina } from '@/types/api';
import type { FiltroVeiculosDisponiveis, VeiculoDisponivel, VeiculoRequisicao, VeiculoRetorno } from '@/types/veiculos';

const CAMINHO_VEICULOS = '/api/veiculos';
const CAMINHO_VEICULOS_DISPONIVEIS = `${CAMINHO_VEICULOS}/disponiveis`;

/** Caminho de um veículo pelo id. */
function caminhoVeiculo(idVeiculo: number): string {
  return `${CAMINHO_VEICULOS}/${idVeiculo}`;
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
