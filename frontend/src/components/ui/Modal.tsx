import { useEffect, useId, useRef, type ReactNode, type SyntheticEvent } from 'react';
import { Icone } from './Icone';

export type TamanhoModal = 'pequeno' | 'medio' | 'grande';

export interface ModalProps {
  /** Controla se o modal está visível. */
  aberto: boolean;
  /** Chamado ao clicar em Fechar, pressionar Esc ou clicar fora (se permitido). */
  aoFechar: () => void;
  /** Título do diálogo (anunciado pelos leitores de tela). */
  titulo: string;
  /** Texto de apoio abaixo do título. */
  descricao?: string;
  children: ReactNode;
  /** Área de ações no rodapé (ex.: botões Cancelar e Confirmar). */
  rodape?: ReactNode;
  /** Padrão: medio. */
  tamanho?: TamanhoModal;
  /** Fecha ao clicar no fundo escurecido. Padrão: true. */
  fecharAoClicarFora?: boolean;
}

const LARGURA_MODAL: Readonly<Record<TamanhoModal, string>> = {
  pequeno: 'max-w-md',
  medio: 'max-w-lg',
  grande: 'max-w-3xl',
};

const SELETOR_FOCAVEIS = [
  'a[href]',
  'button:not([disabled])',
  'input:not([disabled]):not([type="hidden"])',
  'select:not([disabled])',
  'textarea:not([disabled])',
  '[tabindex]:not([tabindex="-1"])',
].join(',');

/**
 * Diálogo modal acessível, sobre o `<dialog>` nativo aberto com `showModal()`:
 * o navegador prende o foco no diálogo e torna o resto da página inerte.
 * Esc fecha, a rolagem da página fica travada e o foco volta ao elemento que
 * abriu o modal. O estado continua controlado pelo React (`aberto`/`aoFechar`).
 */
export function Modal({
  aberto,
  aoFechar,
  titulo,
  descricao,
  children,
  rodape,
  tamanho = 'medio',
  fecharAoClicarFora = true,
}: ModalProps) {
  const idTitulo = useId();
  const idDescricao = useId();
  const refDialogo = useRef<HTMLDialogElement>(null);
  const refConteudo = useRef<HTMLDivElement>(null);
  /** Marca o fechamento feito pelo próprio componente, para não chamar aoFechar duas vezes. */
  const refFechandoPeloComponente = useRef(false);

  useEffect(() => {
    const dialogo = refDialogo.current;
    if (!aberto || !dialogo) return undefined;

    const elementoAnterior = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    const overflowAnterior = document.body.style.overflow;
    document.body.style.overflow = 'hidden';

    if (!dialogo.open) dialogo.showModal();

    // Foco inicial: primeiro controle do conteúdo; se não houver, o botão Fechar.
    const primeiroDoConteudo = refConteudo.current?.querySelector<HTMLElement>(SELETOR_FOCAVEIS);
    primeiroDoConteudo?.focus();

    return () => {
      if (dialogo.open) {
        refFechandoPeloComponente.current = true;
        dialogo.close();
      }
      document.body.style.overflow = overflowAnterior;
      elementoAnterior?.focus();
    };
  }, [aberto]);

  // Clique no fundo escurecido: o evento chega com o próprio <dialog> como alvo.
  // O equivalente por teclado é o Esc (evento "cancel" abaixo).
  useEffect(() => {
    const dialogo = refDialogo.current;
    if (!aberto || !dialogo || !fecharAoClicarFora) return undefined;

    function aoClicarNoFundo(evento: globalThis.MouseEvent) {
      if (evento.target === dialogo) aoFechar();
    }

    dialogo.addEventListener('click', aoClicarNoFundo);
    return () => dialogo.removeEventListener('click', aoClicarNoFundo);
  }, [aberto, fecharAoClicarFora, aoFechar]);

  /** Esc dispara "cancel" no diálogo nativo: quem fecha é o React, via aoFechar. */
  function aoCancelar(evento: SyntheticEvent<HTMLDialogElement>) {
    evento.preventDefault();
    aoFechar();
  }

  /** Fechamento feito pelo navegador (ex.: Esc repetido): mantém o estado do React em sincronia. */
  function aoFecharNativo() {
    if (refFechandoPeloComponente.current) {
      refFechandoPeloComponente.current = false;
      return;
    }
    aoFechar();
  }

  if (!aberto) return null;

  return (
    <dialog
      ref={refDialogo}
      aria-labelledby={idTitulo}
      aria-describedby={descricao ? idDescricao : undefined}
      onCancel={aoCancelar}
      onClose={aoFecharNativo}
      className={`w-[calc(100%-2rem)] rounded-cartao bg-branco p-0 text-texto shadow-elevado backdrop:bg-navy/60 ${LARGURA_MODAL[tamanho]}`}
    >
      <div className="flex max-h-[85vh] flex-col">
        <header className="flex items-start justify-between gap-4 border-b border-borda px-6 py-4">
          <div>
            <h2 id={idTitulo} className="text-secao text-navy">
              {titulo}
            </h2>
            {descricao && (
              <p id={idDescricao} className="mt-1 text-corpo-denso text-texto-suave">
                {descricao}
              </p>
            )}
          </div>
          <button
            type="button"
            onClick={aoFechar}
            aria-label="Fechar"
            className="-mr-2 inline-flex h-8 w-8 shrink-0 items-center justify-center rounded-md text-texto-suave transition-colors hover:bg-offwhite hover:text-navy"
          >
            <Icone nome="close" />
          </button>
        </header>
        <div ref={refConteudo} className="overflow-y-auto px-6 py-4 text-corpo-denso">
          {children}
        </div>
        {rodape && (
          <footer className="flex flex-wrap justify-end gap-2 border-t border-borda px-6 py-4">{rodape}</footer>
        )}
      </div>
    </dialog>
  );
}
