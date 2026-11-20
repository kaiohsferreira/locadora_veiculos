import { describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { CabecalhoManutencao } from './CabecalhoManutencao';

describe('CabecalhoManutencao', () => {
  const marcado = renderToStaticMarkup(<CabecalhoManutencao aoAbrirOrdem={vi.fn()} />);

  it('a trilha usa a cor de contorno do desenho (html:8), não o texto de apoio', () => {
    expect(marcado).toMatch(/<p class="flex flex-wrap[^"]*text-contorno/);
  });

  it('tem o título, a descrição sem os termos cortados e o botão Abrir ordem', () => {
    expect(marcado).toContain('Manutenção da Frota');
    expect(marcado).toContain('Alertas de revisão por quilometragem e controle das ordens de manutenção da frota.');
    expect(marcado).toContain('Abrir ordem');
    expect(marcado).not.toContain('preditivos');
    expect(marcado).not.toContain('Exportar Relatório');
    expect(marcado).not.toContain('Agendar manutenção');
  });
});
