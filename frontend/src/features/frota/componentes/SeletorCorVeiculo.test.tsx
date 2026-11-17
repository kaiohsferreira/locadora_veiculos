import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { SeletorCorVeiculo } from './SeletorCorVeiculo';

function renderizar(valor: string, erro?: string): string {
  return renderToStaticMarkup(<SeletorCorVeiculo valor={valor} erro={erro} aoMudar={() => undefined} />);
}

describe('SeletorCorVeiculo', () => {
  it('mostra o rótulo do desenho e as quatro amostras, em grupo de rádio', () => {
    const html = renderizar('');

    expect(html).toContain('Cor Externa / Acabamento');
    expect(html).toContain('role="radiogroup"');
    expect(html.match(/type="radio"/g)).toHaveLength(4);
    for (const rotulo of ['Preto Carbon', 'Branco Polar', 'Prata Shark', 'Cinza Silver']) {
      expect(html).toContain(`>${rotulo}</span>`);
    }
  });

  it('o valor enviado da cinza é "Cinza Silverstone", não o texto visível', () => {
    expect(renderizar('')).toContain('value="Cinza Silverstone"');
  });

  it('a grade das amostras tem 4px entre as células, como o desenho (gap-space-xs)', () => {
    const html = renderizar('');

    expect(html).toContain('grid grid-cols-4 gap-1"');
    expect(html).not.toContain('gap-2"');
  });

  it('nenhuma cor vem marcada quando o valor está vazio', () => {
    expect(renderizar('')).not.toContain('checked=""');
  });

  it('a cor marcada aparece marcada, com o ícone de conferido na amostra', () => {
    const html = renderizar('Prata Shark');

    expect(html.match(/checked=""/g)).toHaveLength(1);
    expect(html).toContain('>check</span>');
  });

  it('valor gravado fora das quatro vira opção extra marcada, sem trocar a cor', () => {
    const html = renderizar('Azul Marinho');

    expect(html.match(/type="radio"/g)).toHaveLength(5);
    expect(html).toContain('value="Azul Marinho"');
    expect(html.match(/checked=""/g)).toHaveLength(1);
  });

  it('erro da cor aparece com aviso anunciado', () => {
    const html = renderizar('', 'Cor inválida.');

    expect(html).toContain('role="alert"');
    expect(html).toContain('Cor inválida.');
  });
});
