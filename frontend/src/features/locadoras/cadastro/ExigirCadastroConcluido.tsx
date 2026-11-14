import { Navigate, Outlet } from 'react-router-dom';
import { useCadastroLocadora } from './ContextoCadastroLocadora';
import { caminhoSemCadastroConcluido } from './etapasLocadora';

/**
 * Rota-pai da confirmação (etapa 3): sem a conta criada no envio da etapa 2 em memória (entrada direta pela
 * barra de endereços ou recarregamento), volta para a etapa certa, como o cadastro do cliente faz.
 */
export function ExigirCadastroConcluido() {
  const { dadosEmpresa, resultado } = useCadastroLocadora();
  const destino = caminhoSemCadastroConcluido(dadosEmpresa !== null, resultado !== null);
  if (destino !== null) return <Navigate to={destino} replace />;
  return <Outlet />;
}
