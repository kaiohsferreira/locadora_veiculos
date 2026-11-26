import { z } from 'zod';
import { MENSAGENS_VISTORIA } from '@/lib/mensagensVistoria';
import type { RegistrarVistoriaEntrada } from '@/types/vistorias';

export const esquemaVistoriaEntrada = z.object({
  kmRegistrado: z.string().trim().refine(
    (valor) => /^\d+$/.test(valor) && Number(valor) <= 2_147_483_647,
    MENSAGENS_VISTORIA.kmInvalido,
  ),
  nivelCombustivel: z.string().trim().refine(
    (valor) => /^\d+(?:[,.]\d{1,2})?$/.test(valor) && Number(valor.replace(',', '.')) <= 1,
    MENSAGENS_VISTORIA.combustivelInvalido,
  ),
  observacoes: z.string().refine((valor) => !valor.includes('\0'), MENSAGENS_VISTORIA.observacoesInvalidas),
});

export type CamposVistoriaEntrada = z.infer<typeof esquemaVistoriaEntrada>;

/**
 * Usar após validação: preserva zero e normaliza decimal pt-BR. O combustível vai com 2 casas (decisão N1, 26/11/2026):
 * o servidor guarda o nível de 0 a 1 com 2 casas; o medidor em oitavos é só visual.
 */
export function converterVistoriaEntrada(campos: CamposVistoriaEntrada): RegistrarVistoriaEntrada {
  return {
    kmRegistrado: Number(campos.kmRegistrado.trim()),
    nivelCombustivel: Math.round(Number(campos.nivelCombustivel.trim().replace(',', '.')) * 100) / 100,
    observacoes: campos.observacoes.trim() || null,
  };
}

/** Nível digitado como número de 0 a 1 (vírgula ou ponto). Vazio ou inválido vira nulo: o medidor não aparece. */
export function nivelDigitado(texto: string): number | null {
  const limpo = texto.trim();
  if (!/^\d+(?:[,.]\d{1,2})?$/.test(limpo)) return null;
  const numero = Number(limpo.replace(',', '.'));
  return numero >= 0 && numero <= 1 ? numero : null;
}

/** Quilometragem digitada como inteiro. Vazio ou inválido vira nulo. */
export function kmDigitado(texto: string): number | null {
  const limpo = texto.trim();
  return /^\d+$/.test(limpo) ? Number(limpo) : null;
}

export function lerIdLocacao(valor: string | undefined): number | null {
  if (!valor || !/^[1-9]\d*$/.test(valor)) return null;
  const id = Number(valor);
  return Number.isSafeInteger(id) && id <= 2_147_483_647 ? id : null;
}
