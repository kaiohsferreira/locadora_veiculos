import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { AvisoProvider } from './AvisoProvider';
import { classeSecaoAvisos } from './classeSecaoAvisos';
import { useAvisosAcimaDaBarra } from './useAvisosAcimaDaBarra';

/** Marcação da área de avisos como era antes da opção "acima da barra" (não pode mudar com a opção desligada). */
const SECAO_ORIGINAL =
  '<section aria-label="Notificações" class="pointer-events-none fixed inset-x-4 bottom-4 z-[60] flex flex-col gap-2 sm:inset-x-auto sm:bottom-6 sm:right-6 sm:w-96"></section>';

function PaginaComPedido() {
  useAvisosAcimaDaBarra();
  return <p>conteúdo</p>;
}

describe('AvisoProvider: opção "acima da barra" desligada', () => {
  it('a marcação da área de avisos é idêntica à de antes quando nenhuma tela pede a elevação', () => {
    const html = renderToStaticMarkup(
      <AvisoProvider>
        <p>conteúdo</p>
      </AvisoProvider>,
    );

    expect(html).toContain(SECAO_ORIGINAL);
  });

  it('classeSecaoAvisos(false) devolve exatamente a classe de antes', () => {
    expect(classeSecaoAvisos(false)).toBe(
      'pointer-events-none fixed inset-x-4 bottom-4 z-[60] flex flex-col gap-2 sm:inset-x-auto sm:bottom-6 sm:right-6 sm:w-96',
    );
  });

  it('o hook de pedido fora de um provedor não quebra a tela', () => {
    const html = renderToStaticMarkup(<PaginaComPedido />);

    expect(html).toContain('conteúdo');
  });
});

describe('AvisoProvider: opção "acima da barra" ligada', () => {
  it('classeSecaoAvisos(true) sobe a área acima da barra de ações (bottom-40 no celular, bottom-24 a partir de sm)', () => {
    const classe = classeSecaoAvisos(true);

    expect(classe).toContain('bottom-40');
    expect(classe).toContain('sm:bottom-24');
    expect(classe).not.toContain('bottom-4 ');
    expect(classe).not.toContain('sm:bottom-6');
    expect(classe).toContain('z-[60]');
    expect(classe).toContain('pointer-events-none');
  });

  it('a elevação não muda largura nem lado da área: segue à direita, com 384px no desktop', () => {
    const classe = classeSecaoAvisos(true);

    expect(classe).toContain('sm:right-6');
    expect(classe).toContain('sm:w-96');
    expect(classe).toBe(
      'pointer-events-none fixed inset-x-4 bottom-40 z-[60] flex flex-col gap-2 sm:inset-x-auto sm:bottom-24 sm:right-6 sm:w-96',
    );
  });
});
