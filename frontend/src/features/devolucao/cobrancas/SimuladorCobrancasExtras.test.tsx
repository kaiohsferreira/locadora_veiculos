import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { AvisoProvider } from '@/components/ui';
import { MENSAGENS_COBRANCAS_EXTRAS, mensagemConfirmacaoApuracao } from '@/lib/mensagensCobrancasExtras';
import { AutenticacaoContexto, type AutenticacaoContextoValor } from '@/routes/autenticacao/AutenticacaoContexto';
import { ErroApi } from '@/types/api';
import type { CargoFuncionario } from '@/types/enums';
import type { ContextoDevolucao, VistoriaDevolucao } from '@/types/devolucao';
import type { SimulacaoCobrancasExtras } from '@/types/cobrancasExtras';
import { motivoBloqueioSimulacao } from './validacaoCobrancasExtras';
import { ResultadoSimulacao } from './ResultadoSimulacao';
import { SimuladorCobrancasExtras } from './SimuladorCobrancasExtras';
import { useApuracaoCobrancasExtras } from './useCobrancasExtras';
vi.mock('./useCobrancasExtras', () => ({ useApuracaoCobrancasExtras: vi.fn(), useSimularCobrancasExtras: () => ({ isPending: false }), useRegistrarApuracaoCobrancasExtras: () => ({ isPending: false }) }));
const vistoria: VistoriaDevolucao = { idVistoria: 1, idLocacao: 42, idFuncionario: 2, tipo: 'ENTRADA', dataHoraStr: '18/11/2026 10:00', kmRegistrado: 100, nivelCombustivel: 1, observacoes: null };
const comparacao: NonNullable<ContextoDevolucao['comparacao']> = { entrada: vistoria, saida: { ...vistoria, tipo: 'SAIDA', idVistoria: 2 }, comparacaoRealizada: true, avariasPreexistentes: [], avariasNovas: [], avariasIndeterminadas: [] };
const contexto: ContextoDevolucao = { locacao: { idLocacao: 42, idReserva: 3, idVeiculo: 4, kmSaida: 100, status: 'EM_ANDAMENTO' }, entrada: vistoria, avariasEntrada: [], comparacao };
const resultado: SimulacaoCobrancasExtras = { idLocacao: 42, idVistoriaEntrada: 1, idVistoriaSaida: 2, simulacao: true, provisorio: true, aviso: 'Provisório', diariasPrevistas: 2, configuracao: { idConfiguracao: 3, versao: 7, idCategoria: 1, categoriaNome: 'Econômico', updatedAtStr: '18/11/2026', baseFranquia: 'TOTAL', franquiaKm: 100, valorKmExcedente: 2, valorTanqueCompleto: 300, toleranciaAtrasoMinutos: 15, unidadeAtraso: 'HORA', fracionamentoAtraso: 'PROPORCIONAL', valorUnidadeAtraso: 20, baseAvarias: 'ESTIMATIVA_VISTORIA' },
  quilometragem: { kmEntrada: 100, kmSaida: 200, pacoteKmTotal: 100, kmExcedentes: 0, valorKmExcedente: 2, valor: 0 },
  combustivel: { nivelEntrada: 1, nivelSaida: 0.5, fracaoFaltante: 0.5, valorTanqueCompleto: 300, valor: 150 },
  atraso: { dataPrevistaStr: '18/11/2026 10:00', dataSaidaStr: '18/11/2026 10:45', toleranciaAtrasoMinutos: 15, unidadeAtraso: 'HORA', fracionamentoAtraso: 'PROPORCIONAL', unidadesAposTolerancia: 0.5, unidadesCobradas: 0.5, valorUnidadeAtraso: 20, valor: 10 },
  avarias: { valoresEstimados: true, itens: [], valor: 0 }, total: 160 };
const OUTROS_CARGOS: readonly CargoFuncionario[] = ['VENDEDOR', 'GESTOR_FROTA', 'FINANCEIRO', 'ADMINISTRATIVO'];

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

function renderizar(c = contexto, atualizando = false, cargo: CargoFuncionario | null = 'ATENDENTE') {
  return renderToStaticMarkup(
    <AutenticacaoContexto.Provider value={autenticacaoCom(cargo)}>
      <AvisoProvider><SimuladorCobrancasExtras contexto={c} atualizando={atualizando} /></AvisoProvider>
    </AutenticacaoContexto.Provider>,
  );
}

/** Apuração já registrada, como a consulta devolve. */
function apuracaoRegistrada(idPagamentoLancado?: number | null) {
  return {
    idApuracao: 8, dataApuracaoStr: '18/11/2026', versaoConfiguracao: 7, cobrancas: [],
    calculo: { ...resultado, simulacao: false, provisorio: false, total: 0 },
    ...(idPagamentoLancado === undefined ? {} : { idPagamentoLancado }),
  };
}

describe('devolução com política cadastrada', () => {
  beforeEach(() => vi.mocked(useApuracaoCobrancasExtras).mockReturnValue({ data: null, isPending: false, isError: false, isFetching: false } as unknown as ReturnType<typeof useApuracaoCobrancasExtras>));
  it('oferece simulação sem formulário de tarifas nem confirmação antecipada', () => {
    const html = renderizar();
    expect(html).toContain('Simular com política cadastrada');
    expect(html).not.toContain('<input'); expect(html).not.toContain('<form');
    expect(html).not.toContain('Confirmar e registrar');
    expect(renderizar(contexto, true)).toContain('disabled=""');
  });
  it('consulta primeiro e bloqueia simulação enquanto carrega', () => {
    vi.mocked(useApuracaoCobrancasExtras).mockReturnValue({ isPending: true } as unknown as ReturnType<typeof useApuracaoCobrancasExtras>);
    expect(renderizar()).toContain('Consultando apuração'); expect(renderizar()).not.toContain('Simular com');
  });
  it('exibe snapshot registrado e zero sem oferecer novo lançamento mesmo sem comparação', () => {
    vi.mocked(useApuracaoCobrancasExtras).mockReturnValue({ data: apuracaoRegistrada() } as unknown as ReturnType<typeof useApuracaoCobrancasExtras>);
    const html = renderizar({ ...contexto, comparacao: null });
    expect(html).toContain('Total registrado'); expect(html).toContain('sem cobranças adicionais');
    expect(html).toContain('Versão 7'); expect(html).not.toContain('Simular com');
  });
  it('mostra as quatro memórias, política e diárias do servidor', () => {
    const html = renderToStaticMarkup(<ResultadoSimulacao resultado={resultado} />);
    for (const texto of ['Quilometragem:', 'Combustível:', 'Atraso:', 'Novas avarias', '160,00', 'Econômico', 'Versão 7', '2 diária']) expect(html).toContain(texto);
  });
  it('bloqueia contexto legado, comparação ausente e combustível ausente', () => {
    for (const c of [{ ...contexto, comparacao: null }, { ...contexto, comparacao: { ...comparacao, comparacaoRealizada: false } }, { ...contexto, comparacao: { ...comparacao, entrada: null } }]) {
      expect(motivoBloqueioSimulacao(c)).not.toBeNull(); expect(renderizar(c)).not.toContain('Simular com');
    }
  });
});

describe('cargo que simula e apura (B3: só ATENDENTE e ADMINISTRADOR)', () => {
  beforeEach(() => vi.mocked(useApuracaoCobrancasExtras).mockReturnValue({ data: null, isPending: false, isError: false, isFetching: false } as unknown as ReturnType<typeof useApuracaoCobrancasExtras>));
  it('ADMINISTRADOR também vê o simulador', () => {
    expect(renderizar(contexto, false, 'ADMINISTRADOR')).toContain('Simular com política cadastrada');
  });
  it.each([...OUTROS_CARGOS, null] as const)('cargo %s sem apuração: nenhum cartão, nenhum simulador', (cargo) => {
    const html = renderizar(contexto, false, cargo);
    expect(html).not.toContain('Cobranças extras'); expect(html).not.toContain('Simular com');
    expect(html).not.toContain('Confirmar e registrar'); expect(html).not.toContain(MENSAGENS_COBRANCAS_EXTRAS.aviso);
  });
  it.each([...OUTROS_CARGOS, null] as const)('cargo %s com a consulta carregando: nada aparece', (cargo) => {
    vi.mocked(useApuracaoCobrancasExtras).mockReturnValue({ isPending: true } as unknown as ReturnType<typeof useApuracaoCobrancasExtras>);
    const html = renderizar(contexto, false, cargo);
    expect(html).not.toContain('Consultando apuração'); expect(html).not.toContain('Simular com');
  });
  it.each(OUTROS_CARGOS)('cargo %s vê a apuração registrada só para leitura', (cargo) => {
    vi.mocked(useApuracaoCobrancasExtras).mockReturnValue({ data: apuracaoRegistrada() } as unknown as ReturnType<typeof useApuracaoCobrancasExtras>);
    const html = renderizar(contexto, false, cargo);
    expect(html).toContain('Total registrado'); expect(html).not.toContain('Simular com');
    expect(html).not.toContain('Confirmar e registrar');
  });
  it.each(OUTROS_CARGOS)('cargo %s com erro na consulta vê o erro com Tentar novamente', (cargo) => {
    vi.mocked(useApuracaoCobrancasExtras).mockReturnValue({ isPending: false, isError: true, error: new ErroApi({ status: 500, mensagem: 'falha' }), refetch: vi.fn(), isFetching: false } as unknown as ReturnType<typeof useApuracaoCobrancasExtras>);
    const html = renderizar(contexto, false, cargo);
    expect(html).toContain('Tentar novamente'); expect(html).not.toContain('Simular com');
  });
});

describe('pagamento lançado pela apuração (B2)', () => {
  beforeEach(() => vi.mocked(useApuracaoCobrancasExtras).mockReturnValue({ data: null, isPending: false, isError: false, isFetching: false } as unknown as ReturnType<typeof useApuracaoCobrancasExtras>));
  it('mostra o número do pagamento lançado, na apuração registrada', () => {
    vi.mocked(useApuracaoCobrancasExtras).mockReturnValue({ data: apuracaoRegistrada(99) } as unknown as ReturnType<typeof useApuracaoCobrancasExtras>);
    expect(renderizar()).toContain('Pagamento nº 99 lançado para o cliente.');
  });
  it('sem pagamento lançado (total zero ou apuração antiga) não mostra a linha', () => {
    for (const valor of [null, undefined]) {
      vi.mocked(useApuracaoCobrancasExtras).mockReturnValue({ data: apuracaoRegistrada(valor) } as unknown as ReturnType<typeof useApuracaoCobrancasExtras>);
      expect(renderizar()).not.toContain('lançado para o cliente');
    }
  });
  it('a confirmação cita o lançamento do pagamento (texto proposto, em mensagens)', () => {
    expect(MENSAGENS_COBRANCAS_EXTRAS.confirmacao).toContain('e o lançamento do pagamento para o cliente.');
  });
  it('total maior que zero: a confirmação cita o lançamento do pagamento', () => {
    for (const total of [resultado.total, 0.01]) expect(mensagemConfirmacaoApuracao(total)).toBe(MENSAGENS_COBRANCAS_EXTRAS.confirmacao);
  });
  it('total zero: a confirmação volta à frase antiga, sem prometer pagamento (o servidor não lança valor zero)', () => {
    expect(mensagemConfirmacaoApuracao(0)).toBe(MENSAGENS_COBRANCAS_EXTRAS.confirmacaoSemPagamento);
    expect(MENSAGENS_COBRANCAS_EXTRAS.confirmacaoSemPagamento).toBe('Conferi a política, a versão e as quatro memórias de cálculo. Confirmo o registro das cobranças automáticas, incluindo as estimativas das novas avarias.');
    expect(mensagemConfirmacaoApuracao(0)).not.toContain('lançamento do pagamento');
  });
});
