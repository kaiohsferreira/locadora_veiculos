import {
  SEGUROS_STATUS_VIGENTE,
  SEGUROS_VENCE_HOJE,
  textoFaltam,
  textoVenceEmBreve,
  textoVencidaHa,
} from '@/lib/mensagensSeguros';
import type { Apolice } from '@/types/seguros';

/** Tom do status (os mesmos tons da Etiqueta do sistema). */
export type TomVencimento = 'perigo' | 'pendente' | 'sucesso';

/** Status de uma apólice: sempre com texto, nunca só cor (RF-41). */
export interface DescricaoVencimento {
  texto: string;
  tom: TomVencimento;
  /** Ícone Material Symbols do status. */
  icone: string;
}

/** Prazo embaixo da vigência ("Faltam N dias"), com destaque quando vence em até 60 dias. */
export interface DescricaoPrazo {
  texto: string;
  destaque: boolean;
}

type DadosVencimento = Pick<Apolice, 'diasParaVencer' | 'venceEm60Dias'>;

/**
 * Status da coluna "Status" (desenho 13), a partir do que o servidor calculou (`diasParaVencer` e `venceEm60Dias`), sem
 * refazer a conta com a data do navegador:
 * - negativo: "Vencida há N dias" (perigo);
 * - zero: "Vence hoje" (perigo);
 * - de 1 a 60 (`venceEm60Dias`): "Vence em breve (N dias)" (pendente);
 * - mais de 60: "Vigente" (sucesso).
 */
export function descreverVencimento(apolice: DadosVencimento): DescricaoVencimento {
  const dias = apolice.diasParaVencer;
  if (dias < 0) return { texto: textoVencidaHa(Math.abs(dias)), tom: 'perigo', icone: 'error' };
  if (dias === 0) return { texto: SEGUROS_VENCE_HOJE, tom: 'perigo', icone: 'warning' };
  if (apolice.venceEm60Dias) return { texto: textoVenceEmBreve(dias), tom: 'pendente', icone: 'schedule' };
  return { texto: SEGUROS_STATUS_VIGENTE, tom: 'sucesso', icone: 'check_circle' };
}

/**
 * Linha embaixo da vigência: "Faltam N dias" enquanto a apólice vale (em destaque quando vence em até 60 dias), "Vence
 * hoje" no último dia e "Vencida há N dias" depois.
 */
export function descreverPrazo(apolice: DadosVencimento): DescricaoPrazo {
  const dias = apolice.diasParaVencer;
  if (dias < 0) return { texto: textoVencidaHa(Math.abs(dias)), destaque: true };
  if (dias === 0) return { texto: SEGUROS_VENCE_HOJE, destaque: true };
  return { texto: textoFaltam(dias), destaque: apolice.venceEm60Dias };
}

/** Palavras que não entram nas iniciais da seguradora. */
const PALAVRAS_IGNORADAS = new Set(['de', 'da', 'do', 'das', 'dos', 'e', 's.a.', 'sa', 's/a']);

/**
 * Iniciais da seguradora para o círculo da tabela: a primeira letra das duas primeiras palavras ("Porto Seguro" → "PS",
 * "Tokio Marine Seguradora" → "TM"); com uma palavra só, as duas primeiras letras ("Mapfre" → "MA").
 */
export function iniciaisSeguradora(seguradora: string): string {
  const palavras = seguradora
    .trim()
    .split(/\s+/)
    .filter((palavra) => palavra.length > 0 && !PALAVRAS_IGNORADAS.has(palavra.toLowerCase()));
  if (palavras.length === 0) return '?';
  if (palavras.length === 1) return palavras[0].slice(0, 2).toUpperCase();
  return `${palavras[0][0]}${palavras[1][0]}`.toUpperCase();
}

/** Largura da barra do cartão dos 60 dias: a parte das apólices que vencem em até 60 dias, de 0 a 100 (%). */
export function proporcaoAVencer(totalAVencer: number | undefined, totalCadastradas: number | undefined): number {
  if (totalAVencer === undefined || totalCadastradas === undefined || totalCadastradas <= 0) return 0;
  return Math.min(100, Math.max(0, Math.round((totalAVencer / totalCadastradas) * 100)));
}
