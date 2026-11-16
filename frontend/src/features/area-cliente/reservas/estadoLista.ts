import type { RespostaReservas } from '@/types/reservas';

/** Estado de uma lista de reservas (uma aba), como a página entrega aos painéis. */
export interface EstadoListaReservas {
  /** Última resposta conhecida (pode ser de uma consulta anterior da mesma aba). */
  dados: RespostaReservas | undefined;
  /** Não há nenhum dado ainda: mostra o esqueleto. */
  carregando: boolean;
  /** Erro da última consulta (ErroApi ou outro); undefined/null quando não há erro. */
  erro: unknown;
  /** Consulta em andamento (nova tentativa, troca de página ou de busca). */
  atualizando: boolean;
  aoTentarNovamente: () => void;
}
