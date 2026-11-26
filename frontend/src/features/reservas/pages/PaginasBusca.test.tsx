import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { StaticRouter } from 'react-router-dom/server.js';
import { ErroApi, type Pagina } from '@/types/api';
import type { VeiculoDisponivel } from '@/types/veiculos';
import { useCategoriasVeiculos, useCidadesLocadoras, useVeiculosDisponiveis } from '../hooks/useVeiculosDisponiveis';
import InicioPagina from './InicioPagina';
import ResultadosBuscaPagina from './ResultadosBuscaPagina';

vi.mock('../hooks/useVeiculosDisponiveis', () => ({
  useCategoriasVeiculos: vi.fn(),
  useCidadesLocadoras: vi.fn(),
  useVeiculosDisponiveis: vi.fn(),
}));

const PERIODO = 'cidade=S%C3%A3o+Paulo&uf=SP&dataRetirada=2099-06-10&dataDevolucao=2099-06-13';
const VEICULO: VeiculoDisponivel = {
  idVeiculo: 7,
  idCategoria: 3,
  categoriaNome: 'Econômico',
  marca: 'Fiat',
  modelo: 'Argo',
  anoModelo: 2024,
  cor: 'Prata',
  valorDiaria: 129.9,
  valorDiariaStr: 'R$ 129,90',
  locadoraNomeFantasia: 'Locadora Paulista',
  locadoraCidade: 'São Paulo',
  locadoraUf: 'SP',
};
const ERRO_FOTO = 'Foto indisponível no momento.';

function pagina(itens: VeiculoDisponivel[]): Pagina<VeiculoDisponivel> {
  return { itens, total: itens.length, pagina: 1, tamanhoPagina: 12 };
}

function simularDisponibilidade(parcial: Partial<ReturnType<typeof useVeiculosDisponiveis>>) {
  vi.mocked(useVeiculosDisponiveis).mockReturnValue({
    isPending: false, isError: false, isFetching: false, refetch: vi.fn(), ...parcial,
  } as ReturnType<typeof useVeiculosDisponiveis>);
}

function renderizarInicio() {
  return renderToStaticMarkup(<StaticRouter location="/"><InicioPagina /></StaticRouter>);
}

function renderizarResultados(consulta = PERIODO) {
  return renderToStaticMarkup(
    <StaticRouter location={`/veiculos?${consulta}`}>
      <ResultadosBuscaPagina />
    </StaticRouter>,
  );
}

beforeEach(() => {
  vi.clearAllMocks();
  vi.mocked(useCidadesLocadoras).mockReturnValue({
    data: [{ cidade: 'São Paulo', uf: 'SP' }, { cidade: 'Rio de Janeiro', uf: 'RJ' }],
  } as ReturnType<typeof useCidadesLocadoras>);
  vi.mocked(useCategoriasVeiculos).mockReturnValue({
    data: { itens: [{ idCategoria: 3, nome: 'Econômico', descricao: 'Compactos', valorDiariaBase: 100, valorDiariaBaseStr: 'R$ 100,00' }], total: 1, pagina: 1, tamanhoPagina: 100 },
  } as ReturnType<typeof useCategoriasVeiculos>);
  simularDisponibilidade({ data: pagina([]) });
});

describe('InicioPagina', () => {
  it('mostra o formulário, a opção de compra e links de categorias carregadas da API', () => {
    const html = renderizarInicio();
    expect(html).toContain('Alugue ou compre seu carro em minutos');
    expect(html).toContain('Alugar Veículo');
    expect(html).toContain('Comprar Seminovo');
    expect(html).toContain('href="/seminovos"');
    expect(html).toContain('Local de retirada');
    expect(html).toContain('São Paulo – SP');
    expect(html).toContain('Data de retirada');
    expect(html).toContain('Data de devolução');
    expect(html).toContain('Conheça nossas categorias de veículos');
    expect(html).toContain('Econômico');
    expect(html).toContain('href="/veiculos?idCategoria=3"');
    expect(html).not.toContain('R$ 100,00');
  });

  it('mantém o formulário durante carregamento e falha de categorias', () => {
    vi.mocked(useCategoriasVeiculos).mockReturnValue({ isPending: true } as ReturnType<typeof useCategoriasVeiculos>);
    expect(renderizarInicio()).toContain('Carregando categorias');
    vi.mocked(useCategoriasVeiculos).mockReturnValue({ isError: true } as ReturnType<typeof useCategoriasVeiculos>);
    vi.mocked(useCidadesLocadoras).mockReturnValue({ isError: true } as ReturnType<typeof useCidadesLocadoras>);
    const html = renderizarInicio();
    expect(html).toContain('Não foi possível carregar as categorias.');
    expect(html).toContain('Não foi possível carregar as cidades.');
    expect(html).toContain('Buscar veículos');
  });
});

describe('ResultadosBuscaPagina', () => {
  it('exibe os filtros mesmo sem um período e desativa a consulta', () => {
    const html = renderizarResultados('idCategoria=3');
    expect(useVeiculosDisponiveis).toHaveBeenCalledWith(expect.objectContaining({ idCategoria: 3 }), false);
    expect(html).toContain('Informe um período para começar');
    expect(html).toContain('Todos os Grupos');
    expect(html).toContain('Limpar tudo');
    expect(html).toContain('type="range"');
  });

  it('mostra o estado de carregamento e consulta com o par cidade/UF', () => {
    simularDisponibilidade({ isPending: true });
    const html = renderizarResultados();
    expect(useVeiculosDisponiveis).toHaveBeenCalledWith(expect.objectContaining({ cidade: 'São Paulo', uf: 'SP' }), true);
    expect(html).toContain('aria-busy="true"');
    expect(html).toContain('Carregando veículos');
  });

  it('mostra falha da consulta e opção de tentar novamente', () => {
    simularDisponibilidade({ isError: true, error: new ErroApi({ status: 500, mensagem: 'Falha da consulta.' }) });
    const html = renderizarResultados();
    expect(html).toContain('Não foi possível buscar os veículos.');
    expect(html).toContain('Falha da consulta.');
    expect(html).toContain('Tentar novamente');
  });

  it('mostra uma mensagem de validação da API só no campo', () => {
    simularDisponibilidade({ isError: true, error: new ErroApi({
      status: 400, mensagem: 'Verifique os campos.', erros: { Uf: ['Local inválido.'] },
    }) });
    const html = renderizarResultados();
    expect(html.match(/Local inválido\./g)).toHaveLength(1);
    expect(html).toContain('aria-invalid="true"');
    expect(html).not.toContain('Verifique os campos.');
    expect(html).not.toContain('Não foi possível buscar os veículos.');
  });

  it('mostra o estado vazio com filtros saneados', () => {
    const html = renderizarResultados(`${PERIODO}&pagina=-2&idCategoria=errada&valorDiariaMin=-15`);
    expect(html).toContain('Nenhum veículo encontrado');
    expect(useVeiculosDisponiveis).toHaveBeenCalledWith(expect.objectContaining({
      pagina: 1, idCategoria: undefined, valorDiariaMin: undefined,
    }), true);
  });

  it('renderiza os veículos com CTAs, subtítulo e aviso único da capa acima da lista', () => {
    simularDisponibilidade({ data: pagina([
      { ...VEICULO, erroFotoCapa: ERRO_FOTO },
      { ...VEICULO, idVeiculo: 8, marca: 'Renault', modelo: 'Kwid', erroFotoCapa: ERRO_FOTO },
    ]) });
    const html = renderizarResultados();
    expect(html).toContain('2 veículos encontrados');
    expect(html).toContain('São Paulo – SP · Período de 10/06/2099 a 13/06/2099 (3 diárias)');
    expect(html).toContain('Locadora Paulista');
    expect(html).toContain('Reservar Agora');
    expect(html).toContain('Ver detalhes e regras');
    expect(html).toContain('href="/reservas/checkout/7?');
    expect(html).toContain('href="/veiculos/7?');
    expect(html.match(/Foto indisponível no momento\./g)).toHaveLength(1);
    expect(html.indexOf(ERRO_FOTO)).toBeLessThan(html.indexOf('Fiat Argo'));
    expect(html).toContain('Renault Kwid');
  });
});
