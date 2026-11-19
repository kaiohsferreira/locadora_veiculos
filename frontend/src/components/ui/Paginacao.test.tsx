import { describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { Paginacao } from './Paginacao';

// O componente não chama a API: o cliente HTTP só precisa existir se algo o importar.
vi.mock('@/api/client', () => ({
  api: { get: vi.fn(), post: vi.fn(), put: vi.fn() },
}));

const BASE = { pagina: 1, tamanhoPagina: 10, total: 25, aoMudarPagina: () => undefined };

describe('Paginacao', () => {
  it('sem complemento, mostra só o resumo e os botões (como antes)', () => {
    const marcado = renderToStaticMarkup(<Paginacao {...BASE} rotuloItens="veículos" />);

    expect(marcado).toContain('Exibindo');
    expect(marcado).toContain('de');
    expect(marcado).toContain('veículos');
    expect(marcado).not.toContain('•');
  });

  it('com complemento, o conteúdo extra vai ao lado do resumo, separado por um ponto', () => {
    const marcado = renderToStaticMarkup(
      <Paginacao {...BASE} rotuloItens="veículos" complemento={<span>Linhas por página: 10</span>} />,
    );

    expect(marcado).toContain('Linhas por página: 10');
    expect(marcado).toContain('•');
    expect(marcado.indexOf('Exibindo')).toBeLessThan(marcado.indexOf('Linhas por página: 10'));
  });

  it('variante frota: intervalo com espaços em volta do travessão, como no desenho da tela 11', () => {
    const marcado = renderToStaticMarkup(<Paginacao {...BASE} variante="frota" rotuloItens="veículos cadastrados" />);

    expect(marcado).toContain('1 – 10');
    expect(marcado).toContain('bg-steel-fundo');
  });

  it('variante frota: o rodapé é uma faixa de fundo, sem linha de borda no topo (classes da navegação)', () => {
    const marcado = renderToStaticMarkup(<Paginacao {...BASE} variante="frota" />);

    expect(marcado).toMatch(
      /<nav[^>]*class="flex flex-col gap-2 bg-steel-fundo px-4 py-2 sm:flex-row sm:items-center sm:justify-between"/,
    );
    expect(marcado).not.toMatch(/<nav[^>]*class="[^"]*border-t/);
  });

  it('variante frota com total zero: não repete "Nenhum registro encontrado." (a tabela já mostra o vazio)', () => {
    const marcado = renderToStaticMarkup(<Paginacao {...BASE} total={0} variante="frota" />);

    expect(marcado).not.toContain('Nenhum registro encontrado.');
    expect(marcado).toContain('aria-label="Página anterior"');
  });

  it('variante frota com total zero e complemento: o seletor de linhas continua, sem resumo nem separador', () => {
    const marcado = renderToStaticMarkup(
      <Paginacao {...BASE} total={0} variante="frota" complemento={<span>Linhas por página: 10</span>} />,
    );

    expect(marcado).toContain('Linhas por página: 10');
    expect(marcado).not.toContain('•');
    expect(marcado).not.toContain('Nenhum registro encontrado.');
  });

  it('variante padrão com total zero continua mostrando "Nenhum registro encontrado." (as outras listas não mudam)', () => {
    const marcado = renderToStaticMarkup(<Paginacao {...BASE} total={0} />);

    expect(marcado).toContain('Nenhum registro encontrado.');
  });

  it('variante padrão: intervalo sem espaços, como antes', () => {
    const marcado = renderToStaticMarkup(<Paginacao {...BASE} />);

    expect(marcado).toContain('1–10');
    expect(marcado).not.toContain('bg-steel-fundo');
  });

  it('página atual marcada com aria-current', () => {
    const marcado = renderToStaticMarkup(<Paginacao {...BASE} pagina={2} />);

    expect(marcado).toContain('aria-current="page"');
    expect(marcado).toContain('aria-label="Página 2"');
  });
});
