import { PaginaEsqueleto } from '@/components/PaginaEsqueleto';

/** P2-FE-01 · Acesso ao portal. Esqueleto: a tela real será implementada a partir do protótipo. */
export default function AcessoPortalPagina() {
  return (
    <PaginaEsqueleto
      area="publica"
      titulo="Acesso ao portal"
      descricao="Acesso ao portal, com login do cliente e da equipe."
      codigoTarefa="P2-FE-01"
      prototipo="01_acesso_ao_portal_login.png"
    />
  );
}
