import { describe, expect, it } from 'vitest';
import {
  converterMoedaPtBrParaNumero,
  formatarCep,
  formatarCnpj,
  formatarCpf,
  formatarHora,
  formatarNumero,
  formatarTelefone,
  limitarDigitos,
  mascararMoedaDigitada,
} from '@/lib/formatters';

describe('mascararMoedaDigitada', () => {
  it('põe a vírgula dos centavos e o ponto de milhar enquanto a pessoa digita', () => {
    expect(mascararMoedaDigitada('')).toBe('');
    expect(mascararMoedaDigitada('5')).toBe('0,05');
    expect(mascararMoedaDigitada('22900')).toBe('229,00');
    expect(mascararMoedaDigitada('10450000')).toBe('104.500,00');
    expect(mascararMoedaDigitada('123456789')).toBe('1.234.567,89');
  });

  it('ignora o que não é dígito e zeros à esquerda; sem dígito volta vazio', () => {
    expect(mascararMoedaDigitada('R$ 2.290,05')).toBe('2.290,05');
    expect(mascararMoedaDigitada('000')).toBe('');
    expect(mascararMoedaDigitada('0100')).toBe('1,00');
  });
});

describe('converterMoedaPtBrParaNumero', () => {
  it('converte o texto em reais para número, com o ponto de milhar e a vírgula decimal', () => {
    expect(converterMoedaPtBrParaNumero('229,00')).toBe(229);
    expect(converterMoedaPtBrParaNumero('104.500,00')).toBe(104500);
    expect(converterMoedaPtBrParaNumero('1234,5')).toBe(1234.5);
    expect(converterMoedaPtBrParaNumero(' 0,05 ')).toBe(0.05);
    expect(converterMoedaPtBrParaNumero('12')).toBe(12);
  });

  it('vazio ou fora do formato pt-BR vira nulo (ponto decimal, vírgula no lugar errado, letras)', () => {
    expect(converterMoedaPtBrParaNumero('')).toBeNull();
    expect(converterMoedaPtBrParaNumero('   ')).toBeNull();
    expect(converterMoedaPtBrParaNumero('1.2')).toBeNull();
    expect(converterMoedaPtBrParaNumero('1234.567')).toBeNull();
    expect(converterMoedaPtBrParaNumero(',5')).toBeNull();
    expect(converterMoedaPtBrParaNumero('R$ 5')).toBeNull();
  });

  it('é o inverso da máscara: o valor digitado volta ao mesmo número', () => {
    expect(converterMoedaPtBrParaNumero(mascararMoedaDigitada('10450000'))).toBe(104500);
    expect(formatarNumero(229, 2)).toBe('229,00');
    expect(converterMoedaPtBrParaNumero(formatarNumero(104500, 2))).toBe(104500);
  });
});

describe('formatarHora', () => {
  it('formata a hora local em 24 horas, com zero à esquerda', () => {
    expect(formatarHora(new Date(2026, 10, 16, 14, 22))).toBe('14:22');
    expect(formatarHora(new Date(2026, 10, 16, 8, 5).getTime())).toBe('08:05');
  });

  it('nulo, ausente ou data inválida vira o traço de valor ausente', () => {
    expect(formatarHora(null)).toBe('—');
    expect(formatarHora(undefined)).toBe('—');
    expect(formatarHora(new Date('nao é data'))).toBe('—');
  });
});

describe('formatarCpf', () => {
  it('formata valor parcial enquanto o usuário digita', () => {
    expect(formatarCpf('')).toBe('');
    expect(formatarCpf('123')).toBe('123');
    expect(formatarCpf('1234')).toBe('123.4');
    expect(formatarCpf('123456')).toBe('123.456');
    expect(formatarCpf('1234567')).toBe('123.456.7');
  });

  it('formata CPF completo, com ou sem máscara de entrada', () => {
    expect(formatarCpf('12345678909')).toBe('123.456.789-09');
    expect(formatarCpf('123.456.789-09')).toBe('123.456.789-09');
  });

  it('corta em 11 dígitos quando recebe mais', () => {
    expect(formatarCpf('1234567890123')).toBe('123.456.789-01');
  });
});

describe('formatarTelefone', () => {
  it('formata valor parcial com DDD', () => {
    expect(formatarTelefone('')).toBe('');
    expect(formatarTelefone('1')).toBe('(1');
    expect(formatarTelefone('11')).toBe('(11');
    expect(formatarTelefone('113')).toBe('(11) 3');
    expect(formatarTelefone('1134567')).toBe('(11) 3456-7');
  });

  it('formata fixo e celular completos', () => {
    expect(formatarTelefone('1134567890')).toBe('(11) 3456-7890');
    expect(formatarTelefone('11987654321')).toBe('(11) 98765-4321');
  });

  it('corta em 11 dígitos quando recebe mais', () => {
    expect(formatarTelefone('119876543219')).toBe('(11) 98765-4321');
  });
});

describe('formatarCep', () => {
  it('formata valor parcial sem o hífen até o quinto dígito', () => {
    expect(formatarCep('')).toBe('');
    expect(formatarCep('0131')).toBe('0131');
    expect(formatarCep('01310')).toBe('01310');
    expect(formatarCep('013101')).toBe('01310-1');
  });

  it('formata CEP completo e corta em 8 dígitos', () => {
    expect(formatarCep('01310100')).toBe('01310-100');
    expect(formatarCep('0131010012')).toBe('01310-100');
  });
});

describe('limitarDigitos', () => {
  it('remove tudo que não é dígito, mantendo a máscara fora do valor', () => {
    expect(limitarDigitos('123.456.789-09', 11)).toBe('12345678909');
  });

  it('corta no máximo informado', () => {
    expect(limitarDigitos('1234567890123', 11)).toBe('12345678901');
  });

  it('devolve valor menor que o máximo sem alterar', () => {
    expect(limitarDigitos('12a3', 11)).toBe('123');
  });

  it('devolve vazio para texto sem dígitos ou máximo zero e negativo', () => {
    expect(limitarDigitos('abc', 5)).toBe('');
    expect(limitarDigitos('12345', 0)).toBe('');
    expect(limitarDigitos('12345', -3)).toBe('');
  });
});

describe('formatarCnpj', () => {
  it('formata valor parcial enquanto o usuário digita', () => {
    expect(formatarCnpj('')).toBe('');
    expect(formatarCnpj('11')).toBe('11');
    expect(formatarCnpj('112')).toBe('11.2');
    expect(formatarCnpj('11222')).toBe('11.222');
    expect(formatarCnpj('11222333')).toBe('11.222.333');
    expect(formatarCnpj('112223330001')).toBe('11.222.333/0001');
  });

  it('formata os 14 dígitos com pontos, barra e hífen', () => {
    expect(formatarCnpj('11222333000181')).toBe('11.222.333/0001-81');
  });

  it('aceita o valor já mascarado (não duplica a máscara) e corta o excedente', () => {
    expect(formatarCnpj('11.222.333/0001-81')).toBe('11.222.333/0001-81');
    expect(formatarCnpj('11222333000181999')).toBe('11.222.333/0001-81');
  });

  it('nulo ou ausente vira o traço de valor ausente', () => {
    expect(formatarCnpj(null)).toBe('—');
    expect(formatarCnpj(undefined)).toBe('—');
  });
});
