import { describe, expect, it } from 'vitest';
import { CARGO_FUNCIONARIO, type CargoFuncionario } from '@/types/enums';
import { contextoRetiradaComCaucaoTeste as comCaucao, contextoRetiradaTeste as contexto } from './contextoRetirada.fixture';
import { estadoCaucaoNaTela, type EstadoCaucaoNaTela } from './regrasCaucaoRetirada';
import { itensDaConferencia, retiradaDisponivel, type ConferenciaRetirada } from './validacaoRetirada';
import {
  botaoLigado,
  complementoErroAbertura,
  contarConferidos,
  linhaDeEnvio,
  mostrarTotalPrevisto,
  passosDaRetirada,
  prontoParaDespacho,
  selodaSituacaoEscuro,
  subtituloDaRetirada,
  textoAvisoCnhVencida,
  textoDiarias,
  textoResultadoItem,
  textoSituacaoReserva,
} from './regrasRetirada';

const NADA: ConferenciaRetirada = { cnhOriginal: false, titularidadeCartao: false, comprovanteResidencia: false, termoMultas: false };
/** Os três atestados de hoje ligados; a titularidade (só da caução) desligada. */
const TUDO: ConferenciaRetirada = { cnhOriginal: true, titularidadeCartao: false, comprovanteResidencia: true, termoMultas: true };
/** Os quatro atestados ligados (caução exigida). */
const TUDO_COM_CAUCAO: ConferenciaRetirada = { ...TUDO, titularidadeCartao: true };
const SEM_CAUCAO = itensDaConferencia(false);
const COM_CAUCAO = itensDaConferencia(true);
/** Caução que pode ser tentada, saída "Aprovar" e cargo que inicia com caução (ATENDENTE). */
const APROVAR: EstadoCaucaoNaTela = estadoCaucaoNaTela(comCaucao, 'APROVAR', 'ATENDENTE');
/** Cargos que NÃO iniciam a locação com a caução exigida, e a sessão sem cargo (ajuste A1). */
const SEM_PERMISSAO_COM_CAUCAO: readonly (CargoFuncionario | null)[] = ['VENDEDOR', 'GESTOR_FROTA', 'FINANCEIRO', 'ADMINISTRATIVO', null];

describe('contador da conferência', () => {
  it('sem caução, conta 0, 1, 2 e 3 itens ligados (a titularidade não conta)', () => {
    expect(contarConferidos(NADA, SEM_CAUCAO)).toBe(0);
    expect(contarConferidos({ ...NADA, cnhOriginal: true }, SEM_CAUCAO)).toBe(1);
    expect(contarConferidos({ ...NADA, cnhOriginal: true, comprovanteResidencia: true }, SEM_CAUCAO)).toBe(2);
    expect(contarConferidos(TUDO, SEM_CAUCAO)).toBe(3);
    expect(contarConferidos(TUDO_COM_CAUCAO, SEM_CAUCAO)).toBe(3);
    expect(contarConferidos({ ...NADA, titularidadeCartao: true }, SEM_CAUCAO)).toBe(0);
  });
  it('com caução, conta de 0 a 4, com a titularidade', () => {
    expect(contarConferidos(NADA, COM_CAUCAO)).toBe(0);
    expect(contarConferidos({ ...NADA, titularidadeCartao: true }, COM_CAUCAO)).toBe(1);
    expect(contarConferidos(TUDO, COM_CAUCAO)).toBe(3);
    expect(contarConferidos(TUDO_COM_CAUCAO, COM_CAUCAO)).toBe(4);
  });
  it('o total é o tamanho da lista em uso: 3 sem caução e 4 com caução', () => {
    expect(SEM_CAUCAO).toHaveLength(3);
    expect(COM_CAUCAO).toHaveLength(4);
  });
});

describe('subtítulo', () => {
  it('sem caução, o texto de hoje, sem "garantias"', () => {
    expect(subtituloDaRetirada(false)).toBe('Conferência inicial de documentação e validação de reserva para entrega das chaves.');
  });
  it('com caução, o texto do desenho, com "garantias"', () => {
    expect(subtituloDaRetirada(true)).toBe('Conferência inicial de documentação, garantias e validação de reserva para entrega das chaves.');
  });
});

describe('botão principal', () => {
  const disponivel = retiradaDisponivel(contexto);
  it('liga só com os três itens e a retirada disponível', () => {
    expect(botaoLigado(TUDO, disponivel, false)).toBe(true);
  });
  it('sem caução, a titularidade não é exigida (fica como hoje)', () => {
    expect(botaoLigado(TUDO, disponivel, false, estadoCaucaoNaTela(contexto, null, 'ATENDENTE'))).toBe(true);
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

describe('botão principal com a caução exigida', () => {
  const disponivel = retiradaDisponivel(comCaucao);
  it('liga com os quatro itens, a retirada disponível, a caução que pode ser tentada e uma saída escolhida', () => {
    expect(botaoLigado(TUDO_COM_CAUCAO, disponivel, false, APROVAR)).toBe(true);
    expect(botaoLigado(TUDO_COM_CAUCAO, disponivel, false, estadoCaucaoNaTela(comCaucao, 'RECUSAR', 'ATENDENTE'))).toBe(true);
    expect(botaoLigado(TUDO_COM_CAUCAO, disponivel, false, estadoCaucaoNaTela(comCaucao, 'ANALISE', 'ADMINISTRADOR'))).toBe(true);
  });
  it('fica desligado sem a titularidade do cartão (3 de 4)', () => {
    expect(botaoLigado(TUDO, disponivel, false, APROVAR)).toBe(false);
  });
  it('fica desligado sem saída escolhida na simulação', () => {
    expect(botaoLigado(TUDO_COM_CAUCAO, disponivel, false, estadoCaucaoNaTela(comCaucao, null, 'ATENDENTE'))).toBe(false);
  });
  it('fica desligado com a caução indisponível, mesmo com tudo marcado e uma saída escolhida', () => {
    expect(botaoLigado(TUDO_COM_CAUCAO, disponivel, false, estadoCaucaoNaTela({ ...comCaucao, caucaoDisponivel: false }, 'APROVAR', 'ATENDENTE'))).toBe(false);
    expect(botaoLigado(TUDO_COM_CAUCAO, disponivel, false, estadoCaucaoNaTela({ ...comCaucao, caucaoModoSimulacao: false }, 'APROVAR', 'ATENDENTE'))).toBe(false);
  });
  it.each(SEM_PERMISSAO_COM_CAUCAO)('fica desligado com o cargo %s, mesmo com 4 de 4 e a saída "Aprovar" (ajuste A1)', (cargo) => {
    expect(botaoLigado(TUDO_COM_CAUCAO, disponivel, false, estadoCaucaoNaTela(comCaucao, 'APROVAR', cargo))).toBe(false);
  });
  it('fica desligado durante o envio ou a recarga', () => {
    expect(botaoLigado(TUDO_COM_CAUCAO, disponivel, true, APROVAR)).toBe(false);
  });
  it('fica desligado com a retirada indisponível', () => {
    expect(botaoLigado(TUDO_COM_CAUCAO, retiradaDisponivel({ ...comCaucao, statusReserva: 'CANCELADA' }), false, APROVAR)).toBe(false);
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
  it('com caução, vale com 4 de 4, a retirada disponível, a caução disponível e uma saída escolhida', () => {
    expect(prontoParaDespacho(TUDO_COM_CAUCAO, retiradaDisponivel(comCaucao), APROVAR)).toBe(true);
  });
  it('com caução, não vale com 3 de 4, sem saída escolhida ou com a caução indisponível', () => {
    expect(prontoParaDespacho(TUDO, retiradaDisponivel(comCaucao), APROVAR)).toBe(false);
    expect(prontoParaDespacho(TUDO_COM_CAUCAO, retiradaDisponivel(comCaucao), estadoCaucaoNaTela(comCaucao, null, 'ATENDENTE'))).toBe(false);
    expect(prontoParaDespacho(TUDO_COM_CAUCAO, retiradaDisponivel(comCaucao), estadoCaucaoNaTela({ ...comCaucao, caucaoDisponivel: false }, 'APROVAR', 'ATENDENTE'))).toBe(false);
  });
  it.each(SEM_PERMISSAO_COM_CAUCAO)('com caução, não vale com o cargo %s, mesmo com 4 de 4 e a saída "Aprovar" (ajuste A1)', (cargo) => {
    expect(prontoParaDespacho(TUDO_COM_CAUCAO, retiradaDisponivel(comCaucao), estadoCaucaoNaTela(comCaucao, 'APROVAR', cargo))).toBe(false);
  });
  it('com caução, vale também com ADMINISTRADOR', () => {
    expect(prontoParaDespacho(TUDO_COM_CAUCAO, retiradaDisponivel(comCaucao), estadoCaucaoNaTela(comCaucao, 'APROVAR', 'ADMINISTRADOR'))).toBe(true);
  });
});

describe('sem caução, o cargo não pesa (RF-17 intacto)', () => {
  const disponivel = retiradaDisponivel(contexto);
  it.each([...CARGO_FUNCIONARIO, null])('cargo %s: botão ligado e pronto para despacho com os três itens', (cargo) => {
    const semCaucao = estadoCaucaoNaTela(contexto, null, cargo);
    expect(botaoLigado(TUDO, disponivel, false, semCaucao)).toBe(true);
    expect(prontoParaDespacho(TUDO, disponivel, semCaucao)).toBe(true);
  });
});

describe('complemento do erro da abertura', () => {
  const CONFLITO = 'Os dados da retirada mudaram. Confira os dados atualizados antes de continuar.';
  const RECUPERAR = 'Não foi possível confirmar a abertura. Os dados da reserva foram consultados novamente. Se a locação já foi criada, continue para a vistoria.';
  const TENTE_NOVAMENTE = 'Escolha de novo o resultado da simulação e tente outra vez.';
  it.each([true, false])('sem caução, como hoje: 409 é conflito (retirada disponível: %s)', (disponivelAgora) => {
    expect(complementoErroAbertura(409, false, disponivelAgora)).toBe(CONFLITO);
  });
  it.each([0, 400, 403, 404, 408, 429, 500, 502, 503, 504])('sem caução, como hoje: %s pede para recuperar', (status) => {
    expect(complementoErroAbertura(status, false, true)).toBe(RECUPERAR);
  });
  it('com caução, 409 com a retirada ainda disponível pede nova escolha na simulação', () => {
    expect(complementoErroAbertura(409, true, true)).toBe(TENTE_NOVAMENTE);
  });
  it('com caução, 409 com a retirada indisponível é conflito', () => {
    expect(complementoErroAbertura(409, true, false)).toBe(CONFLITO);
  });
  it.each([400, 403, 429])('com caução, %s não tem complemento (o texto do servidor basta)', (status) => {
    expect(complementoErroAbertura(status, true, true)).toBeNull();
    expect(complementoErroAbertura(status, true, false)).toBeNull();
  });
  it.each([0, 401, 404, 408, 500, 502, 503, 504])('com caução, %s pede para recuperar', (status) => {
    expect(complementoErroAbertura(status, true, true)).toBe(RECUPERAR);
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
