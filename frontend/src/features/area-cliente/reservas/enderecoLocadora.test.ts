import { describe, expect, it } from 'vitest';
import {
  montarCidadeUf,
  montarEnderecoLocadora,
  montarLinkMapa,
  type CamposEnderecoLocadora,
} from '@/features/area-cliente/reservas/enderecoLocadora';

const COMPLETO: CamposEnderecoLocadora = {
  locadoraLogradouro: 'Rua X',
  locadoraNumero: '100',
  locadoraBairro: 'Bairro',
  locadoraCidade: 'Cidade',
  locadoraUf: 'SP',
};

describe('montarEnderecoLocadora', () => {
  it('monta "Rua X, 100 - Bairro"', () => {
    expect(montarEnderecoLocadora(COMPLETO)).toBe('Rua X, 100 - Bairro');
  });

  it('partes vazias não deixam vírgula nem traço soltos', () => {
    expect(montarEnderecoLocadora({ ...COMPLETO, locadoraNumero: '' })).toBe('Rua X - Bairro');
    expect(montarEnderecoLocadora({ ...COMPLETO, locadoraBairro: '  ' })).toBe('Rua X, 100');
    expect(montarEnderecoLocadora({ ...COMPLETO, locadoraLogradouro: '', locadoraNumero: '' })).toBe('Bairro');
  });
});

describe('montarCidadeUf', () => {
  it('monta "Cidade - UF" e ignora partes vazias', () => {
    expect(montarCidadeUf(COMPLETO)).toBe('Cidade - SP');
    expect(montarCidadeUf({ ...COMPLETO, locadoraUf: '' })).toBe('Cidade');
  });
});

describe('montarLinkMapa', () => {
  it('começa com o endereço de busca do Google Maps e codifica o texto', () => {
    const link = montarLinkMapa(COMPLETO);
    expect(link.startsWith('https://www.google.com/maps/search/?api=1&query=')).toBe(true);
    expect(link).toBe(
      'https://www.google.com/maps/search/?api=1&query=' +
        encodeURIComponent('Rua X, 100 - Bairro, Cidade - SP'),
    );
  });
});
