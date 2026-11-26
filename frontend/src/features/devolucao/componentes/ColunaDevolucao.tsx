import { Icone, SeloGravidade } from '@/components/ui';
import { formatarMoeda, formatarNumero } from '@/lib/formatters';
import { MENSAGENS_DEVOLUCAO as M } from '@/lib/mensagensDevolucao';
import type { AvariaComparacao, VistoriaDevolucao } from '@/types/devolucao';
import { numeroNovaAvaria } from '../regras/regrasDevolucao';
import { BlocoCombustivel } from './BlocoCombustivel';
import { CabecalhoColuna } from './CabecalhoColuna';

/** Divergência de combustível (só com a memória da simulação): oitavos faltantes e o valor da reposição. */
export interface DivergenciaCombustivel {
  oitavos: number;
  reposicaoStr: string;
}

export interface ColunaDevolucaoProps {
  /** Vistoria de saída: a devolução do veículo (coluna da direita do desenho 06). */
  saida: VistoriaDevolucao;
  /** Quantidade de avarias da entrada: a numeração das novas continua depois dela (#4, #5; N10, item 5). */
  quantidadeAvariasEntrada: number;
  /** Novas avarias da devolução. */
  novas: readonly AvariaComparacao[];
  /** Avarias que a comparação não conseguiu classificar (registro legado). */
  indeterminadas: readonly AvariaComparacao[];
  comparacaoRealizada: boolean;
  /** Quilômetros rodados (saída menos entrada); nulo sem vistoria de entrada. */
  rodados: number | null;
  /** Franquia contratada no texto da simulação; nulo quando a simulação não está disponível. */
  franquia: string | null;
  /** Valor da quilometragem excedente da simulação; nulo quando a simulação não está disponível. */
  excedenteStr: string | null;
  /** Divergência de combustível; nula quando não há falta ou a simulação não está disponível. */
  divergencia: DivergenciaCombustivel | null;
}

/** Coluna "Dados da Devolução (Check-out)": odômetro final, combustível com divergência e novas avarias (somente leitura). */
export function ColunaDevolucao({
  saida, quantidadeAvariasEntrada, novas, indeterminadas, comparacaoRealizada, rodados, franquia, excedenteStr, divergencia,
}: ColunaDevolucaoProps) {
  const linha = saida.operadorStr ? M.operador(saida.dataHoraStr, saida.operadorStr) : saida.dataHoraStr;
  // "Rodados" aparece sempre que houver as duas vistorias; a franquia entra só quando a simulação está disponível (aprovado, 26/11/2026).
  const textoRodados = rodados === null
    ? null
    : franquia === null
      ? M.rodadosSemFranquia(formatarNumero(rodados))
      : M.rodados(formatarNumero(rodados), franquia);
  return (
    <section aria-labelledby="coluna-devolucao" className="flex min-w-0 flex-col gap-4 rounded-cartao bg-branco p-6 shadow-sm ring-1 ring-orange-acao/40">
      <CabecalhoColuna id="coluna-devolucao" icone="flight_land" classesIcone="bg-orange-acao text-branco" titulo={M.colunaDevolucao} linha={linha} />
      <div className="flex flex-col gap-2 rounded bg-steel-fundo p-4">
        <div className="flex items-center justify-between gap-2">
          <span className="text-micro font-semibold uppercase tracking-wider text-texto-apoio">{M.odometroFinal}</span>
          {rodados !== null && (
            <span className="inline-flex items-center gap-1 rounded-sm bg-steel-suave px-2 py-0.5 text-micro font-semibold text-navy-profundo">
              <Icone nome="trending_up" tamanho={14} className="text-frota-oficina" />
              <span>{M.delta(formatarNumero(rodados))}</span>
            </span>
          )}
        </div>
        <div className="flex items-baseline gap-1">
          <span className="text-titulo-hero font-bold tracking-tight text-navy-profundo">{formatarNumero(saida.kmRegistrado)}</span>
          <span className="text-corpo-denso font-semibold uppercase text-texto-apoio">km</span>
        </div>
        {(textoRodados !== null || excedenteStr !== null) && (
          <div className="flex flex-wrap items-center justify-between gap-2">
            {textoRodados !== null && <p className="text-apoio text-texto-apoio">{textoRodados}</p>}
            {excedenteStr !== null && <p className="text-apoio font-semibold text-navy-profundo">{M.cobrancaExcedente(excedenteStr)}</p>}
          </div>
        )}
      </div>
      <BlocoCombustivel
        titulo={divergencia ? M.combustivelDivergenciaTitulo : M.combustivelTitulo}
        nivel={saida.nivelCombustivel}
        divergente={divergencia !== null}
        rodape={divergencia && (
          <div className="flex flex-wrap items-center justify-between gap-2 text-apoio text-navy-profundo">
            <span className="flex items-center gap-1 font-semibold text-frota-oficina">
              <Icone nome="priority_high" tamanho={16} />
              <span>{divergencia.oitavos === 0 ? M.divergenciaAbaixoDeUmOitavo : M.divergencia(divergencia.oitavos)}</span>
            </span>
            <span className="text-dado-tabular font-bold">{M.reposicao(divergencia.reposicaoStr)}</span>
          </div>
        )}
      />
      <div className="flex flex-col gap-3 rounded bg-frota-oficina-fundo/20 p-4">
        <div className="flex flex-wrap items-center justify-between gap-2">
          <span className="flex items-center gap-1 text-micro font-bold uppercase tracking-wider text-frota-oficina">
            <Icone nome="fmd_bad" tamanho={18} />
            <span>{M.novasTitulo}</span>
          </span>
          {novas.length > 0 && <span className="rounded-sm bg-orange-acao px-2 py-0.5 text-micro font-bold text-branco">{M.novasIrregularidades(novas.length)}</span>}
        </div>
        {!comparacaoRealizada && <output className="block rounded-lg bg-status-pendente-fundo p-3 text-apoio">{M.legado}</output>}
        {novas.length === 0 ? <p className="text-apoio text-texto-apoio">{M.semNovasAvarias}</p> : (
          <ul className="flex flex-col gap-3">
            {novas.map((avaria, indice) => {
              const valor = avaria.valorEstimado === null ? M.semValor : formatarMoeda(avaria.valorEstimado);
              return (
                <li key={avaria.idAvaria} className="flex flex-col gap-2 rounded-md bg-branco p-3">
                  <div className="flex items-start justify-between gap-2">
                    <div className="flex min-w-0 items-start gap-2">
                      <span aria-hidden="true" className="mt-1.5 h-2 w-2 shrink-0 rounded-full bg-orange-acao" />
                      <span className="text-corpo-denso font-bold text-navy-profundo break-words">{M.novaAvaria(numeroNovaAvaria(quantidadeAvariasEntrada, indice), avaria.descricao)}</span>
                    </div>
                    <span className="flex shrink-0 items-center gap-1 text-micro text-texto-apoio">
                      <span>{M.gravidadeRotulo}</span>
                      <SeloGravidade gravidade={avaria.gravidade} />
                    </span>
                  </div>
                  <p className="pl-4 text-apoio text-texto-apoio break-words">{M.custoReparo(avaria.descricao, valor)}</p>
                  <p className="flex items-center gap-1 rounded-sm bg-frota-oficina-fundo/40 p-2 pl-4 text-dado-tabular font-bold text-frota-oficina">
                    <Icone nome="info" tamanho={16} />
                    <span>{M.valorACobrar(valor)}</span>
                  </p>
                </li>
              );
            })}
          </ul>
        )}
        {indeterminadas.length > 0 && (
          <ul className="flex flex-col gap-2 text-apoio text-texto-apoio">
            {indeterminadas.map(avaria => <li key={avaria.idAvaria} className="break-words">{avaria.descricao}</li>)}
          </ul>
        )}
      </div>
    </section>
  );
}
