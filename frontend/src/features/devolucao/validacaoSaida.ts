import { z } from 'zod';
import { MENSAGENS_DEVOLUCAO as M } from '@/lib/mensagensDevolucao';
import type { RegistrarSaida } from '@/types/devolucao';

const decimal = (texto: string, maximo: number): boolean => /^\d+(?:[,.]\d{1,2})?$/.test(texto.trim()) && Number(texto.trim().replace(',', '.')) <= maximo;
export function criarEsquemaSaida(kmMinimo: number, idsPermitidos: readonly number[]) {
  return z.object({
    kmRegistrado: z.string().trim().refine(v => /^\d+$/.test(v) && Number(v) <= 2_147_483_647 && Number(v) >= kmMinimo, M.km),
    nivelCombustivel: z.string().trim().refine(v => decimal(v, 1), M.combustivel),
    observacoes: z.string().refine(v => !v.includes('\0'), M.observacoes),
    idsAvariasEntrada: z.array(z.number().int().positive()).refine(ids => new Set(ids).size === ids.length && ids.every(id => idsPermitidos.includes(id)), M.selecao),
    novasAvarias: z.array(z.object({
      descricao: z.string().trim().min(1, M.descricaoInvalida).max(200, M.descricaoInvalida).refine(v => !v.includes('\0'), M.descricaoInvalida),
      gravidade: z.enum(['LEVE', 'MEDIA', 'GRAVE']),
      valorEstimado: z.string().trim().refine(v => v !== '', M.valor).refine(v => v === '' || decimal(v, 99_999_999.99), M.valorFaixa),
    })),
    comparacaoConferida: z.boolean().refine(v => v, M.confirmar),
  });
}
export type CamposSaida = z.infer<ReturnType<typeof criarEsquemaSaida>>;
export function converterSaida(campos: CamposSaida): RegistrarSaida {
  return {
    kmRegistrado: Number(campos.kmRegistrado),
    nivelCombustivel: Number(campos.nivelCombustivel.replace(',', '.')),
    observacoes: campos.observacoes.trim() || null,
    idsAvariasEntrada: [...campos.idsAvariasEntrada],
    novasAvarias: campos.novasAvarias.map(a => ({ descricao: a.descricao.trim(), gravidade: a.gravidade,
      valorEstimado: Number(a.valorEstimado.replace(',', '.')) })),
  };
}
