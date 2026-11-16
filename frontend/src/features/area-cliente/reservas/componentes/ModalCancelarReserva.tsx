import { useEffect, useId, useRef } from 'react';
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
 * A frase de descrição vai no corpo do Modal (não no cabeçalho) e fica ligada ao diálogo por aria-describedby.
 */
export function ModalCancelarReserva({ reserva, pendente, aoConfirmar, aoFechar }: ModalCancelarReservaProps) {
  const aberto = reserva !== null;
  const idDescricao = useId();
  const refDescricao = useRef<HTMLParagraphElement>(null);

  useEffect(() => {
    // O Modal só liga aria-describedby quando recebe `descricao`; como a frase está no corpo, liga aqui no <dialog>.
    const dialogo = refDescricao.current?.closest('dialog');
    dialogo?.setAttribute('aria-describedby', idDescricao);
  }, [aberto, idDescricao]);

  return (
    <Modal
      aberto={aberto}
      aoFechar={() => {
        if (!pendente) aoFechar();
      }}
      titulo={reserva ? mensagemReservasCancelarTitulo(reserva.codigo) : ''}
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
      <p ref={refDescricao} id={idDescricao} className="text-texto-suave">
        {MENSAGEM_RESERVAS_CANCELAR_DESCRICAO}
      </p>
    </Modal>
  );
}
