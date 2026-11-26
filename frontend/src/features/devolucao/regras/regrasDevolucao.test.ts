import { describe, expect, it } from 'vitest';
import { formatarMoeda } from '@/lib/formatters';
import { MENSAGENS_DEVOLUCAO as M } from '@/lib/mensagensDevolucao';
import { ErroApi } from '@/types/api';
import type { AvariaComparacao, ContextoDevolucao, ResultadoEncerramento, VistoriaDevolucao } from '@/types/devolucao';
import {
  CARGOS_ENCERRAM_LOCACAO,
  dadosDeEncerramento,
  desfechoCaucaoPrevisto,
  deveDesmarcarAposFalha,
  falhaDoEncerramento,
  faseDevolucao,
  kmRodados,
  locacaoAberta,
  numeroNovaAvaria,
  podeConfirmarEncerramento,
  podeEncerrarLocacao,
  podeIniciarEncerramento,
  quilometrosRodados,
  rotuloSeloTotal,
  temDadosParaEncerrar,
  textoCaucaoEncerrada,
  textoEstadoDevolucao,
  textoFranquia,
  valorCaucaoPrevista,
  avariasNumeradas,
  estaAtrasadaDevolucao,
} from './regrasDevolucao';

const entrada: VistoriaDevolucao = {
  idVistoria: 1, idLocacao: 42, idFuncionario: 3, tipo: 'ENTRADA', dataHoraStr: '18/11/2026 09:00', kmRegistrado: 100,
  nivelCombustivel: 1, observacoes: null, operadorStr: 'Marina C.',
};
const saida: VistoriaDevolucao = { ...entrada, idVistoria: 2, tipo: 'SAIDA', kmRegistrado: 1020, nivelCombustivel: 0.5, dataHoraStr: '18/11/2026 10:15' };
const avaria: AvariaComparacao = { idAvaria: 7, idVistoria: 1, idAvariaEntrada: null, descricao: 'Risco', gravidade: 'LEVE', valorEstimado: 50, valorEstimadoStr: 'R$ 50,00' };

function contextoBase(parcial: Partial<ContextoDevolucao> = {}, locacao: Partial<ContextoDevolucao['locacao']> = {}): ContextoDevolucao {
  return {
    locacao: { idLocacao: 42, idReserva: 8, idVeiculo: 3, kmSaida: 100, status: 'EM_ANDAMENTO', valorCaucao: 1200, ...locacao },
    entrada,
    avariasEntrada: [avaria],
    comparacao: { entrada, saida, comparacaoRealizada: true, avariasPreexistentes: [], avariasNovas: [], avariasIndeterminadas: [] },
    placa: 'ABC1D23', marca: 'Fiat', modelo: 'Toro', diariasPrevistas: 2, valorDiaria: 100, valorDiariaStr: 'R$ 100,00',
    totalDiarias: 200, totalDiariasStr: 'R$ 200,00', totalExtras: 160, totalExtrasStr: 'R$ 160,00', valorTotalPrevisto: 360,
    valorTotalPrevistoStr: 'R$ 360,00', idConfiguracao: 3, versaoEsperada: 7, caucao: { situacao: 'PENDENTE', acao: null },
    ...parcial,
  };
}

describe('fase da tela 06 (estados A a D do anexo)', () => {
  it('sem vistoria de entrada vem antes de tudo', () => {
    expect(faseDevolucao(contextoBase({ entrada: null, comparacao: null }))).toBe('SEM_ENTRADA');
  });
  it('cancelada e finalizada são só leitura; sem saída mostra o formulário; com saída é aberta', () => {
    expect(faseDevolucao(contextoBase({}, { status: 'CANCELADA' }))).toBe('CANCELADA');
    expect(faseDevolucao(contextoBase({}, { status: 'FINALIZADA' }))).toBe('FINALIZADA');
    expect(faseDevolucao(contextoBase({ comparacao: null }))).toBe('SEM_SAIDA');
    expect(faseDevolucao(contextoBase())).toBe('ABERTA');
    expect(faseDevolucao(contextoBase({}, { status: 'ATRASADA' }))).toBe('ABERTA');
  });
});

describe('estado do cabeçalho', () => {
  it('usa os rótulos aprovados; Atrasada não substitui o estado da fase (sai num selo à parte)', () => {
    expect(textoEstadoDevolucao('SEM_SAIDA')).toBe(M.estadoAguardandoSaida);
    expect(textoEstadoDevolucao('ABERTA')).toBe(M.estadoAguardandoEncerramento);
    expect(textoEstadoDevolucao('FINALIZADA')).toBe(M.estadoEncerrada);
    expect(textoEstadoDevolucao('CANCELADA')).toBeNull();
    expect(textoEstadoDevolucao('SEM_ENTRADA')).toBeNull();
  });

  it('o selo Atrasada aparece só para a locação atrasada', () => {
    expect(estaAtrasadaDevolucao('ATRASADA')).toBe(true);
    expect(estaAtrasadaDevolucao('EM_ANDAMENTO')).toBe(false);
    expect(estaAtrasadaDevolucao('FINALIZADA')).toBe(false);
    expect(estaAtrasadaDevolucao('CANCELADA')).toBe(false);
  });
  it('locação aberta é em andamento ou atrasada', () => {
    expect(locacaoAberta('EM_ANDAMENTO')).toBe(true);
    expect(locacaoAberta('ATRASADA')).toBe(true);
    expect(locacaoAberta('FINALIZADA')).toBe(false);
    expect(locacaoAberta('CANCELADA')).toBe(false);
  });
});

describe('permissão de encerrar (D6): só atendente e administrador', () => {
  it('libera ATENDENTE e ADMINISTRADOR e recusa os demais cargos e a falta de cargo', () => {
    expect(CARGOS_ENCERRAM_LOCACAO).toEqual(['ATENDENTE', 'ADMINISTRADOR']);
    expect(podeEncerrarLocacao('ATENDENTE')).toBe(true);
    expect(podeEncerrarLocacao('ADMINISTRADOR')).toBe(true);
    for (const cargo of ['VENDEDOR', 'GESTOR_FROTA', 'FINANCEIRO', 'ADMINISTRATIVO'] as const) {
      expect(podeEncerrarLocacao(cargo)).toBe(false);
    }
    expect(podeEncerrarLocacao(null)).toBe(false);
  });
});

describe('caução pelos quatro casos da D2 (só exibição; o servidor decide)', () => {
  it('sem caução e sem extras: não há o que dizer', () => {
    expect(desfechoCaucaoPrevisto(0, 0)).toEqual({ temCaucao: false, retido: 0, liberado: 0, selo: 'SEM_COBRANCAS', frase: '' });
  });
  it('sem caução e com extras: cobra por PIX', () => {
    const desfecho = desfechoCaucaoPrevisto(0, 50);
    expect(desfecho).toMatchObject({ temCaucao: false, selo: 'A_COBRAR_POR_PIX', frase: M.semCaucao });
  });
  it('sem extras: libera a caução por inteiro', () => {
    expect(desfechoCaucaoPrevisto(1200, 0)).toEqual({
      temCaucao: true, retido: 0, liberado: 1200, selo: 'SEM_COBRANCAS', frase: M.caucaoSemExtras(formatarMoeda(1200)),
    });
  });
  it('extras até a caução: retém só as extras e libera o resto (com centavos exatos)', () => {
    const desfecho = desfechoCaucaoPrevisto(1200, 395.6);
    expect(desfecho.retido).toBe(395.6);
    expect(desfecho.liberado).toBe(804.4);
    expect(desfecho.selo).toBe('LIQUIDADO_NA_CAUCAO');
    expect(desfecho.frase).toBe(M.caucaoRetida(formatarMoeda(395.6), formatarMoeda(804.4)));
  });
  it('extras iguais à caução: retém tudo e libera zero', () => {
    expect(desfechoCaucaoPrevisto(1200, 1200)).toMatchObject({ retido: 1200, liberado: 0, selo: 'LIQUIDADO_NA_CAUCAO' });
  });
  it('extras acima da caução: libera a caução e cobra por PIX', () => {
    const desfecho = desfechoCaucaoPrevisto(1200, 1500);
    expect(desfecho).toMatchObject({ retido: 0, liberado: 1200, selo: 'A_COBRAR_POR_PIX' });
    expect(desfecho.frase).toBe(M.caucaoAcimaDaCaucao(formatarMoeda(1500), formatarMoeda(1200)));
  });
  it('valores inválidos contam como zero', () => {
    expect(desfechoCaucaoPrevisto(Number.NaN, 0)).toMatchObject({ temCaucao: false, selo: 'SEM_COBRANCAS' });
    expect(desfechoCaucaoPrevisto(1200, Number.NaN)).toMatchObject({ retido: 0, liberado: 1200, selo: 'SEM_COBRANCAS' });
  });
  it('rótulo do selo do total', () => {
    expect(rotuloSeloTotal('LIQUIDADO_NA_CAUCAO')).toBe(M.seloLiquidado);
    expect(rotuloSeloTotal('A_COBRAR_POR_PIX')).toBe(M.seloAcobrarPix);
    expect(rotuloSeloTotal('SEM_COBRANCAS')).toBe(M.seloSemCobrancas);
  });
});

describe('texto da caução depois do encerramento', () => {
  function resultado(caucao: Partial<ResultadoEncerramento['caucao']>, totalExtras: number): ResultadoEncerramento {
    return {
      locacao: {
        idLocacao: 42, status: 'FINALIZADA', kmSaida: 100, kmChegada: 1020, dataDevolucaoEfetiva: '2026-11-18T10:15:00',
        dataDevolucaoEfetivaStr: '18/11/2026 10:15', diariasPrevistas: 2, valorDiaria: 100, valorDiariaStr: 'R$ 100,00',
        valorTotal: 360, valorTotalStr: 'R$ 360,00',
      },
      veiculo: { idVeiculo: 3, status: 'DISPONIVEL', kmAtual: 1020 },
      cobrancas: {
        totalExtras, totalExtrasStr: 'R$ 160,00', totalDiarias: 200, totalDiariasStr: 'R$ 200,00',
        idPagamentoCobrancaExtra: 77, idPagamentoDiaria: 76,
      },
      caucao: {
        acao: 'SEM_CAUCAO', valorReservado: 0, valorReservadoStr: 'R$ 0,00', valorRetido: 0, valorRetidoStr: 'R$ 0,00',
        valorLiberado: 0, valorLiberadoStr: 'R$ 0,00', statusPagamento: 'PENDENTE', tratarAMao: false, ...caucao,
      },
      avisos: [],
    };
  }
  it('baixada sem extras: liberada por inteiro; baixada com extras: por PIX', () => {
    expect(textoCaucaoEncerrada(resultado({ acao: 'BAIXADA' }, 0))).toBe(M.caucaoLiberadaInteira);
    expect(textoCaucaoEncerrada(resultado({ acao: 'BAIXADA' }, 160))).toBe(M.caucaoLiberadaComPix('R$ 160,00'));
  });
  it('retida mostra o retido e o liberado da resposta', () => {
    expect(textoCaucaoEncerrada(resultado({ acao: 'RETIDA_PARCIAL', valorRetidoStr: 'R$ 160,00', valorLiberadoStr: 'R$ 1.040,00' }, 160)))
      .toBe(M.caucaoRetidaLiberada('R$ 160,00', 'R$ 1.040,00'));
  });
  it('não confirmada e sem caução não têm frase própria (o servidor manda o aviso)', () => {
    expect(textoCaucaoEncerrada(resultado({ acao: 'NAO_CONFIRMADA_NO_PROVEDOR' }, 160))).toBeNull();
    expect(textoCaucaoEncerrada(resultado({ acao: 'SEM_CAUCAO' }, 0))).toBeNull();
  });
});

describe('encerramento: corpo, pré-condições e falhas', () => {
  it('o corpo leva só política, versão e total do servidor', () => {
    expect(dadosDeEncerramento(contextoBase())).toEqual({ idConfiguracao: 3, versaoEsperada: 7, totalConfirmado: 360 });
  });
  it('sem política, versão ou total não há corpo nem caixa', () => {
    expect(dadosDeEncerramento(contextoBase({ idConfiguracao: null }))).toBeNull();
    expect(dadosDeEncerramento(contextoBase({ versaoEsperada: null }))).toBeNull();
    expect(dadosDeEncerramento(contextoBase({ valorTotalPrevisto: null, valorTotalPrevistoStr: null }))).toBeNull();
    expect(temDadosParaEncerrar(contextoBase({ idConfiguracao: null }))).toBe(false);
    expect(temDadosParaEncerrar(contextoBase())).toBe(true);
  });
  it('409 recarrega e pede nova conferência; 502 e queda de conexão permitem tentar de novo', () => {
    expect(falhaDoEncerramento(new ErroApi({ status: 409, mensagem: 'O total mudou.' }))).toEqual({
      mensagem: 'O total mudou.', complemento: M.complemento409, tentarDeNovo: false, recarregar: true,
    });
    expect(falhaDoEncerramento(new ErroApi({ status: 502, mensagem: 'Provedor fora.' }))).toEqual({
      mensagem: 'Provedor fora.', complemento: M.complementoFalha, tentarDeNovo: true, recarregar: false,
    });
    expect(falhaDoEncerramento(new ErroApi({ status: 0, mensagem: 'Sem conexão.' })).tentarDeNovo).toBe(true);
  });
  it('outros erros mostram só a mensagem do servidor, sem complemento', () => {
    expect(falhaDoEncerramento(new ErroApi({ status: 500, mensagem: 'Não foi possível encerrar a locação.' }))).toEqual({
      mensagem: 'Não foi possível encerrar a locação.', complemento: null, tentarDeNovo: false, recarregar: false,
    });
    expect(falhaDoEncerramento(new Error('x')).mensagem).not.toBe('x');
  });
  it('confirmação só com a caixa marcada, sem envio e com total', () => {
    expect(podeConfirmarEncerramento({ confirmado: true, enviando: false, temDados: true })).toBe(true);
    expect(podeConfirmarEncerramento({ confirmado: false, enviando: false, temDados: true })).toBe(false);
    expect(podeConfirmarEncerramento({ confirmado: true, enviando: true, temDados: true })).toBe(false);
    expect(podeConfirmarEncerramento({ confirmado: true, enviando: false, temDados: false })).toBe(false);
  });
});

describe('caução baixada não entra na previsão (CANCELADO vira sem caução)', () => {
  it('CANCELADO vira zero e as outras situações mantêm o valor da locação', () => {
    expect(valorCaucaoPrevista(1200, 'CANCELADO')).toBe(0);
    expect(valorCaucaoPrevista(1200, 'PENDENTE')).toBe(1200);
    expect(valorCaucaoPrevista(1200, 'PAGO')).toBe(1200);
    expect(valorCaucaoPrevista(1200, 'SEM_CAUCAO')).toBe(1200);
  });
  it('com a caução baixada, a previsão é a frase de PIX, sem retenção', () => {
    const desfecho = desfechoCaucaoPrevisto(valorCaucaoPrevista(1200, 'CANCELADO'), 160);
    expect(desfecho).toMatchObject({ temCaucao: false, retido: 0, selo: 'A_COBRAR_POR_PIX', frase: M.semCaucao });
  });
});

describe('envio e marcação da caixa (duplo envio e reset depois do 409)', () => {
  it('envio só com dados, sem envio em curso e sem consulta ocupada', () => {
    expect(podeIniciarEncerramento({ temDados: true, enviando: false, ocupado: false })).toBe(true);
    expect(podeIniciarEncerramento({ temDados: false, enviando: false, ocupado: false })).toBe(false);
    expect(podeIniciarEncerramento({ temDados: true, enviando: true, ocupado: false })).toBe(false);
    expect(podeIniciarEncerramento({ temDados: true, enviando: false, ocupado: true })).toBe(false);
  });
  it('só o 409 desfaz a marcação; 502 e erro comum não', () => {
    expect(deveDesmarcarAposFalha(falhaDoEncerramento(new ErroApi({ status: 409, mensagem: 'x' })))).toBe(true);
    expect(deveDesmarcarAposFalha(falhaDoEncerramento(new ErroApi({ status: 502, mensagem: 'x' })))).toBe(false);
    expect(deveDesmarcarAposFalha(null)).toBe(false);
  });
});

describe('textos aprovados travados com a string literal (o teste compara com o texto do anexo, não com a constante)', () => {
  it('linha de rodados e franquia, sem ponto final', () => {
    expect(M.rodados('920', '100 km no total')).toBe('Rodados: 920 km • Franquia: 100 km no total');
    expect(M.rodadosSemFranquia('920')).toBe('Rodados: 920 km');
    expect(M.franquiaContratada('100 km no total')).toBe('Franquia contratada: 100 km no total.');
  });
  it('botão, nota e caixa de confirmação', () => {
    expect(M.encerrarBotao).toBe('Encerrar locação');
    expect(M.encerrarNota).toBe('Ao encerrar, a locação é finalizada, a caução é liberada ou retida, a diária e as cobranças extras são lançadas e o veículo volta a ficar Disponível. Não é possível reabrir.');
    expect(M.caixa.confirmo).toBe('Conferi a comparação, as cobranças extras e a caução. Confirmo o encerramento desta locação. Esta ação não pode ser desfeita.');
    expect(M.caixa.confirmar).toBe('Confirmar encerramento');
    expect(M.caixa.cancelar).toBe('Cancelar');
  });
  it('escala e divergência', () => {
    expect(M.escalaVazio).toBe('Vazio (0/8)');
    expect(M.escalaMeio).toBe('4/8 (50%)');
    expect(M.divergencia(2)).toBe('Divergência de 2/8 de tanque');
    expect(M.reposicao('R$ 117,00')).toBe('Cobrança Reposição: R$ 117,00');
  });
});

describe('números e textos de apoio', () => {
  it('avarias novas continuam a contagem depois das da entrada (#4, #5)', () => {
    expect(numeroNovaAvaria(3, 0)).toBe(4);
    expect(numeroNovaAvaria(3, 1)).toBe(5);
  });
  it('avarias da entrada numeradas pela posição', () => {
    expect(avariasNumeradas([avaria, { ...avaria, idAvaria: 8 }]).map(item => item.posicao)).toEqual([1, 2]);
  });
  it('quilômetros rodados nunca são negativos', () => {
    expect(kmRodados(entrada, saida)).toBe(920);
    expect(quilometrosRodados(500, 400)).toBe(0);
  });
  it('franquia no mesmo padrão do simulador (base total ou por diária)', () => {
    expect(textoFranquia(100, true)).toBe('100 km no total');
    expect(textoFranquia(100, false)).toBe('100 km por diária prevista');
  });
});
