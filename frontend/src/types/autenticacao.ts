import type { CargoFuncionario, StatusCadastroCliente } from '@/types/enums';

/** Perfis de acesso da aplicação. */
export const PERFIS_USUARIO = ['cliente', 'funcionario'] as const;
export type PerfilUsuario = (typeof PERFIS_USUARIO)[number];

/** Perfis como o backend os envia (PerfilUsuario do contrato de login). */
export const PERFIS_API = ['CLIENTE', 'FUNCIONARIO'] as const;
export type PerfilApi = (typeof PERFIS_API)[number];

/**
 * Dados mínimos do usuário guardados na sessão do navegador.
 * Quem converte a resposta do login é o `useEntrar`, pela função `converterRespostaLogin`
 * de `@/lib/conversaoLogin`, antes de chamar `entrar()`.
 * Só `nome` e `perfil` são obrigatórios; os demais vêm do login quando existem.
 */
export interface UsuarioSessao {
  nome: string;
  perfil: PerfilUsuario;
  /** Id do usuário no backend. */
  idUsuario?: number;
  /**
   * Momento em que o token deixa de valer (ISO 8601). Quando o backend enviar o `exp` do
   * token (UTC, com Z), esse valor tem prioridade; o `expiraEm` da API (sem fuso) é o fallback.
   */
  expiraEm?: string;
  /** Cargo do funcionário (só para perfil funcionário). */
  cargo?: CargoFuncionario;
  /** Status do cadastro do cliente (só para perfil cliente). */
  statusCadastro?: StatusCadastroCliente;
}

// Login por CPF e senha ----------------------------------------------------------

/** Corpo do POST /api/auth/login (LoginVO). O CPF vai só com dígitos. */
export interface RequisicaoLogin {
  perfil: PerfilApi;
  cpf: string;
  senha: string;
  /** Mantém a sessão por 30 dias (RF-50). Omitido equivale a false. */
  lembrar?: boolean;
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

// Recuperação de senha (N1, RF-49) -----------------------------------------------

/** Corpo do POST /api/auth/recuperacao-senha (SolicitarRecuperacaoSenhaVO). O CPF vai só com dígitos. */
export interface SolicitarRecuperacaoSenhaVO {
  perfil: PerfilApi;
  cpf: string;
}

/** Resposta 202 de POST /api/auth/recuperacao-senha (ReturnRecuperacaoSolicitadaVO). */
export interface ReturnRecuperacaoSolicitadaVO {
  mensagem: string;
}

/** Corpo do POST /api/auth/redefinicao-senha (RedefinirSenhaVO). */
export interface RedefinirSenhaVO {
  /** Token do link (fragmento #token=), 1 a 128 caracteres. */
  token: string;
  /** 8 a 64 caracteres, com ao menos uma letra e um número. */
  novaSenha: string;
  /** Igual a novaSenha. */
  confirmacaoSenha: string;
}

// Continuar com o Google (N3, RF-51) ---------------------------------------------

/** Resposta de GET /api/auth/google/configuracao (ReturnGoogleConfiguracaoVO). */
export interface ReturnGoogleConfiguracaoVO {
  /** Falso: o botão do Google não aparece e nenhum erro é mostrado. */
  habilitado: boolean;
  /** Só vem preenchido quando habilitado. */
  clientId?: string | null;
  /** Verdadeiro quando o provedor é o FAKE (ambiente de teste). */
  modoTeste: boolean;
}

/** Corpo do POST /api/auth/google/login (GoogleLoginVO). */
export interface GoogleLoginVO {
  /** Credencial JWT devolvida pelo botão do Google (até 4096 caracteres). */
  credencial: string;
  lembrar?: boolean;
}

/** Situação devolvida pelo login com Google. Chega como texto. */
export type SituacaoLoginGoogle =
  | 'AUTENTICADO'
  | 'CONFIRMACAO_NECESSARIA'
  | 'CADASTRO_NECESSARIO'
  | 'CONTA_AMBIGUA';

/**
 * Resposta de POST /api/auth/google/login (ReturnGoogleLoginVO), como chega pela rede.
 * `sessao` só vem com AUTENTICADO; `nome` e `email` vêm com CONFIRMACAO_NECESSARIA e CADASTRO_NECESSARIO.
 */
export interface ReturnGoogleLoginVO {
  situacao: SituacaoLoginGoogle;
  sessao?: RespostaLogin | null;
  nome?: string | null;
  email?: string | null;
}

/**
 * Forma discriminada do resultado do login com Google, para as telas. Quem converte o
 * `ReturnGoogleLoginVO` nesta forma é a camada de hooks, não este arquivo.
 */
export type ResultadoLoginGoogle =
  | { situacao: 'AUTENTICADO'; sessao: RespostaLogin }
  | { situacao: 'CONFIRMACAO_NECESSARIA' | 'CADASTRO_NECESSARIO'; nome: string | null; email: string | null }
  | { situacao: 'CONTA_AMBIGUA' };

/** Corpo do POST /api/auth/google/vinculo (GoogleVinculoVO). Vincula a conta Google a um cliente. */
export interface GoogleVinculoVO {
  /** Credencial JWT guardada em memória desde o login com Google. */
  credencial: string;
  /** CPF com 11 dígitos, sem máscara. */
  cpf: string;
  /** Senha da conta AutoVia, 1 a 128 caracteres. */
  senha: string;
  lembrar?: boolean;
}
