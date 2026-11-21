import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { AvisoProvider } from '@/components/ui';
import type { TituloFinanceiro } from '@/types/painelFinanceiro';
import { useBaixarPagamento } from '@/features/financeiro/hooks/usePainelFinanceiro';
import { ModalDarBaixa } from './ModalDarBaixa';

// Nenhum teste chama a API: a mutação vira dublê.
vi.mock('@/features/financeiro/hooks/usePainelFinanceiro', () => ({
  useBaixarPagamento: vi.fn(),
}));

const useBaixarMock = vi.mocked(useBaixarPagamento);

const TITULO: TituloFinanceiro = {
  idPagamento: 77,
  origem: 'LOCACAO',
  origemStr: 'Locação nº 12',
  tipo: 'DIARIA',
  tipoStr: 'Diária',
  clienteNome: 'Rodrigo Alcantara Dias',
  clienteCpfMascarado: null,
  valor: 980,
  valorStr: 'R$ 980,00',
  dataVencimentoStr: '12/11/2026',
  diasParaVencer: -6,
  situacao: 'ATRASADO',
  dataPagamentoStr: null,
  forma: null,
  podeDarBaixa: true,
};

describe('ModalDarBaixa', () => {
  beforeEach(() => {
    useBaixarMock.mockReset();
    useBaixarMock.mockReturnValue({ mutate: vi.fn(), isPending: false } as unknown as ReturnType<typeof useBaixarPagamento>);
  });

  function renderizar(): string {
    return renderToStaticMarkup(
      <AvisoProvider>
        <ModalDarBaixa titulo={TITULO} aoFechar={() => undefined} />
      </AvisoProvider>,
    );
  }

  it('abre o diálogo com o título, a forma obrigatória e as cinco formas da lista', () => {
    const marcado = renderizar();

    expect(marcado).toContain('<dialog');
    expect(marcado).toContain('Dar baixa no pagamento');
    expect(marcado).toContain('Forma de pagamento');
    expect(marcado).toContain('aria-required="true"');
    for (const rotulo of ['PIX', 'Cartão', 'Boleto', 'Dinheiro', 'Transferência']) {
      expect(marcado).toContain(`>${rotulo}</option>`);
    }
  });

  it('tem os botões Cancelar e Confirmar baixa', () => {
    const marcado = renderizar();

    expect(marcado).toContain('>Cancelar</button>');
    expect(marcado).toContain('>Confirmar baixa</button>');
  });

  it('a caixa do PIX gerado só aparece depois da recusa do servidor (não aparece de início)', () => {
    expect(renderizar()).not.toContain('Confirmo a baixa mesmo com o PIX gerado.');
  });

  it('mostra no corpo a origem, o cliente e o valor da linha recebida, separados por " · "', () => {
    const texto = textoSemTags(renderizar());

    expect(texto).toContain('Locação nº 12 · Rodrigo Alcantara Dias · R$ 980,00');
  });

  it('com outra linha, mostra os dados dessa linha e não os da primeira', () => {
    const outra: TituloFinanceiro = {
      ...TITULO,
      idPagamento: 55,
      origem: 'VENDA',
      origemStr: 'Venda nº 55',
      clienteNome: 'Ana Paula Souza',
      valorStr: 'R$ 1.250,00',
    };
    const texto = textoSemTags(
      renderToStaticMarkup(
        <AvisoProvider>
          <ModalDarBaixa titulo={outra} aoFechar={() => undefined} />
        </AvisoProvider>,
      ),
    );

    expect(texto).toContain('Venda nº 55 · Ana Paula Souza · R$ 1.250,00');
    expect(texto).not.toContain('Locação nº 12');
    expect(texto).not.toContain('Rodrigo Alcantara Dias');
  });

  it('o resumo da cobrança aparece antes do campo Forma de pagamento', () => {
    const texto = textoSemTags(renderizar());
    const posicaoResumo = texto.indexOf('Locação nº 12 · Rodrigo Alcantara Dias · R$ 980,00');
    const posicaoForma = texto.indexOf('Forma de pagamento');

    expect(posicaoResumo).toBeGreaterThan(-1);
    expect(posicaoResumo).toBeLessThan(posicaoForma);
  });
});

/** Texto visível do marcado: tira as tags, para conferir a frase inteira mesmo com partes em elementos separados. */
function textoSemTags(marcado: string): string {
  return marcado.replace(/<[^>]+>/g, '');
}
