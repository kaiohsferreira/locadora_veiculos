import { Navigate, Outlet, useLocation } from 'react-router-dom';
import type { PerfilUsuario } from '@/types/autenticacao';
import { AcessoNegado } from './AcessoNegado';
import { useAutenticacao } from './autenticacao/useAutenticacao';
import { CAMINHOS, type EstadoRedirecionamentoLogin } from './caminhos';

export interface RotaProtegidaProps {
  /** Perfis que podem abrir as rotas filhas. */
  perfisPermitidos: readonly PerfilUsuario[];
}

/**
 * Protege as rotas filhas por perfil.
 * - Sem sessão: vai para /entrar, guardando a página de origem para voltar depois.
 * - Com sessão de outro perfil: mostra "Acesso não permitido".
 * - Com VITE_IGNORAR_AUTENTICACAO=true (só no npm run dev): libera tudo.
 */
export function RotaProtegida({ perfisPermitidos }: RotaProtegidaProps) {
  const { autenticado, autenticacaoIgnorada, possuiPerfil } = useAutenticacao();
  const localizacao = useLocation();

  if (autenticacaoIgnorada) return <Outlet />;

  if (!autenticado) {
    const estado: EstadoRedirecionamentoLogin = { de: `${localizacao.pathname}${localizacao.search}` };
    return <Navigate to={CAMINHOS.entrar} replace state={estado} />;
  }

  if (!possuiPerfil(perfisPermitidos)) return <AcessoNegado />;

  return <Outlet />;
}
