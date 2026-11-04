import { useParams } from 'react-router-dom';
import { PaginaEsqueleto } from '@/components/PaginaEsqueleto';

/** P6-FE-02 · Proposta de compra. Esqueleto: a tela real será implementada a partir do protótipo. */
export default function PropostaCompraPagina() {
  const { veiculoId } = useParams<'veiculoId'>();

  return (
    <PaginaEsqueleto
      area="publica"
      titulo="Proposta de compra"
      descricao="Proposta de compra, com a simulação de financiamento."
      codigoTarefa="P6-FE-02"
      prototipo="31_proposta_de_compra_jeep_renegade.png"
      parametros={{ veiculoId }}
    />
  );
}
