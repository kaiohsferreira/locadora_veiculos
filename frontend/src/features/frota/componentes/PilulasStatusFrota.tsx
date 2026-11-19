import { formatarNumero, VALOR_AUSENTE } from '@/lib/formatters';
import { MENSAGEM_FROTA_FILTRO_SITUACAO } from '@/lib/mensagens';
import {
  PILULAS_FROTA,
  type SituacaoPilulaFrota,
} from '@/features/frota/listagem/parametrosListagemFrota';

/** Cor da contagem de cada pílula, como no desenho 11. Todos e Locados usam o neutro; as outras têm a cor do status. */
const CHIP_PILULA: Readonly<Record<SituacaoPilulaFrota, string>> = {
  '': 'bg-steel-suave text-texto-apoio',
  DISPONIVEL: 'bg-frota-disponivel-fundo font-bold text-frota-disponivel',
  LOCADO: 'bg-steel-suave text-navy',
  MANUTENCAO: 'bg-frota-oficina-fundo font-bold text-frota-oficina',
  RESERVADO: 'bg-steel-alto font-bold text-navy',
  VENDIDO: 'bg-steel-suave text-texto-apoio',
  BAIXADO: 'bg-steel-suave text-texto-apoio',
};

export interface PilulasStatusFrotaProps {
  /** Pílula escolhida ("" = todos). */
  valorSelecionado: SituacaoPilulaFrota;
  /** Contagem por pílula (chave vazia = todos). Nula enquanto o resumo carrega ou falha: mostra o valor ausente. */
  contagens: Record<SituacaoPilulaFrota, number> | null;
  aoEscolher: (valor: SituacaoPilulaFrota) => void;
}

/** Pílulas de situação com a contagem. A escolhida filtra a tabela; o botão marcado usa `aria-pressed`. */
export function PilulasStatusFrota({ valorSelecionado, contagens, aoEscolher }: PilulasStatusFrotaProps) {
  return (
    <fieldset className="flex min-w-0 max-w-full items-center gap-1.5 self-start overflow-x-auto rounded bg-steel-fundo p-1">
      <legend className="sr-only">{MENSAGEM_FROTA_FILTRO_SITUACAO}</legend>
      {PILULAS_FROTA.map((pilula) => {
        const selecionada = pilula.valor === valorSelecionado;
        const contagem = contagens === null ? VALOR_AUSENTE : formatarNumero(contagens[pilula.valor]);
        return (
          <button
            key={pilula.valor === '' ? 'todos' : pilula.valor}
            type="button"
            aria-pressed={selecionada}
            onClick={() => aoEscolher(pilula.valor)}
            className={`flex shrink-0 items-center gap-1.5 rounded px-4 py-1.5 text-corpo-denso transition-colors ${
              selecionada
                ? 'bg-branco font-semibold text-texto-desenho shadow-sm'
                : 'font-medium text-texto-apoio hover:bg-steel-suave hover:text-texto-desenho'
            }`}
          >
            <span>{pilula.rotulo}</span>
            <span className={`rounded-full px-1.5 py-0.5 text-micro ${CHIP_PILULA[pilula.valor]}`}>{contagem}</span>
          </button>
        );
      })}
    </fieldset>
  );
}
