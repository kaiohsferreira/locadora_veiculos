import { Botao, Modal } from '@/components/ui';
import {
  mensagemReservasCancelarTitulo,
  MENSAGEM_RESERVAS_BOTAO_CANCELAR,
  MENSAGEM_RESERVAS_CANCELAR_DESCRICAO,
  MENSAGEM_RESERVAS_MANTER,
} from '@/lib/mensagens';
import type { ReservaCliente } from '@/types/reservas';

export interface ModalCancelarReservaProps {
  /** Reserva a cancelar; null deixa o modal fechado. */
  reserva: ReservaCliente | null;
  /** Cancelamento em andamento: bloqueia fechar e confirmar de novo. */
  pendente: boolean;
  aoConfirmar: () => void;
  aoFechar: () => void;
}

/**
 * Confirmação do cancelamento. O foco entra no primeiro botão do conteúdo e volta ao botão que abriu, pelo Modal.
 * Enquanto o cancelamento roda, não fecha (nem com Esc nem clicando fora).
 */
export function ModalCancelarReserva({ reserva, pendente, aoConfirmar, aoFechar }: ModalCancelarReservaProps) {
  return (
    <Modal
      aberto={reserva !== null}
      aoFechar={() => {
        if (!pendente) aoFechar();
      }}
      titulo={reserva ? mensagemReservasCancelarTitulo(reserva.codigo) : ''}
      descricao={MENSAGEM_RESERVAS_CANCELAR_DESCRICAO}
      tamanho="pequeno"
      fecharAoClicarFora={!pendente}
      rodape={
        <>
          <Botao variante="contorno" onClick={aoFechar} disabled={pendente}>
            {MENSAGEM_RESERVAS_MANTER}
          </Botao>
          <Botao variante="perigo" onClick={aoConfirmar} carregando={pendente}>
            {MENSAGEM_RESERVAS_BOTAO_CANCELAR}
          </Botao>
        </>
      }
    >
      {null}
    </Modal>
  );
}
