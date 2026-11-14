import { Navigate, Outlet } from 'react-router-dom';
import { useCadastroLocadora } from './ContextoCadastroLocadora';
import { caminhoSemDadosEmpresa } from './etapasLocadora';

/**
 * Rota-pai das etapas 2 e 3 do cadastro da locadora: sem os dados da etapa 1 em memória (entrada direta
 * pela barra de endereços, por exemplo), volta para a etapa 1, como o cadastro do cliente faz.
 */
export function ExigirDadosEmpresa() {
  const { dadosEmpresa } = useCadastroLocadora();
  const destino = caminhoSemDadosEmpresa(2, dadosEmpresa !== null);
  if (destino !== null) return <Navigate to={destino} replace />;
  return <Outlet />;
}
