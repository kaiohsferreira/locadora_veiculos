import { useParams } from 'react-router-dom';
import { PaginaEsqueleto } from '@/components/PaginaEsqueleto';

/** P4-FE-02 · Vistoria e inspeção de avarias. Esqueleto: a tela real será implementada a partir do protótipo. */
export default function VistoriaAvariasPagina() {
  const { locacaoId } = useParams<'locacaoId'>();

  return (
    <PaginaEsqueleto
      area="back-office"
      titulo="Vistoria e inspeção de avarias"
      descricao="Vistoria e inspeção de avarias, com o mapeamento por ângulo e as fotos."
      codigoTarefa="P4-FE-02"
      prototipo="19_back_office_vistoria_e_inspe_o_de_avarias.png"
      parametros={{ locacaoId }}
    />
  );
}
