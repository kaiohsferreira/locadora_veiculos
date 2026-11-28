import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter } from 'react-router-dom';
import { AvisoProvider } from '@/components/ui';
import { ErroApi, type Pagina } from '@/types/api';
import type { RespostaVisaoGeral } from '@/types/painel';
import type { VeiculoRetorno } from '@/types/veiculos';
import type { CategoriaVeiculo } from '@/types/categorias';
import type { CargoFuncionario } from '@/types/enums';
import { useAlterarStatusVeiculo } from '@/features/frota/hooks/useAlterarStatusVeiculo';
import { useCategorias } from '@/features/frota/hooks/useCategorias';
import { useListarVeiculos } from '@/features/frota/hooks/useListarVeiculos';
import { usePainelVisaoGeral } from '@/features/visao-geral/hooks/usePainelVisaoGeral';
import { ProvedorFiltrosFrota } from '@/features/frota/listagem/ProvedorFiltrosFrota';
import { AutenticacaoContexto, type AutenticacaoContextoValor } from '@/routes/autenticacao/AutenticacaoContexto';
import ListagemFrotaPagina from './ListagemFrotaPagina';

// Nenhum teste chama a API: as consultas viram dublês, como nas telas de equipe e de visão geral.
vi.mock('@/api/client', () => ({
  api: { get: vi.fn(), post: vi.fn(), put: vi.fn(), patch: vi.fn() },
}));
vi.mock('@/features/frota/hooks/useListarVeiculos', () => ({ useListarVeiculos: vi.fn() }));
vi.mock('@/features/frota/hooks/useCategorias', () => ({ useCategorias: vi.fn() }));
vi.mock('@/features/visao-geral/hooks/usePainelVisaoGeral', () => ({ usePainelVisaoGeral: vi.fn() }));
vi.mock('@/features/frota/hooks/useAlterarStatusVeiculo', () => ({ useAlterarStatusVeiculo: vi.fn() }));

type ConsultaListagem = ReturnType<typeof useListarVeiculos>;
type ConsultaPainel = ReturnType<typeof usePainelVisaoGeral>;
type ConsultaCategorias = ReturnType<typeof useCategorias>;
type TrocaStatus = ReturnType<typeof useAlterarStatusVeiculo>;

const VEICULO: VeiculoRetorno = {
  idVeiculo: 7,
  idCategoria: 2,
  categoriaNome: 'Grupo B',
  placa: 'BRA2E19',
  chassi: '9BD1237A8P849102',
  renavam: '00984521360',
  marca: 'Fiat',
  modelo: 'Argo Drive 1.0',
  anoFabricacao: 2023,
  anoModelo: 2024,
  cor: null,
  kmAtual: 24850,
  valorDiaria: 229,
  valorDiariaStr: 'R$ 229,00',
  valorVenda: null,
  valorVendaStr: '',
  status: 'DISPONIVEL',
  dataAquisicao: null,
  dataAquisicaoStr: '',
  fornecedor: null,
  numeroNotaFiscalCompra: null,
  locadoraNomeFantasia: 'Rota Sul',
  locadoraCidade: 'Campinas',
  locadoraUf: 'SP',
  fotoCapa: null,
  erroFotoCapa: null,
};

const LISTA: Pagina<VeiculoRetorno> = { itens: [VEICULO], total: 1, pagina: 1, tamanhoPagina: 10 };

const PAINEL: RespostaVisaoGeral = {
  periodo: 'TRINTA_DIAS',
  periodoInicioStr: '',
  periodoFimStr: '',
  primeiroAcesso: false,
  valoresVisiveis: false,
  receitaPeriodo: null,
  receitaPeriodoStr: '',
  aReceber: null,
  aReceberStr: '',
  quantidadeCobrancasEmAberto: 0,
  emAtraso: null,
  emAtrasoStr: '',
  quantidadeCobrancasVencidas: 0,
  taxaOcupacaoPercentual: 68,
  veiculosLocados: 34,
  totalVeiculos: 50,
  locacoesAtivas: 34,
  devolucoesPrevistasHoje: 5,
  frotaPorSituacao: [
    { situacao: 'LOCADO', quantidade: 34 },
    { situacao: 'DISPONIVEL', quantidade: 9 },
    { situacao: 'RESERVADO', quantidade: 3 },
    { situacao: 'MANUTENCAO', quantidade: 4 },
    { situacao: 'VENDIDO', quantidade: 1 },
    { situacao: 'BAIXADO', quantidade: 2 },
  ],
  veiculosNovosNoMes: 32,
};

const CATEGORIAS: Pagina<CategoriaVeiculo> = {
  itens: [{ idCategoria: 2, nome: 'Grupo B', descricao: 'Hatch Compacto 1.0', valorDiariaBase: 150, valorDiariaBaseStr: 'R$ 150,00' }],
  total: 1,
  pagina: 1,
  tamanhoPagina: 100,
};

function consultaListagem(parcial: Partial<ConsultaListagem>): ConsultaListagem {
  return {
    data: undefined,
    error: null,
    isPending: false,
    isError: false,
    isFetching: false,
    refetch: vi.fn(),
    ...parcial,
  } as unknown as ConsultaListagem;
}

function consultaPainel(parcial: Partial<ConsultaPainel>): ConsultaPainel {
  return { data: PAINEL, isPending: false, isError: false, ...parcial } as unknown as ConsultaPainel;
}

/** Sessão de funcionário com o cargo dado (null = sessão sem cargo). */
function autenticacaoCom(cargo: CargoFuncionario | null): AutenticacaoContextoValor {
  return {
    usuario: { nome: 'Marina Costa', perfil: 'funcionario', cargo: cargo ?? undefined, empresaNomeFantasia: 'Rota Sul Locadora' },
    autenticado: true,
    autenticacaoIgnorada: false,
    entrar: () => undefined,
    sair: () => undefined,
    possuiPerfil: () => true,
  };
}

function renderizar(
  listagem: Partial<ConsultaListagem>,
  painel: Partial<ConsultaPainel> = {},
  cargo: CargoFuncionario | null = 'GESTOR_FROTA',
  categorias: Partial<ConsultaCategorias> = {},
): string {
  vi.mocked(useListarVeiculos).mockReturnValue(consultaListagem(listagem));
  vi.mocked(usePainelVisaoGeral).mockReturnValue(consultaPainel(painel));
  vi.mocked(useCategorias).mockReturnValue({ data: CATEGORIAS, isPending: false, ...categorias } as unknown as ConsultaCategorias);
  vi.mocked(useAlterarStatusVeiculo).mockReturnValue({
    mutate: vi.fn(),
    isPending: false,
    variables: undefined,
  } as unknown as TrocaStatus);
  return renderToStaticMarkup(
    <AutenticacaoContexto.Provider value={autenticacaoCom(cargo)}>
      <AvisoProvider>
        <MemoryRouter initialEntries={['/back-office/frota/veiculos']}>
          <ProvedorFiltrosFrota>
            <ListagemFrotaPagina />
          </ProvedorFiltrosFrota>
        </MemoryRouter>
      </AvisoProvider>
    </AutenticacaoContexto.Provider>,
  );
}

describe('ListagemFrotaPagina: cabeçalho com a alternância', () => {
  beforeEach(() => {
    vi.mocked(useListarVeiculos).mockReset();
    vi.mocked(usePainelVisaoGeral).mockReset();
    vi.mocked(useCategorias).mockReset();
  });

  it('título e descrição iguais aos da tela 11, com o botão de cadastrar', () => {
    const marcado = renderizar({ data: LISTA });

    expect(marcado).toContain('<h1');
    expect(marcado).toContain('Gestão de Frota');
    expect(marcado).toContain('Controle geral de veículos, status operacional, manutenções e disponibilidade.');
    expect(marcado).toContain('href="/back-office/frota/veiculos/novo"');
    expect(marcado).toContain('Cadastrar Novo Veículo');
  });

  it('na Lista, a opção Lista vem marcada (aria-current) e Painel leva à tela 11', () => {
    const marcado = renderizar({ data: LISTA });

    expect(marcado).toContain('Visão da frota');
    expect(marcado).toMatch(/aria-current="page"[^>]*>Lista</);
    expect(marcado).not.toMatch(/aria-current="page"[^>]*>Painel</);
    expect(marcado).toContain('href="/back-office/frota"');
  });
});

describe('ListagemFrotaPagina: cartões do desenho 12', () => {
  beforeEach(() => {
    vi.mocked(useListarVeiculos).mockReset();
    vi.mocked(usePainelVisaoGeral).mockReset();
    vi.mocked(useCategorias).mockReset();
  });

  it('quatro cartões com os números da visão geral, "Contratos vigentes" e a taxa com uma casa decimal', () => {
    const marcado = renderizar({ data: LISTA });

    for (const titulo of ['Veículos Disponíveis', 'Locados', 'Em Manutenção', 'Taxa de Ocupação']) {
      expect(marcado).toContain(titulo);
    }
    expect(marcado).toContain('>9</p>');
    expect(marcado).toContain('>34</p>');
    expect(marcado).toContain('>4</p>');
    expect(marcado).toContain('68,0%');
    expect(marcado).toContain('Contratos vigentes');
    expect(marcado).toContain('5 encerram hoje');
  });

  it('situação ausente na lista: o cartão mostra zero', () => {
    const marcado = renderizar(
      { data: LISTA },
      { data: { ...PAINEL, frotaPorSituacao: [{ situacao: 'LOCADO', quantidade: 2 }] } },
    );

    expect(marcado).toContain('>0</p>');
  });

  it('resumo carregando: esqueletos com o nome de carregamento', () => {
    const marcado = renderizar({ data: LISTA }, { data: undefined, isPending: true });

    expect(marcado).toContain('Carregando o resumo da frota');
  });

  it('resumo com erro: aviso com Tentar novamente acima dos cartões, e os números como valor ausente', () => {
    const marcado = renderizar({ data: LISTA }, { data: undefined, isPending: false, isError: true });

    expect(marcado).toContain('Não foi possível carregar o resumo da frota.');
    expect(marcado).toContain('Tentar novamente');
    expect(marcado.indexOf('Não foi possível carregar o resumo da frota.')).toBeLessThan(marcado.indexOf('Veículos Disponíveis'));
    expect(marcado).toContain('>—</p>');
  });

  it('nenhum termo que o desenho traz e a tela não tem: exportar, telemetria, patrimonial, flex, laudo, meta, pátio, recall, apólices, check-in', () => {
    const original = renderizar({ data: LISTA });
    // "flex" também é classe CSS no HTML: o texto "Flex" (do desenho) é conferido em maiúsculas, sem as classes.
    expect(original).not.toMatch(/>[^<]*Flex/);
    const marcado = original.toLowerCase();

    for (const termo of [
      'exportar',
      'telemetria',
      'patrimonial',
      'aguardando laudo',
      'meta:',
      'superada',
      'pátio',
      'recall',
      'apólices',
      'check-in',
      'revisões periódicas',
      'prontos no pátio',
      'ver detalhes',
      '+12 hoje',
    ]) {
      expect(marcado, termo).not.toContain(termo);
    }
  });
});

describe('ListagemFrotaPagina: barra de filtros e tabela', () => {
  beforeEach(() => {
    vi.mocked(useListarVeiculos).mockReset();
    vi.mocked(usePainelVisaoGeral).mockReset();
    vi.mocked(useCategorias).mockReset();
  });

  it('busca com o mesmo rótulo da tela 11, categoria e status (Todos os Status, quatro situações)', () => {
    const marcado = renderizar({ data: LISTA });

    expect(marcado).toContain('Buscar veículo');
    expect(marcado).toContain('Todas as Categorias (Grupos)');
    expect(marcado).toContain('Grupo B - Hatch Compacto 1.0');
    expect(marcado).toContain('Todos os Status');
    expect(marcado).toContain('Status do veículo');
    expect(marcado).toContain('Em manutenção');
  });

  it('Limpar aparece e fica desligado quando não há filtro', () => {
    const marcado = renderizar({ data: LISTA });

    expect(marcado).toMatch(/<button[^>]*disabled=""[^>]*aria-label="Limpar Filtros"/);
    expect(marcado).toContain('>Limpar</span>');
  });

  it('a tabela traz a linha do veículo, o lápis e nenhum botão de olho', () => {
    const marcado = renderizar({ data: LISTA });

    expect(marcado).toContain('Fiat Argo Drive 1.0');
    expect(marcado).toContain('href="/back-office/frota/veiculos/7/editar"');
    expect(marcado).toContain('2024 • Chassi: 9BD12...9102');
    expect(marcado).not.toContain('>visibility<');
  });

  it('GESTOR_FROTA vê o menu de três pontos; atendente não vê', () => {
    expect(renderizar({ data: LISTA }, {}, 'GESTOR_FROTA')).toContain('Menu operacional do veículo BRA2E19');
    expect(renderizar({ data: LISTA }, {}, 'ATENDENTE')).not.toContain('Menu operacional do veículo');
  });

  it('vazio sem filtro: convida a cadastrar', () => {
    const marcado = renderizar({ data: { itens: [], total: 0, pagina: 1, tamanhoPagina: 10 } });

    expect(marcado).toContain('Nenhum veículo cadastrado.');
  });

  it('carregando: tabela com aria-busy', () => {
    const marcado = renderizar({ isPending: true, data: undefined });

    expect(marcado).toContain('aria-busy="true"');
  });

  it('erro da lista: mensagem amigável com Tentar novamente', () => {
    const erro = new ErroApi({ status: 500, mensagem: 'Ocorreu um erro inesperado no servidor. Tente novamente em instantes.' });
    const marcado = renderizar({ isError: true, error: erro, data: undefined });

    expect(marcado).toContain('Não foi possível carregar a frota.');
    expect(marcado).toContain('Tentar novamente');
  });
});

describe('ListagemFrotaPagina: erro das categorias (aviso ao lado do filtro)', () => {
  beforeEach(() => {
    vi.mocked(useListarVeiculos).mockReset();
    vi.mocked(usePainelVisaoGeral).mockReset();
    vi.mocked(useCategorias).mockReset();
  });

  it('com erro nas categorias, aparecem o aviso e o botão Tentar novamente dentro do mesmo alerta', () => {
    const marcado = renderizar({ data: LISTA }, {}, 'GESTOR_FROTA', { data: undefined, isPending: false, isError: true });

    expect(marcado).toMatch(
      /role="alert"[^>]*>(?:(?!<\/div>)[\s\S])*Não foi possível carregar as categorias\.(?:(?!<\/div>)[\s\S])*<button[^>]*>(?:(?!<\/button>)[\s\S])*Tentar novamente/,
    );
  });

  it('com as categorias carregadas, aparecem as opções e nenhum aviso de categoria', () => {
    const marcado = renderizar({ data: LISTA });

    expect(marcado).toContain('Grupo B - Hatch Compacto 1.0');
    expect(marcado).not.toContain('Não foi possível carregar as categorias.');
    expect(marcado).not.toContain('role="alert"');
  });

  it('com erro só nas categorias, a lista de veículos continua aparecendo', () => {
    const marcado = renderizar({ data: LISTA }, {}, 'GESTOR_FROTA', { data: undefined, isPending: false, isError: true });

    expect(marcado).toContain('Não foi possível carregar as categorias.');
    expect(marcado).toContain('Fiat Argo Drive 1.0');
    expect(marcado).not.toContain('Não foi possível carregar a frota.');
  });
});
