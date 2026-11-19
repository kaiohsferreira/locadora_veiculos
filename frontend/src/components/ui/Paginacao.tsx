import type { ReactNode } from 'react';
import { formatarNumero } from '@/lib/formatters';
import { MENSAGEM_LISTA_VAZIA } from '@/lib/mensagens';
import type { Pagina } from '@/types/api';
import { Icone } from './Icone';

/**
 * `padrao`: a paginação de todas as listas. `frota`: o rodapé da tela 11 (Gestão de Frota), com fundo azul claro, cores
 * e botões quadrados do desenho. Só muda o visual; a lógica é a mesma.
 */
export type VariantePaginacao = 'padrao' | 'frota';

export interface PaginacaoProps extends Pick<Pagina<unknown>, 'pagina' | 'tamanhoPagina' | 'total'> {
  /** Chamado com o número da nova página (começa em 1). */
  aoMudarPagina: (pagina: number) => void;
  /** Bloqueia a navegação (ex.: enquanto a próxima página carrega). */
  desabilitada?: boolean;
  /** Nome dos itens no resumo ("Exibindo 1–10 de 57 veículos"). Padrão: "registros". */
  rotuloItens?: string;
  /**
   * Conteúdo extra ao lado do resumo, separado por um ponto (ex.: seletor de linhas por página). Sem ele, o
   * componente sai como antes.
   */
  complemento?: ReactNode;
  /** Visual do rodapé. Padrão: `padrao` (as outras listas não mudam). */
  variante?: VariantePaginacao;
}

type ItemPaginacao = number | 'reticencias';

/** Primeira, última e a vizinhança da página atual, com reticências nos saltos. */
function calcularItens(paginaAtual: number, totalPaginas: number): ItemPaginacao[] {
  const paginas = new Set<number>([1, totalPaginas, paginaAtual - 1, paginaAtual, paginaAtual + 1]);
  const ordenadas = [...paginas].filter((p) => p >= 1 && p <= totalPaginas).sort((a, b) => a - b);

  const itens: ItemPaginacao[] = [];
  ordenadas.forEach((pagina, indice) => {
    const anterior = ordenadas[indice - 1];
    if (anterior !== undefined && pagina - anterior > 1) itens.push('reticencias');
    itens.push(pagina);
  });
  return itens;
}

const CLASSES_BOTAO_PADRAO =
  'inline-flex h-8 min-w-8 items-center justify-center rounded-md px-2 text-corpo-denso font-medium transition-colors ' +
  'disabled:cursor-not-allowed disabled:opacity-50';

/** Botões do rodapé da tela 11: quadrados de 32px e canto de 2px, em seminegrito, como no desenho. */
const CLASSES_BOTAO_FROTA =
  'inline-flex h-8 w-8 items-center justify-center rounded-sm text-corpo-denso font-semibold transition-colors ' +
  'disabled:cursor-not-allowed disabled:opacity-40';

/** Classes de um botão de página, conforme a variante e se é a página atual. */
function classesBotao(frota: boolean, ativo: boolean): string {
  if (frota) {
    return ativo
      ? `${CLASSES_BOTAO_FROTA} bg-navy text-branco shadow-sm`
      : `${CLASSES_BOTAO_FROTA} bg-branco text-texto-desenho enabled:hover:bg-steel-suave`;
  }
  return ativo
    ? `${CLASSES_BOTAO_PADRAO} bg-navy text-branco`
    : `${CLASSES_BOTAO_PADRAO} text-navy enabled:hover:bg-steel-claro`;
}

/**
 * Paginação do backend no formato `Pagina<T>`.
 * Uso: `<Paginacao pagina={dados.pagina} tamanhoPagina={dados.tamanhoPagina} total={dados.total} aoMudarPagina={setPagina} />`.
 */
export function Paginacao({
  pagina,
  tamanhoPagina,
  total,
  aoMudarPagina,
  desabilitada = false,
  rotuloItens = 'registros',
  complemento,
  variante = 'padrao',
}: PaginacaoProps) {
  const frota = variante === 'frota';
  const totalPaginas = Math.max(1, Math.ceil(total / Math.max(1, tamanhoPagina)));
  const paginaAtual = Math.min(Math.max(1, pagina), totalPaginas);
  const inicio = total === 0 ? 0 : (paginaAtual - 1) * tamanhoPagina + 1;
  const fim = Math.min(paginaAtual * tamanhoPagina, total);
  const separadorIntervalo = frota ? ' – ' : '–';
  const classesDestaque = frota ? 'font-bold text-texto-desenho' : 'font-semibold text-texto';
  const tamanhoIcone = frota ? 18 : undefined;

  function irPara(destino: number) {
    if (!desabilitada && destino !== paginaAtual && destino >= 1 && destino <= totalPaginas) {
      aoMudarPagina(destino);
    }
  }

  // Na variante da frota, com total zero, a tabela já mostra o estado vazio: o rodapé não repete a mensagem.
  const mostrarResumo = !(frota && total === 0);
  const resumo = (
    <p className={frota ? 'text-rotulo text-texto-apoio' : 'text-corpo-denso text-texto-suave'}>
      {total === 0 ? (
        MENSAGEM_LISTA_VAZIA
      ) : (
        <>
          Exibindo{' '}
          <span className={classesDestaque}>
            {formatarNumero(inicio)}
            {separadorIntervalo}
            {formatarNumero(fim)}
          </span>{' '}
          de <span className={classesDestaque}>{formatarNumero(total)}</span> {rotuloItens}
        </>
      )}
    </p>
  );

  return (
    <nav
      aria-label="Paginação"
      className={
        frota
          ? 'flex flex-col gap-2 bg-steel-fundo px-4 py-2 sm:flex-row sm:items-center sm:justify-between'
          : 'flex flex-col gap-3 px-4 py-3 sm:flex-row sm:items-center sm:justify-between'
      }
    >
      {complemento ? (
        <div className={frota ? 'flex flex-wrap items-center gap-1' : 'flex flex-wrap items-center gap-2'}>
          {mostrarResumo && resumo}
          {mostrarResumo && (
            <span className={frota ? 'text-contorno-suave' : 'text-texto-suave'} aria-hidden="true">
              •
            </span>
          )}
          {complemento}
        </div>
      ) : (
        mostrarResumo && resumo
      )}

      <ul className={frota ? 'flex items-center gap-1 self-center sm:self-auto' : 'flex items-center gap-1'}>
        <li>
          <button
            type="button"
            className={classesBotao(frota, false)}
            onClick={() => irPara(paginaAtual - 1)}
            disabled={desabilitada || paginaAtual <= 1}
            aria-label="Página anterior"
          >
            <Icone nome="chevron_left" tamanho={tamanhoIcone} />
          </button>
        </li>

        {calcularItens(paginaAtual, totalPaginas).map((item, indice) =>
          item === 'reticencias' ? (
            <li
              key={`reticencias-${indice}`}
              className={frota ? 'px-1 text-apoio font-semibold text-texto-apoio' : 'px-1 text-texto-suave'}
              aria-hidden="true"
            >
              …
            </li>
          ) : (
            <li key={item}>
              <button
                type="button"
                className={classesBotao(frota, item === paginaAtual)}
                onClick={() => irPara(item)}
                disabled={desabilitada}
                aria-label={`Página ${formatarNumero(item)}`}
                aria-current={item === paginaAtual ? 'page' : undefined}
              >
                {formatarNumero(item)}
              </button>
            </li>
          ),
        )}

        <li>
          <button
            type="button"
            className={classesBotao(frota, false)}
            onClick={() => irPara(paginaAtual + 1)}
            disabled={desabilitada || paginaAtual >= totalPaginas}
            aria-label="Próxima página"
          >
            <Icone nome="chevron_right" tamanho={tamanhoIcone} />
          </button>
        </li>
      </ul>
    </nav>
  );
}
