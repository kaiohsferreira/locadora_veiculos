import { useParams } from 'react-router-dom';
import { PaginaEsqueleto } from '@/components/PaginaEsqueleto';

/** P3-FE-05 · Confirmação da reserva. Esqueleto: a tela real será implementada a partir do protótipo. */
export default function ConfirmacaoReservaPagina() {
  const { reservaId } = useParams<'reservaId'>();

  return (
    <PaginaEsqueleto
      area="publica"
      titulo="Confirmação da reserva"
      descricao="Confirmação da reserva, com código e voucher."
      codigoTarefa="P3-FE-05"
      prototipo="25_confirma_o_de_reserva.png"
      parametros={{ reservaId }}
    />
  );
}
