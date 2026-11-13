/**
 * Chamadas do cadastro do cliente (contrato cliente.contract.json v2, P2-BE-02): o POST do cadastro e
 * a verificação de CPF, e-mail e CNH já cadastrados. As telas usam estas funções pelos hooks do TanStack Query.
 */
import { api } from './client';
import type {
  RequisicaoCadastroCliente,
  RequisicaoVerificarCadastro,
  RespostaCliente,
  RespostaVerificarCadastro,
} from '@/types/clientes';

/** Limite de espera da verificação de cadastro: curto, pois ela nunca bloqueia o cadastro. */
const TEMPO_LIMITE_VERIFICACAO_MS = 5_000;

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

/**
 * Pergunta se já existe cliente ativo com o CPF, o e-mail ou a CNH (200 `{ existe }`). Endpoint público.
 * O valor é dado pessoal: vai só no corpo, nunca em `params` nem na URL. Erros 400, 429 e 500 chegam
 * como ErroApi; quem chama decide ignorá-los (a verificação falha aberta).
 */
export async function verificarCadastroCliente(
  requisicao: RequisicaoVerificarCadastro,
  signal?: AbortSignal,
): Promise<RespostaVerificarCadastro> {
  const resposta = await api.post<RespostaVerificarCadastro>(
    '/api/clientes/verificar-cadastro',
    { campo: requisicao.campo, valor: requisicao.valor },
    { signal, timeout: TEMPO_LIMITE_VERIFICACAO_MS },
  );
  return resposta.data;
}
