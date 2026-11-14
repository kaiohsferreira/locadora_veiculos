import type { UsuarioSessao } from '@/types/autenticacao';
import type { CargoFuncionario } from '@/types/enums';
import { useAutenticacao } from './useAutenticacao';

/**
 * Permissões de quem está na sessão, derivadas do cargo e da locadora guardados na sessão.
 * Só afeta a tela: a autorização de verdade é do servidor (políticas por cargo).
 */
export interface PermissoesUsuario {
  /** Cargo do funcionário; null para cliente, para sessão sem cargo e sem sessão. */
  cargo: CargoFuncionario | null;
  /** Cargo ADMINISTRADOR. */
  ehAdministrador: boolean;
  /** Pode ver valores monetários (ADMINISTRADOR ou FINANCEIRO). */
  podeVerValores: boolean;
  /** A locadora do funcionário é a operadora da plataforma. */
  ehOperadora: boolean;
}

/** Cargos que veem valores monetários. Deve espelhar a regra do servidor (PodeVerValores). */
const CARGOS_COM_VALORES: readonly CargoFuncionario[] = ['ADMINISTRADOR', 'FINANCEIRO'];

/** Calcula as permissões a partir do usuário da sessão (função pura, sem React). */
export function calcularPermissoes(usuario: UsuarioSessao | null): PermissoesUsuario {
  const ehFuncionario = usuario?.perfil === 'funcionario';
  const cargo: CargoFuncionario | null = ehFuncionario ? (usuario.cargo ?? null) : null;
  return {
    cargo,
    ehAdministrador: cargo === 'ADMINISTRADOR',
    podeVerValores: cargo !== null && CARGOS_COM_VALORES.includes(cargo),
    ehOperadora: ehFuncionario && usuario.empresaOperadora === true,
  };
}

/** Confere se o cargo está na lista de cargos permitidos. Sem cargo, nunca está. */
export function cargoPermitido(cargo: CargoFuncionario | null, cargosPermitidos: readonly CargoFuncionario[]): boolean {
  return cargo !== null && cargosPermitidos.includes(cargo);
}

/** Permissões do usuário da sessão atual. */
export function usePermissoes(): PermissoesUsuario {
  const { usuario } = useAutenticacao();
  return calcularPermissoes(usuario);
}
