/**
 * Tipos do cadastro do cliente (contrato cliente.contract.json, P2-BE-02, RF-07).
 * Datas vão como "AAAA-MM-DD" (DateOnly). O backend devolve os enums como texto.
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
