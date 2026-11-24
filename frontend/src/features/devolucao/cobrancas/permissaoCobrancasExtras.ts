import { cargoPermitido } from '@/routes/autenticacao/usePermissoes';
import type { CargoFuncionario } from '@/types/enums';

/** Cargos que simulam e apuram a cobrança extra (decisão do Kaio em 24/11/2026: "So atendente e admin apuram"). */
export const CARGOS_APURAM_COBRANCAS_EXTRAS: readonly CargoFuncionario[] = ['ATENDENTE', 'ADMINISTRADOR'];

/**
 * Só ATENDENTE e ADMINISTRADOR veem o simulador e o botão de apurar. Sem cargo (cliente ou sessão vazia), nunca. Só afeta a
 * tela: o servidor confere de novo (políticas ApuradorCobrancasExtras, contrato cobrancas-extras v2).
 */
export function podeApurarCobrancasExtras(cargo: CargoFuncionario | null): boolean {
  return cargoPermitido(cargo, CARGOS_APURAM_COBRANCAS_EXTRAS);
}
