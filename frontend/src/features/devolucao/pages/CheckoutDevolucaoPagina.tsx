import { useParams } from 'react-router-dom';
import { PaginaEsqueleto } from '@/components/PaginaEsqueleto';

/** P5-FE-01 · Check-out da devolução. Esqueleto: a tela real será implementada a partir do protótipo. */
export default function CheckoutDevolucaoPagina() {
  const { locacaoId } = useParams<'locacaoId'>();

  return (
    <PaginaEsqueleto
      area="back-office"
      titulo="Check-out da devolução"
      descricao="Check-out, com a comparação entre as vistorias e a apuração das cobranças."
      codigoTarefa="P5-FE-01"
      prototipo="06_back_office_check_out_e_fechamento_de_devolu_o.png"
      parametros={{ locacaoId }}
    />
  );
}
