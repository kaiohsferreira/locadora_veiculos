import type { ContextoDevolucao } from '@/types/devolucao';
import { MENSAGENS_COBRANCAS_EXTRAS as M } from '@/lib/mensagensCobrancasExtras';

export function motivoBloqueioSimulacao(contexto: ContextoDevolucao): string | null {
  const c = contexto.comparacao;
  if (!c) return M.semComparacao;
  if (!c.comparacaoRealizada || c.avariasIndeterminadas.length > 0) return M.legado;
  if (!c.entrada || c.entrada.nivelCombustivel === null || c.saida.nivelCombustivel === null) return M.semEntrada;
  if (c.avariasNovas.some(a => a.valorEstimado === null || !Number.isFinite(a.valorEstimado) || a.valorEstimado < 0)) return M.semEstimativa;
  return null;
}
