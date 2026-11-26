import type { GravidadeAvaria } from '@/types/enums';

/** Aparência do selo de gravidade: cores por token e nome do ícone (Material Symbols). */
export interface EstiloSeloGravidade {
  /** Classes de fundo e texto, só com tokens do tailwind.config.js. */
  cores: string;
  /** Ícone do selo. Grave ainda não tem ícone aprovado: usa "error" (provisório). */
  icone: string;
}

/**
 * Selo único de gravidade das telas 06 (devolução) e 19 (vistoria). Leve e Média usam os tokens avaria.* (desenho 19);
 * Grave usa os tokens manutencao.alerta-* que já existem no tema. Valores aprovados pelo Kaio em 26/11/2026 (N10 e N14).
 */
const ESTILO_POR_GRAVIDADE: Readonly<Record<GravidadeAvaria, EstiloSeloGravidade>> = {
  LEVE: { cores: 'bg-avaria-leve-fundo text-avaria-leve-texto', icone: 'info' },
  MEDIA: { cores: 'bg-avaria-media-fundo text-avaria-media-texto', icone: 'report_problem' },
  GRAVE: { cores: 'bg-manutencao-alerta-trilho text-manutencao-alerta-escuro', icone: 'error' },
};

/** Devolve as classes e o ícone do selo para a gravidade informada. */
export function estiloSeloGravidade(gravidade: GravidadeAvaria): EstiloSeloGravidade {
  return ESTILO_POR_GRAVIDADE[gravidade];
}
