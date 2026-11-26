import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { AvisoProvider } from '@/components/ui';
import { formatarMoeda, formatarQuilometragem } from '@/lib/formatters';
import { MENSAGENS_COBRANCAS_EXTRAS } from '@/lib/mensagensCobrancasExtras';
import { MENSAGENS_DEVOLUCAO as M } from '@/lib/mensagensDevolucao';
import { ROTULO_STATUS_LOCACAO } from '@/types/enums';
import { AutenticacaoContexto, type AutenticacaoContextoValor } from '@/routes/autenticacao/AutenticacaoContexto';
import { ErroApi } from '@/types/api';
import type { CalculoCobrancasExtras, SimulacaoCobrancasExtras } from '@/types/cobrancasExtras';
import type { ContextoDevolucao, ComparacaoSaida, ResultadoEncerramento, VistoriaDevolucao } from '@/types/devolucao';
import { useDevolucao, useEncerrarLocacao, useRegistrarSaida } from '../hooks/useDevolucao';
import { useSimulacaoEncerramento } from '../hooks/useSimulacaoEncerramento';
import CheckoutDevolucaoPagina from './CheckoutDevolucaoPagina';

vi.mock('../hooks/useDevolucao', () => ({ useDevolucao: vi.fn(), useRegistrarSaida: vi.fn(), useEncerrarLocacao: vi.fn() }));
vi.mock('../hooks/useSimulacaoEncerramento', () => ({ useSimulacaoEncerramento: vi.fn() }));

type Consulta = ReturnType<typeof useDevolucao>;
type Registro = ReturnType<typeof useRegistrarSaida>;
type Encerramento = ReturnType<typeof useEncerrarLocacao>;
type Simulacao = ReturnType<typeof useSimulacaoEncerramento>;

const entrada: VistoriaDevolucao = {
  idVistoria: 1, idLocacao: 42, idFuncionario: 3, tipo: 'ENTRADA', dataHoraStr: '18/11/2026 09:00', kmRegistrado: 1000,
  nivelCombustivel: 1, observacoes: null, operadorStr: 'Marina C.',
};
const saida: VistoriaDevolucao = {
  ...entrada, idVistoria: 2, tipo: 'SAIDA', dataHoraStr: '18/11/2026 10:15', kmRegistrado: 1920, nivelCombustivel: 0.5, operadorStr: 'Carlos M.',
};
const avariaEntrada = { idAvaria: 7, idVistoria: 1, idAvariaEntrada: null, descricao: 'Risco original', gravidade: 'LEVE' as const, valorEstimado: 50, valorEstimadoStr: 'R$ 50,00' };
const avariaNova = { idAvaria: 9, idVistoria: 2, idAvariaEntrada: null, descricao: 'Amassado novo', gravidade: 'MEDIA' as const, valorEstimado: 300, valorEstimadoStr: 'R$ 300,00' };
const comparacao: ComparacaoSaida = {
  entrada, saida, comparacaoRealizada: true, avariasPreexistentes: [], avariasNovas: [avariaNova], avariasIndeterminadas: [],
};
const contexto: ContextoDevolucao = {
  locacao: { idLocacao: 42, idReserva: 8, idVeiculo: 3, kmSaida: 1000, status: 'EM_ANDAMENTO', valorCaucao: 1200 },
  entrada, avariasEntrada: [avariaEntrada], comparacao,
  placa: 'ABC1D23', marca: 'Fiat', modelo: 'Toro', diariasPrevistas: 2, valorDiaria: 100, valorDiariaStr: 'R$ 100,00',
  totalDiarias: 200, totalDiariasStr: 'R$ 200,00', totalExtras: 160, totalExtrasStr: 'R$ 160,00', valorTotalPrevisto: 360,
  valorTotalPrevistoStr: 'R$ 360,00', idConfiguracao: 3, versaoEsperada: 7, caucao: { situacao: 'PENDENTE', acao: null },
};

const calculo: CalculoCobrancasExtras = {
  idLocacao: 42, idVistoriaEntrada: 1, idVistoriaSaida: 2, simulacao: true, provisorio: true, aviso: 'Provisório',
  configuracao: {
    idConfiguracao: 3, versao: 7, idCategoria: 1, categoriaNome: 'Econômico', updatedAtStr: '18/11/2026', baseFranquia: 'TOTAL',
    franquiaKm: 100, valorKmExcedente: 2, valorTanqueCompleto: 300, toleranciaAtrasoMinutos: 15, unidadeAtraso: 'HORA',
    fracionamentoAtraso: 'PROPORCIONAL', valorUnidadeAtraso: 20, baseAvarias: 'ESTIMATIVA_VISTORIA',
  },
  diariasPrevistas: 2,
  quilometragem: { kmEntrada: 1000, kmSaida: 1920, pacoteKmTotal: 100, kmExcedentes: 820, valorKmExcedente: 2, valor: 0 },
  combustivel: { nivelEntrada: 1, nivelSaida: 0.5, fracaoFaltante: 0.5, valorTanqueCompleto: 300, valor: 150 },
  atraso: {
    dataPrevistaStr: '18/11/2026 10:00', dataSaidaStr: '18/11/2026 10:15', toleranciaAtrasoMinutos: 15, unidadeAtraso: 'HORA',
    fracionamentoAtraso: 'PROPORCIONAL', unidadesAposTolerancia: 0, unidadesCobradas: 0, valorUnidadeAtraso: 20, valor: 0,
  },
  avarias: { valoresEstimados: true, itens: [{ idAvaria: 9, descricao: 'Amassado novo', valorEstimado: 300 }], valor: 300 },
  total: 450,
};
const simulado: SimulacaoCobrancasExtras = { ...calculo, simulacao: true, provisorio: true };

const SESSAO_ATENDENTE: AutenticacaoContextoValor = {
  usuario: { nome: 'Marina Costa', perfil: 'funcionario', cargo: 'ATENDENTE', empresaNomeFantasia: 'Rota Sul Locadora' },
  autenticado: true, autenticacaoIgnorada: false, entrar: () => undefined, sair: () => undefined, possuiPerfil: () => true,
};
const SESSAO_VENDEDOR: AutenticacaoContextoValor = {
  usuario: { nome: 'Pedro Lima', perfil: 'funcionario', cargo: 'VENDEDOR', empresaNomeFantasia: 'Rota Sul Locadora' },
  autenticado: true, autenticacaoIgnorada: false, entrar: () => undefined, sair: () => undefined, possuiPerfil: () => true,
};

const resultadoEncerrado: ResultadoEncerramento = {
  locacao: {
    idLocacao: 42, status: 'FINALIZADA', kmSaida: 1000, kmChegada: 1920, dataDevolucaoEfetiva: '2026-11-18T10:15:00',
    dataDevolucaoEfetivaStr: '18/11/2026 10:15', diariasPrevistas: 2, valorDiaria: 100, valorDiariaStr: 'R$ 100,00',
    valorTotal: 360, valorTotalStr: 'R$ 360,00',
  },
  veiculo: { idVeiculo: 3, status: 'DISPONIVEL', kmAtual: 1920 },
  cobrancas: {
    totalExtras: 160, totalExtrasStr: 'R$ 160,00', totalDiarias: 200, totalDiariasStr: 'R$ 200,00',
    idPagamentoCobrancaExtra: 77, idPagamentoDiaria: 76,
  },
  caucao: {
    acao: 'RETIDA_PARCIAL', valorReservado: 1200, valorReservadoStr: 'R$ 1.200,00', valorRetido: 160, valorRetidoStr: 'R$ 160,00',
    valorLiberado: 1040, valorLiberadoStr: 'R$ 1.040,00', statusPagamento: 'PAGO', tratarAMao: false,
  },
  avisos: [],
};

function renderizar(options: {
  consulta?: Partial<Consulta>;
  encerramento?: Partial<Encerramento>;
  simulacao?: Partial<Simulacao>;
  sessao?: AutenticacaoContextoValor;
  id?: string;
} = {}): string {
  vi.mocked(useDevolucao).mockReturnValue({
    isPending: false, isError: false, isFetching: false, refetch: vi.fn(), data: contexto, ...options.consulta,
  } as unknown as Consulta);
  vi.mocked(useRegistrarSaida).mockReturnValue({ isPending: false, isError: false, mutateAsync: vi.fn() } as unknown as Registro);
  vi.mocked(useEncerrarLocacao).mockReturnValue({
    data: undefined, error: null, isPending: false, isSuccess: false, mutate: vi.fn(), ...options.encerramento,
  } as unknown as Encerramento);
  vi.mocked(useSimulacaoEncerramento).mockReturnValue({
    data: simulado, isPending: false, isError: false, isFetching: false, error: null, refetch: vi.fn(), ...options.simulacao,
  } as unknown as Simulacao);
  return renderToStaticMarkup(
    <AutenticacaoContexto.Provider value={options.sessao ?? SESSAO_ATENDENTE}>
      <AvisoProvider>
        <MemoryRouter initialEntries={[`/devolucao/${options.id ?? '42'}`]}>
          <Routes>
            <Route path="/devolucao/:locacaoId" element={<CheckoutDevolucaoPagina />} />
          </Routes>
        </MemoryRouter>
      </AvisoProvider>
    </AutenticacaoContexto.Provider>,
  );
}

/** Termos que o anexo da tela 06 manda cortar (falta de lastro). Nenhum pode aparecer na tela. */
const TERMOS_CORTADOS_06 = [
  'Galeria', 'Original Validado', 'Painel Digital', 'Telemetria', 'Cielo', 'Higienização', 'ConectCar', 'Custódia',
  'Proteção Total', 'Franquia Zero', 'Simular com política cadastrada', 'Cálculo em Tempo Real', 'Km Livre', 'Estorno',
  'LOC-', 'Check-out em Validação', 'Dados de Saída (Check-in)', 'Dados de Chegada (Check-out)', 'Capacidade', 'AV-89241',
  'Jeep Renegade', 'Valor líquido a ser estornado', 'Gravidade: Alta', 'Não coberta pela apólice', 'Foto de Saída',
];

describe('tela 06: cabeçalho e estados (anexo, tela 06)', () => {
  beforeEach(() => vi.clearAllMocks());

  it('título, trilha sem link no Despacho, locação e veículo do contexto', () => {
    const html = renderizar();
    expect(html).toContain('<h1');
    expect(html).toContain(M.titulo);
    expect(html).toContain('aria-current="page"');
    expect(html).toContain('Locação nº 42');
    expect(html).toContain('Fiat Toro');
    expect(html).toContain(M.descricao);
    expect(html).toContain('Caminho da tela');
  });

  it('sem vistoria de entrada: aviso e link para a vistoria de entrada, sem formulário', () => {
    const html = renderizar({ consulta: { data: { ...contexto, entrada: null, comparacao: null } } });
    expect(html).toContain(M.entradaNecessariaTitulo);
    expect(html).toContain('/back-office/locacoes/42/vistoria');
    expect(html).not.toContain('<form');
  });

  it('sem vistoria de saída: aviso acima do formulário, que é o primeiro passo', () => {
    const html = renderizar({ consulta: { data: { ...contexto, comparacao: null } } });
    expect(html).toContain(M.avisoSemSaida);
    expect(html.indexOf(M.avisoSemSaida)).toBeLessThan(html.indexOf('<form'));
    expect(html).toContain(M.formulario.titulo);
    expect(html).toContain('comparacaoConferida');
    expect(html).toContain('aria-required="true"');
    expect(html).not.toContain('type="file"');
    expect(html).not.toContain(M.encerrarBotao);
    expect(html).toContain(M.colunaRetirada);
    expect(html).toContain('Risco original');
    expect(html).toContain(M.estadoAguardandoSaida);
  });

  it('saída registrada e locação aberta: desenho com as duas colunas, cobranças e o botão Encerrar', () => {
    const html = renderizar();
    expect(html).toContain(M.colunaRetirada);
    expect(html).toContain(M.colunaDevolucao);
    expect(html).toContain(M.operador('18/11/2026 09:00', 'Marina C.'));
    expect(html).toContain(M.operador('18/11/2026 10:15', 'Carlos M.'));
    expect(html).toContain(M.cobrancasTitulo);
    expect(html).toContain(M.calculoAutomatico);
    expect(html).toContain(M.encerrarBotao);
    expect(html).toContain(M.encerrarNota);
    expect(html).toContain(M.estadoAguardandoEncerramento);
    expect(html).not.toContain('<form');
    expect(html).not.toContain(M.caixa.titulo);
    expect(html).not.toContain(M.encerrando);
  });

  it('o detalhamento vem do servidor: km, combustível, atraso e avarias, com a memória', () => {
    const html = renderizar();
    expect(html).toContain(M.kmExcedenteTitulo);
    expect(html).toContain(M.combustivelItem);
    expect(html).toContain(M.atrasoTitulo);
    expect(html).toContain(M.avariasTitulo);
    expect(html).toContain(M.atrasoApoio('18/11/2026 10:15', 15));
    expect(html).toContain(M.divergencia(4));
    expect(html).toContain(M.franquiaContratada('100 km no total'));
    expect(html).toContain(M.cobrancaExcedente(formatarMoeda(0)));
  });

  it('rodados com a franquia quando a simulação está disponível (920 km = 1.920 - 1.000)', () => {
    const html = renderizar();
    expect(html).toContain(M.rodados('920', '100 km no total'));
  });

  it('rodados sem a franquia quando a simulação não está disponível para o cargo: fica só "Rodados: n km"', () => {
    const html = renderizar({ simulacao: { data: undefined } });
    expect(html).toContain(M.rodadosSemFranquia('920'));
    expect(html).not.toContain('Franquia: ');
    expect(html).not.toContain(M.franquiaContratada('100 km no total'));
  });

  it('locação atrasada: o selo do estado continua e o selo "Atrasada" aparece junto', () => {
    const html = renderizar({ consulta: { data: { ...contexto, locacao: { ...contexto.locacao, status: 'ATRASADA' } } } });
    expect(html).toContain(M.estadoAguardandoEncerramento);
    expect(html).toContain(ROTULO_STATUS_LOCACAO.ATRASADA);
  });

  it('locação em andamento não mostra o selo Atrasada', () => {
    const html = renderizar();
    expect(html).not.toContain(ROTULO_STATUS_LOCACAO.ATRASADA);
  });

  it('novas avarias continuam a contagem depois da entrada (#2 com uma avaria de entrada) e mostram o valor a cobrar', () => {
    const html = renderizar();
    expect(html).toContain(M.novaAvaria(2, 'Amassado novo'));
    expect(html).toContain(M.valorACobrar(formatarMoeda(300)));
    expect(html).toContain(M.custoReparo('Amassado novo', formatarMoeda(300)));
    expect(html).toContain(M.avariaEntrada(1, 'Risco original'));
  });

  it('caução pelo caso: 1.200 com extras de 160 retém 160 e libera 1.040 (frase e selo aprovados)', () => {
    const html = renderizar();
    expect(html).toContain(M.caucaoTitulo);
    expect(html).toContain(M.caucaoRetida(formatarMoeda(160), formatarMoeda(1040)));
    expect(html).toContain(M.caucaoLiberadaAoCliente);
    expect(html).toContain(M.seloLiquidado);
    expect(html).toContain(M.retencaoValor(formatarMoeda(160)));
  });

  it('o total mostrado é o que o servidor devolveu em totalExtras (sem recalcular)', () => {
    const html = renderizar();
    expect(html).toContain(M.totalTitulo);
    expect(html).toContain('R$ 160,00');
    expect(html).not.toContain(formatarMoeda(450));
  });

  it('cargo sem permissão vê a comparação, sem cobranças e sem botão, e a simulação não roda', () => {
    const html = renderizar({ sessao: SESSAO_VENDEDOR });
    expect(html).toContain(M.colunaDevolucao);
    expect(html).toContain(M.semPermissaoEncerrar);
    expect(html).not.toContain(M.encerrarBotao);
    expect(html).not.toContain(M.cobrancasTitulo);
    expect(useSimulacaoEncerramento).toHaveBeenCalledWith(42, 2, false);
  });

  it('a simulação só roda para quem encerra, com a vistoria de saída como chave', () => {
    renderizar();
    expect(useSimulacaoEncerramento).toHaveBeenCalledWith(42, 2, true);
  });

  it('calculando e erro da simulação: o cartão mostra o estado, e o botão de encerrar continua disponível', () => {
    const calculando = renderizar({ simulacao: { data: undefined, isPending: true } as Partial<Simulacao> });
    expect(calculando).toContain(M.calculando);
    expect(calculando).toContain(M.encerrarBotao);
    const erro = renderizar({ simulacao: { data: undefined, isError: true, error: new ErroApi({ status: 500, mensagem: 'Não foi possível calcular.' }) } as Partial<Simulacao> });
    expect(erro).toContain('Não foi possível calcular.');
    expect(erro).toContain('Tentar novamente');
    expect(erro).toContain(M.encerrarBotao);
  });

  it('locação cancelada: texto próprio e nenhum formulário', () => {
    const html = renderizar({ consulta: { data: { ...contexto, locacao: { ...contexto.locacao, status: 'CANCELADA' } } } });
    expect(html).toContain(M.cancelada);
    expect(html).not.toContain('<form');
    expect(html).not.toContain(M.encerrarBotao);
  });

  it('locação encerrada: só leitura, com data, total e veículo, sem formulário nem botão', () => {
    const html = renderizar({
      consulta: { data: { ...contexto, locacao: { ...contexto.locacao, status: 'FINALIZADA' } } },
      encerramento: { data: resultadoEncerrado, isSuccess: true } as Partial<Encerramento>,
    });
    expect(html).toContain(M.finalizadaTitulo('18/11/2026 10:15'));
    expect(html).toContain(M.valorTotalLocacao('R$ 360,00'));
    expect(html).toContain(M.veiculoEstado(true, formatarQuilometragem(1920)));
    expect(html).toContain(M.caucaoRetidaLiberada('R$ 160,00', 'R$ 1.040,00'));
    expect(html).toContain(MENSAGENS_COBRANCAS_EXTRAS.pagamentoLancado(76));
    expect(html).toContain(MENSAGENS_COBRANCAS_EXTRAS.pagamentoLancado(77));
    expect(html).not.toContain('<form');
    expect(html).not.toContain(M.encerrarBotao);
  });

  it('locação encerrada reaberta (sem a resposta desta sessão): a data vem da saída e não há valores da caução', () => {
    const html = renderizar({ consulta: { data: { ...contexto, locacao: { ...contexto.locacao, status: 'FINALIZADA' } } } });
    expect(html).toContain(M.finalizadaTitulo('18/11/2026 10:15'));
    expect(html).not.toContain(M.caucaoRetidaLiberada('R$ 160,00', 'R$ 1.040,00'));
    expect(html).not.toContain('<form');
  });

  it('carregando não mostra formulário nem botão', () => {
    const html = renderizar({ consulta: { isPending: true, data: undefined } });
    expect(html).toContain('Carregando');
    expect(html).not.toContain('<form');
    expect(html).not.toContain(M.encerrarBotao);
  });

  it('erro da consulta mostra a mensagem do servidor e Tentar novamente', () => {
    const html = renderizar({ consulta: { isError: true, data: undefined, error: new ErroApi({ status: 403, mensagem: 'Acesso negado.' }) } });
    expect(html).toContain('Acesso negado.');
    expect(html).toContain('Tentar novamente');
    expect(html).not.toContain('<form');
  });

  it('erro numa atualização com dados já carregados mantém a tela e avisa com role alert (não troca tudo por erro)', () => {
    const html = renderizar({ consulta: { isError: true, error: new ErroApi({ status: 500, mensagem: 'Não foi possível atualizar.' }) } });
    expect(html).toContain('role="alert"');
    expect(html).toContain('Não foi possível atualizar.');
    expect(html).toContain(M.encerrarBotao);
    expect(html).toContain(M.colunaRetirada);
  });

  it('ID inválido não consulta nada e mostra o texto de identificador', () => {
    const html = renderizar({ id: '0' });
    expect(html).toContain(M.idInvalido);
    expect(useDevolucao).toHaveBeenCalledWith(null);
  });

  it('atualização em curso marca a região como ocupada e desabilita o formulário da saída', () => {
    const html = renderizar({ consulta: { data: { ...contexto, comparacao: null }, isFetching: true } });
    expect(html).toContain('aria-busy="true"');
    expect(html).toContain('<fieldset disabled=""');
  });

  it('nenhum termo cortado do anexo aparece na tela aberta, com ou sem o cartão', () => {
    const html = renderizar();
    const presentes = TERMOS_CORTADOS_06.filter(termo => html.includes(termo));
    expect(presentes).toEqual([]);
    const semCartao = renderizar({ sessao: SESSAO_VENDEDOR });
    expect(TERMOS_CORTADOS_06.filter(termo => semCartao.includes(termo))).toEqual([]);
  });
});
