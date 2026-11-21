import { Icone } from '@/components/ui';
import { CATALOGO_REMOVER_FILTRO } from '@/lib/mensagensCatalogo';
import type { ChaveFiltroCatalogo, EtiquetaFiltro } from './filtrosCatalogo';

export interface EtiquetasFiltrosProps {
  etiquetas: EtiquetaFiltro[];
  aoRemover: (chave: ChaveFiltroCatalogo) => void;
}

/**
 * Etiquetas dos filtros ativos, cada uma com ✕ para tirar só aquele filtro. No celular a linha rola para o lado em vez de
 * quebrar. Sem filtros ativos, não aparece.
 */
export function EtiquetasFiltros({ etiquetas, aoRemover }: EtiquetasFiltrosProps) {
  if (etiquetas.length === 0) return null;

  return (
    <ul className="-mx-4 flex gap-2 overflow-x-auto px-4 pb-1 sm:mx-0 sm:flex-wrap sm:overflow-visible sm:px-0" aria-label="Filtros ativos">
      {etiquetas.map((etiqueta) => (
        <li key={etiqueta.chave} className="shrink-0">
          <button
            type="button"
            onClick={() => aoRemover(etiqueta.chave)}
            className="inline-flex items-center gap-1 rounded-full border border-borda-forte bg-branco py-1 pl-3 pr-2 text-corpo-denso text-navy hover:bg-steel-claro"
            aria-label={`${CATALOGO_REMOVER_FILTRO}: ${etiqueta.rotulo}`}
          >
            {etiqueta.rotulo}
            <Icone nome="close" tamanho={16} />
          </button>
        </li>
      ))}
    </ul>
  );
}
