import { describe, expect, it } from 'vitest';
import {
  interpretarErroVeiculo,
  primeiroCampoVeiculoComErro,
} from '@/lib/mapeamentoErroVeiculo';
import { MENSAGEM_ERRO_API_PADRAO } from '@/lib/mensagens';
import {
  TEXTO_SERVIDOR_VEICULO_CHASSI_DUPLICADO,
  TEXTO_SERVIDOR_VEICULO_NAO_ENCONTRADO,
  TEXTO_SERVIDOR_VEICULO_PLACA_DUPLICADA,
  TEXTO_SERVIDOR_VEICULO_RENAVAM_DUPLICADO,
} from '@/lib/mensagensVeiculos';
import { ErroApi } from '@/types/api';
// Código-fonte do back-end lido como texto (Vite, ?raw): se a frase mudar lá, este teste falha.
import codigoServidor from '../../../backend/AutoVia.Helpers/ConstantsMessage.Veiculo.cs?raw';

/**
 * Os textos de duplicidade e de não encontrado são comparados com o que o servidor manda. Este teste lê o arquivo
 * de mensagens do back-end (ConstantsMessage.Veiculo.cs): se a frase mudar lá e não aqui, ele falha e avisa.
 */
describe('textos do servidor espelhados (origem: ConstantsMessage.Veiculo.cs)', () => {
  it.each([
    ['placa duplicada', TEXTO_SERVIDOR_VEICULO_PLACA_DUPLICADA],
    ['chassi duplicado', TEXTO_SERVIDOR_VEICULO_CHASSI_DUPLICADO],
    ['renavam duplicado', TEXTO_SERVIDOR_VEICULO_RENAVAM_DUPLICADO],
    ['veículo não encontrado', TEXTO_SERVIDOR_VEICULO_NAO_ENCONTRADO],
  ])('o texto de %s continua igual no servidor', (_nome, texto) => {
    expect(codigoServidor).toContain(`"${texto}"`);
  });
});

function erroApi(status: number, mensagem: string, erros?: Record<string, string[]>): ErroApi {
  return new ErroApi({ status, mensagem, erros });
}

describe('interpretarErroVeiculo: 400 por campo', () => {
  it('cada chave de errors vai para o campo, mesmo com a primeira letra maiúscula', () => {
    const resultado = interpretarErroVeiculo(
      erroApi(400, 'Informe uma placa válida.', {
        Placa: ['Informe uma placa válida, no padrão Mercosul (ABC1D23) ou antigo (ABC1234).'],
        marca: ['Informe a marca do veículo.'],
      }),
    );

    expect(resultado.campos.placa).toBe('Informe uma placa válida, no padrão Mercosul (ABC1D23) ou antigo (ABC1234).');
    expect(resultado.campos.marca).toBe('Informe a marca do veículo.');
    expect(resultado.aviso).toBeNull();
    expect(resultado.veiculoNaoEncontrado).toBe(false);
  });

  it('chave que não é campo do formulário vira aviso geral com a mensagem do cliente HTTP', () => {
    const resultado = interpretarErroVeiculo(erroApi(400, 'Alguns dados estão inválidos.', { Outro: ['x'] }));

    expect(resultado.campos).toEqual({});
    expect(resultado.aviso).toBe('Alguns dados estão inválidos.');
  });

  it('fornecedor e número da nota fiscal (campos novos da tela 05) vão para os próprios campos', () => {
    const resultado = interpretarErroVeiculo(
      erroApi(400, 'Alguns dados estão inválidos.', {
        Fornecedor: ['O fornecedor deve ter no máximo 100 caracteres.'],
        NumeroNotaFiscalCompra: ['O número da nota fiscal de compra deve ter no máximo 20 caracteres.'],
      }),
    );

    expect(resultado.campos.fornecedor).toBe('O fornecedor deve ter no máximo 100 caracteres.');
    expect(resultado.campos.numeroNotaFiscalCompra).toBe(
      'O número da nota fiscal de compra deve ter no máximo 20 caracteres.',
    );
    expect(resultado.aviso).toBeNull();
  });
});

describe('interpretarErroVeiculo: 409 pelo texto', () => {
  it('placa, chassi e renavam vão para o próprio campo', () => {
    expect(interpretarErroVeiculo(erroApi(409, TEXTO_SERVIDOR_VEICULO_PLACA_DUPLICADA)).campos).toEqual({
      placa: TEXTO_SERVIDOR_VEICULO_PLACA_DUPLICADA,
    });
    expect(interpretarErroVeiculo(erroApi(409, TEXTO_SERVIDOR_VEICULO_CHASSI_DUPLICADO)).campos).toEqual({
      chassi: TEXTO_SERVIDOR_VEICULO_CHASSI_DUPLICADO,
    });
    expect(interpretarErroVeiculo(erroApi(409, TEXTO_SERVIDOR_VEICULO_RENAVAM_DUPLICADO)).campos).toEqual({
      renavam: TEXTO_SERVIDOR_VEICULO_RENAVAM_DUPLICADO,
    });
  });

  it('outro 409 vai para o aviso geral, com o texto do servidor', () => {
    const resultado = interpretarErroVeiculo(erroApi(409, 'Outro conflito qualquer.'));

    expect(resultado.campos).toEqual({});
    expect(resultado.aviso).toBe('Outro conflito qualquer.');
  });
});

describe('interpretarErroVeiculo: 404', () => {
  it('o próprio veículo não encontrado é sinalizado para a tela mostrar o estado de erro', () => {
    const resultado = interpretarErroVeiculo(erroApi(404, TEXTO_SERVIDOR_VEICULO_NAO_ENCONTRADO));

    expect(resultado.veiculoNaoEncontrado).toBe(true);
    expect(resultado.aviso).toBe(TEXTO_SERVIDOR_VEICULO_NAO_ENCONTRADO);
    expect(resultado.campos).toEqual({});
  });

  it('404 de categoria é aviso geral e não marca o veículo como não encontrado', () => {
    const resultado = interpretarErroVeiculo(erroApi(404, 'A categoria informada não foi encontrada.'));

    expect(resultado.veiculoNaoEncontrado).toBe(false);
    expect(resultado.aviso).toBe('A categoria informada não foi encontrada.');
  });
});

describe('interpretarErroVeiculo: demais erros', () => {
  it('500 e falha de rede usam a mensagem que o cliente HTTP já preparou', () => {
    expect(interpretarErroVeiculo(erroApi(500, 'Ocorreu um erro inesperado no servidor.')).aviso).toBe(
      'Ocorreu um erro inesperado no servidor.',
    );
  });

  it('o que não é ErroApi vira a mensagem padrão, nunca o texto técnico', () => {
    const resultado = interpretarErroVeiculo(new Error('TypeError: x is undefined'));

    expect(resultado.aviso).toBe(MENSAGEM_ERRO_API_PADRAO);
    expect(resultado.campos).toEqual({});
  });
});

describe('primeiroCampoVeiculoComErro', () => {
  it('devolve o primeiro campo na ordem da tela que tem erro', () => {
    expect(primeiroCampoVeiculoComErro({ marca: 'x', placa: 'y' })).toBe('placa');
    expect(primeiroCampoVeiculoComErro({})).toBeNull();
  });
});
