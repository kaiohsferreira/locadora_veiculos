import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { AvisoProvider } from '@/components/ui';
import { useEnviarPropostaCompra } from '@/features/vendas/hooks/useEnviarPropostaCompra';
import { useSimulacaoParcelamento } from '@/features/vendas/hooks/useSimulacaoParcelamento';
import { useVeiculoAVenda } from '@/features/vendas/hooks/useVeiculoAVenda';
import { formatarMoeda } from '@/lib/formatters';
import { AutenticacaoContexto, type AutenticacaoContextoValor } from '@/routes/autenticacao/AutenticacaoContexto';
import { ErroApi } from '@/types/api';
import type { SimulacaoParcelamento, VeiculoAVenda } from '@/types/vendas';
import PropostaCompraPagina from './PropostaCompraPagina';

// Nenhum teste chama a API: o cliente HTTP vira dublê e os hooks devolvem consultas prontas.
vi.mock('@/api/client', () => ({
  api: { get: vi.fn(), post: vi.fn() },
}));
vi.mock('@/features/vendas/hooks/useVeiculoAVenda', () => ({ useVeiculoAVenda: vi.fn() }));
vi.mock('@/features/vendas/hooks/useSimulacaoParcelamento', () => ({ useSimulacaoParcelamento: vi.fn() }));
vi.mock('@/features/vendas/hooks/useEnviarPropostaCompra', () => ({ useEnviarPropostaCompra: vi.fn() }));

type ConsultaVeiculo = ReturnType<typeof useVeiculoAVenda>;
type ConsultaSimulacao = ReturnType<typeof useSimulacaoParcelamento>;

const VEICULO: VeiculoAVenda = {
  idVeiculo: 4,
  marca: 'Jeep',
  modelo: 'Renegade',
  anoFabricacao: 2022,
  anoModelo: 2023,
  cor: 'Branco',
  kmAtual: 30000,
  categoriaNome: 'SUV',
  valorVenda: 10000,
  valorVendaStr: 'R$ 10.000,00',
  locadoraNomeFantasia: 'Locadora Um',
  locadoraCidade: 'Campinas',
  locadoraUf: 'SP',
  fotoCapa: null,
  erroFotoCapa: null,
};

const SIMULACAO: SimulacaoParcelamento = {
  idVeiculo: 4,
  valorVenda: 10000,
  valorVendaStr: '',
  valorEntrada: 0,
  valorEntradaStr: '',
  valorFinanciado: 10000,
  valorFinanciadoStr: '',
  numeroParcelas: 3,
  valorParcela: 3333.33,
  valorParcelaStr: '',
  valorUltimaParcela: 3333.34,
  valorUltimaParcelaStr: '',
};

function autenticacao(autenticado: boolean): AutenticacaoContextoValor {
  return {
    usuario: autenticado ? { nome: 'Maria', perfil: 'cliente' } : null,
    autenticado,
    autenticacaoIgnorada: false,
    entrar: () => undefined,
    sair: () => undefined,
    possuiPerfil: () => autenticado,
  };
}

function consulta<T>(parcial: Record<string, unknown>): T {
  return {
    data: undefined,
    error: null,
    isPending: false,
    isError: false,
    isFetching: false,
    refetch: vi.fn(),
    ...parcial,
  } as unknown as T;
}

interface Cenario {
  veiculo: ConsultaVeiculo;
  simulacao?: ConsultaSimulacao;
  autenticado?: boolean;
  rota?: string;
}

function renderizar({ veiculo, simulacao, autenticado = true, rota = '/seminovos/4/proposta' }: Cenario): string {
  vi.mocked(useVeiculoAVenda).mockReturnValue(veiculo);
  vi.mocked(useSimulacaoParcelamento).mockReturnValue(simulacao ?? consulta<ConsultaSimulacao>({ data: SIMULACAO }));
  vi.mocked(useEnviarPropostaCompra).mockReturnValue({
    mutate: vi.fn(),
    isPending: false,
  } as unknown as ReturnType<typeof useEnviarPropostaCompra>);

  return renderToStaticMarkup(
    <MemoryRouter initialEntries={[rota]}>
      <AutenticacaoContexto.Provider value={autenticacao(autenticado)}>
        <AvisoProvider>
          <Routes>
            <Route path="/seminovos/:veiculoId/proposta" element={<PropostaCompraPagina />} />
          </Routes>
        </AvisoProvider>
      </AutenticacaoContexto.Provider>
    </MemoryRouter>,
  );
}

describe('PropostaCompraPagina (P6-FE-02)', () => {
  beforeEach(() => {
    vi.mocked(useVeiculoAVenda).mockReset();
    vi.mocked(useSimulacaoParcelamento).mockReset();
    vi.mocked(useEnviarPropostaCompra).mockReset();
  });

  it('mostra o cartão do carro com nome, anos, km, locadora e preço', () => {
    const html = renderizar({ veiculo: consulta<ConsultaVeiculo>({ data: VEICULO }) });

    expect(html).toContain('Proposta de compra');
    expect(html).toContain('Jeep Renegade');
    expect(html).toContain('2022/2023');
    expect(html).toContain('30.000 km');
    expect(html).toContain('Locadora Um');
    expect(html).toContain('Campinas/SP');
    expect(html).toContain(formatarMoeda(10000));
  });

  it('o resumo mostra exatamente a parcela devolvida pela API e a última parcela diferente', () => {
    const html = renderizar({ veiculo: consulta<ConsultaVeiculo>({ data: VEICULO }) });

    expect(html).toContain(`3 × ${formatarMoeda(3333.33)}`);
    expect(html).toContain(`Última parcela: ${formatarMoeda(3333.34)}`);
    expect(html).toContain(formatarMoeda(10000));
    expect(html).toContain('Enviar proposta');
  });

  it('sem diferença de centavos, não mostra a observação da última parcela', () => {
    const exata = { ...SIMULACAO, numeroParcelas: 2, valorParcela: 5000, valorUltimaParcela: 5000 };
    const html = renderizar({
      veiculo: consulta<ConsultaVeiculo>({ data: VEICULO }),
      simulacao: consulta<ConsultaSimulacao>({ data: exata }),
    });

    expect(html).toContain(`2 × ${formatarMoeda(5000)}`);
    expect(html).not.toContain('Última parcela');
  });

  it('com erroFotoCapa, mostra o aviso do servidor junto da foto', () => {
    const html = renderizar({
      veiculo: consulta<ConsultaVeiculo>({ data: { ...VEICULO, erroFotoCapa: 'A foto do veículo está indisponível no momento.' } }),
    });

    expect(html).toContain('A foto do veículo está indisponível no momento.');
  });

  it('sem login, avisa que a pessoa vai entrar e voltar para a proposta', () => {
    const html = renderizar({ veiculo: consulta<ConsultaVeiculo>({ data: VEICULO }), autenticado: false });

    expect(html).toContain('Você vai entrar na sua conta e voltar para esta proposta.');
  });

  it('enquanto o carro carrega, mostra o esqueleto e não o cartão', () => {
    const html = renderizar({ veiculo: consulta<ConsultaVeiculo>({ isPending: true }) });

    expect(html).toContain('aria-busy="true"');
    expect(html).not.toContain('Jeep Renegade');
  });

  it('veículo fora do catálogo (404) mostra que não está mais à venda', () => {
    const html = renderizar({
      veiculo: consulta<ConsultaVeiculo>({ isError: true, error: new ErroApi({ status: 404, mensagem: 'x' }) }),
    });

    expect(html).toContain('Este veículo não está mais à venda');
    expect(html).toContain('Ver catálogo');
  });

  it('id inválido na rota mostra que não está à venda, sem consultar', () => {
    const html = renderizar({ veiculo: consulta<ConsultaVeiculo>({}), rota: '/seminovos/abc/proposta' });

    expect(html).toContain('Este veículo não está mais à venda');
    expect(vi.mocked(useVeiculoAVenda)).toHaveBeenCalledWith(null);
  });

  it('falha de conexão mostra o estado de erro com nova tentativa', () => {
    const html = renderizar({
      veiculo: consulta<ConsultaVeiculo>({ isError: true, error: new ErroApi({ status: 0, mensagem: 'Sem conexão.' }) }),
    });

    expect(html).toContain('Sem conexão.');
    expect(html).toContain('Tentar novamente');
  });
});
