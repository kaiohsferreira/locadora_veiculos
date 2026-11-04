import { PaginaEsqueleto } from '@/components/PaginaEsqueleto';

/** P7-FE-01 · Painel financeiro. Esqueleto: a tela real será implementada a partir do protótipo. */
export default function PainelFinanceiroPagina() {
  return (
    <PaginaEsqueleto
      area="back-office"
      titulo="Painel financeiro"
      descricao="Painel financeiro, com as cobranças e a inadimplência."
      codigoTarefa="P7-FE-01"
      prototipo="09_back_office_financeiro_e_cobran_as.png"
    />
  );
}
