/**
 * Quem vê o painel financeiro (tela 09, contrato painel-financeiro: política Financeiro, PodeVerValores). Só FINANCEIRO e
 * ADMINISTRADOR. A rota e o item do menu usam esta lista; o servidor confere de novo (403 para os demais).
 */
import type { CargoFuncionario } from '@/types/enums';

export const CARGOS_VER_PAINEL_FINANCEIRO: readonly CargoFuncionario[] = ['FINANCEIRO', 'ADMINISTRADOR'];
