import type { StatusReserva } from '@/types/enums';

const REGEX_DATA_ISO = /^(\d{4})-(\d{2})-(\d{2})$/;
const MILISSEGUNDOS_POR_DIA = 86_400_000;

/**
 * "AAAA-MM-DD" → número de dias desde 1970-01-01, contado em UTC (sem fuso, então não "volta um dia").
 * Devolve null para texto fora do formato ou data que não existe (ex.: 30 de fevereiro).
 */
function diaDoCalendario(valor: string): number | null {
  const partes = REGEX_DATA_ISO.exec(valor);
  if (!partes) return null;
  const ano = Number(partes[1]);
  const mes = Number(partes[2]);
  const dia = Number(partes[3]);
  const instante = Date.UTC(ano, mes - 1, dia);
  const data = new Date(instante);
  if (data.getUTCFullYear() !== ano || data.getUTCMonth() !== mes - 1 || data.getUTCDate() !== dia) return null;
  return Math.round(instante / MILISSEGUNDOS_POR_DIA);
}

/**
 * Diárias entre a retirada e a devolução (diferença de dias do calendário). Mesma data, data invertida ou
 * texto inválido dão 0.
 */
export function calcularDiarias(inicioIso: string, fimIso: string): number {
  const inicio = diaDoCalendario(inicioIso);
  const fim = diaDoCalendario(fimIso);
  if (inicio === null || fim === null) return 0;
  return Math.max(0, fim - inicio);
}

/** Valor de uma diária (valor estimado ÷ diárias), com centavos. Nulo quando não há valor ou diárias. */
export function valorPorDiaria(valorEstimado: number | null, diarias: number): number | null {
  if (valorEstimado === null || !Number.isFinite(valorEstimado) || diarias <= 0) return null;
  return Math.round((valorEstimado / diarias) * 100) / 100;
}

/** Só ATIVA e CONFIRMADA podem ser canceladas pelo cliente (CONCLUIDA responde 409; CANCELADA já está cancelada). */
export function podeCancelarReserva(status: StatusReserva): boolean {
  return status === 'ATIVA' || status === 'CONFIRMADA';
}
