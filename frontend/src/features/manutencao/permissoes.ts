/**
 * Quem vê e usa a manutenção da frota (decisão P13 do Kaio, 20/11/2026): abrir e encerrar ordem só GESTOR_FROTA e
 * ADMINISTRADOR. A rota e o item do menu usam esta lista; o servidor confere de novo (política GestorFrota).
 */
import type { CargoFuncionario } from '@/types/enums';

export const CARGOS_OPERAR_MANUTENCAO: readonly CargoFuncionario[] = ['GESTOR_FROTA', 'ADMINISTRADOR'];
