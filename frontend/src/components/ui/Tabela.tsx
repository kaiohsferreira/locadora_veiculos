import type { ReactNode } from 'react';
import { EstadoVazio } from './EstadoVazio';
import { Esqueleto } from './Esqueleto';

export type AlinhamentoColuna = 'esquerda' | 'centro' | 'direita';

export interface ColunaTabela<T> {
  /** Identificador único da coluna. */
  chave: string;
  /** Texto do cabeçalho. */
  titulo: string;
  /** Conteúdo da célula para cada item (use os formatadores de `@/lib/formatters`). */
  renderizar: (item: T) => ReactNode;
  /** Padrão: esquerda. Use direita para valores numéricos e monetários. */
  alinhamento?: AlinhamentoColuna;
  /** Classes extras do cabeçalho e das células da coluna (ex.: largura "w-32"). */
  classe?: string;
  /** Mantém o título só para leitores de tela (ex.: coluna de ações). */
  ocultarTitulo?: boolean;
}

export interface TabelaProps<T> {
  colunas: readonly ColunaTabela<T>[];
  /** Itens da página atual (ex.: `pagina.itens`). A paginação é sempre do backend. */
  dados: readonly T[];
  /** Chave única de cada linha (ex.: `(veiculo) => veiculo.id`). */
  obterChave: (item: T) => string | number;
  /** Descrição da tabela para leitores de tela (ex.: "Veículos da frota"). */
  legenda: string;
  /** Mostra linhas de esqueleto no lugar dos dados. */
  carregando?: boolean;
  /** Quantidade de linhas de esqueleto. Padrão: 5. */
  linhasEsqueleto?: number;
  /** Conteúdo quando não há dados. Padrão: `<EstadoVazio />`. */
  vazio?: ReactNode;
}

const CLASSES_ALINHAMENTO: Readonly<Record<AlinhamentoColuna, string>> = {
  esquerda: 'text-left',
  centro: 'text-center',
  direita: 'text-right',
};

/** Tabela densa para listagens do back office, com estados de carregando e vazio. */
export function Tabela<T>({
  colunas,
  dados,
  obterChave,
  legenda,
  carregando = false,
  linhasEsqueleto = 5,
  vazio,
}: TabelaProps<T>) {
  const semDados = !carregando && dados.length === 0;

  return (
    <div className="w-full overflow-x-auto">
      <table className="min-w-full border-collapse text-corpo-denso" aria-busy={carregando || undefined}>
        <caption className="sr-only">{legenda}</caption>
        <thead className="bg-offwhite">
          <tr>
            {colunas.map((coluna) => (
              <th
                key={coluna.chave}
                scope="col"
                className={`whitespace-nowrap px-4 py-3 text-legenda font-semibold uppercase tracking-wide text-texto-suave ${CLASSES_ALINHAMENTO[coluna.alinhamento ?? 'esquerda']} ${coluna.classe ?? ''}`}
              >
                <span className={coluna.ocultarTitulo ? 'sr-only' : undefined}>{coluna.titulo}</span>
              </th>
            ))}
          </tr>
        </thead>
        <tbody className="divide-y divide-borda">
          {carregando &&
            Array.from({ length: linhasEsqueleto }, (_, indiceLinha) => (
              <tr key={`esqueleto-${indiceLinha}`}>
                {colunas.map((coluna) => (
                  <td key={coluna.chave} className={`px-4 py-3 ${coluna.classe ?? ''}`}>
                    <Esqueleto className="h-4 w-full" />
                  </td>
                ))}
              </tr>
            ))}

          {semDados && (
            <tr>
              <td colSpan={colunas.length}>{vazio ?? <EstadoVazio />}</td>
            </tr>
          )}

          {!carregando &&
            dados.map((item) => (
              <tr key={obterChave(item)} className="transition-colors hover:bg-offwhite">
                {colunas.map((coluna) => (
                  <td
                    key={coluna.chave}
                    className={`px-4 py-3 align-middle text-texto ${CLASSES_ALINHAMENTO[coluna.alinhamento ?? 'esquerda']} ${coluna.classe ?? ''}`}
                  >
                    {coluna.renderizar(item)}
                  </td>
                ))}
              </tr>
            ))}
        </tbody>
      </table>
    </div>
  );
}
