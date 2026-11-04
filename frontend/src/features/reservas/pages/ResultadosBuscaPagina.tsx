import { PaginaEsqueleto } from '@/components/PaginaEsqueleto';

/** P3-FE-02 · Resultados da busca. Esqueleto: a tela real será implementada a partir do protótipo. */
export default function ResultadosBuscaPagina() {
  return (
    <PaginaEsqueleto
      area="publica"
      titulo="Resultados da busca"
      descricao="Resultados da busca, com os filtros laterais."
      codigoTarefa="P3-FE-02"
      prototipo="04_aluguel_de_carros_web_desktop.png"
      versaoMobile={{ codigoTarefa: 'P3-FE-07', prototipo: '02_aluguel_de_carros.png' }}
    />
  );
}
