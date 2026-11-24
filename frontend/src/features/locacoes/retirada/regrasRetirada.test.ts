import { describe, expect, it } from 'vitest';
import { contextoRetiradaTeste as contexto } from './contextoRetirada.fixture';
import { retiradaDisponivel, type ConferenciaRetirada } from './validacaoRetirada';
import {
  botaoLigado,
  contarConferidos,
  linhaDeEnvio,
  mostrarTotalPrevisto,
  passosDaRetirada,
  prontoParaDespacho,
  selodaSituacaoEscuro,
  textoAvisoCnhVencida,
  textoDiarias,
  textoResultadoItem,
  textoSituacaoReserva,
} from './regrasRetirada';

const NADA: ConferenciaRetirada = { cnhOriginal: false, comprovanteResidencia: false, termoMultas: false };
const TUDO: ConferenciaRetirada = { cnhOriginal: true, comprovanteResidencia: true, termoMultas: true };

describe('contador da conferência', () => {
  it('conta 0, 1, 2 e 3 itens ligados', () => {
    expect(contarConferidos(NADA)).toBe(0);
    expect(contarConferidos({ ...NADA, cnhOriginal: true })).toBe(1);
    expect(contarConferidos({ ...NADA, cnhOriginal: true, comprovanteResidencia: true })).toBe(2);
    expect(contarConferidos(TUDO)).toBe(3);
  });
});

describe('botão principal', () => {
  const disponivel = retiradaDisponivel(contexto);
  it('liga só com os três itens e a retirada disponível', () => {
    expect(botaoLigado(TUDO, disponivel, false)).toBe(true);
  });
  it('fica desligado com dois itens', () => {
    expect(botaoLigado({ ...TUDO, termoMultas: false }, disponivel, false)).toBe(false);
  });
  it('desliga de novo quando um item é desmarcado', () => {
    expect(botaoLigado({ ...TUDO, cnhOriginal: false }, disponivel, false)).toBe(false);
  });
  it('fica desligado com três itens e reserva CANCELADA', () => {
    expect(botaoLigado(TUDO, retiradaDisponivel({ ...contexto, statusReserva: 'CANCELADA' }), false)).toBe(false);
  });
  it('fica desligado com três itens e veículo LOCADO', () => {
    expect(botaoLigado(TUDO, retiradaDisponivel({ ...contexto, statusVeiculo: 'LOCADO' }), false)).toBe(false);
  });
  it('fica desligado com três itens e locação já existente', () => {
    expect(botaoLigado(TUDO, retiradaDisponivel({ ...contexto, idLocacaoExistente: 42 }), false)).toBe(false);
  });
  it('fica desligado durante recarga ou envio', () => {
    expect(botaoLigado(TUDO, disponivel, true)).toBe(false);
  });
  it('não muda com CNH vencida: o aviso só informa', () => {
    expect(botaoLigado(TUDO, retiradaDisponivel({ ...contexto, cnhVencida: true }), false)).toBe(true);
  });
});

describe('pronto para despacho', () => {
  it('vale com os três itens e a retirada disponível', () => {
    expect(prontoParaDespacho(TUDO, retiradaDisponivel(contexto))).toBe(true);
  });
  it('não vale com a retirada indisponível', () => {
    expect(prontoParaDespacho(TUDO, retiradaDisponivel({ ...contexto, statusVeiculo: 'DISPONIVEL' }))).toBe(false);
  });
  it('não vale com itens faltando', () => {
    expect(prontoParaDespacho({ ...TUDO, comprovanteResidencia: false }, retiradaDisponivel(contexto))).toBe(false);
  });
});

describe('passo a passo', () => {
  it('tem quatro etapas com os nomes do desenho', () => {
    expect(passosDaRetirada(true).map((passo) => passo.nome)).toEqual(['Conferência', 'Vistoria', 'Contrato', 'Entrega']);
  });
  it('com a retirada disponível, só a etapa 1 é ativa', () => {
    expect(passosDaRetirada(true).map((passo) => passo.ativa)).toEqual([true, false, false, false]);
  });
  it('com CNH vencida, a etapa 1 continua a única ativa', () => {
    expect(passosDaRetirada(retiradaDisponivel({ ...contexto, cnhVencida: true })).map((passo) => passo.ativa)).toEqual([true, false, false, false]);
  });
  it('sem a retirada disponível, nenhuma etapa é ativa (locação existente, reserva CANCELADA ou CONCLUIDA, veículo diferente de RESERVADO)', () => {
    expect(passosDaRetirada(retiradaDisponivel({ ...contexto, idLocacaoExistente: 42 })).some((passo) => passo.ativa)).toBe(false);
    expect(passosDaRetirada(retiradaDisponivel({ ...contexto, statusReserva: 'CANCELADA' })).some((passo) => passo.ativa)).toBe(false);
    expect(passosDaRetirada(retiradaDisponivel({ ...contexto, statusReserva: 'CONCLUIDA' })).some((passo) => passo.ativa)).toBe(false);
    expect(passosDaRetirada(retiradaDisponivel({ ...contexto, statusVeiculo: 'LOCADO' })).some((passo) => passo.ativa)).toBe(false);
  });
});

describe('aviso de CNH vencida', () => {
  it('mostra a data quando a CNH está vencida', () => {
    expect(textoAvisoCnhVencida({ ...contexto, cnhVencida: true, cnhValidadeStr: '17/11/2026' }))
      .toBe('CNH vencida: a validade terminou em 17/11/2026. O sistema não bloqueia a retirada; confira o documento com o cliente.');
  });
  it('não mostra nada quando a CNH está em dia', () => {
    expect(textoAvisoCnhVencida(contexto)).toBeNull();
  });
});

describe('situação da reserva', () => {
  it('mostra o rótulo real de cada status', () => {
    expect(textoSituacaoReserva('ATIVA')).toBe('Reserva Ativa');
    expect(textoSituacaoReserva('CONFIRMADA')).toBe('Reserva Confirmada');
    expect(textoSituacaoReserva('CANCELADA')).toBe('Reserva Cancelada');
    expect(textoSituacaoReserva('CONCLUIDA')).toBe('Reserva Concluída');
  });
  it('usa o selo escuro só para ATIVA e CONFIRMADA', () => {
    expect(selodaSituacaoEscuro('ATIVA')).toBe(true);
    expect(selodaSituacaoEscuro('CONFIRMADA')).toBe(true);
    expect(selodaSituacaoEscuro('CANCELADA')).toBe(false);
    expect(selodaSituacaoEscuro('CONCLUIDA')).toBe(false);
  });
});

describe('diárias', () => {
  it('usa o singular com uma diária e o plural nos demais casos', () => {
    expect(textoDiarias(1)).toBe('1 diária completa contratada');
    expect(textoDiarias(4)).toBe('4 diárias completas contratadas');
  });
});

describe('linha de envio de documento', () => {
  const comData = (data: string) => `Enviada pelo cliente em ${data}.`;
  const semEnvio = 'O cliente não enviou a CNH pelo site.';
  it('mostra a data e a hora quando o cliente enviou', () => {
    expect(linhaDeEnvio('10/11/2026 10:30', comData, semEnvio)).toBe('Enviada pelo cliente em 10/11/2026 10:30.');
  });
  it('mostra o texto de não enviado quando o Str vem vazio', () => {
    expect(linhaDeEnvio('', comData, semEnvio)).toBe(semEnvio);
  });
});

describe('linha de resultado do item', () => {
  it('diz quem conferiu, nesta tela', () => {
    expect(textoResultadoItem('Ana Souza')).toBe('Conferido nesta tela por Ana Souza');
  });
  it('sem nome disponível, usa a variante sem nome', () => {
    expect(textoResultadoItem(null)).toBe('Conferido nesta tela pelo atendente');
    expect(textoResultadoItem('   ')).toBe('Conferido nesta tela pelo atendente');
  });
});

describe('total previsto', () => {
  it('aparece quando há valor estimado', () => {
    expect(mostrarTotalPrevisto('R$ 480,00')).toBe(true);
  });
  it('some quando o valor estimado vem vazio', () => {
    expect(mostrarTotalPrevisto('')).toBe(false);
  });
});
