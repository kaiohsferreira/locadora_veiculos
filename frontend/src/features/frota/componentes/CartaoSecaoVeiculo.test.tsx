import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { CartaoSecaoVeiculo } from './CartaoSecaoVeiculo';

describe('CartaoSecaoVeiculo', () => {
  it('mostra o título com o número, a etiqueta à direita e o conteúdo, sem linha divisória no cabeçalho', () => {
    const html = renderToStaticMarkup(
      <CartaoSecaoVeiculo icone="badge" titulo="1. Identificação Técnica do Veículo" etiqueta="Dados Obrigatórios">
        <p>conteúdo</p>
      </CartaoSecaoVeiculo>,
    );

    expect(html).toContain('<h2');
    expect(html).toContain('1. Identificação Técnica do Veículo');
    expect(html).toContain('Dados Obrigatórios');
    expect(html).toContain('<p>conteúdo</p>');
    expect(html).toContain('rounded-cartao');
    expect(html).toContain('bg-steel-alto');
    expect(html).not.toContain('border-b');
  });

  it('o ícone da caixa é decorativo (sem rótulo para leitor de tela)', () => {
    const html = renderToStaticMarkup(
      <CartaoSecaoVeiculo icone="badge" titulo="T" etiqueta="E">
        <span />
      </CartaoSecaoVeiculo>,
    );

    expect(html).toContain('aria-hidden="true"');
    expect(html).toContain('>badge</span>');
  });

  it('o cabeçalho quebra de linha em telas estreitas (sem rolagem lateral), sem mudar o desenho largo', () => {
    const html = renderToStaticMarkup(
      <CartaoSecaoVeiculo icone="badge" titulo="T" etiqueta="Auditoria Contábil">
        <span />
      </CartaoSecaoVeiculo>,
    );

    expect(html).toMatch(/<header class="flex flex-wrap items-center justify-between gap-x-4 gap-y-2 pb-1">/);
    expect(html).toMatch(/<span class="text-micro[^"]*">Auditoria Contábil<\/span>/);
  });
});
