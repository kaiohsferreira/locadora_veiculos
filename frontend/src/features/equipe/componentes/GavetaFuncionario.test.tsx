import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { Icone } from '@/components/ui';
import { MENSAGEM_EQUIPE_FECHAR } from '@/lib/mensagens';
import { GavetaFuncionario } from './GavetaFuncionario';

/** Troca os ids gerados pelo React (useId) por um valor fixo, para comparar a marcação inteira. */
function normalizarIds(html: string): string {
  return html.replace(/(id|aria-labelledby)="[^"]*"/g, '$1="ID"');
}

/**
 * Fixa a marcação e as regras visíveis da gaveta de funcionário antes da extração do casco compartilhado. O foco, o Esc e
 * o clique no fundo dependem do navegador (sem jsdom aqui) e ficam para a conferência no navegador.
 */
function renderizar(bloqueada: boolean): string {
  return renderToStaticMarkup(
    <GavetaFuncionario
      titulo="Novo funcionário"
      icone="person_add"
      bloqueada={bloqueada}
      aoFechar={() => undefined}
      refGatilho={{ current: null }}
    >
      <p>conteúdo da gaveta</p>
    </GavetaFuncionario>,
  );
}

function botaoFechar(html: string): string {
  return /<button[^>]*>/.exec(html)?.[0] ?? '';
}

describe('GavetaFuncionario (marcação fixada antes da extração)', () => {
  it('mostra o título no cabeçalho, ligado ao diálogo por aria-labelledby', () => {
    const html = renderizar(false);
    const idTitulo = /aria-labelledby="([^"]+)"/.exec(html)?.[1];
    expect(idTitulo).toBeDefined();
    expect(html).toContain(`<h2 id="${idTitulo}"`);
    expect(html).toContain('Novo funcionário');
  });

  it('usa o ícone recebido e a largura de 440 px no diálogo', () => {
    const html = renderizar(false);
    expect(html).toContain('person_add');
    expect(html).toMatch(/<dialog[^>]*class="[^"]*max-w-\[440px\][^"]*"/);
  });

  it('o botão de fechar tem o rótulo de equipe', () => {
    expect(botaoFechar(renderizar(false))).toContain(`aria-label="${MENSAGEM_EQUIPE_FECHAR}"`);
  });

  it('com envio em curso, o botão de fechar fica desabilitado', () => {
    expect(botaoFechar(renderizar(true))).toContain('disabled=""');
  });

  it('sem envio em curso, o botão de fechar fica habilitado', () => {
    expect(botaoFechar(renderizar(false))).not.toContain('disabled=""');
  });

  it('mostra o conteúdo filho dentro da gaveta', () => {
    expect(renderizar(false)).toContain('<p>conteúdo da gaveta</p>');
  });

  it('a marcação inteira (diálogo, cabeçalho, botão de fechar e corpo) é a mesma de antes da extração (commit 428a832)', () => {
    const esperado =
      '<dialog aria-labelledby="ID" class="fixed inset-y-0 right-0 left-auto m-0 h-full max-h-none w-full max-w-[440px] flex-col bg-branco p-0 text-texto shadow-elevado backdrop:bg-navy/40 open:flex">' +
      '<header class="flex items-center justify-between gap-4 bg-steel-fundo px-6 py-4">' +
      '<div class="flex items-center gap-2">' +
      '<span class="flex h-8 w-8 items-center justify-center rounded-lg bg-orange-claro text-orange-escuro">' +
      renderToStaticMarkup(<Icone nome="person_add" tamanho={20} />) +
      '</span>' +
      '<h2 id="ID" class="text-secao text-navy">Novo funcionário</h2>' +
      '</div>' +
      `<button type="button" aria-label="${MENSAGEM_EQUIPE_FECHAR}" class="-mr-2 inline-flex h-8 w-8 shrink-0 items-center justify-center rounded-md text-texto-suave transition-colors hover:bg-offwhite hover:text-navy disabled:cursor-not-allowed disabled:opacity-60">` +
      renderToStaticMarkup(<Icone nome="close" />) +
      '</button>' +
      '</header>' +
      '<div class="flex min-h-0 flex-1 flex-col"><p>conteúdo da gaveta</p></div>' +
      '</dialog>';
    const html = renderToStaticMarkup(
      <GavetaFuncionario
        titulo="Novo funcionário"
        icone="person_add"
        bloqueada={false}
        aoFechar={() => undefined}
        refGatilho={{ current: null }}
      >
        <p>conteúdo da gaveta</p>
      </GavetaFuncionario>,
    );
    expect(normalizarIds(html)).toBe(esperado);
  });
});
