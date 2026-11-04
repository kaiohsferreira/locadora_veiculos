import { formatarNumero } from '@/lib/formatters';
import { MENSAGEM_LISTA_VAZIA } from '@/lib/mensagens';
import type { Pagina } from '@/types/api';
import { Icone } from './Icone';

export interface PaginacaoProps extends Pick<Pagina<unknown>, 'pagina' | 'tamanhoPagina' | 'total'> {
  /** Chamado com o número da nova página (começa em 1). */
  aoMudarPagina: (pagina: number) => void;
  /** Bloqueia a navegação (ex.: enquanto a próxima página carrega). */
  desabilitada?: boolean;
  /** Nome dos itens no resumo ("Exibindo 1–10 de 57 veículos"). Padrão: "registros". */
  rotuloItens?: string;
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

const CLASSES_BOTAO =
  'inline-flex h-8 min-w-8 items-center justify-center rounded-md px-2 text-corpo-denso font-medium transition-colors ' +
  'disabled:cursor-not-allowed disabled:opacity-50';

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
}: PaginacaoProps) {
  const totalPaginas = Math.max(1, Math.ceil(total / Math.max(1, tamanhoPagina)));
  const paginaAtual = Math.min(Math.max(1, pagina), totalPaginas);
  const inicio = total === 0 ? 0 : (paginaAtual - 1) * tamanhoPagina + 1;
  const fim = Math.min(paginaAtual * tamanhoPagina, total);

  function irPara(destino: number) {
    if (!desabilitada && destino !== paginaAtual && destino >= 1 && destino <= totalPaginas) {
      aoMudarPagina(destino);
    }
  }

  return (
    <nav
      aria-label="Paginação"
      className="flex flex-col gap-3 px-4 py-3 sm:flex-row sm:items-center sm:justify-between"
    >
      <p className="text-corpo-denso text-texto-suave">
        {total === 0 ? (
          MENSAGEM_LISTA_VAZIA
        ) : (
          <>
            Exibindo{' '}
            <span className="font-semibold text-texto">
              {formatarNumero(inicio)}–{formatarNumero(fim)}
            </span>{' '}
            de <span className="font-semibold text-texto">{formatarNumero(total)}</span> {rotuloItens}
          </>
        )}
      </p>

      <ul className="flex items-center gap-1">
        <li>
          <button
            type="button"
            className={`${CLASSES_BOTAO} text-navy enabled:hover:bg-steel-claro`}
            onClick={() => irPara(paginaAtual - 1)}
            disabled={desabilitada || paginaAtual <= 1}
            aria-label="Página anterior"
          >
            <Icone nome="chevron_left" />
          </button>
        </li>

        {calcularItens(paginaAtual, totalPaginas).map((item, indice) =>
          item === 'reticencias' ? (
            <li key={`reticencias-${indice}`} className="px-1 text-texto-suave" aria-hidden="true">
              …
            </li>
          ) : (
            <li key={item}>
              <button
                type="button"
                className={
                  item === paginaAtual
                    ? `${CLASSES_BOTAO} bg-navy text-branco`
                    : `${CLASSES_BOTAO} text-navy enabled:hover:bg-steel-claro`
                }
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
            className={`${CLASSES_BOTAO} text-navy enabled:hover:bg-steel-claro`}
            onClick={() => irPara(paginaAtual + 1)}
            disabled={desabilitada || paginaAtual >= totalPaginas}
            aria-label="Próxima página"
          >
            <Icone nome="chevron_right" />
          </button>
        </li>
      </ul>
    </nav>
  );
}
