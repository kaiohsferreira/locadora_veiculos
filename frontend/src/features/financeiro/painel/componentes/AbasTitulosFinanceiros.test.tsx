import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { AbasTitulosFinanceiras } from './AbasTitulosFinanceiros';

const CONTADORES = { quantidadeTodos: 166, quantidadeEmAtraso: 24, quantidadeVenceHoje: 18 };

describe('AbasTitulosFinanceiras', () => {
  it('mostra as quatro abas do desenho, com contador nas três primeiras', () => {
    const marcado = renderToStaticMarkup(
      <AbasTitulosFinanceiras abaAtual="TODOS" contadores={CONTADORES} aoMudarAba={() => undefined} />,
    );

    expect(marcado).toContain('Todos os Títulos (166)');
    expect(marcado).toContain('Em Atraso (24)');
    expect(marcado).toContain('A Vencer Hoje (18)');
    expect(marcado).toContain('Pagos Recentemente');
    expect(marcado).not.toContain('Pagos Recentemente (');
  });

  it('marca só a aba atual como pressionada', () => {
    const marcado = renderToStaticMarkup(
      <AbasTitulosFinanceiras abaAtual="EM_ATRASO" contadores={CONTADORES} aoMudarAba={() => undefined} />,
    );

    expect(marcado.match(/aria-pressed="true"/g)).toHaveLength(1);
    expect(marcado).toMatch(/aria-pressed="true"[^>]*>[^<]*<span[^>]*><\/span>Em Atraso \(24\)/);
  });

  it('a faixa das abas usa o canto de 4px (rounded); os botões das abas ficam em rounded-md, como no desenho', () => {
    const marcado = renderToStaticMarkup(
      <AbasTitulosFinanceiras abaAtual="TODOS" contadores={CONTADORES} aoMudarAba={() => undefined} />,
    );

    expect(marcado).toContain('flex flex-wrap items-center gap-1 rounded bg-steel-fundo p-1');
    expect(marcado).toContain('inline-flex items-center gap-1 rounded-md px-3 py-1.5');
  });

  it('sem contadores (carregando), mostra só os rótulos', () => {
    const marcado = renderToStaticMarkup(
      <AbasTitulosFinanceiras abaAtual="TODOS" contadores={undefined} aoMudarAba={() => undefined} />,
    );

    expect(marcado).toContain('Todos os Títulos</button>');
    expect(marcado).toContain('Em Atraso</button>');
    expect(marcado).not.toContain('(166)');
  });
});
