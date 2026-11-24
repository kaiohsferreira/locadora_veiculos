import { Link } from 'react-router-dom';
import { Icone } from '@/components/ui';
import { MENSAGENS_RETIRADA as M } from '@/lib/mensagensRetirada';
import { CAMINHOS } from '@/routes/caminhos';

/** Trilha do desenho 16: "Locações > Despacho > Retirada do veículo". Só "Locações" é link. */
export function TrilhaRetirada() {
  return (
    <nav aria-label={M.trilhaRotulo} className="flex items-center gap-1 text-rotulo text-texto-apoio">
      <Link to={CAMINHOS.locacoes} className="transition-colors hover:text-navy-profundo">{M.trilhaLocacoes}</Link>
      <Icone nome="chevron_right" tamanho={16} className="text-contorno" />
      <span>{M.trilhaDespacho}</span>
      <Icone nome="chevron_right" tamanho={16} className="text-contorno" />
      <span aria-current="page" className="font-semibold text-frota-oficina">{M.titulo}</span>
    </nav>
  );
}
