import { describe, expect, it } from 'vitest';
import { camposConfiguracaoCobrancas, converterConfiguracaoCobrancas, esquemaConfiguracaoCobrancas } from './validacaoConfiguracao';
const campos = { idCategoria: '2', baseFranquia: 'POR_DIARIA', franquiaKm: '0', valorKmExcedente: '1,25', valorTanqueCompleto: '300,50', toleranciaAtrasoMinutos: '0', unidadeAtraso: 'HORA', fracionamentoAtraso: 'PROPORCIONAL', valorUnidadeAtraso: '15,99', baseAvarias: 'ESTIMATIVA_VISTORIA' };
describe('política de cobranças', () => {
  it('inicia todos os campos vazios, incluindo seleções e avarias', () => {
    expect(Object.values(camposConfiguracaoCobrancas()).every(v => v === '')).toBe(true);
    expect(esquemaConfiguracaoCobrancas.safeParse(camposConfiguracaoCobrancas()).success).toBe(false);
  });
  it('aceita vírgula e zero nas franquias/tolerância sem tarifa padrão', () => {
    expect(converterConfiguracaoCobrancas(campos)).toEqual({ idCategoria: 2, baseFranquia: 'POR_DIARIA', franquiaKm: 0, valorKmExcedente: 1.25, valorTanqueCompleto: 300.5, toleranciaAtrasoMinutos: 0, unidadeAtraso: 'HORA', fracionamentoAtraso: 'PROPORCIONAL', valorUnidadeAtraso: 15.99, baseAvarias: 'ESTIMATIVA_VISTORIA' });
  });
  it.each(['0', '-1', '1,001', '100000000', 'NaN', '1e3'])('rejeita tarifa inválida %s', valor => {
    for (const nome of ['valorKmExcedente', 'valorTanqueCompleto', 'valorUnidadeAtraso']) expect(esquemaConfiguracaoCobrancas.safeParse({ ...campos, [nome]: valor }).success).toBe(false);
  });
  it.each(['-1', '1,2', '2147483648', ''])('rejeita inteiro inválido %s', valor => {
    for (const nome of ['franquiaKm', 'toleranciaAtrasoMinutos']) expect(esquemaConfiguracaoCobrancas.safeParse({ ...campos, [nome]: valor }).success).toBe(false);
  });
  it('exige seleção explícita da base das avarias e aceita limites monetários', () => {
    expect(esquemaConfiguracaoCobrancas.safeParse({ ...campos, baseAvarias: '' }).success).toBe(false);
    expect(esquemaConfiguracaoCobrancas.safeParse({ ...campos, valorKmExcedente: '99999999,99' }).success).toBe(true);
  });
});
