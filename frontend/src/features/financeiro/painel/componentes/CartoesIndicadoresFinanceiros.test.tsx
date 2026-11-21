import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import type { RespostaIndicadoresFinanceiros } from '@/types/painelFinanceiro';
import { CartoesIndicadoresFinanceiros } from './CartoesIndicadoresFinanceiros';

const INDICADORES: RespostaIndicadoresFinanceiros = {
  de: '2026-11-01',
  ate: '2026-11-30',
  deStr: '01/11/2026',
  ateStr: '30/11/2026',
  recebidoValor: 3842.65,
  recebidoStr: 'R$ 3.842,65',
  quantidadeRecebido: 2,
  aReceberValor: 1194.2,
  aReceberStr: 'R$ 1.194,20',
  quantidadeAReceber: 142,
  emAtrasoValor: 186.45,
  emAtrasoStr: 'R$ 186,45',
  quantidadeEmAtraso: 24,
  ticketMedioValor: 1485,
  ticketMedioStr: 'R$ 1.485,00',
  quantidadeTicket: 3,
};

function renderizar(parcial: Partial<Parameters<typeof CartoesIndicadoresFinanceiros>[0]>): string {
  return renderToStaticMarkup(
    <CartoesIndicadoresFinanceiros
      indicadores={INDICADORES}
      carregando={false}
      erro={false}
      mensagemErro={null}
      tentando={false}
      aoTentarNovamente={() => undefined}
      {...parcial}
    />,
  );
}

describe('CartoesIndicadoresFinanceiros: com dados', () => {
  it('mostra os quatro títulos, os valores do servidor e as linhas de baixo', () => {
    const marcado = renderizar({});

    for (const texto of [
      'Recebido no Mês',
      'R$ 3.842,65',
      '2 pagamentos recebidos',
      'A receber no mês',
      'R$ 1.194,20',
      '142 títulos',
      'Em atraso',
      'R$ 186,45',
      '24 títulos vencidos',
      'Ticket médio',
      'R$ 1.485,00',
      'por cobrança paga',
    ]) {
      expect(marcado).toContain(texto);
    }
  });

  it('canto de 4px (rounded) no ícone e na linha de baixo, como o desenho (não rounded-md)', () => {
    const marcado = renderizar({});

    expect(marcado).toContain('shrink-0 rounded p-2');
    expect(marcado).toContain('relative mt-4 rounded p-2.5 pt-3');
    expect(marcado).not.toContain('rounded-md');
  });

  it('a linha de A receber leva o número de títulos em seminegrito navy, e as outras linhas ficam cinza', () => {
    const marcado = renderizar({});

    expect(marcado).toMatch(/text-micro font-semibold text-navy-profundo">[^<]*títulos<\/p>/);
    expect(marcado).toContain('text-micro text-texto-apoio">2 pagamentos recebidos</p>');
    expect(marcado).toContain('text-micro text-texto-apoio">por cobrança paga</p>');
  });

  it('usa o singular quando só há um título em atraso', () => {
    const marcado = renderizar({ indicadores: { ...INDICADORES, quantidadeEmAtraso: 1 } });

    expect(marcado).toContain('1 título vencido');
    expect(marcado).not.toContain('1 títulos');
  });

  it('o cartão de atraso leva o tom de alerta do desenho', () => {
    const marcado = renderizar({});

    expect(marcado).toContain('text-manutencao-alerta');
    expect(marcado).toContain('bg-manutencao-alerta-trilho');
  });

  it('não mostra o estado de carregamento quando há dados', () => {
    const marcado = renderizar({});

    expect(marcado).not.toContain('Carregando...');
    expect(marcado).not.toContain('animate-pulse');
  });
});

describe('CartoesIndicadoresFinanceiros: carregando', () => {
  it('mostra esqueletos e o texto de carregamento para leitor de tela, sem valores', () => {
    const marcado = renderizar({ indicadores: undefined, carregando: true });

    expect(marcado).toContain('Carregando...');
    expect(marcado).toContain('animate-pulse');
    expect(marcado).toContain('aria-busy="true"');
    expect(marcado).not.toContain('R$');
  });
});

describe('CartoesIndicadoresFinanceiros: erro', () => {
  it('mostra o erro com a mensagem recebida e o botão de tentar de novo', () => {
    const marcado = renderizar({
      indicadores: undefined,
      erro: true,
      mensagemErro: 'Não foi possível carregar as informações.',
    });

    expect(marcado).toContain('role="alert"');
    expect(marcado).toContain('Tentar novamente');
    expect(marcado).not.toContain('Recebido no Mês');
  });
});
