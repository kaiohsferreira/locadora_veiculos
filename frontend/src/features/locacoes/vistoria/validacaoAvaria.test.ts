import { describe, expect, it } from 'vitest';
import {
  esquemaAvaria,
  normalizarAvaria,
  posicaoNoQuadrante,
  validarFotoAvaria,
  type CamposAvaria,
} from './validacaoAvaria';
import { MENSAGENS_DEVOLUCAO } from '@/lib/mensagensDevolucao';
import {
  MENSAGEM_FORM_AVARIA_ESCOLHA_FOTO,
  MENSAGEM_FORM_AVARIA_POSICAO_INCOMPLETA,
  MENSAGEM_FORM_AVARIA_POSICAO_INVALIDA,
} from '@/lib/mensagensVistoriaAvarias';
import { MENSAGEM_FOTOS_ERRO_TIPO, MENSAGEM_FOTOS_ERRO_TAMANHO } from '@/lib/mensagensFotosVeiculo';

const VALIDO: CamposAvaria = {
  angulo: 'LATERAL_DIREITA', descricao: '  Risco na porta  ', gravidade: 'LEVE', valorEstimado: '150,5', posX: '', posY: '',
};

/** Mensagens de todas as falhas do esquema, na ordem em que aparecem. */
function falhas(campos: CamposAvaria): string[] {
  const resultado = esquemaAvaria.safeParse(campos);
  return resultado.success ? [] : resultado.error.issues.map((issue) => issue.message);
}

describe('esquemaAvaria', () => {
  it('aceita a avaria sem ângulo, sem posição e com valor com vírgula', () => {
    expect(falhas({ ...VALIDO, angulo: '' })).toEqual([]);
  });

  it('ângulo sem posição é incompleto, mesmo com o resto válido', () => {
    expect(falhas(VALIDO)).toEqual([MENSAGEM_FORM_AVARIA_POSICAO_INCOMPLETA]);
  });

  it('aceita a avaria com ângulo e posição juntos, no formato do servidor', () => {
    expect(falhas({ ...VALIDO, posX: '12.5', posY: '100' })).toEqual([]);
  });

  it('recusa descrição vazia, longa ou com caractere nulo', () => {
    expect(falhas({ ...VALIDO, descricao: '   ' })).toContain(MENSAGENS_DEVOLUCAO.descricaoInvalida);
    expect(falhas({ ...VALIDO, descricao: 'a'.repeat(201) })).toContain(MENSAGENS_DEVOLUCAO.descricaoInvalida);
    expect(falhas({ ...VALIDO, descricao: 'a\0b' })).toContain(MENSAGENS_DEVOLUCAO.descricaoInvalida);
  });

  it('recusa gravidade vazia e valor acima da faixa ou com mais de duas casas', () => {
    expect(falhas({ ...VALIDO, gravidade: '' })).toContain('Escolha a gravidade da avaria.');
    expect(falhas({ ...VALIDO, valorEstimado: '100000000' })).toContain(MENSAGENS_DEVOLUCAO.valorFaixa);
    expect(falhas({ ...VALIDO, valorEstimado: '1,234' })).toContain(MENSAGENS_DEVOLUCAO.valorFaixa);
    expect(falhas({ ...VALIDO, valorEstimado: 'abc' })).toContain(MENSAGENS_DEVOLUCAO.valorFaixa);
  });

  it('posição em tudo ou nada: ângulo sem X e Y, ou X sem os outros, é incompleta', () => {
    expect(falhas({ ...VALIDO, posX: '10', posY: '' })).toContain(MENSAGEM_FORM_AVARIA_POSICAO_INCOMPLETA);
    expect(falhas({ ...VALIDO, angulo: '', posX: '10', posY: '20' })).toContain(MENSAGEM_FORM_AVARIA_POSICAO_INCOMPLETA);
  });

  it('posição fora de 0 a 100, com mais de duas casas ou com vírgula, é inválida', () => {
    expect(falhas({ ...VALIDO, posX: '100.01', posY: '5' })).toContain(MENSAGEM_FORM_AVARIA_POSICAO_INVALIDA);
    expect(falhas({ ...VALIDO, posX: '5', posY: '1.234' })).toContain(MENSAGEM_FORM_AVARIA_POSICAO_INVALIDA);
    expect(falhas({ ...VALIDO, posX: '5', posY: '12,5' })).toContain(MENSAGEM_FORM_AVARIA_POSICAO_INVALIDA);
    expect(falhas({ ...VALIDO, posX: '-1', posY: '5' })).toContain(MENSAGEM_FORM_AVARIA_POSICAO_INVALIDA);
  });
});

describe('normalizarAvaria', () => {
  it('troca vírgula por ponto, fixa duas casas e tira espaços', () => {
    expect(normalizarAvaria(VALIDO)).toEqual({
      angulo: 'LATERAL_DIREITA', descricao: 'Risco na porta', gravidade: 'LEVE', valorEstimado: '150.50', posX: null, posY: null,
    });
  });

  it('sem valor e sem ângulo, tudo vira nulo', () => {
    expect(normalizarAvaria({ ...VALIDO, angulo: '', valorEstimado: '  ' })).toMatchObject({
      angulo: null, valorEstimado: null, posX: null, posY: null,
    });
  });

  it('a posição sai com duas casas e ponto decimal', () => {
    expect(normalizarAvaria({ ...VALIDO, posX: '7', posY: '33.3' })).toMatchObject({ posX: '7.00', posY: '33.30' });
  });
});

describe('validarFotoAvaria', () => {
  it('sem foto, pede a foto', () => {
    expect(validarFotoAvaria(null)).toBe(MENSAGEM_FORM_AVARIA_ESCOLHA_FOTO);
  });

  it('aceita JPEG e PNG até 10 MB e recusa o resto com os textos da tela 05', () => {
    expect(validarFotoAvaria({ type: 'image/jpeg', size: 1000 })).toBeNull();
    expect(validarFotoAvaria({ type: 'image/png', size: 1000 })).toBeNull();
    expect(validarFotoAvaria({ type: 'image/gif', size: 1000 })).toBe(MENSAGEM_FOTOS_ERRO_TIPO);
    expect(validarFotoAvaria({ type: 'image/png', size: 10_485_761 })).toBe(MENSAGEM_FOTOS_ERRO_TAMANHO);
  });
});

describe('posicaoNoQuadrante', () => {
  it('converte o ponto do clique em percentual com duas casas', () => {
    expect(posicaoNoQuadrante(200, 100, 50, 25)).toEqual({ posX: 25, posY: 25 });
    expect(posicaoNoQuadrante(300, 200, 100, 50)).toEqual({ posX: 33.33, posY: 25 });
  });

  it('limita o ponto ao quadrante e trata medida zero', () => {
    expect(posicaoNoQuadrante(100, 100, -5, 150)).toEqual({ posX: 0, posY: 100 });
    expect(posicaoNoQuadrante(0, 0, 10, 10)).toEqual({ posX: 0, posY: 0 });
  });
});
