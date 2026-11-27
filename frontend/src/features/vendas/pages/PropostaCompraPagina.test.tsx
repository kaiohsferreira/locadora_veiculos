import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { AvisoProvider } from '@/components/ui';
import { useEnviarPropostaCompra } from '@/features/vendas/hooks/useEnviarPropostaCompra';
import { useSimulacaoParcelamento } from '@/features/vendas/hooks/useSimulacaoParcelamento';
import { useVeiculoAVenda } from '@/features/vendas/hooks/useVeiculoAVenda';
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

// Textos como o servidor manda: a tela mostra estes, sem formatar de novo no navegador.
const SIMULACAO: SimulacaoParcelamento = {
  idVeiculo: 4,
  valorVenda: 10000,
  valorVendaStr: 'R$ 10.000,00',
  valorEntrada: 0,
  valorEntradaStr: 'R$ 0,00',
  valorFinanciado: 10000,
  valorFinanciadoStr: 'R$ 10.000,00',
  numeroParcelas: 12,
  valorParcela: 833.33,
  valorParcelaStr: 'R$ 833,33',
  valorUltimaParcela: 833.37,
  valorUltimaParcelaStr: 'R$ 833,37',
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

  it('mostra a trilha, o título e o cartão do carro como no desenho', () => {
    const html = renderizar({ veiculo: consulta<ConsultaVeiculo>({ data: VEICULO }) });

    expect(html).toContain('Início');
    expect(html).toContain('Carros à venda');
    expect(html).toContain('Enviar Proposta de Compra');
    expect(html).toContain('Proposta de Compra &amp; Simulação');
    expect(html).toContain('Jeep Renegade');
    expect(html).toContain('Valor anunciado à vista');
    expect(html).toMatch(/R\$ 10\.000<span[^>]*>,00<\/span>/);
    expect(html).toContain('Ficha Técnica do Veículo');
    expect(html).toContain('2022 / 2023');
    expect(html).toContain('Cor Externa');
    expect(html).toContain('Branco');
    expect(html).toMatch(/data-testid="km-sobre-foto"[^>]*>30\.000 km/);
    expect(html).toMatch(/data-testid="cartao-locadora".*Locadora Um.*Campinas\/SP/s);
  });

  it('com a simulação dos valores dos campos, mostra o resultado do servidor e libera o envio', () => {
    const html = renderizar({ veiculo: consulta<ConsultaVeiculo>({ data: VEICULO }) });

    expect(html).toContain('Simulador Inteligente');
    expect(html).toContain('12x de R$ 833,33');
    expect(html).toContain('Última parcela: R$ 833,37');
    expect(html).not.toContain('Calculando…');
    expect(html).not.toMatch(/<button[^>]*disabled=""[^>]*>(?:(?!<\/button>).)*Enviar Proposta de Compra/s);
  });

  it('sem diferença de centavos, não mostra a observação da última parcela', () => {
    const exata = { ...SIMULACAO, valorParcela: 5000, valorParcelaStr: 'R$ 5.000,00', valorUltimaParcela: 5000 };
    const html = renderizar({
      veiculo: consulta<ConsultaVeiculo>({ data: VEICULO }),
      simulacao: consulta<ConsultaSimulacao>({ data: exata }),
    });

    expect(html).toContain('12x de R$ 5.000,00');
    expect(html).not.toContain('Última parcela');
  });

  it('enquanto a simulação não é a dos valores digitados, mostra "Calculando…" e desliga o envio', () => {
    const antiga = { ...SIMULACAO, numeroParcelas: 24, valorParcelaStr: 'R$ 416,66' };
    const html = renderizar({
      veiculo: consulta<ConsultaVeiculo>({ data: VEICULO }),
      simulacao: consulta<ConsultaSimulacao>({ data: antiga, isFetching: true }),
    });

    expect(html).toContain('Calculando…');
    expect(html).not.toContain('R$ 416,66');
    expect(html).toMatch(/<button[^>]*disabled=""[^>]*>(?:(?!<\/button>).)*Enviar Proposta de Compra/s);
  });

  it('antes da primeira simulação voltar, também mostra "Calculando…"', () => {
    const html = renderizar({
      veiculo: consulta<ConsultaVeiculo>({ data: VEICULO }),
      simulacao: consulta<ConsultaSimulacao>({ isPending: true, isFetching: true }),
    });

    expect(html).toContain('Calculando…');
  });

  it('erro da simulação: mostra a mensagem do servidor, sem "Calculando…", e não deixa enviar', () => {
    const html = renderizar({
      veiculo: consulta<ConsultaVeiculo>({ data: VEICULO }),
      simulacao: consulta<ConsultaSimulacao>({
        isError: true,
        error: new ErroApi({ status: 400, mensagem: 'Com esse valor financiado, cada parcela ficaria abaixo de R$ 0,01.' }),
      }),
    });

    expect(html).toContain('role="alert"');
    expect(html).toContain('Com esse valor financiado, cada parcela ficaria abaixo de R$ 0,01.');
    expect(html).not.toContain('Calculando…');
    expect(html).toMatch(/<button[^>]*disabled=""[^>]*>(?:(?!<\/button>).)*Enviar Proposta de Compra/s);
  });

  it('na volta do login, restaura a entrada e as parcelas do endereço', () => {
    const simulacaoDaVolta = { ...SIMULACAO, valorEntrada: 3000, numeroParcelas: 48, valorParcelaStr: 'R$ 145,83' };
    const html = renderizar({
      veiculo: consulta<ConsultaVeiculo>({ data: VEICULO }),
      simulacao: consulta<ConsultaSimulacao>({ data: simulacaoDaVolta }),
      rota: '/seminovos/4/proposta?entrada=3000.00&parcelas=48',
    });

    expect(html).toContain('value="3.000,00"');
    expect(html).toMatch(/checked="" value="48"/);
    expect(html).toContain('48x de R$ 145,83');
    expect(vi.mocked(useSimulacaoParcelamento)).toHaveBeenCalledWith(4, 3000, 48, 10000);
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
