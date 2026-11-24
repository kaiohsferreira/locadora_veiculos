import { describe, expect, it } from 'vitest';
import { lerCodigoReserva } from './codigoReserva';

describe('lerCodigoReserva (o que o atendente digita na página de entrada)', () => {
  it.each([
    ['AV-00042', 42],
    ['av-42', 42],
    ['#AV-00042', 42],
    [' 42 ', 42],
    ['# 42', 42],
    ['00042', 42],
    ['2147483647', 2147483647],
  ])('aceita %j como reserva %s', (texto, esperado) => {
    expect(lerCodigoReserva(texto)).toBe(esperado);
  });

  it.each(['', '   ', '#', 'AV-', 'AV-4a', '4 2', '-42', '+42', '4.2', 'AV-AV-42', '0', 'AV-0', '2147483648'])(
    'recusa %j',
    (texto) => {
      expect(lerCodigoReserva(texto)).toBeNull();
    },
  );
});
