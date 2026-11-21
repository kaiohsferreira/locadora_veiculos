import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { Gaveta } from './Gaveta';

function renderizar(bloqueada: boolean): string {
  return renderToStaticMarkup(
    <Gaveta
      titulo="Pagamento Seguro AutoVia"
      icone="bolt"
      bloqueada={bloqueada}
      aoFechar={() => undefined}
      refGatilho={{ current: null }}
      rotuloFechar="Fechar gaveta"
    >
      <p>conteúdo</p>
    </Gaveta>,
  );
}

function botaoFechar(html: string): string {
  return /<button[^>]*>/.exec(html)?.[0] ?? '';
}

describe('Gaveta (casco compartilhado)', () => {
  it('mostra o título no cabeçalho, ligado ao diálogo por aria-labelledby', () => {
    const html = renderizar(false);
    const idTitulo = /aria-labelledby="([^"]+)"/.exec(html)?.[1];
    expect(idTitulo).toBeDefined();
    expect(html).toContain(`<h2 id="${idTitulo}"`);
    expect(html).toContain('Pagamento Seguro AutoVia');
  });

  it('usa o ícone recebido e a largura de 440 px', () => {
    const html = renderizar(false);
    expect(html).toContain('bolt');
    expect(html).toMatch(/<dialog[^>]*class="[^"]*max-w-\[440px\][^"]*"/);
  });

  it('o botão de fechar usa o rótulo recebido, não um texto fixo', () => {
    expect(botaoFechar(renderizar(false))).toContain('aria-label="Fechar gaveta"');
  });

  it('com envio em curso, o botão de fechar fica desabilitado', () => {
    expect(botaoFechar(renderizar(true))).toContain('disabled=""');
  });

  it('sem envio em curso, o botão de fechar fica habilitado', () => {
    expect(botaoFechar(renderizar(false))).not.toContain('disabled=""');
  });

  it('mostra o conteúdo filho dentro da gaveta', () => {
    expect(renderizar(false)).toContain('<p>conteúdo</p>');
  });

  it('a variante padrão mantém a largura de 440 px e o ícone laranja', () => {
    const html = renderizar(false);
    expect(html).toMatch(/<dialog[^>]*class="[^"]*max-w-\[440px\][^"]*"/);
    expect(html).toContain('bg-orange-claro text-orange-escuro');
    expect(html).not.toContain('max-w-md');
  });

  it('a variante pagamento usa max-w-md, ícone frota-oficina e fundo de 60%', () => {
    const html = renderizarPagamento();
    expect(html).toMatch(/<dialog[^>]*class="[^"]*max-w-md[^"]*"/);
    expect(html).not.toContain('max-w-[440px]');
    expect(html).toContain('bg-frota-oficina text-branco');
    expect(html).not.toContain('bg-orange-claro');
    expect(html).toContain('backdrop:bg-navy/60');
  });

  it('o subtítulo recebido aparece abaixo do título, no cabeçalho', () => {
    const html = renderToStaticMarkup(
      <Gaveta
        titulo="Pagamento Seguro AutoVia"
        icone="bolt"
        bloqueada={false}
        aoFechar={() => undefined}
        refGatilho={{ current: null }}
        rotuloFechar="Fechar gaveta"
        variante="pagamento"
        subtitulo={<span>Locação nº 123</span>}
      >
        <p>conteúdo</p>
      </Gaveta>,
    );
    expect(html).toMatch(/<\/h2><span>Locação nº 123<\/span>/);
  });

  it('sem subtítulo, o título vai direto no cabeçalho, sem wrapper extra', () => {
    const html = renderizar(false);
    expect(html).toMatch(/<\/h2><\/div><button/);
    expect(html).not.toContain('min-w-0 flex-col');
  });
});

function renderizarPagamento(): string {
  return renderToStaticMarkup(
    <Gaveta
      titulo="Pagamento Seguro AutoVia"
      icone="bolt"
      bloqueada={false}
      aoFechar={() => undefined}
      refGatilho={{ current: null }}
      rotuloFechar="Fechar gaveta"
      variante="pagamento"
    >
      <p>conteúdo</p>
    </Gaveta>,
  );
}
