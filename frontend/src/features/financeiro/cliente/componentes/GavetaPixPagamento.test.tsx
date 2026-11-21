import { describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ErroApi } from '@/types/api';
import type { CobrancaPix, MeuPagamento } from '@/types/pagamentos';
import { GavetaPixApresentacao, GavetaPixPagamento } from './GavetaPixPagamento';
import { termosCortadosPresentes } from './termosCortadosPix';

// A camada de API carrega o cliente HTTP, que usa `window` ao ser importado: no ambiente de teste ele é trocado por um mock.
vi.mock('@/api/client', () => ({ api: { get: vi.fn(), post: vi.fn() } }));

const PAGAMENTO: MeuPagamento = {
  idPagamento: 77,
  tipo: 'DIARIA',
  valor: 395.6,
  valorStr: 'R$ 395,60',
  dataVencimento: '2026-11-22',
  dataVencimentoStr: '22/11/2026',
  dataPagamentoStr: null,
  diasParaVencer: 2,
  situacao: 'PENDENTE',
  forma: null,
  idLocacao: 123,
  idVenda: null,
  veiculoDescricao: 'Jeep Renegade Longitude',
};

const COBRANCA: CobrancaPix = {
  idPagamento: 77,
  forma: 'PIX',
  valor: 395.6,
  valorStr: 'R$ 395,60',
  qrCodeBase64: 'iVBORw0KGgoAAAANSUhEUgAAAMgAAADICAIAAAAiOjnJ',
  copiaECola: '00020126580014BR.GOV.BCB.PIX0136FAKE-COPIA-PAG-77',
  expiraEmStr: '20/11/2026 15:30:00',
  segundosParaExpirar: 900,
};

interface EstadoTeste {
  nome: string;
  estado: 'ERRO' | 'GERANDO' | 'PRONTO';
  cobranca?: CobrancaPix;
  erro?: unknown;
}

/** Os quatro estados da gaveta: gerando, pronto, expirado (contagem em zero) e erro. */
const ESTADOS: readonly EstadoTeste[] = [
  { nome: 'gerando', estado: 'GERANDO' },
  { nome: 'pronto', estado: 'PRONTO', cobranca: COBRANCA },
  { nome: 'expirado', estado: 'PRONTO', cobranca: { ...COBRANCA, segundosParaExpirar: 0 } },
  { nome: 'erro', estado: 'ERRO', erro: new ErroApi({ status: 409, mensagem: 'Este pagamento já foi pago. Não é preciso gerar um PIX.' }) },
];

function gavetaNoEstado(teste: EstadoTeste): string {
  return renderToStaticMarkup(
    <GavetaPixApresentacao
      pagamento={PAGAMENTO}
      estado={teste.estado}
      cobranca={teste.cobranca}
      erro={teste.erro}
      chaveGeracao="geracao-1"
      aoGerarNovo={vi.fn()}
      aoFechar={() => undefined}
      refGatilho={{ current: null }}
    />,
  );
}

/** Marcação inicial do contêiner (antes de qualquer resposta: a geração ainda não começou no servidor). */
function marcacaoInicial(): string {
  return renderToStaticMarkup(
    <QueryClientProvider client={new QueryClient()}>
      <GavetaPixPagamento pagamento={PAGAMENTO} aoFechar={() => undefined} refGatilho={{ current: null }} />
    </QueryClientProvider>,
  );
}

describe('GavetaPixPagamento (contêiner da gaveta)', () => {
  it('abre com o título, a origem da cobrança no cabeçalho e o rótulo aprovado de fechar', () => {
    const html = marcacaoInicial();
    expect(html).toContain('Pagamento Seguro AutoVia');
    expect(html).toContain('Locação nº 123');
    expect(html).not.toContain('Cobrança nº');
    expect(html).toContain('aria-label="Fechar"');
  });

  it('começa no estado gerando, sem QR ainda', () => {
    const html = marcacaoInicial();
    expect(html).toContain('Gerando seu PIX...');
    expect(html).not.toContain('data:image/png;base64');
  });

  it('não mostra nenhum dos textos cortados pela decisão P4', () => {
    expect(termosCortadosPresentes(marcacaoInicial())).toEqual([]);
  });
});

describe('GavetaPixApresentacao (gaveta inteira, em cada estado)', () => {
  it.each(ESTADOS)('estado $nome: não mostra nenhum dos textos cortados pela decisão P4', (teste) => {
    expect(termosCortadosPresentes(gavetaNoEstado(teste))).toEqual([]);
  });

  it.each(ESTADOS)('estado $nome: a gaveta tem a largura do desenho (max-w-md) e o título', (teste) => {
    const html = gavetaNoEstado(teste);
    expect(html).toMatch(/<dialog[^>]*class="[^"]*max-w-md[^"]*"/);
    expect(html).toContain('Pagamento Seguro AutoVia');
  });

  it.each(ESTADOS)('estado $nome: a origem da cobrança fica no cabeçalho, logo abaixo do título', (teste) => {
    const html = gavetaNoEstado(teste);
    expect(html).toMatch(/<\/h2><div class="flex flex-col text-apoio text-texto-apoio"><span>Locação nº 123<\/span><\/div>/);
  });

  it.each(ESTADOS)('estado $nome: "Cobrança nº" não aparece na gaveta (o cabeçalho mostra só a origem)', (teste) => {
    expect(gavetaNoEstado(teste)).not.toContain('Cobrança nº');
  });

  it('estado pronto: o QR, a contagem e o copia e cola aparecem depois do valor', () => {
    const html = gavetaNoEstado(ESTADOS[1]);
    expect(html.indexOf('Valor da Fatura')).toBeLessThan(html.indexOf('data:image/png;base64'));
    expect(html).toContain('15:00');
    expect(html).toContain('Copiar');
  });

  it('estado expirado: diz que o código expirou e oferece gerar outro', () => {
    const html = gavetaNoEstado(ESTADOS[2]);
    expect(html).toContain('O código PIX expirou.');
    expect(html).toContain('Gerar novo código');
    expect(html).not.toContain('data:image/png;base64');
  });

  it('estado erro: mostra a mensagem da API e o botão Tentar novamente', () => {
    const html = gavetaNoEstado(ESTADOS[3]);
    expect(html).toContain('Este pagamento já foi pago. Não é preciso gerar um PIX.');
    expect(html).toContain('Tentar novamente');
  });
});
