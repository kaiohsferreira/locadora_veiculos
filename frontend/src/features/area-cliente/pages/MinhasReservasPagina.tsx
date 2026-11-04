import { PaginaEsqueleto } from '@/components/PaginaEsqueleto';

/** T-FE-03 · Minhas reservas. Esqueleto: a tela real será implementada a partir do protótipo. */
export default function MinhasReservasPagina() {
  return (
    <PaginaEsqueleto
      area="publica"
      titulo="Minhas reservas"
      descricao="Minhas reservas, com as ativas e o histórico."
      codigoTarefa="T-FE-03"
      prototipo="29_minhas_reservas_web_desktop.png"
      versaoMobile={{ codigoTarefa: 'T-FE-04', prototipo: '28_minhas_reservas.png' }}
    />
  );
}
