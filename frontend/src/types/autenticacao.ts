/** Perfis de acesso da aplicação. */
export const PERFIS_USUARIO = ['cliente', 'funcionario'] as const;
export type PerfilUsuario = (typeof PERFIS_USUARIO)[number];

/**
 * Dados mínimos do usuário guardados na sessão do navegador.
 * Quando o contrato do login (P2-BE-01) sair, a tela de acesso converte a
 * resposta do backend para este formato antes de chamar `entrar()`.
 */
export interface UsuarioSessao {
  nome: string;
  perfil: PerfilUsuario;
}
