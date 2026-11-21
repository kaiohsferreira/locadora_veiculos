import { useEffect, useId, useRef, type MutableRefObject, type ReactNode, type SyntheticEvent } from 'react';
import { Icone } from './Icone';
import { decidirFechamentoNativo } from './decisaoGaveta';

const SELETOR_FOCAVEIS = [
  'a[href]',
  'button:not([disabled])',
  'input:not([disabled]):not([type="hidden"])',
  'select:not([disabled])',
  'textarea:not([disabled])',
  '[tabindex]:not([tabindex="-1"])',
].join(',');

/**
 * Variante visual da gaveta. `padrao` é a gaveta de funcionário (440 px, ícone laranja). `pagamento` é a gaveta de PIX da
 * tela 30 do desenho (`#pix-drawer`): largura `max-w-md`, ícone na cor de destaque da tela, cabeçalho mais alto e fundo
 * mais escuro. Só usa tokens que já existem no tailwind.config.js.
 */
export type VarianteGaveta = 'padrao' | 'pagamento';

const CLASSES_VARIANTE: Readonly<
  Record<VarianteGaveta, { largura: string; fundo: string; cabecalho: string; icone: string }>
> = {
  padrao: {
    largura: 'max-w-[440px]',
    fundo: 'backdrop:bg-navy/40',
    cabecalho: 'py-4',
    icone: 'bg-orange-claro text-orange-escuro',
  },
  pagamento: {
    largura: 'max-w-md',
    fundo: 'backdrop:bg-navy/60 backdrop:backdrop-blur-sm',
    cabecalho: 'py-6',
    icone: 'bg-frota-oficina text-branco',
  },
};

export interface GavetaProps {
  /** Título da gaveta (anunciado pelos leitores de tela). */
  titulo: string;
  /** Ícone Material Symbols do cabeçalho. */
  icone: string;
  /** Enquanto grava, nada fecha a gaveta (X, Esc e clique no fundo). */
  bloqueada: boolean;
  aoFechar: () => void;
  /** Botão que abriu a gaveta: recebe o foco de volta quando ela fecha. */
  refGatilho: MutableRefObject<HTMLElement | null>;
  /** Texto do botão de fechar (quem usa escolhe o texto, para não puxar a mensagem de outro módulo). */
  rotuloFechar: string;
  /** Linha abaixo do título no cabeçalho (ex.: a origem da cobrança). Sem ela, o cabeçalho fica como antes. */
  subtitulo?: ReactNode;
  /** Aparência da gaveta. Padrão: a gaveta de funcionário; `pagamento`: a gaveta de PIX da tela 30. */
  variante?: VarianteGaveta;
  children: ReactNode;
}

/**
 * Gaveta lateral sobre um `<dialog>` nativo aberto com `showModal()`: o navegador prende o foco e torna a página inerte, e
 * o Esc chega como evento "cancel". O conteúdo só existe enquanto a gaveta está aberta (quem a renderiza decide), então o
 * formulário é esquecido ao fechar. Foco inicial: o campo marcado com `data-foco-inicial`.
 */
export function Gaveta({
  titulo,
  icone,
  bloqueada,
  aoFechar,
  refGatilho,
  rotuloFechar,
  subtitulo,
  variante = 'padrao',
  children,
}: GavetaProps) {
  const visual = CLASSES_VARIANTE[variante];
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
   * Com envio em curso, a gaveta abre de novo; sem envio, avisa quem controla o estado. Um "close" que chega com o
   * diálogo já aberto é atrasado (ex.: a limpeza do efeito no StrictMode) e é ignorado. A decisão está em
   * `decidirFechamentoNativo`.
   */
  function aoFecharNativo() {
    const dialogo = refDialogo.current;
    const decisao = decidirFechamentoNativo({
      bloqueada,
      conectada: dialogo?.isConnected === true,
      aberta: dialogo?.open === true,
    });
    if (decisao === 'reabrir' && dialogo && !dialogo.open) dialogo.showModal();
    if (decisao === 'avisar') aoFechar();
  }

  const cabecalhoTitulo = (
    <h2 id={idTitulo} className="text-secao text-navy">
      {titulo}
    </h2>
  );
  // Sem subtítulo, o título fica direto no cabeçalho (igual à gaveta de funcionário antes da extração). O wrapper só existe
  // para empilhar o subtítulo embaixo do título.
  const temSubtitulo = subtitulo !== undefined && subtitulo !== null;

  return (
    <dialog
      ref={refDialogo}
      aria-labelledby={idTitulo}
      onCancel={aoCancelar}
      onClose={aoFecharNativo}
      className={`fixed inset-y-0 right-0 left-auto m-0 h-full max-h-none w-full ${visual.largura} flex-col bg-branco p-0 text-texto shadow-elevado ${visual.fundo} open:flex`}
    >
      <header className={`flex items-center justify-between gap-4 bg-steel-fundo px-6 ${visual.cabecalho}`}>
        <div className="flex items-center gap-2">
          <span className={`flex h-8 w-8 items-center justify-center rounded-lg ${visual.icone}`}>
            <Icone nome={icone} tamanho={20} />
          </span>
          {temSubtitulo ? (
            <div className="flex min-w-0 flex-col">
              {cabecalhoTitulo}
              {subtitulo}
            </div>
          ) : (
            cabecalhoTitulo
          )}
        </div>
        <button
          type="button"
          onClick={aoFechar}
          disabled={bloqueada}
          aria-label={rotuloFechar}
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
