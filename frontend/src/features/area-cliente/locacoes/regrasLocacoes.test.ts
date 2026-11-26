import { describe, expect, it } from 'vitest';
import {
  calcularContagemRegressiva,
  combustivelCheio,
  doisDigitos,
  locacaoEstaAtrasada,
  mensagemErroLista,
  percentualCombustivel,
  percentualProgresso,
  periodoDaLocacao,
  ultimaPaginaLocacoes,
  valorTotalDaLocacao,
} from '@/features/area-cliente/locacoes/regrasLocacoes';
import { MENSAGEM_ERRO_SERVIDOR, MENSAGEM_SESSAO_EXPIRADA } from '@/lib/mensagens';
import {
  mensagemLocacoesCombustivel,
  mensagemLocacoesDentroDoPrazo,
  MENSAGEM_LOCACOES_ERRO_LISTA,
} from '@/lib/mensagensLocacoesCliente';
import { ErroApi } from '@/types/api';

describe('mensagemErroLista', () => {
  it('sem resposta do servidor (status 0) ou falha 500, vale o texto ERRO-1 aprovado', () => {
    expect(mensagemErroLista(new ErroApi({ status: 0, mensagem: 'Falha de conexão' }))).toBe(MENSAGEM_LOCACOES_ERRO_LISTA);
    expect(mensagemErroLista(new Error('rede'))).toBe(MENSAGEM_LOCACOES_ERRO_LISTA);
    expect(mensagemErroLista(new ErroApi({ status: 500, mensagem: MENSAGEM_ERRO_SERVIDOR }))).toBe(
      MENSAGEM_LOCACOES_ERRO_LISTA,
    );
  });

  it('sessão expirada (401) mantém a mensagem tratada pela API, sem o texto da lista', () => {
    expect(mensagemErroLista(new ErroApi({ status: 401, mensagem: MENSAGEM_SESSAO_EXPIRADA }))).toBe(MENSAGEM_SESSAO_EXPIRADA);
  });
});

/** Relógio local para os testes: a mesma hora local que o servidor usa (sem fuso no texto). */
function relogio(ano: number, mes: number, dia: number, hora: number, minuto: number, segundo = 0): Date {
  return new Date(ano, mes - 1, dia, hora, minuto, segundo);
}

describe('calcularContagemRegressiva (relógio injetado)', () => {
  const DEVOLUCAO = '2026-11-18T10:00:00';

  it('conta dias, horas, minutos e segundos até a devolução', () => {
    expect(calcularContagemRegressiva(DEVOLUCAO, relogio(2026, 11, 16, 10, 0))).toEqual({
      dias: 2,
      horas: 0,
      minutos: 0,
      segundos: 0,
      vencida: false,
    });
  });

  it('um segundo antes da devolução ainda está no prazo, com 1 segundo', () => {
    expect(calcularContagemRegressiva(DEVOLUCAO, relogio(2026, 11, 18, 9, 59, 59))).toEqual({
      dias: 0,
      horas: 0,
      minutos: 0,
      segundos: 1,
      vencida: false,
    });
  });

  it('exatamente na hora da devolução: zero, e ainda não é atraso', () => {
    expect(calcularContagemRegressiva(DEVOLUCAO, relogio(2026, 11, 18, 10, 0))).toEqual({
      dias: 0,
      horas: 0,
      minutos: 0,
      segundos: 0,
      vencida: false,
    });
  });

  it('um segundo depois da devolução: vencida, com os blocos em zero', () => {
    expect(calcularContagemRegressiva(DEVOLUCAO, relogio(2026, 11, 18, 10, 0, 1))).toEqual({
      dias: 0,
      horas: 0,
      minutos: 0,
      segundos: 0,
      vencida: true,
    });
  });

  it('virada do dia: 23:59:59 do dia anterior faltam 1 segundo, não 1 dia', () => {
    expect(calcularContagemRegressiva('2026-11-18T00:00:00', relogio(2026, 11, 17, 23, 59, 59))).toEqual({
      dias: 0,
      horas: 0,
      minutos: 0,
      segundos: 1,
      vencida: false,
    });
  });

  it('lê também o formato dd/MM/yyyy HH:mm que o servidor manda nas datas formatadas', () => {
    const contagem = calcularContagemRegressiva('18/11/2026 10:00', relogio(2026, 11, 17, 10, 0));
    expect(contagem?.dias).toBe(1);
    expect(contagem?.vencida).toBe(false);
  });

  it('data que não pode ser lida devolve null, para a tela mostrar o travessão', () => {
    expect(calcularContagemRegressiva('31/02/2026 10:00', relogio(2026, 11, 17, 10, 0))).toBeNull();
  });
});

describe('locacaoEstaAtrasada', () => {
  it('sem marca do servidor e sem contagem vencida, não está atrasada', () => {
    expect(locacaoEstaAtrasada(false, { dias: 0, horas: 1, minutos: 0, segundos: 0, vencida: false })).toBe(false);
  });

  it('marcada como ATRASADA pelo servidor, mesmo sem contagem', () => {
    expect(locacaoEstaAtrasada(true, null)).toBe(true);
  });

  it('a contagem local já venceu, mesmo sem a marca do servidor', () => {
    expect(locacaoEstaAtrasada(false, { dias: 0, horas: 0, minutos: 0, segundos: 0, vencida: true })).toBe(true);
  });
});

describe('doisDigitos', () => {
  it('completa com zero à esquerda até dois dígitos', () => {
    expect(doisDigitos(3)).toBe('03');
    expect(doisDigitos(12)).toBe('12');
    expect(doisDigitos(0)).toBe('00');
  });
});

describe('percentualProgresso', () => {
  const RETIRADA = '2026-11-14T10:00:00';
  const DEVOLUCAO = '2026-11-18T10:00:00';

  it('metade do caminho entre a retirada e a devolução dá 50', () => {
    expect(percentualProgresso(RETIRADA, DEVOLUCAO, relogio(2026, 11, 16, 10, 0))).toBe(50);
  });

  it('antes da retirada, fica em 0', () => {
    expect(percentualProgresso(RETIRADA, DEVOLUCAO, relogio(2026, 11, 10, 10, 0))).toBe(0);
  });

  it('depois da devolução, fica em 100', () => {
    expect(percentualProgresso(RETIRADA, DEVOLUCAO, relogio(2026, 11, 25, 10, 0))).toBe(100);
  });

  it('devolução igual à retirada não divide por zero: vale 100', () => {
    expect(percentualProgresso(RETIRADA, RETIRADA, relogio(2026, 11, 14, 10, 0))).toBe(100);
  });

  it('data que não pode ser lida devolve null', () => {
    expect(percentualProgresso('nao-e-data', DEVOLUCAO, relogio(2026, 11, 16, 10, 0))).toBeNull();
  });
});

describe('combustível na retirada', () => {
  it('mostra o nível como percentual', () => {
    expect(percentualCombustivel(0.5)).toBe('50%');
    expect(percentualCombustivel(0.33)).toBe('33%');
  });

  it('só é "cheio" quando o percentual arredondado dá 100', () => {
    expect(combustivelCheio(1)).toBe(true);
    expect(combustivelCheio(0.995)).toBe(true);
    expect(combustivelCheio(0.5)).toBe(false);
  });

  it('o texto ganha "(cheio)" só no tanque cheio', () => {
    expect(mensagemLocacoesCombustivel('100%', true)).toBe('100% (cheio)');
    expect(mensagemLocacoesCombustivel('50%', false)).toBe('50%');
  });
});

describe('textos do cronômetro', () => {
  it('com tolerância, mostra o número de minutos', () => {
    expect(mensagemLocacoesDentroDoPrazo(59)).toBe('Dentro do prazo • Tolerância: 59 min');
  });

  it('sem tolerância da categoria, mostra só "Dentro do prazo"', () => {
    expect(mensagemLocacoesDentroDoPrazo(null)).toBe('Dentro do prazo');
  });
});

describe('dados de exibição da locação', () => {
  it('o período mostra só as datas, com a devolução efetiva quando existe', () => {
    expect(
      periodoDaLocacao({
        dataRetirada: '2026-10-18T10:00:00',
        dataDevolucaoEfetiva: '2026-10-21T09:40:00',
        dataDevolucaoPrevista: '2026-10-21T10:00:00',
      }),
    ).toBe('18/10/2026 a 21/10/2026');
  });

  it('locação sem devolução efetiva usa a devolução prevista, também só com as datas', () => {
    expect(
      periodoDaLocacao({
        dataRetirada: '2026-11-14T10:00:00',
        dataDevolucaoEfetiva: null,
        dataDevolucaoPrevista: '2026-11-18T10:00:00',
      }),
    ).toBe('14/11/2026 a 18/11/2026');
  });

  it('aceita datas sem hora (ISO só com a data) e não desloca o dia', () => {
    expect(
      periodoDaLocacao({
        dataRetirada: '2026-10-18',
        dataDevolucaoEfetiva: '2026-10-21',
        dataDevolucaoPrevista: '2026-10-21',
      }),
    ).toBe('18/10/2026 a 21/10/2026');
  });

  it('valor total ausente (em andamento ou cancelada) vira o travessão', () => {
    expect(valorTotalDaLocacao({ valorTotalStr: null })).toBe('—');
    expect(valorTotalDaLocacao({ valorTotalStr: 'R$ 780,00' })).toBe('R$ 780,00');
  });
});

describe('ultimaPaginaLocacoes', () => {
  it('sem itens, a última página é a 1', () => {
    expect(ultimaPaginaLocacoes(0, 5)).toBe(1);
  });

  it('arredonda para cima pelo tamanho da página', () => {
    expect(ultimaPaginaLocacoes(10, 5)).toBe(2);
    expect(ultimaPaginaLocacoes(11, 5)).toBe(3);
  });
});
