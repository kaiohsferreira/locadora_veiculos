import { SEGUROS_EM_DIA, SEGUROS_VENCE_HOJE, textoAte, textoVenceEm, textoVencidaHa } from '@/lib/mensagensSeguros';
import type { Apolice } from '@/types/seguros';

/** Tom da etiqueta de vencimento (os mesmos tons da Etiqueta do sistema). */
export type TomVencimento = 'perigo' | 'pendente' | 'sucesso';

/** Situação do vencimento de uma apólice: sempre com texto, nunca só cor (RF-41). */
export interface DescricaoVencimento {
  texto: string;
  tom: TomVencimento;
  /** Ícone Material Symbols da etiqueta. */
  icone: string;
  /** Linha de apoio (só no "Em dia": "até dd/MM/aaaa"). */
  detalhe: string | null;
}

/**
 * Descreve o vencimento a partir do que o servidor calculou (`diasParaVencer` e `venceEm60Dias`), sem refazer a conta
 * com a data do navegador:
 * - negativo: "Vencida há N dias" (perigo);
 * - zero: "Vence hoje" (perigo);
 * - de 1 a 60 (`venceEm60Dias`): "Vence em N dias" (pendente);
 * - mais de 60: "Em dia", com "até dd/MM/aaaa" (sucesso).
 */
export function descreverVencimento(apolice: Pick<Apolice, 'diasParaVencer' | 'venceEm60Dias' | 'dataFimStr'>): DescricaoVencimento {
  const dias = apolice.diasParaVencer;
  if (dias < 0) return { texto: textoVencidaHa(Math.abs(dias)), tom: 'perigo', icone: 'error', detalhe: null };
  if (dias === 0) return { texto: SEGUROS_VENCE_HOJE, tom: 'perigo', icone: 'warning', detalhe: null };
  if (apolice.venceEm60Dias) return { texto: textoVenceEm(dias), tom: 'pendente', icone: 'schedule', detalhe: null };
  return { texto: SEGUROS_EM_DIA, tom: 'sucesso', icone: 'check_circle', detalhe: textoAte(apolice.dataFimStr) };
}
