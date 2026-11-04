import { PaginaEsqueleto } from '@/components/PaginaEsqueleto';

/** P6-FE-01 · Catálogo de seminovos. Esqueleto: a tela real será implementada a partir do protótipo. */
export default function CatalogoSeminovosPagina() {
  return (
    <PaginaEsqueleto
      area="publica"
      titulo="Catálogo de seminovos"
      descricao="Catálogo de seminovos, com os filtros."
      codigoTarefa="P6-FE-01"
      prototipo="24_cat_logo_de_seminovos_web_desktop.png"
      versaoMobile={{ codigoTarefa: 'P6-FE-04', prototipo: '23_cat_logo_de_seminovos.png' }}
    />
  );
}
