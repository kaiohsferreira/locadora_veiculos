import { useParams } from 'react-router-dom';
import { PaginaEsqueleto } from '@/components/PaginaEsqueleto';

/** P3-FE-03 · Detalhe do veículo. Esqueleto: a tela real será implementada a partir do protótipo. */
export default function DetalheVeiculoPagina() {
  const { veiculoId } = useParams<'veiculoId'>();

  return (
    <PaginaEsqueleto
      area="publica"
      titulo="Detalhe do veículo"
      descricao="Detalhe do veículo, com ficha técnica e resumo da locação."
      codigoTarefa="P3-FE-03"
      prototipo="26_detalhes_do_ve_culo_jeep_renegade.png"
      parametros={{ veiculoId }}
    />
  );
}
