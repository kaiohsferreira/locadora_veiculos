import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter } from 'react-router-dom';
import { AvisoProvider } from '@/components/ui';
import { useCatalogoSeminovos } from '@/features/vendas/hooks/useCatalogoSeminovos';
import { ErroApi } from '@/types/api';
import type { FiltroCatalogo, RespostaCatalogo, VeiculoAVenda } from '@/types/vendas';
import CatalogoSeminovosPagina from './CatalogoSeminovosPagina';

// Nenhum teste chama a API: o cliente HTTP vira dublê e o hook devolve consultas prontas.
vi.mock('@/api/client', () => ({
  api: { get: vi.fn() },
}));
vi.mock('@/features/vendas/hooks/useCatalogoSeminovos', () => ({ useCatalogoSeminovos: vi.fn() }));

type ConsultaCatalogo = ReturnType<typeof useCatalogoSeminovos>;

function veiculo(id: number, parcial: Partial<VeiculoAVenda> = {}): VeiculoAVenda {
  return {
    idVeiculo: id,
    marca: 'Jeep',
    modelo: `Renegade ${id}`,
    anoFabricacao: 2022,
    anoModelo: 2023,
    cor: 'Preto',
    kmAtual: 30000,
    categoriaNome: 'SUV',
    valorVenda: 80000 + id,
    valorVendaStr: `R$ 80.00${id},00`,
    locadoraNomeFantasia: 'Locadora Um',
    locadoraCidade: 'Campinas',
    locadoraUf: 'SP',
    fotoCapa: null,
    erroFotoCapa: null,
    ...parcial,
  };
}

function pagina(itens: VeiculoAVenda[], total: number, numero = 1): RespostaCatalogo {
  return { itens, total, pagina: numero, tamanhoPagina: 20 };
}

function consulta(parcial: Record<string, unknown>): ConsultaCatalogo {
  return {
    data: undefined,
    error: null,
    isPending: false,
    isError: false,
    isFetching: false,
    isPlaceholderData: false,
    refetch: vi.fn(),
    ...parcial,
  } as unknown as ConsultaCatalogo;
}

function renderizar(resultado: ConsultaCatalogo, rota = '/seminovos'): string {
  vi.mocked(useCatalogoSeminovos).mockReturnValue(resultado);
  return renderToStaticMarkup(
    <MemoryRouter initialEntries={[rota]}>
      <AvisoProvider>
        <CatalogoSeminovosPagina />
      </AvisoProvider>
    </MemoryRouter>,
  );
}

/** Filtro que a tela pediu ao hook na última renderização. */
function filtroPedido(): FiltroCatalogo {
  const chamadas = vi.mocked(useCatalogoSeminovos).mock.calls;
  return chamadas[chamadas.length - 1][0];
}

describe('CatalogoSeminovosPagina (P6-FE-01)', () => {
  beforeEach(() => vi.mocked(useCatalogoSeminovos).mockReset());

  it('mostra a faixa de destaque com o título e o total de veículos', () => {
    const html = renderizar(consulta({ data: pagina([veiculo(4), veiculo(5)], 148) }));

    expect(html).toContain('Seminovos Exclusivos da Frota AutoVia');
    expect(html).toMatch(/data-testid="total-estoque"[^>]*>148</);
    expect(html).toContain('Veículos em estoque');
  });

  it('mostra "N veículos em destaque · Ordenado por: Menor Preço" com os carros da página', () => {
    const html = renderizar(consulta({ data: pagina([veiculo(4), veiculo(5)], 148) }));

    expect(html).toMatch(/data-testid="em-destaque"[^>]*>2 veículos em destaque</);
    expect(html).toContain('Ordenado por:');
    expect(html).toContain('Menor Preço');
  });

  it('a barra "Filtros Avançados" abre os campos de marca, preço, ano e km', () => {
    const html = renderizar(consulta({ data: pagina([veiculo(4)], 1) }));

    expect(html).toMatch(/aria-expanded="false"[^>]*>.*Filtros Avançados/s);
    expect(html).toContain('id="catalogo-filtros-avancados" hidden=""');
    for (const rotulo of ['Marca', 'Faixa de preço (R$)', 'Ano do modelo', 'Quilometragem máxima (km)']) {
      expect(html).toContain(rotulo);
    }
  });

  it('o cartão tem a marca em caixa alta acima do modelo, o ano fabricação/modelo, a cor e "Preço à vista" com o texto do servidor', () => {
    const html = renderizar(
      consulta({ data: pagina([veiculo(4, { anoFabricacao: 2023, anoModelo: 2023, valorVendaStr: 'R$ 78.900,00' })], 1) }),
    );

    expect(html).toMatch(/data-testid="marca-cartao"[^>]*>Jeep<\/span><h2[^>]*>Renegade 4<\/h2>/);
    expect(html).toContain('uppercase');
    expect(html).toContain('2023/2023');
    expect(html).toContain('Cor Preto');
    expect(html).toMatch(/Preço à vista<\/span><p[^>]*>R\$ 78\.900,00<\/p>/);
    expect(html).toContain('30.000 km');
  });

  it('a grade tem 3 colunas e cada cartão leva à proposta', () => {
    const html = renderizar(consulta({ data: pagina([veiculo(4), veiculo(5)], 37) }));

    expect(html).toContain('lg:grid-cols-3');
    expect(html).toContain('href="/seminovos/4/proposta"');
    expect(html).toContain('href="/seminovos/5/proposta"');
    expect(html).toContain('Simular e fazer proposta');
  });

  it('pede ao servidor a página e os filtros do endereço, com 20 por página', () => {
    renderizar(consulta({ data: pagina([], 0) }), '/seminovos?marca=jeep&precoMaximo=100000&pagina=2');

    expect(filtroPedido()).toEqual({ pagina: 2, tamanhoPagina: 20, marca: 'jeep', precoMaximo: 100000 });
  });

  it('mostra as etiquetas dos filtros ativos e o contador no botão do celular', () => {
    const html = renderizar(consulta({ data: pagina([veiculo(4)], 1) }), '/seminovos?marca=jeep&kmMaximo=50000');

    expect(html).toContain('Marca: jeep');
    expect(html).toContain('Até 50.000 km');
    expect(html).toContain('Filtros Avançados (2)');
    expect(html).toContain('1 veículo em destaque');
  });

  it('com erroFotoCapa, o cartão mostra o aviso do servidor', () => {
    const html = renderizar(
      consulta({ data: pagina([veiculo(4, { erroFotoCapa: 'A foto do veículo está indisponível no momento.' })], 1) }),
    );

    expect(html).toContain('A foto do veículo está indisponível no momento.');
  });

  it('o texto da paginação diz o intervalo da página e o total', () => {
    const itens = Array.from({ length: 20 }, (_, indice) => veiculo(indice + 1));
    const html = renderizar(consulta({ data: pagina(itens, 45, 2) }), '/seminovos?pagina=2');

    expect(html).toMatch(/Exibindo <span[^>]*>21–40<\/span> de <span[^>]*>45<\/span> seminovos disponíveis/);
    expect(html).toContain('aria-label="Página 3"');
  });

  it('com ?pagina= além da última, não diz que o catálogo está vazio: carrega a última página', () => {
    const html = renderizar(consulta({ data: pagina([], 45, 99) }), '/seminovos?pagina=99');

    expect(filtroPedido().pagina).toBe(99);
    expect(html).not.toContain('Nenhum seminovo à venda');
    expect(html).not.toContain('Nenhum seminovo encontrado');
    expect(html).toContain('aria-busy="true"');
  });

  it('sem resultado com filtro, sugere limpar os filtros', () => {
    const html = renderizar(consulta({ data: pagina([], 0) }), '/seminovos?marca=ferrari');

    expect(html).toContain('Nenhum seminovo encontrado com esses filtros');
    expect(html).toContain('Limpar filtros');
  });

  it('catálogo vazio sem filtro tem outra mensagem', () => {
    const html = renderizar(consulta({ data: pagina([], 0) }));

    expect(html).toContain('Nenhum seminovo à venda no momento');
  });

  it('enquanto a primeira página carrega, mostra os cartões cinzas', () => {
    const html = renderizar(consulta({ isPending: true }));

    expect(html).toContain('aria-busy="true"');
    expect(html).not.toContain('em destaque');
  });

  it('falha de conexão mostra o estado de erro com nova tentativa', () => {
    const html = renderizar(consulta({ isError: true, error: new ErroApi({ status: 0, mensagem: 'Sem conexão.' }) }));

    expect(html).toContain('Sem conexão.');
    expect(html).toContain('Tentar novamente');
  });
});
