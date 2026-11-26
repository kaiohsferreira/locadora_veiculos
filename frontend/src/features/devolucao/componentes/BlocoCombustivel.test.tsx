import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { VALOR_AUSENTE } from '@/lib/formatters';
import { MENSAGENS_DEVOLUCAO as M } from '@/lib/mensagensDevolucao';
import { BlocoCombustivel } from './BlocoCombustivel';

describe('bloco de combustível da tela 06 (desenho 06, N1 e N10 item 8)', () => {
  it('retirada: selo com o oitavo, o nome do ponto e o percentual exato, e a escala Vazio e 4/8', () => {
    const html = renderToStaticMarkup(<BlocoCombustivel titulo={M.combustivelTitulo} nivel={0.5} comEscala />);
    expect(html).toContain('4/8 - 1/2 Tanque (50%)');
    expect(html).toContain(M.escalaVazio);
    expect(html).toContain(M.escalaMeio);
    expect(html).toContain('local_gas_station');
    expect(html).not.toContain('bg-orange-acao');
  });

  it('devolução com divergência: selo laranja sem percentual e ícone de alerta', () => {
    const html = renderToStaticMarkup(<BlocoCombustivel titulo={M.combustivelDivergenciaTitulo} nivel={0.75} divergente />);
    expect(html).toContain('6/8 - 3/4 de Tanque');
    expect(html).not.toContain('(75%)');
    expect(html).toContain('bg-orange-acao');
    expect(html).toContain('warning');
    expect(html).not.toContain(M.escalaVazio);
  });

  it('oitavo sem nome mostra só a fração e o percentual exato, como 3/8 (33%)', () => {
    const html = renderToStaticMarkup(<BlocoCombustivel titulo={M.combustivelTitulo} nivel={0.33} />);
    expect(html).toContain('3/8 (33%)');
  });

  it('divergência sem nome mostra só a fração, sem percentual', () => {
    const html = renderToStaticMarkup(<BlocoCombustivel titulo={M.combustivelDivergenciaTitulo} nivel={0.6} divergente />);
    expect(html).toContain('5/8');
    expect(html).not.toContain('60%');
  });

  it('sem nível, mostra o valor ausente e nenhuma régua', () => {
    const html = renderToStaticMarkup(<BlocoCombustivel titulo={M.combustivelTitulo} nivel={null} />);
    expect(html).toContain(VALOR_AUSENTE);
    expect(html).not.toContain('grid-cols-8');
  });

  it('o rodapé (divergência e reposição) aparece embaixo da régua', () => {
    const html = renderToStaticMarkup(
      <BlocoCombustivel titulo={M.combustivelDivergenciaTitulo} nivel={0.75} divergente rodape={<p>{M.reposicao('R$ 117,00')}</p>} />,
    );
    expect(html.indexOf('grid-cols-8')).toBeLessThan(html.indexOf(M.reposicao('R$ 117,00')));
  });
});
