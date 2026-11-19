import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { AvisoProvider } from '@/components/ui';
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

function renderizar(c = contexto, atualizando = false) { return renderToStaticMarkup(<AvisoProvider><SimuladorCobrancasExtras contexto={c} atualizando={atualizando} /></AvisoProvider>); }
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
    vi.mocked(useApuracaoCobrancasExtras).mockReturnValue({ data: { idApuracao: 8, dataApuracaoStr: '18/11/2026', versaoConfiguracao: 7, cobrancas: [], calculo: { ...resultado, simulacao: false, provisorio: false, total: 0 } } } as unknown as ReturnType<typeof useApuracaoCobrancasExtras>);
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
