import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { Select } from './Select';

const OPCOES = [{ valor: '1', rotulo: 'São Paulo – SP' }];

describe('Select: ícone inicial', () => {
  it('mostra o ícone e o espaço à esquerda na variante padrão', () => {
    const html = renderToStaticMarkup(<Select rotulo="Local" opcoes={OPCOES} iconeInicial="location_on" />);
    expect(html).toContain('location_on');
    expect(html).toMatch(/<select [^>]*class="[^"]* pl-10 /);
  });

  it('não mostra o ícone nem o espaço nas outras variantes', () => {
    for (const variante of ['cartao', 'desenho', 'frota'] as const) {
      const html = renderToStaticMarkup(<Select rotulo="Local" opcoes={OPCOES} variante={variante} iconeInicial="location_on" />);
      expect(html).not.toContain('location_on');
      expect(html).not.toContain(' pl-10');
    }
  });

  it('sem ícone, a variante padrão fica como antes', () => {
    const html = renderToStaticMarkup(<Select rotulo="Local" opcoes={OPCOES} />);
    expect(html).not.toContain('location_on');
    expect(html).not.toContain(' pl-10');
  });
});
