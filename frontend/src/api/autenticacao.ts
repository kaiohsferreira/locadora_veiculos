/**
 * Chamadas do módulo de acesso (contrato auth.contract.json, P2-BE-01).
 * Só o login existe por enquanto. As telas usam estas funções pelos hooks do TanStack Query.
 */
import { api } from './client';
import type { RequisicaoLogin, RespostaLogin } from '@/types/autenticacao';

/** Entra no portal como cliente ou funcionário. Erros chegam como ErroApi. */
export async function autenticar(requisicao: RequisicaoLogin): Promise<RespostaLogin> {
  const resposta = await api.post<RespostaLogin>('/api/auth/login', requisicao);
  return resposta.data;
}
