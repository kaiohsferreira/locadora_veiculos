/**
 * Chamadas do cadastro da locadora (contrato empresa.contract.json v1, P10-BE-01): o POST do cadastro e a
 * verificação de CNPJ e e-mail já cadastrados. As telas usam estas funções pelos hooks do TanStack Query.
 */
import { api } from './client';
import type {
  RequisicaoCadastroLocadora,
  RequisicaoVerificarEmpresa,
  RespostaEmpresa,
  RespostaVerificarEmpresa,
} from '@/types/empresas';

/** Limite de espera da verificação: curto, pois ela nunca bloqueia o cadastro. */
const TEMPO_LIMITE_VERIFICACAO_MS = 5_000;

/**
 * Cadastra a locadora e o responsável (201, sem header Location e sem token). Erros chegam como ErroApi.
 * Chamada pelo hook `useCadastrarLocadora`, no envio da etapa 2 (P10-FE-02).
 */
export async function cadastrarLocadora(requisicao: RequisicaoCadastroLocadora): Promise<RespostaEmpresa> {
  const resposta = await api.post<RespostaEmpresa>('/api/empresas', requisicao);
  return resposta.data;
}

/**
 * Pergunta se já existe empresa com o CNPJ ou o e-mail (200 `{ existe }`). Endpoint público.
 * O valor vai só no corpo, nunca em `params` nem na URL. Erros 400, 429 e 500 chegam como ErroApi;
 * quem chama decide ignorá-los (a verificação falha aberta).
 */
export async function verificarCadastroEmpresa(
  requisicao: RequisicaoVerificarEmpresa,
  signal?: AbortSignal,
): Promise<RespostaVerificarEmpresa> {
  const resposta = await api.post<RespostaVerificarEmpresa>(
    '/api/empresas/verificar-cadastro',
    { campo: requisicao.campo, valor: requisicao.valor },
    { signal, timeout: TEMPO_LIMITE_VERIFICACAO_MS },
  );
  return resposta.data;
}
