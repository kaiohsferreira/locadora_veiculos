import { MENSAGEM_PAINEL_PERIODO_GRUPO } from '@/lib/mensagens';
import type { PeriodoVisaoGeral } from '@/types/painel';
import { OPCOES_PERIODO } from '@/features/visao-geral/periodo';

export interface SeletorPeriodoProps {
  periodo: PeriodoVisaoGeral;
  aoMudar: (periodo: PeriodoVisaoGeral) => void;
}

/**
 * Seletor segmentado de período (desenho da tela 39). É um grupo (fieldset com legenda) de botões com estado: o botão
 * escolhido leva `aria-pressed="true"`, e a página anuncia a mudança num texto de leitor de tela.
 */
export function SeletorPeriodo({ periodo, aoMudar }: SeletorPeriodoProps) {
  return (
    <fieldset className="inline-flex">
      <legend className="sr-only">{MENSAGEM_PAINEL_PERIODO_GRUPO}</legend>
      <div className="inline-flex flex-wrap gap-1 rounded-cartao bg-steel-alto p-1 shadow-cartao">
        {OPCOES_PERIODO.map((opcao) => {
          const escolhido = opcao.valor === periodo;
          return (
            <button
              key={opcao.valor}
              type="button"
              aria-pressed={escolhido}
              onClick={() => aoMudar(opcao.valor)}
              className={`rounded-md px-3 py-1.5 text-corpo-denso font-semibold transition-colors focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-orange ${
                escolhido ? 'bg-navy text-branco shadow-cartao' : 'text-texto-suave hover:text-navy'
              }`}
            >
              {opcao.rotulo}
            </button>
          );
        })}
      </div>
    </fieldset>
  );
}
