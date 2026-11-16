import { formatarData, formatarHora } from '@/lib/formatters';
import { mensagemReservasAtualizadoEm, mensagemReservasAtualizadoHoje } from '@/lib/mensagens';

/**
 * Texto de "Atualizado" a partir do `dataUpdatedAt` do TanStack Query (ms). Mesmo dia de `agora` mostra
 * "hoje às HH:mm"; outro dia mostra a data. Zero (nunca carregou) devolve null: a faixa some.
 */
export function descreverAtualizacao(dataUpdatedAt: number, agora: Date): string | null {
  if (!dataUpdatedAt) return null;
  const momento = new Date(dataUpdatedAt);
  const hora = formatarHora(momento);
  const mesmoDia =
    momento.getFullYear() === agora.getFullYear() &&
    momento.getMonth() === agora.getMonth() &&
    momento.getDate() === agora.getDate();
  return mesmoDia ? mensagemReservasAtualizadoHoje(hora) : mensagemReservasAtualizadoEm(formatarData(momento), hora);
}
