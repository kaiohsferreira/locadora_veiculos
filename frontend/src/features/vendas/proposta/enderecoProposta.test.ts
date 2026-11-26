import { describe, expect, it } from 'vitest';
import { lerCamposDoEndereco, montarRetornoLogin } from './enderecoProposta';

describe('volta do login com a simulação', () => {
  it('leva a entrada e as parcelas no endereço de volta', () => {
    expect(montarRetornoLogin('/seminovos/4/proposta', { entrada: '31.350,00', parcelas: '48' })).toBe(
      '/seminovos/4/proposta?entrada=31350.00&parcelas=48',
    );
  });

  it('campo inválido não vai no endereço', () => {
    expect(montarRetornoLogin('/seminovos/4/proposta', { entrada: '', parcelas: 'x' })).toBe('/seminovos/4/proposta');
  });

  it('restaura os valores na volta, como estavam nos campos', () => {
    expect(lerCamposDoEndereco('?entrada=31350.00&parcelas=48')).toEqual({ entrada: '31.350,00', parcelas: '48' });
    expect(lerCamposDoEndereco('?entrada=0.5&parcelas=7')).toEqual({ entrada: '0,50', parcelas: '7' });
  });

  it('ida e volta devolvem os mesmos campos', () => {
    const campos = { entrada: '20.900,55', parcelas: '36' };
    const caminho = montarRetornoLogin('/seminovos/4/proposta', campos);

    expect(lerCamposDoEndereco(caminho.slice(caminho.indexOf('?')))).toEqual(campos);
  });

  it('sem parâmetros, ou com valores fora do formato e do intervalo, fica R$ 0,00 e 12', () => {
    expect(lerCamposDoEndereco('')).toEqual({ entrada: '0,00', parcelas: '12' });
    expect(lerCamposDoEndereco('?entrada=-5&parcelas=61')).toEqual({ entrada: '0,00', parcelas: '12' });
    expect(lerCamposDoEndereco('?entrada=abc&parcelas=0')).toEqual({ entrada: '0,00', parcelas: '12' });
    expect(lerCamposDoEndereco('?entrada=1.234&parcelas=1')).toEqual({ entrada: '0,00', parcelas: '1' });
  });
});
