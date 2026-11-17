import { describe, expect, it } from 'vitest';
import { criarEsquemaVeiculo, type ValoresFormularioVeiculo } from '@/lib/esquemaVeiculo';
import { CARACTERE_NULO } from '@/lib/normalizacaoVeiculo';
import {
  MENSAGEM_VEICULO_ANO_MODELO_ANTERIOR,
  MENSAGEM_VEICULO_CATEGORIA_OBRIGATORIA,
  MENSAGEM_VEICULO_CHASSI_INVALIDO,
  MENSAGEM_VEICULO_DATA_FUTURA,
  MENSAGEM_VEICULO_DATA_INVALIDA,
  MENSAGEM_VEICULO_KM_OBRIGATORIO,
  MENSAGEM_VEICULO_MARCA_OBRIGATORIA,
  MENSAGEM_VEICULO_PLACA_INVALIDA,
  MENSAGEM_VEICULO_RENAVAM_INVALIDO,
  MENSAGEM_VEICULO_VALOR_DIARIA,
  MENSAGEM_VEICULO_VALOR_DIARIA_PRECISAO,
  MENSAGEM_VEICULO_VALOR_VENDA,
  MENSAGEM_VEICULO_VALOR_VENDA_PRECISAO,
  mensagemVeiculoAnoFabricacaoIntervalo,
  mensagemVeiculoAnoModeloMaximo,
  mensagemVeiculoCampoInvalido,
  mensagemVeiculoCorTamanho,
  mensagemVeiculoFornecedorTamanho,
  mensagemVeiculoMarcaTamanho,
  mensagemVeiculoNotaFiscalTamanho,
} from '@/lib/mensagensVeiculos';

// Hoje = 16/11/2026: o ano limite de fabricação e de modelo é 2027 (ano atual mais um).
const HOJE = new Date(2026, 10, 16);
const ESQUEMA = criarEsquemaVeiculo(HOJE);

const VALIDO: ValoresFormularioVeiculo = {
  idCategoria: '2',
  placa: 'BRA2E19',
  chassi: '9BWZZZ377VT004251',
  renavam: '00984521360',
  marca: 'Fiat',
  modelo: 'Argo Drive 1.0',
  anoFabricacao: '2023',
  anoModelo: '2024',
  cor: 'Prata Shark',
  kmAtual: '12',
  valorDiaria: '229,00',
  valorVenda: '',
  dataAquisicao: '2024-10-15',
  fornecedor: 'Jeep Dahruj SP Motors',
  numeroNotaFiscalCompra: 'NF-e 492.180',
};

/** Mensagem do primeiro erro do campo, ou undefined quando o campo passa. */
function erroDo(campo: keyof ValoresFormularioVeiculo, valor: string, base: ValoresFormularioVeiculo = VALIDO) {
  const resultado = ESQUEMA.safeParse({ ...base, [campo]: valor });
  if (resultado.success) return undefined;
  return resultado.error.issues.find((issue) => issue.path[0] === campo)?.message;
}

describe('criarEsquemaVeiculo: formulário inteiro', () => {
  it('aceita um cadastro válido, com cor, venda e data opcionais', () => {
    expect(ESQUEMA.safeParse(VALIDO).success).toBe(true);
    expect(ESQUEMA.safeParse({ ...VALIDO, cor: '', valorVenda: '', dataAquisicao: '' }).success).toBe(true);
  });
});

describe('categoria', () => {
  it('é obrigatória e precisa ser um id maior que zero', () => {
    expect(erroDo('idCategoria', '')).toBe(MENSAGEM_VEICULO_CATEGORIA_OBRIGATORIA);
    expect(erroDo('idCategoria', '0')).toBe(MENSAGEM_VEICULO_CATEGORIA_OBRIGATORIA);
    expect(erroDo('idCategoria', '3')).toBeUndefined();
  });
});

describe('placa', () => {
  it('aceita o padrão antigo e o Mercosul, com hífen ou minúsculas (normalizados antes)', () => {
    expect(erroDo('placa', 'ABC1234')).toBeUndefined();
    expect(erroDo('placa', 'ABC1D23')).toBeUndefined();
    expect(erroDo('placa', 'abc-1234')).toBeUndefined();
  });

  it('recusa tamanho errado, letras onde são dígitos e campo vazio', () => {
    expect(erroDo('placa', 'ABC123')).toBe(MENSAGEM_VEICULO_PLACA_INVALIDA);
    expect(erroDo('placa', 'ABCD123')).toBe(MENSAGEM_VEICULO_PLACA_INVALIDA);
    expect(erroDo('placa', 'AB1D234')).toBe(MENSAGEM_VEICULO_PLACA_INVALIDA);
    expect(erroDo('placa', '')).toBe(MENSAGEM_VEICULO_PLACA_INVALIDA);
  });
});

describe('chassi', () => {
  it('exige 17 caracteres sem I, O e Q; minúsculas valem depois de maiúsculas', () => {
    expect(erroDo('chassi', '9BWZZZ377VT004251')).toBeUndefined();
    expect(erroDo('chassi', '9bwzzz377vt004251')).toBeUndefined();
    expect(erroDo('chassi', '9BWZZZ377VT00425')).toBe(MENSAGEM_VEICULO_CHASSI_INVALIDO);
    expect(erroDo('chassi', '9BWZZZ377VT0042511')).toBe(MENSAGEM_VEICULO_CHASSI_INVALIDO);
    expect(erroDo('chassi', '9BWZZZ377OT004251')).toBe(MENSAGEM_VEICULO_CHASSI_INVALIDO);
    expect(erroDo('chassi', '9BWZZZ377IT004251')).toBe(MENSAGEM_VEICULO_CHASSI_INVALIDO);
    expect(erroDo('chassi', '9BWZZZ377QT004251')).toBe(MENSAGEM_VEICULO_CHASSI_INVALIDO);
  });
});

describe('renavam', () => {
  it('exige exatamente 11 dígitos, e zeros à esquerda contam', () => {
    expect(erroDo('renavam', '00984521360')).toBeUndefined();
    expect(erroDo('renavam', '0098452136')).toBe(MENSAGEM_VEICULO_RENAVAM_INVALIDO);
    expect(erroDo('renavam', '009845213601')).toBe(MENSAGEM_VEICULO_RENAVAM_INVALIDO);
    expect(erroDo('renavam', '0098452136A')).toBe(MENSAGEM_VEICULO_RENAVAM_INVALIDO);
  });
});

describe('marca e modelo', () => {
  it('marca obrigatória e até 40 caracteres sem contar as pontas', () => {
    expect(erroDo('marca', '')).toBe(MENSAGEM_VEICULO_MARCA_OBRIGATORIA);
    expect(erroDo('marca', '   ')).toBe(MENSAGEM_VEICULO_MARCA_OBRIGATORIA);
    expect(erroDo('marca', 'M'.repeat(40))).toBeUndefined();
    expect(erroDo('marca', `  ${'M'.repeat(40)}  `)).toBeUndefined();
    expect(erroDo('marca', 'M'.repeat(41))).toBe(mensagemVeiculoMarcaTamanho(40));
  });

  it('modelo até 60 caracteres (decisão do Kaio, 16/11/2026) e sem caractere nulo', () => {
    expect(erroDo('modelo', 'M'.repeat(60))).toBeUndefined();
    expect(erroDo('modelo', 'M'.repeat(61))).toBe('O modelo deve ter no máximo 60 caracteres.');
    expect(erroDo('modelo', 'Renegade Longitude 1.3 T270 Turbo Flex Automático')).toBeUndefined();
    expect(erroDo('modelo', `Argo${CARACTERE_NULO}`)).toBe(mensagemVeiculoCampoInvalido('Modelo'));
  });

  it('caractere nulo é recusado com a mensagem do campo', () => {
    expect(erroDo('marca', `Fi${CARACTERE_NULO}at`)).toBe(mensagemVeiculoCampoInvalido('Marca'));
  });
});

describe('anos', () => {
  it('fabricação entre 1980 e o ano atual mais um (2027 em 2026); 1979 e 2028 recusados', () => {
    expect(erroDo('anoFabricacao', '1980')).toBeUndefined();
    expect(erroDo('anoFabricacao', '2027')).toBeUndefined();
    expect(erroDo('anoFabricacao', '1979')).toBe(mensagemVeiculoAnoFabricacaoIntervalo(1980, 2027));
    expect(erroDo('anoFabricacao', '2028')).toBe(mensagemVeiculoAnoFabricacaoIntervalo(1980, 2027));
    expect(erroDo('anoFabricacao', '')).toBe(mensagemVeiculoAnoFabricacaoIntervalo(1980, 2027));
  });

  it('modelo não pode ser anterior à fabricação, e nem posterior ao ano limite; o erro vai no campo do modelo', () => {
    expect(erroDo('anoModelo', '2022')).toBe(MENSAGEM_VEICULO_ANO_MODELO_ANTERIOR);
    expect(erroDo('anoModelo', '2023')).toBeUndefined();
    expect(erroDo('anoModelo', '2027')).toBeUndefined();
    expect(erroDo('anoModelo', '2028')).toBe(mensagemVeiculoAnoModeloMaximo(2027));
  });

  it('a regra do modelo depende da fabricação: com fabricação 2025, modelo 2024 falha', () => {
    expect(erroDo('anoModelo', '2024', { ...VALIDO, anoFabricacao: '2025' })).toBe(
      MENSAGEM_VEICULO_ANO_MODELO_ANTERIOR,
    );
  });
});

describe('cor e quilometragem', () => {
  it('cor é opcional e vai até 30 caracteres sem contar as pontas', () => {
    expect(erroDo('cor', '')).toBeUndefined();
    expect(erroDo('cor', 'C'.repeat(30))).toBeUndefined();
    expect(erroDo('cor', 'C'.repeat(31))).toBe(mensagemVeiculoCorTamanho(30));
    expect(erroDo('cor', `Preto${CARACTERE_NULO}`)).toBe(mensagemVeiculoCampoInvalido('Cor'));
  });

  it('odômetro de entrega é obrigatório na tela, e zero é valor válido', () => {
    expect(erroDo('kmAtual', '')).toBe(MENSAGEM_VEICULO_KM_OBRIGATORIO);
    expect(erroDo('kmAtual', '   ')).toBe(MENSAGEM_VEICULO_KM_OBRIGATORIO);
    expect(erroDo('kmAtual', '0')).toBeUndefined();
    expect(erroDo('kmAtual', '24850')).toBeUndefined();
  });
});

describe('fornecedor e nota fiscal de compra (decisão do Kaio, 16/11/2026)', () => {
  it('são opcionais: vazio passa', () => {
    expect(erroDo('fornecedor', '')).toBeUndefined();
    expect(erroDo('numeroNotaFiscalCompra', '')).toBeUndefined();
  });

  it('fornecedor até 100 caracteres sem contar as pontas; nota fiscal até 20', () => {
    expect(erroDo('fornecedor', 'F'.repeat(100))).toBeUndefined();
    expect(erroDo('fornecedor', 'F'.repeat(101))).toBe(mensagemVeiculoFornecedorTamanho(100));
    expect(erroDo('numeroNotaFiscalCompra', 'N'.repeat(20))).toBeUndefined();
    expect(erroDo('numeroNotaFiscalCompra', 'N'.repeat(21))).toBe(mensagemVeiculoNotaFiscalTamanho(20));
  });

  it('as mensagens da nota fiscal são as mesmas que o servidor manda', () => {
    expect(erroDo('numeroNotaFiscalCompra', 'N'.repeat(21))).toBe(
      'O número da nota fiscal de compra deve ter no máximo 20 caracteres.',
    );
    expect(erroDo('numeroNotaFiscalCompra', `492${CARACTERE_NULO}`)).toBe(
      'O valor informado para o campo Nota fiscal de compra é inválido.',
    );
  });

  it('caractere nulo é recusado nos dois campos, com a mensagem do campo', () => {
    expect(erroDo('fornecedor', `Dahruj${CARACTERE_NULO}`)).toBe(mensagemVeiculoCampoInvalido('Fornecedor'));
    expect(erroDo('numeroNotaFiscalCompra', `492${CARACTERE_NULO}`)).toBe(
      mensagemVeiculoCampoInvalido('Nota fiscal de compra'),
    );
  });
});

describe('valor da diária', () => {
  it('maior que zero e no máximo 8 dígitos inteiros e 2 casas', () => {
    expect(erroDo('valorDiaria', '229,00')).toBeUndefined();
    expect(erroDo('valorDiaria', '12.345.678,00')).toBeUndefined();
    expect(erroDo('valorDiaria', '0,00')).toBe(MENSAGEM_VEICULO_VALOR_DIARIA);
    expect(erroDo('valorDiaria', '')).toBe(MENSAGEM_VEICULO_VALOR_DIARIA);
    expect(erroDo('valorDiaria', '123.456.789,00')).toBe(MENSAGEM_VEICULO_VALOR_DIARIA_PRECISAO);
    expect(erroDo('valorDiaria', '229,001')).toBe(MENSAGEM_VEICULO_VALOR_DIARIA_PRECISAO);
  });
});

describe('valor de venda', () => {
  it('é opcional; quando preenchido, maior que zero e até 10 dígitos inteiros e 2 casas', () => {
    expect(erroDo('valorVenda', '')).toBeUndefined();
    expect(erroDo('valorVenda', '108.200,00')).toBeUndefined();
    expect(erroDo('valorVenda', '1.234.567.890,00')).toBeUndefined();
    expect(erroDo('valorVenda', '0,00')).toBe(MENSAGEM_VEICULO_VALOR_VENDA);
    expect(erroDo('valorVenda', '12.345.678.901,00')).toBe(MENSAGEM_VEICULO_VALOR_VENDA_PRECISAO);
    expect(erroDo('valorVenda', '10,005')).toBe(MENSAGEM_VEICULO_VALOR_VENDA_PRECISAO);
  });
});

describe('data de aquisição', () => {
  it('é opcional; quando preenchida, precisa ser uma data real e não futura (hoje passa)', () => {
    expect(erroDo('dataAquisicao', '')).toBeUndefined();
    expect(erroDo('dataAquisicao', '2026-11-16')).toBeUndefined();
    expect(erroDo('dataAquisicao', '2026-11-17')).toBe(MENSAGEM_VEICULO_DATA_FUTURA);
    expect(erroDo('dataAquisicao', '2026-02-30')).toBe(MENSAGEM_VEICULO_DATA_INVALIDA);
  });
});

describe('campo com erro do cadastro completo', () => {
  it('um campo inválido não impede a leitura dos outros: a categoria vazia aparece no próprio campo', () => {
    const resultado = ESQUEMA.safeParse({ ...VALIDO, idCategoria: '' });
    expect(resultado.success).toBe(false);
    expect(resultado.error?.issues.map((issue) => issue.path[0])).toContain('idCategoria');
  });
});
