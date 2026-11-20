import { describe, expect, it } from 'vitest';
import {
  criarEsquemaAbrirOrdem,
  criarEsquemaEncerrarOrdem,
  dataIsoLocal,
  montarRequisicaoAbrir,
  montarRequisicaoEncerrar,
  type ValoresAbrirOrdem,
  type ValoresEncerrarOrdem,
} from './esquemasOrdem';

const HOJE = '2026-11-20';

const ABRIR_VALIDO: ValoresAbrirOrdem = {
  idVeiculo: '7',
  tipo: 'PREVENTIVA',
  oficina: 'Oficina Central AutoVia CGH',
  descricao: 'Troca de óleo e filtros',
  dataEntrada: '2026-11-19',
};

const ENCERRAR_VALIDO: ValoresEncerrarOrdem = { kmAtual: '40120', dataSaida: '2026-11-20', custo: '890,50' };

/** Primeira mensagem de erro do formulário, ou null quando passa. */
function erroAbrir(valores: Partial<ValoresAbrirOrdem>): string | null {
  const resultado = criarEsquemaAbrirOrdem(HOJE).safeParse({ ...ABRIR_VALIDO, ...valores });
  return resultado.success ? null : resultado.error.issues[0].message;
}

function erroEncerrar(valores: Partial<ValoresEncerrarOrdem>, dataEntrada = '2026-11-18'): string | null {
  const resultado = criarEsquemaEncerrarOrdem(HOJE, dataEntrada).safeParse({ ...ENCERRAR_VALIDO, ...valores });
  return resultado.success ? null : resultado.error.issues[0].message;
}

describe('dataIsoLocal', () => {
  it('monta AAAA-MM-DD pela data local, com zero à esquerda', () => {
    expect(dataIsoLocal(new Date(2026, 10, 20, 23, 30))).toBe('2026-11-20');
    expect(dataIsoLocal(new Date(2026, 0, 5, 0, 10))).toBe('2026-01-05');
  });
});

describe('abrir ordem: validações do formulário (mesmos textos do servidor)', () => {
  it('aceita um pedido completo', () => {
    expect(erroAbrir({})).toBeNull();
  });

  it('pede o veículo', () => {
    expect(erroAbrir({ idVeiculo: '' })).toBe('Informe o veículo.');
  });

  it('pede o tipo, e só aceita PREVENTIVA ou CORRETIVA', () => {
    expect(erroAbrir({ tipo: '' })).toBe('Informe o tipo da manutenção.');
    expect(erroAbrir({ tipo: 'preventiva' })).toBe('Informe o tipo da manutenção.');
  });

  it('recusa data de entrada futura, e aceita a de hoje', () => {
    expect(erroAbrir({ dataEntrada: '2026-11-21' })).toBe('A data de entrada não pode ser futura.');
    expect(erroAbrir({ dataEntrada: HOJE })).toBeNull();
  });

  it('a data de entrada é opcional', () => {
    expect(erroAbrir({ dataEntrada: '' })).toBeNull();
  });

  it('a oficina tem no máximo 100 caracteres e a descrição 200', () => {
    expect(erroAbrir({ oficina: 'x'.repeat(100) })).toBeNull();
    expect(erroAbrir({ oficina: 'x'.repeat(101) })).toBe('A oficina deve ter no máximo 100 caracteres.');
    expect(erroAbrir({ descricao: 'x'.repeat(200) })).toBeNull();
    expect(erroAbrir({ descricao: 'x'.repeat(201) })).toBe('A descrição deve ter no máximo 200 caracteres.');
  });

  it('a conferência de tamanho ignora espaços nas pontas', () => {
    expect(erroAbrir({ oficina: `  ${'x'.repeat(100)}  ` })).toBeNull();
  });
});

describe('montarRequisicaoAbrir', () => {
  it('converte o veículo para número e apara a oficina e a descrição', () => {
    const requisicao = montarRequisicaoAbrir({
      ...ABRIR_VALIDO,
      oficina: '  Oficina Central  ',
      descricao: ' Troca ',
    });

    expect(requisicao).toEqual({
      idVeiculo: 7,
      tipo: 'PREVENTIVA',
      oficina: 'Oficina Central',
      descricao: 'Troca',
      dataEntrada: '2026-11-19',
    });
  });

  it('não manda oficina, descrição nem data quando vazias (o servidor usa o padrão)', () => {
    const requisicao = montarRequisicaoAbrir({ idVeiculo: '3', tipo: 'CORRETIVA', oficina: '  ', descricao: '', dataEntrada: '' });

    expect(requisicao).toEqual({ idVeiculo: 3, tipo: 'CORRETIVA' });
    expect(requisicao).not.toHaveProperty('oficina');
    expect(requisicao).not.toHaveProperty('descricao');
    expect(requisicao).not.toHaveProperty('dataEntrada');
  });

  it('não manda quilometragem', () => {
    expect(montarRequisicaoAbrir(ABRIR_VALIDO)).not.toHaveProperty('kmAtual');
  });
});

describe('encerrar ordem: validações do formulário', () => {
  it('aceita um encerramento completo', () => {
    expect(erroEncerrar({})).toBeNull();
  });

  it('pede a quilometragem na saída, em número inteiro', () => {
    expect(erroEncerrar({ kmAtual: '' })).toBe('Informe a quilometragem do veículo na saída.');
    expect(erroEncerrar({ kmAtual: '12.5' })).toBe('Informe a quilometragem do veículo na saída.');
    expect(erroEncerrar({ kmAtual: '-3' })).toBe('Informe a quilometragem do veículo na saída.');
  });

  it('aceita até o maior inteiro de 32 bits e recusa acima dele (o servidor guarda int)', () => {
    expect(erroEncerrar({ kmAtual: '2147483647' })).toBeNull();
    expect(erroEncerrar({ kmAtual: '2147483648' })).toBe('Informe a quilometragem inteira entre 0 e 2.147.483.647 km.');
    expect(erroEncerrar({ kmAtual: '99999999999999999999' })).toBe(
      'Informe a quilometragem inteira entre 0 e 2.147.483.647 km.',
    );
  });

  it('recusa data de saída futura', () => {
    expect(erroEncerrar({ dataSaida: '2026-11-21' })).toBe('A data de saída não pode ser futura.');
  });

  it('recusa data de saída anterior à data de entrada da ordem', () => {
    expect(erroEncerrar({ dataSaida: '2026-11-17' }, '2026-11-18')).toBe(
      'A data de saída não pode ser anterior à data de entrada.',
    );
    expect(erroEncerrar({ dataSaida: '2026-11-18' }, '2026-11-18')).toBeNull();
  });

  it('o custo aceita até 8 dígitos inteiros e fica vazio quando não informado', () => {
    expect(erroEncerrar({ custo: '' })).toBeNull();
    expect(erroEncerrar({ custo: '12.345.678,90' })).toBeNull();
    expect(erroEncerrar({ custo: '123.456.789,00' })).toBe(
      'O custo deve ter no máximo 8 dígitos inteiros e 2 casas decimais.',
    );
  });
});

describe('montarRequisicaoEncerrar', () => {
  it('converte km e custo para número', () => {
    expect(montarRequisicaoEncerrar(ENCERRAR_VALIDO)).toEqual({
      kmAtual: 40120,
      dataSaida: '2026-11-20',
      custo: 890.5,
    });
  });

  it('sem custo e sem data de saída, não manda os dois campos', () => {
    const requisicao = montarRequisicaoEncerrar({ kmAtual: '40120', dataSaida: '', custo: '' });

    expect(requisicao).toEqual({ kmAtual: 40120 });
    expect(requisicao).not.toHaveProperty('custo');
    expect(requisicao).not.toHaveProperty('dataSaida');
  });
});
