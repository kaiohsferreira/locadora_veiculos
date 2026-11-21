import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter } from 'react-router-dom';
import { AvisoProvider } from '@/components/ui';
import { useCatalogoSeminovos } from '@/features/vendas/hooks/useCatalogoSeminovos';
import { formatarMoeda } from '@/lib/formatters';
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
    cor: null,
    kmAtual: 30000,
    categoriaNome: 'SUV',
    valorVenda: 80000 + id,
    valorVendaStr: '',
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

  it('mostra o total, os cartões com preço e o link de cada carro para a proposta', () => {
    const html = renderizar(consulta({ data: pagina([veiculo(4), veiculo(5)], 37) }));

    expect(html).toContain('Seminovos');
    expect(html).toContain('37 veículos à venda');
    expect(html).toContain('Jeep Renegade 4');
    expect(html).toContain(formatarMoeda(80004));
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
    expect(html).toContain('Filtros (2)');
    expect(html).toContain('1 veículo à venda');
  });

  it('com erroFotoCapa, o cartão mostra o aviso do servidor', () => {
    const html = renderizar(
      consulta({ data: pagina([veiculo(4, { erroFotoCapa: 'A foto do veículo está indisponível no momento.' })], 1) }),
    );

    expect(html).toContain('A foto do veículo está indisponível no momento.');
  });

  it('mostra a paginação quando há mais de uma página', () => {
    const html = renderizar(consulta({ data: pagina([veiculo(1)], 45) }));

    expect(html).toContain('45');
    expect(html).toContain('veículos');
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
    expect(html).not.toContain('veículos à venda');
  });

  it('falha de conexão mostra o estado de erro com nova tentativa', () => {
    const html = renderizar(consulta({ isError: true, error: new ErroApi({ status: 0, mensagem: 'Sem conexão.' }) }));

    expect(html).toContain('Sem conexão.');
    expect(html).toContain('Tentar novamente');
  });
});
