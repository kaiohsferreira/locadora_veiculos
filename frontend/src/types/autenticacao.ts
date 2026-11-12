import type { CargoFuncionario, StatusCadastroCliente } from '@/types/enums';

/** Perfis de acesso da aplicação. */
export const PERFIS_USUARIO = ['cliente', 'funcionario'] as const;
export type PerfilUsuario = (typeof PERFIS_USUARIO)[number];

/** Perfis como o backend os envia (PerfilUsuario do contrato de login). */
export const PERFIS_API = ['CLIENTE', 'FUNCIONARIO'] as const;
export type PerfilApi = (typeof PERFIS_API)[number];

/**
 * Dados mínimos do usuário guardados na sessão do navegador.
 * A tela de acesso converte a resposta do backend para este formato
 * (`converterRespostaLogin` em `@/lib/conversaoLogin`) antes de chamar `entrar()`.
 * Só `nome` e `perfil` são obrigatórios; os demais vêm do login quando existem.
 */
export interface UsuarioSessao {
  nome: string;
  perfil: PerfilUsuario;
  /** Id do usuário no backend. */
  idUsuario?: number;
  /** Momento em que o token deixa de valer (ISO 8601, sem fuso, hora local do servidor). */
  expiraEm?: string;
  /** Cargo do funcionário (só para perfil funcionário). */
  cargo?: CargoFuncionario;
  /** Status do cadastro do cliente (só para perfil cliente). */
  statusCadastro?: StatusCadastroCliente;
}

/** Corpo do POST /api/auth/login. O CPF vai só com dígitos. */
export interface RequisicaoLogin {
  perfil: PerfilApi;
  cpf: string;
  senha: string;
}

/** Resposta do POST /api/auth/login (ReturnLoginVO do contrato). Enums chegam como texto. */
export interface RespostaLogin {
  token: string;
  tipoToken: string;
  expiraEm: string;
  expiraEmStr: string;
  perfil: PerfilApi;
  idUsuario: number;
  nome: string;
  cargo?: CargoFuncionario | null;
  statusCadastro?: StatusCadastroCliente | null;
}
