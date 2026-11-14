import { Outlet } from 'react-router-dom';
import type { CargoFuncionario } from '@/types/enums';
import { AcessoNegado } from './AcessoNegado';
import { useAutenticacao } from './autenticacao/useAutenticacao';
import { cargoPermitido, usePermissoes } from './autenticacao/usePermissoes';

export interface RotaPorCargoProps {
  /** Cargos que podem abrir as rotas filhas. */
  cargosPermitidos: readonly CargoFuncionario[];
}

/**
 * Protege as rotas filhas por cargo de funcionário. Use dentro de `RotaProtegida` (perfil funcionário):
 * sem cargo permitido, mostra "Acesso não permitido". Não substitui a política do servidor.
 * Com VITE_IGNORAR_AUTENTICACAO=true (só no npm run dev) libera tudo, como `RotaProtegida`.
 */
export function RotaPorCargo({ cargosPermitidos }: RotaPorCargoProps) {
  const { autenticacaoIgnorada } = useAutenticacao();
  const { cargo } = usePermissoes();
  if (autenticacaoIgnorada) return <Outlet />;
  if (!cargoPermitido(cargo, cargosPermitidos)) return <AcessoNegado />;
  return <Outlet />;
}
