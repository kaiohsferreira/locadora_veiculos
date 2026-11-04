import { useParams } from 'react-router-dom';
import { PaginaEsqueleto } from '@/components/PaginaEsqueleto';

/** P4-FE-03 · Contrato digital. Esqueleto: a tela real será implementada a partir do protótipo. */
export default function ContratoDigitalPagina() {
  const { locacaoId } = useParams<'locacaoId'>();

  return (
    <PaginaEsqueleto
      area="back-office"
      titulo="Contrato digital"
      descricao="Emissão e assinatura do contrato digital."
      codigoTarefa="P4-FE-03"
      prototipo="08_back_office_emiss_o_de_contrato_digital.png"
      parametros={{ locacaoId }}
    />
  );
}
