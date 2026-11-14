import { useEffect, useId, useRef, type MutableRefObject, type ReactNode, type SyntheticEvent } from 'react';
import { Icone } from '@/components/ui';
import { MENSAGEM_EQUIPE_FECHAR } from '@/lib/mensagens';
import { decidirFechamentoNativo } from './decisaoGaveta';

const SELETOR_FOCAVEIS = [
  'a[href]',
  'button:not([disabled])',
  'input:not([disabled]):not([type="hidden"])',
  'select:not([disabled])',
  'textarea:not([disabled])',
  '[tabindex]:not([tabindex="-1"])',
].join(',');

export interface GavetaFuncionarioProps {
  /** Título da gaveta (anunciado pelos leitores de tela). */
  titulo: string;
  /** Ícone Material Symbols do cabeçalho. */
  icone: string;
  /** Enquanto grava, nada fecha a gaveta (X, Cancelar, Esc e clique no fundo). */
  bloqueada: boolean;
  aoFechar: () => void;
  /** Botão que abriu a gaveta: recebe o foco de volta quando ela fecha. */
  refGatilho: MutableRefObject<HTMLElement | null>;
  children: ReactNode;
}

/**
 * Gaveta lateral de 440 px sobre um `<dialog>` nativo aberto com `showModal()`: o navegador prende o foco e torna a
 * página inerte, e o Esc chega como evento "cancel". O conteúdo só existe enquanto a gaveta está aberta (quem a
 * renderiza decide), então o formulário é esquecido ao fechar. Foco inicial: o campo marcado com `data-foco-inicial`.
 *
 * Candidata a componente compartilhado: hoje vive em `features/equipe` porque a tela 41 é a primeira a usá-la.
 */
export function GavetaFuncionario({ titulo, icone, bloqueada, aoFechar, refGatilho, children }: GavetaFuncionarioProps) {
  const idTitulo = useId();
  const refDialogo = useRef<HTMLDialogElement>(null);
  const refConteudo = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const dialogo = refDialogo.current;
    if (!dialogo) return undefined;

    const overflowAnterior = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    if (!dialogo.open) dialogo.showModal();

    const alvoInicial =
      refConteudo.current?.querySelector<HTMLElement>('[data-foco-inicial]') ??
      refConteudo.current?.querySelector<HTMLElement>(SELETOR_FOCAVEIS);
    alvoInicial?.focus();

    // O gatilho é lido agora (ele já foi definido antes de a gaveta abrir) e não na limpeza.
    const gatilho = refGatilho.current;
    return () => {
      if (dialogo.open) dialogo.close();
      document.body.style.overflow = overflowAnterior;
      gatilho?.focus();
    };
  }, [refGatilho]);

  // Clique no fundo escurecido: o alvo do clique é o próprio <dialog> (o painel ocupa só a faixa da direita).
  useEffect(() => {
    const dialogo = refDialogo.current;
    if (!dialogo) return undefined;

    function aoClicarNoFundo(evento: globalThis.MouseEvent) {
      if (evento.target === dialogo && !bloqueada) aoFechar();
    }

    dialogo.addEventListener('click', aoClicarNoFundo);
    return () => dialogo.removeEventListener('click', aoClicarNoFundo);
  }, [aoFechar, bloqueada]);

  /** Esc: o React decide o fechamento (o navegador não fecha sozinho). */
  function aoCancelar(evento: SyntheticEvent<HTMLDialogElement>) {
    evento.preventDefault();
    if (!bloqueada) aoFechar();
  }

  /**
   * Fechamento que não veio do React (o navegador fecha o diálogo mesmo com o "cancel" cancelado, ex.: Esc repetido).
   * Com envio em curso, a gaveta abre de novo; sem envio, avisa quem controla o estado. A decisão está em
   * `decidirFechamentoNativo`.
   */
  function aoFecharNativo() {
    const dialogo = refDialogo.current;
    const decisao = decidirFechamentoNativo({ bloqueada, conectada: dialogo?.isConnected === true });
    if (decisao === 'reabrir' && dialogo && !dialogo.open) dialogo.showModal();
    if (decisao === 'avisar') aoFechar();
  }

  return (
    <dialog
      ref={refDialogo}
      aria-labelledby={idTitulo}
      onCancel={aoCancelar}
      onClose={aoFecharNativo}
      className="fixed inset-y-0 right-0 left-auto m-0 h-full max-h-none w-full max-w-[440px] flex-col bg-branco p-0 text-texto shadow-elevado backdrop:bg-navy/40 open:flex"
    >
      <header className="flex items-center justify-between gap-4 bg-steel-fundo px-6 py-4">
        <div className="flex items-center gap-2">
          <span className="flex h-8 w-8 items-center justify-center rounded-lg bg-orange-claro text-orange-escuro">
            <Icone nome={icone} tamanho={20} />
          </span>
          <h2 id={idTitulo} className="text-secao text-navy">
            {titulo}
          </h2>
        </div>
        <button
          type="button"
          onClick={aoFechar}
          disabled={bloqueada}
          aria-label={MENSAGEM_EQUIPE_FECHAR}
          className="-mr-2 inline-flex h-8 w-8 shrink-0 items-center justify-center rounded-md text-texto-suave transition-colors hover:bg-offwhite hover:text-navy disabled:cursor-not-allowed disabled:opacity-60"
        >
          <Icone nome="close" />
        </button>
      </header>
      <div ref={refConteudo} className="flex min-h-0 flex-1 flex-col">
        {children}
      </div>
    </dialog>
  );
}
