import { useParams } from 'react-router-dom';
import { PaginaEsqueleto } from '@/components/PaginaEsqueleto';

/** P9-FE-02 · Detalhe do sinistro. Esqueleto: a tela real será implementada a partir do protótipo. */
export default function DetalheSinistroPagina() {
  const { sinistroId } = useParams<'sinistroId'>();

  return (
    <PaginaEsqueleto
      area="back-office"
      titulo="Detalhe do sinistro"
      descricao="Detalhe do sinistro, com a linha do tempo do processo."
      codigoTarefa="P9-FE-02"
      prototipo="07_back_office_detalhe_do_sinistro.png"
      parametros={{ sinistroId }}
    />
  );
}
