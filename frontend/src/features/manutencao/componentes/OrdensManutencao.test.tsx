import { describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import type { ManutencaoListaRetorno, ResumoManutencaoRetorno } from '@/types/manutencao';
import { BarraFiltrosManutencao } from './BarraFiltrosManutencao';
import { FaixaMetricasManutencao } from './FaixaMetricasManutencao';
import { RodapeOrdensManutencao } from './RodapeOrdensManutencao';
import { TabelaOrdensManutencao } from './TabelaOrdensManutencao';

const TEXTOS_CORTADOS = [
  'Aguardando Peças',
  'Aguard. Peças',
  'Aprovação Pendente',
  'Colunas',
  'Exportar Relatório',
  'Visualizar Laudo',
  'Editar OS',
  'Arquivar',
  'Notificar Oficina',
  'Garantia / Troca de Injetor',
  'Box 04',
  '(Previsão)',
  '(Atrasado)',
  'Saída Estimada',
  'Oficina Credenciada',
  'Acompanhamento em tempo real',
];

const ORDEM_ABERTA: ManutencaoListaRetorno = {
  idManutencao: 8841,
  idVeiculo: 7,
  placa: 'PUL2C55',
  marca: 'Fiat',
  modelo: 'Pulse Drive 1.3',
  fotoCapa: null,
  erroFotoCapa: null,
  tipo: 'PREVENTIVA',
  dataEntrada: '2026-11-12',
  dataEntradaStr: '12/11/2026',
  dataSaida: null,
  dataSaidaStr: '',
  kmRegistrado: 30120,
  oficina: 'Oficina Central AutoVia CGH',
  descricao: 'Revisão dos 30 mil km',
  custo: null,
  custoStr: '',
  aberta: true,
};

const ORDEM_ENCERRADA: ManutencaoListaRetorno = {
  ...ORDEM_ABERTA,
  idManutencao: 8839,
  placa: 'RNG8B42',
  modelo: 'Renegade Sport',
  tipo: 'CORRETIVA',
  dataSaida: '2026-11-15',
  dataSaidaStr: '15/11/2026',
  oficina: 'Concessionária Jeep Dahruj',
  descricao: null,
  custo: 1450,
  custoStr: 'R$ 1.450,00',
  aberta: false,
};

const ACOES_TABELA = { aoFalharFotoCapa: vi.fn(), aoEncerrar: vi.fn() };

describe('FaixaMetricasManutencao', () => {
  const resumo: ResumoManutencaoRetorno = {
    ordensAbertas: 18,
    periodoInicio: '2026-11-01',
    periodoInicioStr: '01/11/2026',
    periodoFim: '2026-11-30',
    periodoFimStr: '30/11/2026',
    custoPeriodo: 48920,
    custoPeriodoStr: 'R$ 48.920,00',
  };

  it('com dados: OS Ativas no plural e Custo Mês em reais', () => {
    const marcado = renderToStaticMarkup(
      <FaixaMetricasManutencao carregando={false} erro={false} tentando={false} resumo={resumo} aoTentarNovamente={vi.fn()} />,
    );

    expect(marcado).toContain('OS Ativas');
    expect(marcado).toContain('18 ordens');
    expect(marcado).toContain('Custo Mês');
    expect(marcado).toContain('48.920,00');
  });

  it('uma ordem usa o singular', () => {
    const marcado = renderToStaticMarkup(
      <FaixaMetricasManutencao
        carregando={false}
        erro={false}
        tentando={false}
        resumo={{ ...resumo, ordensAbertas: 1 }}
        aoTentarNovamente={vi.fn()}
      />,
    );

    expect(marcado).toContain('1 ordem<');
  });

  it('erro: a mensagem e Tentar de novo no lugar dos dois valores', () => {
    const marcado = renderToStaticMarkup(
      <FaixaMetricasManutencao carregando={false} erro tentando={false} resumo={undefined} aoTentarNovamente={vi.fn()} />,
    );

    expect(marcado).toContain('Não foi possível carregar o resumo da manutenção.');
    expect(marcado).toContain('Tentar de novo');
    expect(marcado).not.toContain('OS Ativas');
  });

  it('carregando: sem valores e com aria-busy', () => {
    const marcado = renderToStaticMarkup(
      <FaixaMetricasManutencao carregando erro={false} tentando={false} resumo={undefined} aoTentarNovamente={vi.fn()} />,
    );

    expect(marcado).toContain('aria-busy="true"');
    expect(marcado).not.toContain('R$');
  });
});

describe('BarraFiltrosManutencao', () => {
  const base = {
    busca: '',
    aoMudarBusca: vi.fn(),
    tipo: '' as const,
    aoMudarTipo: vi.fn(),
    situacao: '' as const,
    aoMudarSituacao: vi.fn(),
    podeLimpar: false,
    aoLimpar: vi.fn(),
    aoAtualizar: vi.fn(),
    atualizando: false,
  };

  it('tem a busca, o tipo (Todas, Preventiva, Corretiva), o status e Atualizar', () => {
    const marcado = renderToStaticMarkup(<BarraFiltrosManutencao {...base} />);

    expect(marcado).toContain('Buscar por placa, modelo ou código OS...');
    for (const rotulo of ['Todas', 'Preventiva', 'Corretiva', 'Todos os Status', 'Em Execução', 'Concluída']) {
      expect(marcado).toContain(rotulo);
    }
    expect(marcado).toContain('title="Atualizar"');
  });

  it('o tipo marca o botão ativo com aria-pressed', () => {
    const marcado = renderToStaticMarkup(<BarraFiltrosManutencao {...base} tipo="CORRETIVA" />);

    expect(marcado).toMatch(/aria-pressed="true"[^>]*>Corretiva</);
    expect(marcado).toMatch(/aria-pressed="false"[^>]*>Todas</);
  });

  it('Limpar filtros só aparece quando há filtro', () => {
    expect(renderToStaticMarkup(<BarraFiltrosManutencao {...base} />)).not.toContain('Limpar filtros');
    expect(renderToStaticMarkup(<BarraFiltrosManutencao {...base} podeLimpar />)).toContain('Limpar filtros');
  });

  it('não tem os controles cortados pelo P7', () => {
    const marcado = renderToStaticMarkup(<BarraFiltrosManutencao {...base} />);

    expect(marcado).not.toContain('Colunas');
    expect(marcado).not.toContain('Aguardando Peças');
    expect(marcado).not.toContain('Aprovação Pendente');
  });
});

describe('TabelaOrdensManutencao', () => {
  it('mostra as colunas do desenho, a placa, a OS e as datas', () => {
    const marcado = renderToStaticMarkup(
      <TabelaOrdensManutencao itens={[ORDEM_ABERTA]} carregando={false} vazio={null} {...ACOES_TABELA} />,
    );

    for (const titulo of ['Veículo &amp; Placa', 'Tipo', 'Entrada', 'Saída', 'Oficina', 'Custo Total', 'Status', 'Ações']) {
      expect(marcado).toContain(titulo);
    }
    expect(marcado).toContain('Fiat Pulse Drive 1.3');
    expect(marcado).toContain('PUL2C55');
    expect(marcado).toContain('OS #8841');
    expect(marcado).toContain('12/11/2026');
  });

  it('ordem aberta: Em Execução, Em aberto, custo com traço e o botão Encerrar OS', () => {
    const marcado = renderToStaticMarkup(
      <TabelaOrdensManutencao itens={[ORDEM_ABERTA]} carregando={false} vazio={null} {...ACOES_TABELA} />,
    );

    expect(marcado).toContain('Em Execução');
    expect(marcado).toContain('Em aberto');
    expect(marcado).toContain('title="Encerrar OS"');
    expect(marcado).toContain('>—<');
  });

  it('ordem encerrada: Concluída, data de saída, custo formatado e sem Encerrar OS', () => {
    const marcado = renderToStaticMarkup(
      <TabelaOrdensManutencao itens={[ORDEM_ENCERRADA]} carregando={false} vazio={null} {...ACOES_TABELA} />,
    );

    expect(marcado).toContain('Concluída');
    expect(marcado).toContain('15/11/2026');
    expect(marcado).toContain('1.450,00');
    expect(marcado).not.toContain('title="Encerrar OS"');
  });

  it('a segunda linha da oficina é a descrição da ordem (quando há)', () => {
    const marcado = renderToStaticMarkup(
      <TabelaOrdensManutencao itens={[ORDEM_ABERTA]} carregando={false} vazio={null} {...ACOES_TABELA} />,
    );

    expect(marcado).toContain('Revisão dos 30 mil km');
  });

  it('nenhum texto cortado pelo P7 aparece', () => {
    const marcado = renderToStaticMarkup(
      <TabelaOrdensManutencao itens={[ORDEM_ABERTA, ORDEM_ENCERRADA]} carregando={false} vazio={null} {...ACOES_TABELA} />,
    );

    for (const texto of TEXTOS_CORTADOS) expect(marcado).not.toContain(texto);
  });

  it('medidas do desenho: tabela em texto de apoio, cabeçalho em texto de apoio, margens de primeira e última coluna', () => {
    const marcado = renderToStaticMarkup(
      <TabelaOrdensManutencao itens={[ORDEM_ABERTA]} carregando={false} vazio={null} {...ACOES_TABELA} />,
    );

    expect(marcado).toContain('border-collapse text-apoio');
    expect(marcado).toMatch(/<th scope="col" class="[^"]*text-texto-apoio/);
    expect(marcado).toContain('first:px-6 last:px-6');
    expect(marcado).toContain('py-4');
    expect(marcado).toContain('rounded-sm bg-steel-suave px-1');
  });

  it('nenhum uso de font-tabular: a classe não existe no tema e não faz nada', () => {
    const marcado = renderToStaticMarkup(
      <TabelaOrdensManutencao itens={[ORDEM_ABERTA, ORDEM_ENCERRADA]} carregando={false} vazio={null} {...ACOES_TABELA} />,
    );

    expect(marcado).not.toContain('font-tabular');
    expect(marcado).toContain('text-dado-tabular');
  });

  it('carregando: linhas de esqueleto e aria-busy', () => {
    const marcado = renderToStaticMarkup(
      <TabelaOrdensManutencao itens={[]} carregando vazio={null} {...ACOES_TABELA} />,
    );

    expect(marcado).toContain('aria-busy="true"');
    expect(marcado).not.toContain('OS #');
  });

  it('sem linhas, mostra o estado vazio que a página passa', () => {
    const marcado = renderToStaticMarkup(
      <TabelaOrdensManutencao itens={[]} carregando={false} vazio={<p>Nenhuma ordem de manutenção encontrada.</p>} {...ACOES_TABELA} />,
    );

    expect(marcado).toContain('Nenhuma ordem de manutenção encontrada.');
  });
});

describe('medidas do desenho na faixa de métricas e na barra de filtros', () => {
  it('a faixa de métricas tem gap-4 entre os blocos (desenho 14, html:239)', () => {
    const marcado = renderToStaticMarkup(
      <FaixaMetricasManutencao
        carregando={false}
        erro={false}
        tentando={false}
        resumo={{ ordensAbertas: 18, periodoInicio: '2026-11-01', periodoInicioStr: '01/11/2026', periodoFim: '2026-11-30', periodoFimStr: '30/11/2026', custoPeriodo: 48920, custoPeriodoStr: 'R$ 48.920,00' }}
        aoTentarNovamente={vi.fn()}
      />,
    );

    expect(marcado).toContain('gap-4 rounded-cartao bg-branco p-1 shadow-sm');
  });

  it('o seletor Tipo e o botão Atualizar têm rounded (0,25rem do desenho); os botões do seletor seguem rounded-sm', () => {
    const marcado = renderToStaticMarkup(
      <BarraFiltrosManutencao
        busca=""
        aoMudarBusca={vi.fn()}
        tipo=""
        aoMudarTipo={vi.fn()}
        situacao=""
        aoMudarSituacao={vi.fn()}
        podeLimpar={false}
        aoLimpar={vi.fn()}
        aoAtualizar={vi.fn()}
        atualizando={false}
      />,
    );

    expect(marcado).toContain('flex rounded bg-steel-fundo p-1');
    expect(marcado).toContain('inline-flex h-10 items-center justify-center rounded bg-steel-fundo px-2');
    expect(marcado).toContain('rounded-sm px-2 py-1 text-rotulo');
    expect(marcado).not.toContain('font-tabular');
  });
});

describe('RodapeOrdensManutencao', () => {
  it('oferece 10, 20 e 50 linhas, com a escolhida, e o resumo com ordens de serviço', () => {
    const marcado = renderToStaticMarkup(
      <RodapeOrdensManutencao
        pagina={1}
        tamanhoPagina={20}
        total={58}
        desabilitada={false}
        aoMudarPagina={vi.fn()}
        aoMudarLinhas={vi.fn()}
      />,
    );

    expect(marcado).toContain('Linhas por página:');
    expect(marcado).toContain('ordens de serviço');
    expect(marcado).toMatch(/<option value="10"[^>]*>10<\/option>/);
    expect(marcado).toMatch(/<option value="20"[^>]*>20<\/option>/);
    expect(marcado).toMatch(/<option value="50"[^>]*>50<\/option>/);
    expect(marcado).toMatch(/<option value="20" selected="">20<\/option>/);
    expect(marcado).not.toContain('value="100"');
    expect(marcado).not.toContain('value="25"');
  });
});
