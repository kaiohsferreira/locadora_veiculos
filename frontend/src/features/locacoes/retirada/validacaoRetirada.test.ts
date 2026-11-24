import { describe, expect, it } from 'vitest';
import {
  esquemaConferenciaRetirada,
  ITENS_CONFERENCIA,
  ITENS_CONFERENCIA_COM_CAUCAO,
  itensDaConferencia,
  lerIdReserva,
  retiradaDisponivel,
  SEM_CONFERENCIA,
} from './validacaoRetirada';
import { contextoRetiradaComCaucaoTeste, contextoRetiradaTeste as contexto } from './contextoRetirada.fixture';
describe('validação da retirada', () => {
  it.each([undefined, '', '0', '-1', '1.2', '1e2', ' 8', '01', '2147483648'])('rejeita id %s', (valor) => expect(lerIdReserva(valor)).toBeNull());
  it('aceita id inteiro de domínio', () => expect(lerIdReserva('8')).toBe(8));
  it.each([
    [false, false, false],
    [true, false, false],
    [false, true, false],
    [false, false, true],
    [true, true, false],
    [true, false, true],
    [false, true, true],
  ])('sem caução, exige os três itens da conferência (%s, %s, %s)', (cnhOriginal, comprovanteResidencia, termoMultas) => {
    const esquema = esquemaConferenciaRetirada(itensDaConferencia(false));
    expect(esquema.safeParse({ cnhOriginal, titularidadeCartao: true, comprovanteResidencia, termoMultas }).success).toBe(false);
  });
  it('sem caução, aceita os três itens conferidos e não exige a titularidade do cartão', () => {
    const esquema = esquemaConferenciaRetirada(itensDaConferencia(false));
    expect(esquema.safeParse({ cnhOriginal: true, titularidadeCartao: false, comprovanteResidencia: true, termoMultas: true }).success).toBe(true);
  });
  it('com caução, exige os quatro itens: falta a titularidade, não passa', () => {
    const esquema = esquemaConferenciaRetirada(itensDaConferencia(true));
    expect(esquema.safeParse({ cnhOriginal: true, titularidadeCartao: false, comprovanteResidencia: true, termoMultas: true }).success).toBe(false);
    expect(esquema.safeParse({ cnhOriginal: true, titularidadeCartao: true, comprovanteResidencia: true, termoMultas: true }).success).toBe(true);
  });
  it.each(ITENS_CONFERENCIA_COM_CAUCAO.map((item) => [item]))('com caução, falta %s e não passa', (item) => {
    const esquema = esquemaConferenciaRetirada(itensDaConferencia(true));
    expect(esquema.safeParse({ cnhOriginal: true, titularidadeCartao: true, comprovanteResidencia: true, termoMultas: true, [item]: false }).success).toBe(false);
  });
  it('lista de itens: três sem caução e quatro com caução, na ordem do desenho', () => {
    expect(itensDaConferencia(false)).toEqual(['cnhOriginal', 'comprovanteResidencia', 'termoMultas']);
    expect(itensDaConferencia(false)).toBe(ITENS_CONFERENCIA);
    expect(itensDaConferencia(true)).toEqual(['cnhOriginal', 'titularidadeCartao', 'comprovanteResidencia', 'termoMultas']);
  });
  it('a conferência começa com tudo desmarcado, inclusive a titularidade', () => {
    expect(SEM_CONFERENCIA).toEqual({ cnhOriginal: false, titularidadeCartao: false, comprovanteResidencia: false, termoMultas: false });
  });
  it('espelha estados existentes sem introduzir restrição cadastral ou de CNH', () => {
    expect(retiradaDisponivel(contexto)).toBe(true);
    expect(retiradaDisponivel({ ...contexto, statusReserva: 'CONFIRMADA', statusCadastroLocadora: 'RECUSADO', cnhValidadeStr: '01/01/2020' })).toBe(true);
    expect(retiradaDisponivel({ ...contexto, cnhVencida: true })).toBe(true);
    expect(retiradaDisponivel({ ...contexto, statusReserva: 'CANCELADA' })).toBe(false);
    expect(retiradaDisponivel({ ...contexto, statusVeiculo: 'DISPONIVEL' })).toBe(false);
    expect(retiradaDisponivel({ ...contexto, idLocacaoExistente: 42 })).toBe(false);
  });
  it('a caução não muda a disponibilidade da retirada (ela entra só no botão)', () => {
    expect(retiradaDisponivel(contextoRetiradaComCaucaoTeste)).toBe(true);
    expect(retiradaDisponivel({ ...contextoRetiradaComCaucaoTeste, caucaoDisponivel: false, caucaoModoSimulacao: false })).toBe(true);
  });
});
