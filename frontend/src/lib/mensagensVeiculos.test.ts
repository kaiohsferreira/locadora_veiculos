import { describe, expect, it } from 'vitest';
import {
  mensagemVeiculoAnoFabricacaoIntervalo,
  mensagemVeiculoRotuloCategoria,
  mensagemVeiculoMarcaTamanho,
} from '@/lib/mensagensVeiculos';

describe('mensagemVeiculoRotuloCategoria', () => {
  it('junta nome e descrição com " - "; sem descrição (ou só espaços) fica só o nome', () => {
    expect(mensagemVeiculoRotuloCategoria('Grupo G', 'SUV Compacto Automático')).toBe('Grupo G - SUV Compacto Automático');
    expect(mensagemVeiculoRotuloCategoria('Grupo G', null)).toBe('Grupo G');
    expect(mensagemVeiculoRotuloCategoria('Grupo G', '   ')).toBe('Grupo G');
  });
});

describe('textos com número', () => {
  it('usam o limite recebido, nunca um número fixo no texto', () => {
    expect(mensagemVeiculoMarcaTamanho(40)).toBe('A marca deve ter no máximo 40 caracteres.');
    expect(mensagemVeiculoAnoFabricacaoIntervalo(1980, 2027)).toBe('O ano de fabricação deve estar entre 1980 e 2027.');
  });
});
