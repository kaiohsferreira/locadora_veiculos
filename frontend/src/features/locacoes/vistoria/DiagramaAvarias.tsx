import type { MouseEvent } from 'react';
import type { AnguloAvaria } from '@/types/avarias';
import { MENSAGENS_DEVOLUCAO } from '@/lib/mensagensDevolucao';
import {
  MENSAGEM_DIAGRAMA_DICA,
  MENSAGEM_DIAGRAMA_LEGENDA_ATIVO,
  MENSAGEM_DIAGRAMA_LEGENDA_CONFORME,
  MENSAGEM_DIAGRAMA_LEGENDA_ROTULO,
  MENSAGEM_DIAGRAMA_LEITURA,
  MENSAGEM_DIAGRAMA_REDEFINIR,
  MENSAGEM_DIAGRAMA_TITULO,
  mensagemBalaoAvariaDetalhe,
  mensagemBalaoAvariaTitulo,
  mensagemContagemQuadrante,
  mensagemMarcarNoAngulo,
  mensagemNomeAcessivelAvaria,
} from '@/lib/mensagensVistoriaAvarias';
import type { GravidadeAvaria } from '@/types/enums';
import { Icone } from '@/components/ui';
import { posicaoNoQuadrante } from './validacaoAvaria';
import { DesenhoAngulo } from './desenhoCarro';
import { LEGENDA_QUADRANTE, NOME_QUADRANTE, ORDEM_ANGULOS, ROTULO_ANGULO, contarPorAngulo } from './avariasVistoria';

/** Avaria como o diagrama precisa dela: número na lista, ângulo e posição (quando existem). */
export interface MarcadorDiagrama {
  numero: number;
  angulo: AnguloAvaria | null;
  posX: number | null;
  posY: number | null;
  descricao: string;
  gravidade: GravidadeAvaria;
}

/** Marcação feita no diagrama. Pelo teclado a posição não existe: a pessoa digita X e Y no formulário. */
export interface MarcacaoDiagrama {
  angulo: AnguloAvaria;
  posX: number | null;
  posY: number | null;
}

export interface DiagramaAvariasProps {
  avarias: readonly MarcadorDiagrama[];
  /** Vistoria registrada: o mapa só mostra, sem marcar nem redefinir. */
  somenteLeitura?: boolean;
  aoMarcar?: (marcacao: MarcacaoDiagrama) => void;
  aoRedefinir?: () => void;
  /** Na edição o botão Redefinir Marcadores fica visível; sem marcador para tirar, fica desabilitado (como no desenho 19). */
  podeRedefinir?: boolean;
}

/** Marcador com ângulo e posição: só assim ele aparece no mapa (avaria sem posição fica só na lista). */
type MarcadorPosicionado = MarcadorDiagrama & { angulo: AnguloAvaria; posX: number; posY: number };

/** Ponto do marcador como percentual do quadrante. Sem posição, o marcador não aparece no mapa (só na lista). */
function marcadorVisivel(marcador: MarcadorDiagrama): marcador is MarcadorPosicionado {
  return marcador.angulo !== null && marcador.posX !== null && marcador.posY !== null;
}

interface QuadranteProps {
  angulo: AnguloAvaria;
  quantidade: number;
  marcadores: readonly MarcadorPosicionado[];
  somenteLeitura: boolean;
  aoMarcar?: (marcacao: MarcacaoDiagrama) => void;
}

function Quadrante({ angulo, quantidade, marcadores, somenteLeitura, aoMarcar }: QuadranteProps) {
  const rotulo = ROTULO_ANGULO[angulo];
  // Contagem zero usa o cinza do desenho ("Sem Avarias"); com avarias, o laranja suave.
  const estiloContagem = quantidade === 0 ? 'bg-steel-suave font-medium text-texto-apoio' : 'bg-orange-acao/10 font-semibold text-frota-oficina';

  function marcarComClique(evento: MouseEvent<HTMLButtonElement>): void {
    const retangulo = evento.currentTarget.getBoundingClientRect();
    // Clique do teclado (detail 0) não tem ponto: a posição fica para o formulário.
    if (evento.detail === 0) {
      aoMarcar?.({ angulo, posX: null, posY: null });
      return;
    }
    const ponto = posicaoNoQuadrante(retangulo.width, retangulo.height, evento.clientX - retangulo.left, evento.clientY - retangulo.top);
    aoMarcar?.({ angulo, posX: ponto.posX, posY: ponto.posY });
  }

  return (
    <div className="relative flex flex-col rounded-lg bg-branco p-4 shadow-sm">
      <div className="mb-2 flex items-center justify-between gap-2">
        <span className="text-rotulo font-bold uppercase tracking-wide text-texto-desenho">{NOME_QUADRANTE[angulo]}</span>
        <span className={`rounded px-2 py-0.5 text-micro ${estiloContagem}`}>
          {mensagemContagemQuadrante(quantidade)}
        </span>
      </div>
      <div className="relative flex h-48 w-full items-center justify-center rounded bg-steel-fundo/40">
        {/* Grade técnica do desenho: o raio do ponto usa o token contorno-suave. */}
        <div
          aria-hidden="true"
          className="absolute inset-0 rounded opacity-40 bg-[radial-gradient(theme(colors.contorno-suave)_1px,transparent_1px)] [background-size:12px_12px]"
        />
        <DesenhoAngulo angulo={angulo} />
        {!somenteLeitura && (
          <button
            type="button"
            aria-label={mensagemMarcarNoAngulo(rotulo)}
            onClick={marcarComClique}
            className="absolute inset-0 cursor-crosshair rounded focus-visible:outline focus-visible:outline-2 focus-visible:outline-navy"
          />
        )}
        {marcadores.map((marcador) => (
          <span
            key={marcador.numero}
            style={{ left: `${marcador.posX}%`, top: `${marcador.posY}%` }}
            className="group/pin absolute z-10 flex h-7 w-7 -translate-x-1/2 -translate-y-1/2 items-center justify-center rounded-xl bg-orange-acao font-tabular-data text-dado-tabular font-bold text-branco shadow-lg ring-2 ring-branco"
          >
            <span aria-hidden="true" className="absolute inset-0 animate-ping rounded-xl bg-orange-acao opacity-60 motion-reduce:animate-none" />
            <span aria-hidden="true" className="relative">{marcador.numero}</span>
            {/* Nome acessível do marcador (anexo, tela 19): lido pelo leitor de tela, sem role forçado. */}
            <span className="sr-only">{mensagemNomeAcessivelAvaria(marcador.numero, marcador.descricao, MENSAGENS_DEVOLUCAO.gravidades[marcador.gravidade])}</span>
            {/* Balão do desenho 19: aparece com o ponteiro sobre o marcador. Não recebe clique. */}
            <span
              aria-hidden="true"
              className="pointer-events-none absolute bottom-full left-1/2 z-20 mb-2 hidden -translate-x-1/2 flex-col whitespace-nowrap rounded bg-navy-profundo px-2 py-1 text-left font-normal shadow-xl group-hover/pin:flex"
            >
              <span className="text-apoio font-bold text-branco">{mensagemBalaoAvariaTitulo(marcador.numero, ROTULO_ANGULO[marcador.angulo])}</span>
              <span className="text-apoio text-frota-oficina-fundo">
                {mensagemBalaoAvariaDetalhe(marcador.descricao, MENSAGENS_DEVOLUCAO.gravidades[marcador.gravidade])}
              </span>
            </span>
          </span>
        ))}
      </div>
      <span className="mt-2 text-center text-apoio text-texto-apoio">{LEGENDA_QUADRANTE[angulo]}</span>
    </div>
  );
}

/**
 * Mapa de avarias da tela 19: quatro quadrantes (frente, traseira e laterais) com um marcador por avaria posicionada.
 * O clique no quadrante marca a avaria no ponto clicado; o teclado marca o ângulo, e o X e o Y se digitam no formulário.
 */
export function DiagramaAvarias({ avarias, somenteLeitura = false, aoMarcar, aoRedefinir, podeRedefinir = false }: DiagramaAvariasProps) {
  const contagem = contarPorAngulo(avarias);
  const visiveis = avarias.filter(marcadorVisivel);

  return (
    <section aria-labelledby="titulo-mapa-avarias" className="flex flex-col gap-4 rounded-xl bg-branco p-6 shadow-sm">
      <div className="flex flex-col gap-2 pb-2 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h2 id="titulo-mapa-avarias" className="text-valor-destaque font-bold text-texto-desenho">{MENSAGEM_DIAGRAMA_TITULO}</h2>
          <p className="mt-1 text-apoio text-texto-apoio">
            {somenteLeitura ? MENSAGEM_DIAGRAMA_LEITURA : MENSAGEM_DIAGRAMA_DICA}
          </p>
        </div>
      </div>
      <div className="grid grid-cols-1 gap-4 rounded-lg bg-steel-fundo p-4 md:grid-cols-2">
        {ORDEM_ANGULOS.map((angulo) => (
          <Quadrante
            key={angulo}
            angulo={angulo}
            quantidade={contagem[angulo]}
            marcadores={visiveis.filter((marcador) => marcador.angulo === angulo)}
            somenteLeitura={somenteLeitura}
            aoMarcar={aoMarcar}
          />
        ))}
      </div>
      <div className="flex flex-wrap items-center justify-between gap-2 pt-1">
        <div className="flex flex-wrap items-center gap-4">
          <span className="text-micro font-bold uppercase text-texto-desenho">{MENSAGEM_DIAGRAMA_LEGENDA_ROTULO}</span>
          <span className="flex items-center gap-1">
            <span aria-hidden="true" className="h-3 w-3 rounded-full bg-orange-acao" />
            <span className="text-apoio text-texto-apoio">{MENSAGEM_DIAGRAMA_LEGENDA_ATIVO}</span>
          </span>
          <span className="flex items-center gap-1">
            <span aria-hidden="true" className="h-3 w-3 rounded-full bg-vistoria-conferido" />
            <span className="text-apoio text-texto-apoio">{MENSAGEM_DIAGRAMA_LEGENDA_CONFORME}</span>
          </span>
        </div>
        {!somenteLeitura && (
          <button
            type="button"
            onClick={aoRedefinir}
            disabled={!podeRedefinir}
            className="inline-flex items-center gap-1 text-corpo-denso font-semibold text-frota-oficina transition-opacity hover:opacity-80 disabled:cursor-not-allowed disabled:opacity-40 disabled:hover:opacity-40"
          >
            <Icone nome="restart_alt" tamanho={18} />
            {MENSAGEM_DIAGRAMA_REDEFINIR}
          </button>
        )}
      </div>
    </section>
  );
}
