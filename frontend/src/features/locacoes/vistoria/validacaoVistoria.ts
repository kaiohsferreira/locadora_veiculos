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

/** Usar após validação: preserva zero e normaliza decimal pt-BR sem arredondar. */
export function converterVistoriaEntrada(campos: CamposVistoriaEntrada): RegistrarVistoriaEntrada {
  return {
    kmRegistrado: Number(campos.kmRegistrado.trim()),
    nivelCombustivel: Number(campos.nivelCombustivel.trim().replace(',', '.')),
    observacoes: campos.observacoes.trim() || null,
  };
}

export function lerIdLocacao(valor: string | undefined): number | null {
  if (!valor || !/^[1-9]\d*$/.test(valor)) return null;
  const id = Number(valor);
  return Number.isSafeInteger(id) && id <= 2_147_483_647 ? id : null;
}
