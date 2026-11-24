import { MENSAGENS_RETIRADA as M } from '@/lib/mensagensRetirada';
import type { PassoRetirada } from '../regrasRetirada';
import { CartaoRetirada } from './CartaoRetirada';

export interface PassoAPassoRetiradaProps { passos: PassoRetirada[]; }

/**
 * Passo a passo de quatro etapas do desenho 16. Não tem link em nenhuma etapa. A etapa ativa leva o selo "Em andamento"
 * e aria-current; nos outros estados nenhuma etapa fica ativa.
 */
export function PassoAPassoRetirada({ passos }: PassoAPassoRetiradaProps) {
  return (
    <CartaoRetirada semEspacamento className="p-4 md:p-6">
      <ol className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-4">
        {passos.map((passo) => (
          <li
            key={passo.numero}
            aria-current={passo.ativa ? 'step' : undefined}
            className={`flex min-w-0 items-center gap-4 rounded p-2 transition-colors ${passo.ativa ? 'bg-steel-alto/60' : 'hover:bg-steel-fundo'}`}
          >
            <span
              className={`flex h-10 w-10 shrink-0 items-center justify-center rounded text-titulo-md ${passo.ativa ? 'bg-orange-acao font-bold text-branco shadow-md' : 'bg-steel-alto font-semibold text-texto-apoio'}`}
            >
              {passo.numero}
            </span>
            <span className="flex min-w-0 flex-col">
              <span className="flex items-center gap-1">
                <span className={`text-micro uppercase tracking-wider ${passo.ativa ? 'font-bold text-frota-oficina' : 'font-medium text-contorno'}`}>
                  {M.etapa(passo.numero)}
                </span>
                {passo.ativa && (
                  <span className="inline-flex items-center gap-1 rounded-sm bg-orange-acao/20 px-1.5 py-0.5 text-chip-frota font-bold uppercase tracking-wider text-financeiro-acao-hover">
                    <span className="h-1.5 w-1.5 animate-ping rounded-full bg-orange-acao motion-reduce:animate-none" aria-hidden="true" />
                    {M.emAndamento}
                  </span>
                )}
              </span>
              <span className={`truncate text-corpo-denso ${passo.ativa ? 'font-bold text-texto-desenho' : 'font-medium text-texto-apoio'}`}>{passo.nome}</span>
            </span>
          </li>
        ))}
      </ol>
    </CartaoRetirada>
  );
}
