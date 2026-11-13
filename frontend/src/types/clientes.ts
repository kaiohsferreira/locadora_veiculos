/**
 * Tipos do cadastro do cliente (contrato cliente.contract.json, P2-BE-02, RF-07).
 * Datas vão como "AAAA-MM-DD" (DateOnly). O backend devolve os enums como texto.
 * Contrato v2 (13/11/2026): inclui a verificação de cadastro (POST /api/clientes/verificar-cadastro).
 */
import type { CategoriaCnh, StatusCadastroCliente } from '@/types/enums';

/**
 * Corpo do POST /api/clientes (ClienteVO). Os opcionais vão como `null` quando vazios.
 * O CPF, o telefone, o número da CNH e o CEP vão só com dígitos.
 */
export interface RequisicaoCadastroCliente {
  nome: string;
  cpf: string;
  dataNascimento: string;
  email: string;
  telefone: string;
  cnhNumero: string;
  cnhCategoria: CategoriaCnh;
  cnhValidade: string;
  logradouro: string | null;
  numero: string | null;
  complemento: string | null;
  bairro: string | null;
  cidade: string | null;
  uf: string | null;
  cep: string | null;
  senha: string;
}

/**
 * Resposta do POST /api/clientes (201, ReturnClienteVO). Não traz senha nem token.
 * O cadastro nasce PENDENTE.
 */
export interface RespostaCliente {
  idCliente: number;
  nome: string;
  cpf: string;
  cpfStr: string;
  dataNascimento: string;
  dataNascimentoStr: string;
  email: string;
  telefone: string;
  cnhNumero: string;
  cnhCategoria: CategoriaCnh;
  cnhValidade: string;
  cnhValidadeStr: string;
  logradouro: string | null;
  numero: string | null;
  complemento: string | null;
  bairro: string | null;
  cidade: string | null;
  uf: string | null;
  cep: string | null;
  dataCadastro: string;
  dataCadastroStr: string;
  statusCadastro: StatusCadastroCliente;
}

/** Campo que a verificação de cadastro consulta (enum texto `CampoVerificacaoCadastro` do backend). */
export type CampoVerificacaoCadastro = 'CPF' | 'EMAIL' | 'CNH';

/**
 * Corpo do POST /api/clientes/verificar-cadastro (VerificarCadastroVO). Um campo por chamada.
 * O valor vai SEMPRE no corpo (dado pessoal): nunca em query string nem na URL.
 */
export interface RequisicaoVerificarCadastro {
  campo: CampoVerificacaoCadastro;
  valor: string;
}

/** Resposta 200 (ReturnVerificacaoCadastroVO): só diz se já existe cliente ativo com o valor. */
export interface RespostaVerificarCadastro {
  existe: boolean;
}
