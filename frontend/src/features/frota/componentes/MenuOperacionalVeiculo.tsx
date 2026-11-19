import { useEffect, useId, useLayoutEffect, useRef } from 'react';
import { createPortal } from 'react-dom';
import { Icone } from '@/components/ui';
import {
  MENSAGEM_FROTA_MENU_TITULO,
  mensagemFrotaMenuDoVeiculo,
} from '@/lib/mensagens';
import type { ItemMenuOperacional, MenuOperacional } from '@/features/frota/status/menuOperacional';

/** Espaço entre o botão e o menu, e a folga mínima até a borda da janela. */
const DISTANCIA_PX = 4;
const MARGEM_JANELA_PX = 8;

/**
 * Efeito de posição: no navegador roda antes da pintura (sem piscar no lugar errado); na renderização do servidor (testes
 * com renderToStaticMarkup) roda como efeito comum, que não dá aviso.
 */
const useEfeitoDePosicao = typeof window === 'undefined' ? useEffect : useLayoutEffect;

export interface ConteudoMenuOperacionalProps {
  /** Placa do veículo: nome acessível da lista de itens. */
  placa: string;
  menu: MenuOperacional;
  /** Chamado com o item escolhido (o menu fecha antes). */
  aoEscolher: (item: ItemMenuOperacional) => void;
}

/**
 * Conteúdo do menu: os itens (ou nenhum) e a frase quando a situação não tem troca. Sem posição nem comportamento: é a
 * parte que o teste lê por renderToStaticMarkup.
 */
export function ConteudoMenuOperacional({ placa, menu, aoEscolher }: ConteudoMenuOperacionalProps) {
  return (
    <>
      {menu.itens.length > 0 && (
        <div role="menu" aria-label={mensagemFrotaMenuDoVeiculo(placa)} className="flex flex-col py-1">
          {menu.itens.map((item) => (
            <button
              key={item.chave}
              type="button"
              role="menuitem"
              tabIndex={-1}
              onClick={() => aoEscolher(item)}
              className={`flex h-10 items-center px-4 text-left text-corpo-denso font-medium transition-colors ${
                item.perigoso
                  ? 'text-status-perigo hover:bg-status-perigo-fundo'
                  : 'text-texto-desenho hover:bg-steel-suave'
              }`}
            >
              {item.rotulo}
            </button>
          ))}
        </div>
      )}
      {menu.frase && <p className="px-4 py-3 text-corpo-denso text-texto-apoio">{menu.frase}</p>}
    </>
  );
}

export interface MenuOperacionalVeiculoProps extends ConteudoMenuOperacionalProps {
  /** Menu aberto nesta linha (o pai garante um por vez). */
  aberto: boolean;
  /**
   * Há um pedido de troca em andamento: o botão fica com aria-disabled e ignora o clique. Não usa `disabled`, que tira o
   * foco do botão e o perde de vez.
   */
  desabilitado: boolean;
  aoAlternar: () => void;
  aoFechar: () => void;
}

function posicionarMenu(botao: HTMLElement, menu: HTMLElement): void {
  const retangulo = botao.getBoundingClientRect();
  // Largura e altura vêm do próprio cartão já montado (w-56 no JSX): uma só fonte para o alinhamento.
  const larguraMenu = menu.offsetWidth;
  const alturaMenu = menu.offsetHeight;
  const esquerda = Math.min(
    Math.max(MARGEM_JANELA_PX, retangulo.right - larguraMenu),
    window.innerWidth - larguraMenu - MARGEM_JANELA_PX,
  );
  const abaixo = retangulo.bottom + DISTANCIA_PX;
  const acima = retangulo.top - DISTANCIA_PX - alturaMenu;
  // Abre para cima quando embaixo não cabe e em cima cabe.
  const abreParaCima = abaixo + alturaMenu > window.innerHeight - MARGEM_JANELA_PX && acima >= MARGEM_JANELA_PX;
  menu.style.left = `${esquerda}px`;
  menu.style.top = `${abreParaCima ? acima : abaixo}px`;
}

/** Anda pelos itens com as setas (do último volta ao primeiro e vice-versa). */
function moverFocoNoMenu(cartao: HTMLElement, direcao: 1 | -1): void {
  const itens = Array.from(cartao.querySelectorAll<HTMLElement>('[role="menuitem"]'));
  if (itens.length === 0) return;
  const atual = itens.indexOf(document.activeElement as HTMLElement);
  const proximo = atual === -1 ? (direcao === 1 ? 0 : itens.length - 1) : (atual + direcao + itens.length) % itens.length;
  itens[proximo].focus({ preventScroll: true });
}

/** Teclas com o menu aberto: Esc e Tab fecham (o foco volta ao botão); as setas andam pelos itens. */
function tratarTeclaNoMenu(evento: KeyboardEvent, cartao: HTMLElement, fechar: () => void): void {
  if (evento.key === 'Escape') {
    evento.preventDefault();
    evento.stopPropagation();
    fechar();
  } else if (evento.key === 'Tab') {
    // Fecha e deixa o Tab seguir a partir do botão, como em qualquer controle da página.
    fechar();
  } else if (evento.key === 'ArrowDown' || evento.key === 'ArrowUp') {
    evento.preventDefault();
    moverFocoNoMenu(cartao, evento.key === 'ArrowDown' ? 1 : -1);
  }
}

/**
 * Botão de três pontos da linha, com o menu. O menu vai para o fim do documento em posição fixa, então a tabela (que tem
 * rolagem lateral) e a seção (que esconde o que passa) não o cortam. Fecha com Esc, com clique fora, ao rolar, ao
 * redimensionar e ao escolher um item; as setas andam pelos itens; o foco volta ao botão.
 */
export function MenuOperacionalVeiculo({
  placa,
  menu,
  aoEscolher,
  aberto,
  desabilitado,
  aoAlternar,
  aoFechar,
}: MenuOperacionalVeiculoProps) {
  const idMenu = useId();
  const refBotao = useRef<HTMLButtonElement>(null);
  const refMenu = useRef<HTMLElement>(null);
  /** Sempre a função mais nova, para os ouvintes do documento não serem refeitos a cada render. */
  const refFechar = useRef(aoFechar);
  useEfeitoDePosicao(() => {
    refFechar.current = aoFechar;
  });

  // Ao abrir: posiciona, e o foco vai para o primeiro item (ou para o próprio menu, quando só há frase, para o Esc valer).
  useEfeitoDePosicao(() => {
    const botao = refBotao.current;
    const cartao = refMenu.current;
    if (!aberto || !botao || !cartao) return;
    posicionarMenu(botao, cartao);
    const primeiroItem = cartao.querySelector<HTMLElement>('[role="menuitem"]');
    (primeiroItem ?? cartao).focus({ preventScroll: true });
  }, [aberto]);

  /** Fecha e devolve o foco ao botão que abriu o menu. */
  const fecharDevolvendoFoco = (): void => {
    refFechar.current();
    refBotao.current?.focus({ preventScroll: true });
  };

  // Enquanto aberto: clique fora, rolagem e redimensionamento fecham o menu; o teclado trata Esc, Tab e setas quando o
  // foco está no menu (ouvinte do documento, para o cartão não precisar de onKeyDown num elemento sem papel de controle).
  useEffect(() => {
    if (!aberto) return undefined;

    function aoClicarNoDocumento(evento: MouseEvent) {
      const alvo = evento.target;
      if (!(alvo instanceof Node)) return;
      if (refBotao.current?.contains(alvo) || refMenu.current?.contains(alvo)) return;
      refFechar.current();
    }
    function aoMudarAPosicao() {
      refFechar.current();
    }
    function aoTeclarNoDocumento(evento: KeyboardEvent) {
      const cartao = refMenu.current;
      const alvo = evento.target;
      if (!cartao || !(alvo instanceof Node) || !cartao.contains(alvo)) return;
      tratarTeclaNoMenu(evento, cartao, fecharDevolvendoFoco);
    }

    document.addEventListener('mousedown', aoClicarNoDocumento);
    document.addEventListener('keydown', aoTeclarNoDocumento);
    window.addEventListener('scroll', aoMudarAPosicao, true);
    window.addEventListener('resize', aoMudarAPosicao);
    return () => {
      document.removeEventListener('mousedown', aoClicarNoDocumento);
      document.removeEventListener('keydown', aoTeclarNoDocumento);
      window.removeEventListener('scroll', aoMudarAPosicao, true);
      window.removeEventListener('resize', aoMudarAPosicao);
    };
  }, [aberto]);

  function escolher(item: ItemMenuOperacional) {
    fecharDevolvendoFoco();
    aoEscolher(item);
  }

  return (
    <>
      <button
        ref={refBotao}
        type="button"
        title={MENSAGEM_FROTA_MENU_TITULO}
        aria-label={mensagemFrotaMenuDoVeiculo(placa)}
        aria-haspopup={menu.itens.length > 0 ? 'menu' : undefined}
        aria-expanded={aberto}
        aria-controls={aberto ? idMenu : undefined}
        aria-disabled={desabilitado || undefined}
        onClick={() => {
          if (!desabilitado) aoAlternar();
        }}
        className={`inline-flex h-8 w-8 items-center justify-center rounded-sm text-texto-apoio transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-orange-acao ${
          desabilitado ? 'cursor-not-allowed opacity-50' : 'hover:bg-steel-suave'
        }`}
      >
        <Icone nome="more_vert" tamanho={18} />
      </button>
      {aberto &&
        createPortal(
          <section
            ref={refMenu}
            id={idMenu}
            aria-label={mensagemFrotaMenuDoVeiculo(placa)}
            tabIndex={-1}
            className="fixed z-50 w-56 rounded-cartao border border-borda bg-branco shadow-elevado focus:outline-none"
          >
            <ConteudoMenuOperacional placa={placa} menu={menu} aoEscolher={escolher} />
          </section>,
          document.body,
        )}
    </>
  );
}
