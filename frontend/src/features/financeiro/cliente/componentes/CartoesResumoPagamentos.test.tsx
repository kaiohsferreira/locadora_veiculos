import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import type { ResumoMeusPagamentos } from '@/types/pagamentos';
import { CartoesResumoPagamentos } from './CartoesResumoPagamentos';
import { FaixaPendenciaPagamentos } from './FaixaPendenciaPagamentos';

const RESUMO: ResumoMeusPagamentos = {
  anoExercicio: 2026,
  totalLiquidadoAno: 4763.85,
  totalLiquidadoAnoStr: 'R$ 4.763,85',
  quantidadeQuitadasAno: 4,
  totalPendente: 395.6,
  totalPendenteStr: 'R$ 395,60',
  quantidadePendentes: 1,
  quantidadeAbaPendentes: 1,
  quantidadeAbaPagos: 4,
};

/** Todo botão que contém o texto de pagar ou regularizar precisa estar desligado. */
function botoesSemDesligar(html: string): string[] {
  const botoes = html.match(/<button(?![^>]*(?<![-\w])disabled)[^>]*>[\s\S]*?<\/button>/g) ?? [];
  return botoes.filter((botao) => /Pagar|Regularizar/.test(botao));
}

describe('CartoesResumoPagamentos (tela 30)', () => {
  it('mostra os dois cartões com os valores como vieram do servidor', () => {
    const html = renderToStaticMarkup(<CartoesResumoPagamentos resumo={RESUMO} />);
    expect(html).toContain('Total Liquidado no Ano');
    expect(html).toContain('R$ 4.763,85');
    expect(html).toContain('4 cobranças quitadas');
    expect(html).toContain('Exercício 2026');
    expect(html).toContain('Cobranças Pendentes');
    expect(html).toContain('R$ 395,60');
    expect(html).toContain('1 fatura aguardando regularização');
    expect(html).not.toContain('Forma Padrão');
  });

  it('botão Regularizar aparece desligado quando há pendente', () => {
    const html = renderToStaticMarkup(<CartoesResumoPagamentos resumo={RESUMO} />);
    expect(html).toContain('Regularizar');
    expect(html).toContain('Pagamento online disponível em breve.');
    expect(botoesSemDesligar(html)).toEqual([]);
  });

  it('botão Regularizar vem desligado e com opacity-60', () => {
    const html = renderToStaticMarkup(<CartoesResumoPagamentos resumo={RESUMO} />);
    const botao = (html.match(/<button[^>]*>[\s\S]*?<\/button>/g) ?? []).find((b) => b.includes('Regularizar')) ?? '';
    expect(botao).toMatch(/<button[^>]*\sdisabled[\s=>]/);
    expect(botao).toMatch(/aria-disabled="true"/);
    expect(botao).toMatch(/class="[^"]*\bopacity-60\b[^"]*"/);
  });

  it('com zero pendente: valor zerado, sem botão e sem aviso', () => {
    const html = renderToStaticMarkup(
      <CartoesResumoPagamentos
        resumo={{ ...RESUMO, quantidadePendentes: 0, totalPendenteStr: 'R$ 0,00', totalPendente: 0 }}
      />,
    );
    expect(html).toContain('R$ 0,00');
    expect(html).toContain('Nenhuma fatura aguardando regularização');
    expect(html).not.toContain('Regularizar');
  });

  it('com 1 cobrança quitada usa o singular', () => {
    const html = renderToStaticMarkup(
      <CartoesResumoPagamentos resumo={{ ...RESUMO, quantidadeQuitadasAno: 1 }} />,
    );
    expect(html).toContain('1 cobrança quitada');
    expect(html).not.toContain('1 cobranças');
  });

  it('carregando: esqueleto, sem valores', () => {
    const html = renderToStaticMarkup(<CartoesResumoPagamentos resumo={null} />);
    expect(html).toContain('animate-pulse');
    expect(html).not.toContain('R$');
  });
});

describe('FaixaPendenciaPagamentos (tela 30)', () => {
  it('sem pendente a faixa não aparece', () => {
    const html = renderToStaticMarkup(<FaixaPendenciaPagamentos quantidadePendentes={0} totalPendenteStr="R$ 0,00" />);
    expect(html).toBe('');
  });

  it('com pendente: texto no singular e botão Pagar Fatura desligado', () => {
    const html = renderToStaticMarkup(<FaixaPendenciaPagamentos quantidadePendentes={1} totalPendenteStr="R$ 395,60" />);
    expect(html).toContain('Você possui 1 cobrança pendente.');
    expect(html).toContain('Pagar Fatura R$ 395,60');
    expect(html).toContain('Pagamento online disponível em breve.');
    expect(botoesSemDesligar(html)).toEqual([]);
  });

  it('botão Pagar Fatura vem desligado e com opacity-60', () => {
    const html = renderToStaticMarkup(<FaixaPendenciaPagamentos quantidadePendentes={1} totalPendenteStr="R$ 395,60" />);
    const botao = (html.match(/<button[^>]*>[\s\S]*?<\/button>/g) ?? []).find((b) => b.includes('Pagar Fatura')) ?? '';
    expect(botao).toMatch(/<button[^>]*\sdisabled[\s=>]/);
    expect(botao).toMatch(/aria-disabled="true"/);
    expect(botao).toMatch(/class="[^"]*\bopacity-60\b[^"]*"/);
  });

  it('com várias pendências: texto no plural', () => {
    const html = renderToStaticMarkup(<FaixaPendenciaPagamentos quantidadePendentes={3} totalPendenteStr="R$ 900,00" />);
    expect(html).toContain('Você possui 3 cobranças pendentes.');
  });
});
