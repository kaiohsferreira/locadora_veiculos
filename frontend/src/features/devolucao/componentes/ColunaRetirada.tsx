import { Icone } from '@/components/ui';
import { formatarMoeda, formatarNumero } from '@/lib/formatters';
import { MENSAGENS_DEVOLUCAO as M } from '@/lib/mensagensDevolucao';
import type { AvariaComparacao, VistoriaDevolucao } from '@/types/devolucao';
import { avariasNumeradas } from '../regras/regrasDevolucao';
import { BlocoCombustivel } from './BlocoCombustivel';
import { CabecalhoColuna } from './CabecalhoColuna';

export interface ColunaRetiradaProps {
  /** Vistoria de entrada: a retirada do veículo (coluna da esquerda do desenho 06). */
  entrada: VistoriaDevolucao;
  /** Avarias registradas na entrada, na ordem da vistoria. */
  avarias: readonly AvariaComparacao[];
  /** Franquia contratada no texto da simulação; nulo quando a simulação não está disponível para o cargo. */
  franquia: string | null;
}

/** Coluna "Dados da Retirada (Check-in)": odômetro, nível de combustível e avarias da entrada (somente leitura). */
export function ColunaRetirada({ entrada, avarias, franquia }: ColunaRetiradaProps) {
  const linha = entrada.operadorStr ? M.operador(entrada.dataHoraStr, entrada.operadorStr) : entrada.dataHoraStr;
  return (
    <section aria-labelledby="coluna-retirada" className="flex min-w-0 flex-col gap-4 rounded-cartao bg-branco p-6 shadow-sm">
      <CabecalhoColuna id="coluna-retirada" icone="flight_takeoff" classesIcone="bg-steel-alto text-navy-profundo" titulo={M.colunaRetirada} linha={linha} />
      <div className="flex flex-col gap-1 rounded bg-steel-fundo p-4">
        <span className="text-micro font-semibold uppercase tracking-wider text-texto-apoio">{M.odometroInicial}</span>
        <div className="flex items-baseline gap-1">
          <span className="text-titulo-hero font-bold tracking-tight text-navy-profundo">{formatarNumero(entrada.kmRegistrado)}</span>
          <span className="text-corpo-denso font-semibold uppercase text-texto-apoio">km</span>
        </div>
        {franquia !== null && <p className="text-apoio text-texto-apoio">{M.franquiaContratada(franquia)}</p>}
      </div>
      <BlocoCombustivel titulo={M.combustivelTitulo} nivel={entrada.nivelCombustivel} comEscala />
      <div className="flex flex-col gap-2 rounded bg-steel-fundo p-4">
        <div className="flex items-center justify-between gap-2">
          <span className="text-micro font-semibold uppercase tracking-wider text-texto-apoio">{M.avariasEntradaTitulo}</span>
          <span className="rounded-sm bg-steel-suave px-2 py-0.5 text-micro font-bold text-texto-apoio">{M.registros(avarias.length)}</span>
        </div>
        {avarias.length === 0 ? <p className="text-apoio text-texto-apoio">{M.vazio}</p> : (
          <ul className="flex flex-col gap-2">
            {avariasNumeradas(avarias).map(({ posicao, avaria }) => (
              <li key={avaria.idAvaria} className="flex items-start gap-2 rounded-md bg-branco p-2">
                <Icone nome="history" tamanho={18} className="mt-0.5 shrink-0 text-contorno" />
                <div className="flex min-w-0 flex-col leading-snug">
                  <span className="text-corpo-denso font-semibold text-navy-profundo break-words">{M.avariaEntrada(posicao, avaria.descricao)}</span>
                  <span className="text-apoio text-texto-apoio">
                    {M.avariaApoio(M.gravidades[avaria.gravidade], avaria.valorEstimado === null ? M.semValor : formatarMoeda(avaria.valorEstimado))}
                  </span>
                </div>
              </li>
            ))}
          </ul>
        )}
      </div>
    </section>
  );
}
