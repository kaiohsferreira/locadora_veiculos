import { Icone } from '@/components/ui';
import { MENSAGEM_PAGAMENTOS_COMPENSADO } from '@/lib/mensagens';
import { ROTULO_STATUS_PAGAMENTO, type StatusPagamento } from '@/types/enums';

export interface EtiquetaSituacaoPagamentoProps {
  situacao: StatusPagamento;
}

const CLASSES_BASE = 'inline-flex items-center gap-1.5 whitespace-nowrap rounded-sm px-2.5 py-1 text-rotulo';

/**
 * Etiqueta de situação da tela 30 (peça própria, igual ao desenho; a Etiqueta compartilhada não muda).
 * Pago: fundo neutro, check verde e "Compensado". Pendente: ponto pulsando (desenho). Atrasado e cancelado: os tons de
 * status que o projeto já tem.
 */
export function EtiquetaSituacaoPagamento({ situacao }: EtiquetaSituacaoPagamentoProps) {
  switch (situacao) {
    case 'PAGO':
      return (
        <span className={`${CLASSES_BASE} bg-steel-alto font-semibold text-pagamento-texto-compensado`}>
          <Icone nome="check_circle" tamanho={14} className="text-pagamento-sucesso" />
          {MENSAGEM_PAGAMENTOS_COMPENSADO}
        </span>
      );
    case 'PENDENTE':
      return (
        <span className={`${CLASSES_BASE} bg-frota-oficina-fundo font-bold text-pagamento-texto-pendente`}>
          <span className="h-1.5 w-1.5 rounded-full bg-frota-oficina motion-safe:animate-pulse" aria-hidden="true" />
          {ROTULO_STATUS_PAGAMENTO.PENDENTE}
        </span>
      );
    case 'ATRASADO':
      return (
        <span className={`${CLASSES_BASE} bg-status-perigo-fundo font-bold text-status-perigo`}>
          <span className="h-1.5 w-1.5 rounded-full bg-status-perigo motion-safe:animate-pulse" aria-hidden="true" />
          {ROTULO_STATUS_PAGAMENTO.ATRASADO}
        </span>
      );
    case 'CANCELADO':
      return (
        <span className={`${CLASSES_BASE} bg-status-neutro-fundo font-semibold text-status-neutro`}>
          <span className="h-1.5 w-1.5 rounded-full bg-status-neutro" aria-hidden="true" />
          {ROTULO_STATUS_PAGAMENTO.CANCELADO}
        </span>
      );
  }
}
