/**
 * Chamadas do módulo de acesso (contratos auth, auth-recuperacao-senha e auth-google).
 * As telas usam estas funções pelos hooks do TanStack Query, nunca direto.
 */
import { api } from './client';
import type {
  GoogleLoginVO,
  GoogleVinculoVO,
  RedefinirSenhaVO,
  RequisicaoLogin,
  ReturnGoogleConfiguracaoVO,
  ReturnGoogleLoginVO,
  ReturnRecuperacaoSolicitadaVO,
  RespostaLogin,
  SolicitarRecuperacaoSenhaVO,
} from '@/types/autenticacao';

/** Entra no portal como cliente ou funcionário. Erros chegam como ErroApi. */
export async function autenticar(requisicao: RequisicaoLogin): Promise<RespostaLogin> {
  const resposta = await api.post<RespostaLogin>('/api/auth/login', requisicao);
  return resposta.data;
}

/** Pede o link de redefinição de senha. A resposta é sempre a mesma, exista ou não a conta. */
export async function solicitarRecuperacaoSenha(
  requisicao: SolicitarRecuperacaoSenhaVO,
): Promise<ReturnRecuperacaoSolicitadaVO> {
  const resposta = await api.post<ReturnRecuperacaoSolicitadaVO>(
    '/api/auth/recuperacao-senha',
    requisicao,
  );
  return resposta.data;
}

/** Define a nova senha com o token do link. Sucesso devolve 204, sem corpo. Não faz login. */
export async function redefinirSenha(requisicao: RedefinirSenhaVO): Promise<void> {
  await api.post('/api/auth/redefinicao-senha', requisicao);
}

/** Configuração pública do login com Google (se o botão deve aparecer). */
export async function obterConfiguracaoGoogle(): Promise<ReturnGoogleConfiguracaoVO> {
  const resposta = await api.get<ReturnGoogleConfiguracaoVO>('/api/auth/google/configuracao');
  return resposta.data;
}

/** Envia a credencial do Google. A situação diz o que a tela deve fazer a seguir. */
export async function loginComGoogle(requisicao: GoogleLoginVO): Promise<ReturnGoogleLoginVO> {
  const resposta = await api.post<ReturnGoogleLoginVO>('/api/auth/google/login', requisicao);
  return resposta.data;
}

/** Vincula a conta Google a um cliente, provando a senha da conta AutoVia. Abre a sessão do cliente. */
export async function vincularGoogle(requisicao: GoogleVinculoVO): Promise<RespostaLogin> {
  const resposta = await api.post<RespostaLogin>('/api/auth/google/vinculo', requisicao);
  return resposta.data;
}
