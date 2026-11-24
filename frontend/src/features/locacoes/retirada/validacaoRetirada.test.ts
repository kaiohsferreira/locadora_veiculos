import { describe, expect, it } from 'vitest';
import { esquemaConferenciaRetirada, lerIdReserva, retiradaDisponivel } from './validacaoRetirada';
import { contextoRetiradaTeste as contexto } from './contextoRetirada.fixture';
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
  ])('exige os três itens da conferência (%s, %s, %s)', (cnhOriginal, comprovanteResidencia, termoMultas) => {
    expect(esquemaConferenciaRetirada.safeParse({ cnhOriginal, comprovanteResidencia, termoMultas }).success).toBe(false);
  });
  it('aceita os três itens conferidos', () => {
    expect(esquemaConferenciaRetirada.safeParse({ cnhOriginal: true, comprovanteResidencia: true, termoMultas: true }).success).toBe(true);
  });
  it('espelha estados existentes sem introduzir restrição cadastral ou de CNH', () => {
    expect(retiradaDisponivel(contexto)).toBe(true);
    expect(retiradaDisponivel({ ...contexto, statusReserva: 'CONFIRMADA', statusCadastroLocadora: 'RECUSADO', cnhValidadeStr: '01/01/2020' })).toBe(true);
    expect(retiradaDisponivel({ ...contexto, cnhVencida: true })).toBe(true);
    expect(retiradaDisponivel({ ...contexto, statusReserva: 'CANCELADA' })).toBe(false);
    expect(retiradaDisponivel({ ...contexto, statusVeiculo: 'DISPONIVEL' })).toBe(false);
    expect(retiradaDisponivel({ ...contexto, idLocacaoExistente: 42 })).toBe(false);
  });
});
