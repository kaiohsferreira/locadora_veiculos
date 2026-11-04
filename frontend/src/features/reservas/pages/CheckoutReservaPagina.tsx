import { useParams } from 'react-router-dom';
import { PaginaEsqueleto } from '@/components/PaginaEsqueleto';

/** P3-FE-04 · Checkout da reserva. Esqueleto: a tela real será implementada a partir do protótipo. */
export default function CheckoutReservaPagina() {
  const { veiculoId } = useParams<'veiculoId'>();

  return (
    <PaginaEsqueleto
      area="publica"
      titulo="Checkout da reserva"
      descricao="Checkout da reserva, com extrato, proteções e pagamento."
      codigoTarefa="P3-FE-04"
      prototipo="33_reserva_e_pagamento_web_desktop.png"
      versaoMobile={{ codigoTarefa: 'P3-FE-08', prototipo: '32_reserva_e_pagamento.png' }}
      parametros={{ veiculoId }}
    />
  );
}
