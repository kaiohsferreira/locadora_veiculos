import { useParams } from 'react-router-dom';
import { PaginaEsqueleto } from '@/components/PaginaEsqueleto';

/** P4-FE-01 · Retirada do veículo. Esqueleto: a tela real será implementada a partir do protótipo. */
export default function RetiradaVeiculoPagina() {
  const { reservaId } = useParams<'reservaId'>();

  return (
    <PaginaEsqueleto
      area="back-office"
      titulo="Retirada do veículo"
      descricao="Retirada do veículo, com a conferência documental."
      codigoTarefa="P4-FE-01"
      prototipo="16_back_office_retirada_do_ve_culo_check_in.png"
      parametros={{ reservaId }}
    />
  );
}
