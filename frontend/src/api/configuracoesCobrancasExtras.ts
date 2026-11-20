import { api } from './client';
import type { Pagina } from '@/types/api';
import type { ConfiguracaoCobrancasExtras, DadosConfiguracaoCobrancasExtras, EditarConfiguracaoCobrancasExtras, FiltroConfiguracoesCobrancasExtras } from '@/types/configuracoesCobrancasExtras';

const CAMINHO = '/api/configuracoes-cobrancas-extras';
export async function listarConfiguracoesCobrancasExtras(filtro: FiltroConfiguracoesCobrancasExtras, signal?: AbortSignal): Promise<Pagina<ConfiguracaoCobrancasExtras>> {
  return (await api.get<Pagina<ConfiguracaoCobrancasExtras>>(CAMINHO, { params: filtro, signal })).data;
}
export async function buscarConfiguracaoCobrancasExtras(id: number, signal?: AbortSignal): Promise<ConfiguracaoCobrancasExtras> {
  return (await api.get<ConfiguracaoCobrancasExtras>(`${CAMINHO}/${id}`, { signal })).data;
}
export async function criarConfiguracaoCobrancasExtras(dados: DadosConfiguracaoCobrancasExtras): Promise<ConfiguracaoCobrancasExtras> {
  return (await api.post<ConfiguracaoCobrancasExtras>(CAMINHO, dados)).data;
}
export async function editarConfiguracaoCobrancasExtras(id: number, dados: EditarConfiguracaoCobrancasExtras): Promise<void> {
  await api.put(`${CAMINHO}/${id}`, dados);
}
export async function desativarConfiguracaoCobrancasExtras(id: number): Promise<void> {
  await api.delete(`${CAMINHO}/${id}`);
}
