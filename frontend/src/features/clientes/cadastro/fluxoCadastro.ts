/**
 * Lógica pura do envio do cadastro (P2-FE-02): sem React, sem Axios. As dependências
 * (cadastrar e entrar) chegam por parâmetro, para o teste usar dublês.
 */
import { somenteDigitos } from '@/lib/formatters';
import type { RequisicaoLogin } from '@/types/autenticacao';
import type { RequisicaoCadastroCliente, RespostaCliente } from '@/types/clientes';
import { converterFormularioParaRequisicao } from './conversaoCadastro';
import type { FormularioCadastro } from './esquemaCadastro';

/** Campos que a 409 (conflito) pode apontar. */
export type CampoConflito = 'cpf' | 'cnhNumero';

/**
 * Qual campo o conflito 409 aponta, pela mensagem da API ("...CNH..." ou "...CPF...").
 * Null quando a mensagem não cita nenhum dos dois: vai para o alerta geral.
 */
export function escolherCampoDoConflito(mensagem: string): CampoConflito | null {
  if (mensagem.includes('CNH')) return 'cnhNumero';
  if (mensagem.includes('CPF')) return 'cpf';
  return null;
}

/** Ordem em que os campos aparecem na tela (seções, de cima para baixo). */
export const ORDEM_CAMPOS_TELA: readonly (keyof FormularioCadastro)[] = [
  'nome',
  'cpf',
  'dataNascimento',
  'email',
  'telefone',
  'cnhNumero',
  'cnhCategoria',
  'cnhValidade',
  'cep',
  'logradouro',
  'numero',
  'complemento',
  'bairro',
  'cidade',
  'uf',
  'senha',
  'confirmarSenha',
];

/** Primeiro campo da lista na ordem da tela (não na ordem em que o backend a enviou). */
export function primeiroCampoNaTela(campos: readonly string[]): keyof FormularioCadastro | undefined {
  return ORDEM_CAMPOS_TELA.find((campo) => campos.includes(campo));
}

/** Dependências do envio: o POST do cadastro e o login automático. */
export interface DependenciasCadastro {
  cadastrar: (requisicao: RequisicaoCadastroCliente) => Promise<RespostaCliente>;
  entrar: (requisicao: RequisicaoLogin) => Promise<unknown>;
}

/**
 * Resultado do envio:
 * - `erro-cadastro`: o POST (ou a conversão) falhou; o login não foi tentado;
 * - `com-login`: cadastro criado e sessão aberta;
 * - `sem-login`: cadastro criado, mas o login automático falhou (não se refaz o POST).
 */
export type ResultadoCadastro =
  | { tipo: 'erro-cadastro'; erro: unknown }
  | { tipo: 'com-login' }
  | { tipo: 'sem-login' };

/**
 * Sequência do envio: POST uma vez; se criou, login automático com perfil CLIENTE e
 * CPF só com dígitos. Nunca chama o POST duas vezes.
 */
export async function executarCadastro(
  dependencias: DependenciasCadastro,
  valores: FormularioCadastro,
): Promise<ResultadoCadastro> {
  try {
    await dependencias.cadastrar(converterFormularioParaRequisicao(valores));
  } catch (erro) {
    return { tipo: 'erro-cadastro', erro };
  }

  try {
    await dependencias.entrar({
      perfil: 'CLIENTE',
      cpf: somenteDigitos(valores.cpf),
      senha: valores.senha,
    });
  } catch {
    return { tipo: 'sem-login' };
  }

  return { tipo: 'com-login' };
}
