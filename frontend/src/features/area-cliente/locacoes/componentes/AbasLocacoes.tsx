import { useRef, type KeyboardEvent } from 'react';
import { Icone } from '@/components/ui';
import { formatarNumero } from '@/lib/formatters';
import {
  MENSAGEM_LOCACOES_ABA_ANDAMENTO,
  MENSAGEM_LOCACOES_ABA_HISTORICO,
  MENSAGEM_LOCACOES_NAVEGACAO,
} from '@/lib/mensagensLocacoesCliente';
import type { SituacaoLocacao } from '@/types/locacoesCliente';

/** Contador de uma aba: número, ainda carregando, ou indisponível (a aba sai sem número). */
export type ContagemAbaLocacao = number | 'carregando' | 'indisponivel';

export const ID_ABA_LOCACOES_ANDAMENTO = 'aba-locacoes-andamento';
export const ID_ABA_LOCACOES_HISTORICO = 'aba-locacoes-historico';
export const ID_PAINEL_LOCACOES = 'painel-locacoes';

export interface AbasLocacoesProps {
  situacao: SituacaoLocacao;
  aoMudar: (situacao: SituacaoLocacao) => void;
  contagens: Readonly<Record<SituacaoLocacao, ContagemAbaLocacao>>;
  /** Texto de "Atualizado"; null esconde a faixa. */
  atualizacao: string | null;
  /** Consulta em andamento: o ícone gira. */
  atualizando: boolean;
}

interface AbaLocacoes {
  situacao: SituacaoLocacao;
  rotulo: string;
  id: string;
}

const ABAS: readonly AbaLocacoes[] = [
  { situacao: 'EM_ANDAMENTO', rotulo: MENSAGEM_LOCACOES_ABA_ANDAMENTO, id: ID_ABA_LOCACOES_ANDAMENTO },
  { situacao: 'HISTORICO', rotulo: MENSAGEM_LOCACOES_ABA_HISTORICO, id: ID_ABA_LOCACOES_HISTORICO },
];

/** Trilho steel-suave no celular (anexo, item 3); steel-fundo no computador (desenho 27). */
const CLASSES_TRILHO =
  'flex w-full items-center gap-1 rounded-cartao bg-steel-suave p-1.5 shadow-inner md:w-fit md:bg-steel-fundo';

const CLASSES_ABA =
  'inline-flex flex-1 items-center justify-center gap-2 rounded-lg px-3 py-2 text-corpo-denso font-semibold transition-colors ' +
  'md:flex-none md:px-4 ' +
  'focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-steel';

const CLASSES_CONTADOR =
  'inline-flex min-w-5 items-center justify-center rounded-full px-1.5 py-0.5 text-rotulo leading-none md:bg-steel-suave md:text-texto-apoio';

/** Celular (desenho 27, item 3 do anexo): contador laranja na aba ativa e cinza-azulado na inativa. No computador, o da tela. */
const CLASSE_CONTADOR_ATIVA = 'bg-frota-oficina font-bold text-branco';
const CLASSE_CONTADOR_INATIVA = 'bg-steel-alto font-medium text-status-neutro';

/**
 * Abas Em andamento e Histórico (desenho 27), com contadores e "Atualizado". Setas, Home e End trocam de aba pelo
 * teclado (padrão de tablist): só a aba ativa entra na ordem de Tab. Não há aba Próximas (decisão N13.5).
 */
export function AbasLocacoes({ situacao, aoMudar, contagens, atualizacao, atualizando }: AbasLocacoesProps) {
  const refsAbas = useRef<Array<HTMLButtonElement | null>>([]);

  function aoTeclar(evento: KeyboardEvent<HTMLButtonElement>, indice: number) {
    let proximo: number | null = null;
    if (evento.key === 'ArrowRight') proximo = (indice + 1) % ABAS.length;
    else if (evento.key === 'ArrowLeft') proximo = (indice - 1 + ABAS.length) % ABAS.length;
    else if (evento.key === 'Home') proximo = 0;
    else if (evento.key === 'End') proximo = ABAS.length - 1;
    if (proximo === null) return;
    evento.preventDefault();
    aoMudar(ABAS[proximo].situacao);
    refsAbas.current[proximo]?.focus();
  }

  return (
    <div className="flex flex-wrap items-center justify-between gap-2">
      <div role="tablist" aria-label={MENSAGEM_LOCACOES_NAVEGACAO} className={CLASSES_TRILHO}>
        {ABAS.map((aba, indice) => {
          const ativa = aba.situacao === situacao;
          const contagem = contagens[aba.situacao];
          return (
            <button
              key={aba.situacao}
              ref={(elemento) => {
                refsAbas.current[indice] = elemento;
              }}
              id={aba.id}
              type="button"
              role="tab"
              aria-selected={ativa}
              aria-controls={ID_PAINEL_LOCACOES}
              tabIndex={ativa ? 0 : -1}
              onClick={() => aoMudar(aba.situacao)}
              onKeyDown={(evento) => aoTeclar(evento, indice)}
              className={`${CLASSES_ABA} ${
                ativa ? 'bg-branco text-navy-profundo shadow-sm' : 'bg-transparent text-texto-apoio hover:text-navy-profundo'
              }`}
            >
              {aba.situacao === 'EM_ANDAMENTO' && (
                <span aria-hidden="true" className="h-2 w-2 rounded-full bg-orange-acao motion-safe:animate-pulse" />
              )}
              <span>{aba.rotulo}</span>
              {contagem !== 'indisponivel' && (
                <span className={`${CLASSES_CONTADOR} ${ativa ? CLASSE_CONTADOR_ATIVA : CLASSE_CONTADOR_INATIVA}`}>
                  {contagem === 'carregando' ? (
                    <span className="inline-block h-3 w-4 rounded bg-steel-linha motion-safe:animate-pulse" aria-hidden="true" />
                  ) : (
                    formatarNumero(contagem)
                  )}
                </span>
              )}
            </button>
          );
        })}
      </div>

      {atualizacao && (
        <div className="hidden items-center gap-1 text-micro text-texto-apoio sm:flex">
          <Icone nome="sync" tamanho={16} className={atualizando ? 'motion-safe:animate-spin' : ''} />
          <span>{atualizacao}</span>
        </div>
      )}
    </div>
  );
}
