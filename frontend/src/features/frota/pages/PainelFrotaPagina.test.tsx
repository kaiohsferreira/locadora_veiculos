import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter } from 'react-router-dom';
import { ErroApi, type Pagina } from '@/types/api';
import type { RespostaVisaoGeral } from '@/types/painel';
import type { VeiculoRetorno } from '@/types/veiculos';
import type { CategoriaVeiculo } from '@/types/categorias';
import { useCategorias } from '@/features/frota/hooks/useCategorias';
import { useListarVeiculos } from '@/features/frota/hooks/useListarVeiculos';
import { usePainelVisaoGeral } from '@/features/visao-geral/hooks/usePainelVisaoGeral';
import { EstadoVazioFrota } from '@/features/frota/componentes/EstadoVazioFrota';
import { PilulasStatusFrota } from '@/features/frota/componentes/PilulasStatusFrota';
import PainelFrotaPagina from './PainelFrotaPagina';

// Nenhum teste chama a API: as consultas viram dublês, como nas telas de equipe e de visão geral.
vi.mock('@/api/client', () => ({
  api: { get: vi.fn(), post: vi.fn(), put: vi.fn() },
}));
vi.mock('@/features/frota/hooks/useListarVeiculos', () => ({ useListarVeiculos: vi.fn() }));
vi.mock('@/features/frota/hooks/useCategorias', () => ({ useCategorias: vi.fn() }));
vi.mock('@/features/visao-geral/hooks/usePainelVisaoGeral', () => ({ usePainelVisaoGeral: vi.fn() }));

type ConsultaListagem = ReturnType<typeof useListarVeiculos>;
type ConsultaPainel = ReturnType<typeof usePainelVisaoGeral>;
type ConsultaCategorias = ReturnType<typeof useCategorias>;

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

function renderizar(listagem: Partial<ConsultaListagem>, painel: Partial<ConsultaPainel> = {}): string {
  vi.mocked(useListarVeiculos).mockReturnValue(consultaListagem(listagem));
  vi.mocked(usePainelVisaoGeral).mockReturnValue(consultaPainel(painel));
  vi.mocked(useCategorias).mockReturnValue({ data: CATEGORIAS, isPending: false } as unknown as ConsultaCategorias);
  return renderToStaticMarkup(
    <MemoryRouter>
      <PainelFrotaPagina />
    </MemoryRouter>,
  );
}

describe('PainelFrotaPagina: cabeçalho e cartões', () => {
  beforeEach(() => {
    vi.mocked(useListarVeiculos).mockReset();
    vi.mocked(usePainelVisaoGeral).mockReset();
    vi.mocked(useCategorias).mockReset();
  });

  it('título, descrição e botão de cadastro apontam para a rota de cadastro', () => {
    const marcado = renderizar({ data: LISTA });

    expect(marcado).toContain('<h1');
    expect(marcado).toContain('Gestão de Frota');
    expect(marcado).toContain('Controle geral de veículos, status operacional, manutenções e disponibilidade.');
    expect(marcado).toContain('href="/back-office/frota/veiculos/novo"');
    expect(marcado).toContain('Cadastrar Novo Veículo');
  });

  it('os cinco cartões aparecem com os textos do desenho e Reservados no quinto lugar', () => {
    const marcado = renderizar({ data: LISTA });

    for (const titulo of ['Total da Frota', 'Disponíveis', 'Locados em Trânsito', 'Oficina / Revisão', 'Reservados']) {
      expect(marcado).toContain(titulo);
    }
    expect(marcado).toContain('+32');
    expect(marcado).toContain('novos neste mês');
    expect(marcado).toContain('18,0% de disponibilidade');
    expect(marcado).toContain('68,0% taxa ocupação');
  });

  it('servidor antigo (sem veiculosNovosNoMes): a tela não quebra e não mostra a linha de novos', () => {
    const painelAntigo: RespostaVisaoGeral = { ...PAINEL };
    delete painelAntigo.veiculosNovosNoMes;
    const marcado = renderizar({ data: LISTA }, { data: painelAntigo });

    expect(marcado).toContain('Total da Frota');
    expect(marcado).not.toContain('novos neste mês');
  });

  it('com o resumo ainda carregando, os cartões são esqueletos com o nome de carregamento', () => {
    const marcado = renderizar({ data: LISTA }, { data: undefined, isPending: true });

    expect(marcado).toContain('Carregando o resumo da frota');
  });

  it('resumo com erro: aviso com Tentar novamente acima dos cartões, e os números ficam como valor ausente', () => {
    const marcado = renderizar({ data: LISTA }, { data: undefined, isPending: false, isError: true });

    expect(marcado).toContain('Não foi possível carregar o resumo da frota.');
    expect(marcado).toContain('Tentar novamente');
    expect(marcado).not.toContain('Carregando o resumo da frota');
    expect(marcado.indexOf('Não foi possível carregar o resumo da frota.')).toBeLessThan(marcado.indexOf('Total da Frota'));
  });

  it('resumo com dados: não mostra o aviso de erro', () => {
    const marcado = renderizar({ data: LISTA });

    expect(marcado).not.toContain('Não foi possível carregar o resumo da frota.');
  });
});

describe('PainelFrotaPagina: pílulas, busca e categoria', () => {
  beforeEach(() => {
    vi.mocked(useListarVeiculos).mockReset();
    vi.mocked(usePainelVisaoGeral).mockReset();
    vi.mocked(useCategorias).mockReset();
  });

  it('pílulas com a contagem: Todos soma os seis status; Todos vem marcado no começo', () => {
    const marcado = renderizar({ data: LISTA });

    expect(marcado).toContain('Todos');
    expect(marcado).toContain('Disponíveis');
    expect(marcado).toContain('Locados');
    expect(marcado).toContain('Manutenção');
    expect(marcado).toContain('Reservados');
    expect(marcado).toContain('>53<');
    expect(marcado).toMatch(/aria-pressed="true"[^>]*>(<span>Todos<\/span>)/);
  });

  it('sem o resumo, cada pílula mostra o valor ausente no próprio chip (e não só os cartões)', () => {
    const marcado = renderToStaticMarkup(
      <PilulasStatusFrota valorSelecionado="" contagens={null} aoEscolher={() => undefined} />,
    );
    const botoes = marcado.match(/<button[^>]*>.*?<\/button>/g) ?? [];

    expect(botoes).toHaveLength(5);
    for (const botao of botoes) {
      expect(botao).toContain('>—</span>');
    }
  });

  it('a busca e a categoria têm rótulo para leitor de tela e a opção de todas as categorias', () => {
    const marcado = renderizar({ data: LISTA });

    expect(marcado).toContain('Buscar veículo');
    expect(marcado).toContain('Filtrar por placa, chassi, modelo ou marca...');
    expect(marcado).toContain('Todas as Categorias (Grupos)');
    expect(marcado).toContain('Grupo B - Hatch Compacto 1.0');
  });
});

describe('PainelFrotaPagina: estados da lista', () => {
  beforeEach(() => {
    vi.mocked(useListarVeiculos).mockReset();
    vi.mocked(usePainelVisaoGeral).mockReset();
    vi.mocked(useCategorias).mockReset();
  });

  it('carregando: tabela com esqueletos e aria-busy', () => {
    const marcado = renderizar({ isPending: true, data: undefined });

    expect(marcado).toContain('aria-busy="true"');
    expect(marcado).not.toContain('Nenhum veículo');
  });

  it('vazio sem filtro: convida a cadastrar, com o botão de cadastro', () => {
    const marcado = renderizar({ data: { itens: [], total: 0, pagina: 1, tamanhoPagina: 10 } });

    expect(marcado).toContain('Nenhum veículo cadastrado.');
    expect(marcado).toContain('Cadastre o primeiro veículo para ele aparecer nesta lista.');
    expect(marcado).not.toContain('Nenhum registro encontrado.');
  });

  it('enquanto a próxima consulta carrega, a lista vazia é a da resposta anterior: texto sem filtro, rodapé sem repetir o vazio', () => {
    const marcado = renderizar({
      data: { itens: [], total: 0, pagina: 1, tamanhoPagina: 10 },
      isPlaceholderData: true,
    });

    expect(marcado).toContain('Nenhum veículo cadastrado.');
    expect(marcado).not.toContain('Nenhum veículo encontrado.');
    expect(marcado).not.toContain('Nenhum registro encontrado.');
  });

  it('enquanto a próxima consulta carrega, o rodapé mostra a página e as linhas da tela e o total da resposta na tela', () => {
    const marcado = renderizar({
      data: { itens: [VEICULO], total: 25, pagina: 1, tamanhoPagina: 10 },
      isPlaceholderData: true,
    });

    expect(marcado).toContain('1 – 10');
    expect(marcado).toContain('>25</span>');
  });

  it('vazio com filtro: oferece limpar os filtros (componente de estado vazio)', () => {
    const marcado = renderToStaticMarkup(<EstadoVazioFrota comFiltro aoLimpar={() => undefined} />);

    expect(marcado).toContain('Nenhum veículo encontrado.');
    expect(marcado).toContain('Ajuste a busca ou os filtros e tente de novo.');
    expect(marcado).toContain('Limpar filtros');
  });

  it('erro: mensagem amigável, sem a técnica, com Tentar novamente', () => {
    const erro = new ErroApi({ status: 500, mensagem: 'Ocorreu um erro inesperado no servidor. Tente novamente em instantes.' });
    const marcado = renderizar({ isError: true, error: erro, data: undefined });

    expect(marcado).toContain('Não foi possível carregar a frota.');
    expect(marcado).toContain('Ocorreu um erro inesperado no servidor. Tente novamente em instantes.');
    expect(marcado).toContain('Tentar novamente');
    expect(marcado).not.toContain('Nenhum veículo cadastrado.');
  });

  it('lista com dados: linha do veículo, rodapé com o total e o seletor de linhas', () => {
    const marcado = renderizar({ data: LISTA });

    expect(marcado).toContain('Fiat Argo Drive 1.0');
    expect(marcado).toContain('veículos cadastrados');
    expect(marcado).toContain('Linhas por página:');
    expect(marcado).toContain('href="/back-office/frota/veiculos/7/editar"');
  });

  it('erro de capa vindo do servidor aparece como aviso, com o texto que ele mandou', () => {
    const marcado = renderizar({
      data: { ...LISTA, itens: [{ ...VEICULO, fotoCapa: null, erroFotoCapa: 'Não foi possível carregar a foto agora.' }] },
    });

    expect(marcado).toContain('Não foi possível carregar a foto agora.');
  });
});

describe('PainelFrotaPagina: o que não pode aparecer', () => {
  it('nenhum termo de patio, telemetria, exportação, sinistro, laudo ou transferência na tela', () => {
    const marcado = renderizar({ data: LISTA }).toLowerCase();

    for (const termo of ['pátio', 'patio', 'telemetria', 'exportar', 'sinistro', 'laudo', 'transferir', 'agência', 'câmbio', 'motor', 'lote', 'histórico do veículo']) {
      expect(marcado, termo).not.toContain(termo);
    }
  });
});
