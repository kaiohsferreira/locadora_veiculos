import type { ReactNode } from 'react';
import { Esqueleto, Icone } from '@/components/ui';
import { VALOR_AUSENTE, formatarNumero } from '@/lib/formatters';
import {
  MENSAGEM_FROTA_CARREGANDO_RESUMO,
  MENSAGEM_FROTA_RESUMO_CONTRATOS_VIGENTES,
  MENSAGEM_FROTA_RESUMO_DISPONIVEIS,
  MENSAGEM_FROTA_RESUMO_EM_MANUTENCAO,
  MENSAGEM_FROTA_RESUMO_LOCADOS,
  MENSAGEM_FROTA_RESUMO_TAXA_OCUPACAO,
} from '@/lib/mensagens';
import {
  formatarTaxaOcupacaoFrota,
  proporcaoAnelFrota,
  textoEncerramHojeFrota,
  type ResumoCartoesFrota,
} from '@/features/frota/cartoes/cartoesResumoFrota';

/** Trajeto do anel (círculo de raio 15.9155 em um quadro de 36, como no desenho 12). */
const TRAJETO_ANEL = 'M18 2.0845 a 15.9155 15.9155 0 0 1 0 31.831 a 15.9155 15.9155 0 0 1 0 -31.831';

export interface CartoesResumoFrotaProps {
  /** Resumo ainda carregando: aparecem os esqueletos. */
  carregando: boolean;
  /** Números dos cartões. Nulo (erro do resumo): os números saem como valor ausente. */
  resumo: ResumoCartoesFrota | null;
}

/**
 * Quatro cartões do desenho 12: disponíveis, locados (com "N encerram hoje"), em manutenção e taxa de ocupação (com o
 * anel e a barra preenchidos na proporção, até 100%). Sem variação, sem "Meta" e sem "Superada": ver o contrato da tela 12.
 */
export function CartoesResumoFrota({ carregando, resumo }: CartoesResumoFrotaProps) {
  if (carregando) {
    return (
      <ul className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4" aria-label={MENSAGEM_FROTA_CARREGANDO_RESUMO}>
        {[0, 1, 2, 3].map((indice) => (
          <li key={indice} aria-hidden="true">
            <section className="flex h-full min-w-0 flex-col justify-between overflow-hidden rounded-cartao bg-branco p-4 shadow-sm">
              <Esqueleto className="h-4 w-24" />
              <div className="mt-3 flex flex-col gap-2">
                <Esqueleto className="h-8 w-20" />
                <Esqueleto className="h-3 w-28" />
              </div>
            </section>
          </li>
        ))}
      </ul>
    );
  }

  const valorNumero = (numero: number | undefined): string =>
    resumo === null || numero === undefined ? VALOR_AUSENTE : formatarNumero(numero);
  const taxa = resumo === null ? 0 : proporcaoAnelFrota(resumo.taxaOcupacaoPercentual);

  return (
    <ul className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
      <CartaoResumo
        chave="disponiveis"
        titulo={MENSAGEM_FROTA_RESUMO_DISPONIVEIS}
        valor={valorNumero(resumo?.disponiveis)}
        direita={<IconeCartao nome="directions_car" classes="bg-steel-alto text-navy" />}
      />
      <CartaoResumo
        chave="locados"
        titulo={MENSAGEM_FROTA_RESUMO_LOCADOS}
        valor={valorNumero(resumo?.locados)}
        direita={<IconeCartao nome="key" classes="bg-steel-alto text-navy" />}
        rodape={
          <div className="flex flex-wrap items-baseline justify-between gap-x-2 gap-y-0.5 text-apoio">
            <span className="text-contorno">{MENSAGEM_FROTA_RESUMO_CONTRATOS_VIGENTES}</span>
            <span className="whitespace-nowrap text-rotulo text-texto-desenho">
              {resumo === null ? VALOR_AUSENTE : textoEncerramHojeFrota(resumo.devolucoesPrevistasHoje)}
            </span>
          </div>
        }
      />
      <CartaoResumo
        chave="manutencao"
        titulo={MENSAGEM_FROTA_RESUMO_EM_MANUTENCAO}
        valor={valorNumero(resumo?.emManutencao)}
        direita={<IconeCartao nome="build" classes="bg-steel-alto text-frota-oficina" />}
      />
      <CartaoResumo
        chave="taxa"
        titulo={MENSAGEM_FROTA_RESUMO_TAXA_OCUPACAO}
        valor={resumo === null ? VALOR_AUSENTE : formatarTaxaOcupacaoFrota(resumo.taxaOcupacaoPercentual)}
        direita={<AnelTaxa proporcao={taxa} />}
        rodape={
          <div className="flex flex-col gap-1">
            <div aria-hidden="true" className="h-1.5 w-full overflow-hidden rounded-full bg-steel-suave">
              <div className="h-1.5 rounded-full bg-orange-acao" style={{ width: `${taxa}%` }} />
            </div>
          </div>
        }
      />
    </ul>
  );
}

interface CartaoResumoProps {
  chave: string;
  titulo: string;
  valor: string;
  /** Lado direito do topo: quadrado com ícone (cartões 1 a 3) ou anel (taxa). */
  direita: ReactNode;
  /** Linha de baixo do cartão (margem e respiro do desenho). Sem ela, o cartão fica só com o topo. */
  rodape?: ReactNode;
}

/** Um cartão: título em caixa alta, número grande, o quadrado ou o anel, e a linha de baixo quando há. */
function CartaoResumo({ chave, titulo, valor, direita, rodape }: CartaoResumoProps) {
  const idTitulo = `cartao-resumo-frota-${chave}`;
  return (
    <li className="min-w-0">
      <section
        aria-labelledby={idTitulo}
        className="group relative flex h-full min-w-0 flex-col justify-between overflow-hidden rounded-cartao bg-branco p-4 shadow-sm"
      >
        <span
          aria-hidden="true"
          className="pointer-events-none absolute right-0 top-0 h-24 w-24 rounded-bl-xl bg-steel-fundo/50 transition-transform group-hover:scale-110"
        />
        <div className="relative z-10 flex items-start justify-between gap-2">
          <div className="flex min-w-0 flex-col">
            <h2 id={idTitulo} className="text-micro font-semibold uppercase tracking-wider text-contorno">
              {titulo}
            </h2>
            <p className="mt-1 text-titulo-hero font-bold tracking-tight text-texto-desenho">{valor}</p>
          </div>
          {direita}
        </div>
        {rodape !== undefined && <div className="relative z-10 mt-4 pt-1">{rodape}</div>}
      </section>
    </li>
  );
}

function IconeCartao({ nome, classes }: { nome: string; classes: string }) {
  return (
    <span aria-hidden="true" className={`flex h-10 w-10 shrink-0 items-center justify-center rounded ${classes}`}>
      <Icone nome={nome} tamanho={22} />
    </span>
  );
}

/** Anel da taxa: o trajeto em cinza-azulado e o arco laranja na proporção (0 a 100), como no desenho. */
function AnelTaxa({ proporcao }: { proporcao: number }) {
  return (
    <span aria-hidden="true" className="relative flex h-10 w-10 shrink-0 items-center justify-center">
      <svg className="h-10 w-10 -rotate-90" viewBox="0 0 36 36">
        <path className="text-steel-suave" d={TRAJETO_ANEL} fill="none" stroke="currentColor" strokeWidth="3.5" />
        <path
          className="text-orange-acao"
          d={TRAJETO_ANEL}
          fill="none"
          stroke="currentColor"
          strokeDasharray={`${proporcao}, 100`}
          strokeLinecap={proporcao > 0 ? 'round' : 'butt'}
          strokeWidth="3.5"
        />
      </svg>
      <Icone nome="pie_chart" tamanho={16} className="absolute text-texto-apoio" />
    </span>
  );
}
