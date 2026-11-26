import { Icone } from '@/components/ui';
import { ROTULO_STATUS_RESERVA, type StatusReserva } from '@/types/enums';

export interface ChipSituacaoReservaProps {
  status: StatusReserva;
  /**
   * cartao: selo da faixa do cartão ativo (pílula no celular, retângulo a partir de md).
   * tabela: chip pequeno da tabela do histórico no computador (cancelada cinza, como antes).
   * celular: chip do cartão do histórico no celular (desenho 28): a cancelada fica vermelha, com ícone.
   */
  variante?: 'cartao' | 'tabela' | 'celular';
}

/** Cor da bolinha por status. Sempre acompanhada do texto do rótulo (nunca só cor). */
const COR_PONTO: Readonly<Record<StatusReserva, string>> = {
  ATIVA: 'bg-status-pendente',
  CONFIRMADA: 'bg-status-sucesso',
  CANCELADA: 'bg-status-neutro',
  CONCLUIDA: 'bg-status-sucesso',
};

/**
 * Chip da reserva cancelada no celular: as cores exatas do desenho 28 (#FFDAD6 de fundo e #93000A de texto), com os
 * tokens que já existem em manutencao (alerta-trilho e alerta-escuro).
 */
const CLASSES_CANCELADA_CELULAR = 'bg-manutencao-alerta-trilho text-manutencao-alerta-escuro';

/** Rótulos vêm de types/enums (Ativa, Confirmada, Cancelada, Concluída), não de texto novo. */
export function ChipSituacaoReserva({ status, variante = 'cartao' }: ChipSituacaoReservaProps) {
  if (variante === 'celular') return <ChipCelular status={status} />;

  const cartao = variante === 'cartao';
  return (
    <span
      className={`inline-flex items-center ${cartao ? 'gap-1.5' : 'gap-1'} ${
        cartao
          ? 'rounded-full bg-steel-linha px-2.5 py-1 text-micro font-semibold text-navy md:rounded md:px-3 md:text-rotulo'
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

function ChipCelular({ status }: { status: StatusReserva }) {
  const cancelada = status === 'CANCELADA';
  return (
    <span
      className={`inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 text-micro font-semibold ${
        cancelada ? CLASSES_CANCELADA_CELULAR : 'bg-steel-suave text-status-neutro'
      }`}
    >
      {cancelada ? (
        <Icone nome="cancel" tamanho={12} />
      ) : (
        <span aria-hidden="true" className={`h-1.5 w-1.5 rounded-full ${COR_PONTO[status]}`} />
      )}
      {ROTULO_STATUS_RESERVA[status]}
    </span>
  );
}
