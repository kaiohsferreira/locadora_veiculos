import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MedidorCombustivel } from './MedidorCombustivel';

describe('MedidorCombustivel', () => {
  it('meio tanque acende 4 de 8 segmentos e mostra o texto com o nome e o percentual', () => {
    const html = renderToStaticMarkup(<MedidorCombustivel nivel={0.5} />);

    expect(html.split('bg-navy-profundo').length - 1).toBe(4);
    expect(html.split('bg-steel-alto').length - 1).toBe(4);
    expect(html).toContain('4/8 • 1/2 Tanque (50%)');
  });

  it('os segmentos são decorativos (aria-hidden) e o transition respeita motion-reduce', () => {
    const html = renderToStaticMarkup(<MedidorCombustivel nivel={1} />);

    expect(html).toContain('aria-hidden="true"');
    expect(html).toContain('motion-reduce:transition-none');
    expect(html.split('<span').length - 1).toBe(8);
  });

  it('aceita o separador da tela 06', () => {
    const html = renderToStaticMarkup(<MedidorCombustivel nivel={0} separador=" - " />);

    expect(html).toContain('0/8 - Tanque Vazio (0%)');
  });
});
