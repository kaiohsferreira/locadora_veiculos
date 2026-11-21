import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { CabecalhoPainelFinanceiro } from './CabecalhoPainelFinanceiro';

describe('CabecalhoPainelFinanceiro', () => {
  it('mostra o rótulo, o título e o subtítulo do desenho', () => {
    const marcado = renderToStaticMarkup(<CabecalhoPainelFinanceiro carregandoPeriodo={false} />);

    expect(marcado).toContain('Gestão Financeira');
    expect(marcado).toContain('<h1');
    expect(marcado).toContain('Dashboard &amp; Contas a Receber');
    expect(marcado).toContain('Recebido, a receber, em atraso e ticket médio das cobranças da locadora.');
  });

  it('mostra a linha de período com as datas do servidor', () => {
    const marcado = renderToStaticMarkup(
      <CabecalhoPainelFinanceiro periodo={{ deStr: '01/11/2026', ateStr: '30/11/2026' }} carregandoPeriodo={false} />,
    );

    expect(marcado).toContain('Período: 01/11/2026 a 30/11/2026');
  });

  it('a pílula do período usa o canto de 4px (rounded), como o desenho (não rounded-md)', () => {
    const marcado = renderToStaticMarkup(
      <CabecalhoPainelFinanceiro periodo={{ deStr: '01/11/2026', ateStr: '30/11/2026' }} carregandoPeriodo={false} />,
    );

    expect(marcado).toContain('self-start rounded bg-branco');
    expect(marcado).not.toContain('rounded-md');
  });

  it('enquanto carrega, mostra o esqueleto no lugar do período', () => {
    const marcado = renderToStaticMarkup(<CabecalhoPainelFinanceiro carregandoPeriodo />);

    expect(marcado).toContain('animate-pulse');
    expect(marcado).not.toContain('Período:');
  });

  it('sem período e sem carregamento (erro), não mostra a linha de período', () => {
    const marcado = renderToStaticMarkup(<CabecalhoPainelFinanceiro carregandoPeriodo={false} />);

    expect(marcado).not.toContain('Período:');
    expect(marcado).not.toContain('animate-pulse');
  });
});
