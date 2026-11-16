import { ROTULO_STATUS_RESERVA, type StatusReserva } from '@/types/enums';

export interface ChipSituacaoReservaProps {
  status: StatusReserva;
  /** cartao: selo da faixa do cartão. tabela: chip pequeno da tabela do histórico. */
  variante?: 'cartao' | 'tabela';
}

/** Cor da bolinha por status. Sempre acompanhada do texto do rótulo (nunca só cor). */
const COR_PONTO: Readonly<Record<StatusReserva, string>> = {
  ATIVA: 'bg-status-pendente',
  CONFIRMADA: 'bg-status-sucesso',
  CANCELADA: 'bg-status-neutro',
  CONCLUIDA: 'bg-status-sucesso',
};

/** Rótulos vêm de types/enums (Ativa, Confirmada, Cancelada, Concluída), não de texto novo. */
export function ChipSituacaoReserva({ status, variante = 'cartao' }: ChipSituacaoReservaProps) {
  const cartao = variante === 'cartao';
  return (
    <span
      className={`inline-flex items-center ${cartao ? 'gap-1.5' : 'gap-1'} ${
        cartao
          ? 'rounded bg-steel-linha px-3 py-1 text-rotulo font-semibold text-navy'
          : 'rounded-full bg-steel-suave px-2.5 py-0.5 text-micro font-semibold text-status-neutro'
      }`}
    >
      <span
        aria-hidden="true"
        className={`rounded-full ${cartao ? 'h-2 w-2' : 'h-1.5 w-1.5'} ${COR_PONTO[status]}`}
      />
      {ROTULO_STATUS_RESERVA[status]}
    </span>
  );
}
