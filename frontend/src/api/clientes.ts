/**
 * Chamadas do cadastro do cliente (contrato cliente.contract.json v1, P2-BE-02).
 * Só o POST existe por enquanto. A tela usa esta função pelo hook do TanStack Query.
 */
import { api } from './client';
import type { RequisicaoCadastroCliente, RespostaCliente } from '@/types/clientes';

/**
 * Cadastra o cliente (201). O cadastro nasce PENDENTE e a resposta não traz token:
 * para entrar, a tela chama o login depois. Erros chegam como ErroApi.
 */
export async function cadastrarCliente(
  requisicao: RequisicaoCadastroCliente,
): Promise<RespostaCliente> {
  const resposta = await api.post<RespostaCliente>('/api/clientes', requisicao);
  return resposta.data;
}
