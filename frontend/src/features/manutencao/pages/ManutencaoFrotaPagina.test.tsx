import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { AvisoProvider } from '@/components/ui';
import { ErroApi, type Pagina } from '@/types/api';
import type {
  AlertasRevisaoRetorno,
  ManutencaoListaRetorno,
  ResumoManutencaoRetorno,
} from '@/types/manutencao';
import { useVeiculosParaOrdem } from '@/features/manutencao/hooks/useVeiculosParaOrdem';
import { useAbrirManutencao } from '@/features/manutencao/hooks/useAbrirManutencao';
import { useEncerrarManutencao } from '@/features/manutencao/hooks/useEncerrarManutencao';
import {
  useAlertasRevisao,
  useListarManutencoes,
  useResumoManutencao,
} from '@/features/manutencao/hooks/useConsultasManutencao';
import ManutencaoFrotaPagina from './ManutencaoFrotaPagina';

// Nenhum teste chama a API: as consultas viram dublês.
vi.mock('@/api/client', () => ({
  api: { get: vi.fn(), post: vi.fn(), put: vi.fn(), patch: vi.fn() },
}));
vi.mock('@/features/manutencao/hooks/useConsultasManutencao', () => ({
  useListarManutencoes: vi.fn(),
  useAlertasRevisao: vi.fn(),
  useResumoManutencao: vi.fn(),
}));
vi.mock('@/features/manutencao/hooks/useAbrirManutencao', () => ({ useAbrirManutencao: vi.fn() }));
vi.mock('@/features/manutencao/hooks/useEncerrarManutencao', () => ({ useEncerrarManutencao: vi.fn() }));
vi.mock('@/features/manutencao/hooks/useVeiculosParaOrdem', () => ({ useVeiculosParaOrdem: vi.fn() }));

type ConsultaLista = ReturnType<typeof useListarManutencoes>;
type ConsultaAlertas = ReturnType<typeof useAlertasRevisao>;
type ConsultaResumo = ReturnType<typeof useResumoManutencao>;

const ORDEM: ManutencaoListaRetorno = {
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

const LISTA: Pagina<ManutencaoListaRetorno> = {
  itens: [ORDEM, { ...ORDEM, idManutencao: 8839, placa: 'RNG8B42', modelo: 'Renegade Sport', aberta: false, dataSaida: '2026-11-15', dataSaidaStr: '15/11/2026', custo: 1450, custoStr: 'R$ 1.450,00' }],
  total: 58,
  pagina: 1,
  tamanhoPagina: 20,
};

const ALERTAS: AlertasRevisaoRetorno = {
  total: 1,
  intervaloRevisaoKm: 10000,
  itens: [
    {
      idVeiculo: 3,
      placa: 'ONX9A11',
      marca: 'Chevrolet',
      modelo: 'Onix Plus LTZ',
      categoriaNome: 'Grupo B',
      kmAtual: 19820,
      kmUltimaPreventiva: 10000,
      kmProximaRevisao: 20000,
      faltamKm: 180,
      percentualCiclo: 98.2,
      situacao: 'PROXIMA',
      fotoCapa: null,
      erroFotoCapa: null,
    },
  ],
};

const RESUMO: ResumoManutencaoRetorno = {
  ordensAbertas: 18,
  periodoInicio: '2026-11-01',
  periodoInicioStr: '01/11/2026',
  periodoFim: '2026-11-30',
  periodoFimStr: '30/11/2026',
  custoPeriodo: 48920,
  custoPeriodoStr: 'R$ 48.920,00',
};

/** Textos que o P7 cortou do desenho: nenhum pode estar na página, nem desligado. */
const TEXTOS_CORTADOS = [
  'Exportar Relatório',
  'Agendar manutenção',
  'Agendar Revisão',
  'Aguard. Peças',
  'Aguardando Peças',
  'Colunas',
  'Programar',
  'Revisão Nível 3',
  'Mais Opções',
  'Ver Telemetria',
  'Visualizar Laudo',
  'Editar OS',
  'Arquivar',
  'Notificar Oficina',
  'Oficinas Credenciadas',
  'Garantia / Troca de Injetor',
  'Acompanhamento em tempo real',
  'Box 04',
  '(Previsão)',
  '(Atrasado)',
];

function consulta<T>(parcial: Record<string, unknown>): T {
  return { data: undefined, error: null, isPending: false, isError: false, isFetching: false, isSuccess: false, refetch: vi.fn(), ...parcial } as unknown as T;
}

function renderizar(): string {
  return renderToStaticMarkup(
    <AvisoProvider>
      <ManutencaoFrotaPagina />
    </AvisoProvider>,
  );
}

function prepararConsultas(
  lista: Record<string, unknown>,
  alertas: Record<string, unknown> = {},
  resumo: Record<string, unknown> = {},
) {
  vi.mocked(useListarManutencoes).mockReturnValue(consulta<ConsultaLista>(lista));
  vi.mocked(useAlertasRevisao).mockReturnValue(consulta<ConsultaAlertas>({ data: ALERTAS, isSuccess: true, ...alertas }));
  vi.mocked(useResumoManutencao).mockReturnValue(consulta<ConsultaResumo>({ data: RESUMO, isSuccess: true, ...resumo }));
}

beforeEach(() => {
  vi.mocked(useAbrirManutencao).mockReturnValue({ mutate: vi.fn(), isPending: false } as never);
  vi.mocked(useEncerrarManutencao).mockReturnValue({ mutate: vi.fn(), isPending: false } as never);
  vi.mocked(useVeiculosParaOrdem).mockReturnValue({ data: undefined, isError: false, isSuccess: false } as never);
});

describe('ManutencaoFrotaPagina: com dados', () => {
  it('mostra o cabeçalho com Abrir ordem, os alertas, as métricas e a tabela', () => {
    prepararConsultas({ data: LISTA, isSuccess: true });
    const marcado = renderizar();

    expect(marcado).toContain('Manutenção da Frota');
    expect(marcado).toContain('>Abrir ordem<');
    expect(marcado).toContain('Alertas de Veículos Próximos da Revisão');
    expect(marcado).toContain('1 Requer Atenção');
    expect(marcado).toContain('Intervalo Padrão: 10.000 km');
    expect(marcado).toContain('OS Ativas');
    expect(marcado).toContain('18 ordens');
    expect(marcado).toContain('Custo Mês');
    expect(marcado).toContain('Histórico e Ordens de Manutenção');
    expect(marcado).toContain('Fiat Pulse Drive 1.3');
    expect(marcado).toContain('OS #8841');
    expect(marcado).toContain('Linhas por página:');
  });

  it('nenhum texto cortado pelo P7 aparece na página', () => {
    prepararConsultas({ data: LISTA, isSuccess: true });
    const marcado = renderizar();

    for (const texto of TEXTOS_CORTADOS) expect(marcado).not.toContain(texto);
  });

  it('o rodapé oferece 10, 20 e 50 linhas', () => {
    prepararConsultas({ data: LISTA, isSuccess: true });
    const marcado = renderizar();

    expect(marcado).toMatch(/<option value="10"[^>]*>10<\/option>/);
    expect(marcado).toMatch(/<option value="20"[^>]*>20<\/option>/);
    expect(marcado).toMatch(/<option value="50"[^>]*>50<\/option>/);
    expect(marcado).not.toContain('value="100"');
  });
});

describe('ManutencaoFrotaPagina: estados da tabela', () => {
  it('carregando: a tabela mostra esqueleto e aria-busy, sem linhas', () => {
    prepararConsultas({ isPending: true });
    const marcado = renderizar();

    expect(marcado).toContain('aria-busy="true"');
    expect(marcado).not.toContain('OS #8841');
  });

  it('vazio: a mensagem de lista vazia, sem Limpar filtros (não há filtro)', () => {
    prepararConsultas({ data: { itens: [], total: 0, pagina: 1, tamanhoPagina: 20 }, isSuccess: true });
    const marcado = renderizar();

    expect(marcado).toContain('Nenhuma ordem de manutenção encontrada.');
    expect(marcado).not.toContain('Limpar filtros');
  });

  it('erro da lista: a mensagem da tabela com Tentar de novo, e o resto da página segue', () => {
    prepararConsultas({ isError: true, error: new ErroApi({ status: 500, mensagem: 'Não foi possível carregar as ordens de manutenção.' }) });
    const marcado = renderizar();

    expect(marcado).toContain('Não foi possível carregar as ordens de manutenção.');
    expect(marcado).toContain('Tentar de novo');
    expect(marcado).toContain('Alertas de Veículos Próximos da Revisão');
    expect(marcado).toContain('OS Ativas');
  });
});

describe('ManutencaoFrotaPagina: erro dos blocos de cima', () => {
  it('erro dos alertas: mensagem e Tentar de novo no lugar dos cartões; a tabela segue', () => {
    prepararConsultas({ data: LISTA, isSuccess: true }, { data: undefined, isError: true, error: new Error('falha') });
    const marcado = renderizar();

    expect(marcado).toContain('Não foi possível carregar os alertas de revisão.');
    expect(marcado).toContain('Tentar de novo');
    expect(marcado).toContain('OS #8841');
  });

  it('erro do resumo: a faixa mostra a mensagem no lugar dos valores; a tabela segue', () => {
    prepararConsultas({ data: LISTA, isSuccess: true }, {}, { data: undefined, isError: true, error: new Error('falha') });
    const marcado = renderizar();

    expect(marcado).toContain('Não foi possível carregar o resumo da manutenção.');
    expect(marcado).not.toContain('OS Ativas');
    expect(marcado).toContain('OS #8841');
  });
});
