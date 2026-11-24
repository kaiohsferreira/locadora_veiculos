import { describe, expect, it } from 'vitest';
import type { ItemPainelOperacional } from '@/types/painelOperacional';
import { acaoDaLinha, ehStatusConhecido, textoColunaReserva } from './regrasEntradaRetirada';

const daReserva: ItemPainelOperacional = {
  origem: 'RESERVA', idReserva: 31, idLocacao: null, codigoReserva: 'AV-00031', dataHora: '24/11/2026 00:00',
  status: 'CONFIRMADA', idCliente: 5, nomeCliente: 'Ana Souza', idVeiculo: 2, placa: 'ABC1D23', marca: 'Jeep', modelo: 'Renegade',
};
const daLocacao: ItemPainelOperacional = {
  ...daReserva, origem: 'LOCACAO', idReserva: null, idLocacao: 37, codigoReserva: 'AV-00037', status: 'EM_ANDAMENTO',
};

describe('acaoDaLinha (a ação de cada linha leva ao passo certo)', () => {
  it('origem RESERVA com idReserva: iniciar retirada da reserva', () => {
    expect(acaoDaLinha(daReserva)).toEqual({ tipo: 'retirada', id: 31 });
  });

  it('origem LOCACAO com idLocacao: continuar para a vistoria da locação', () => {
    expect(acaoDaLinha(daLocacao)).toEqual({ tipo: 'vistoria', id: 37 });
  });

  it('item sem o id esperado não tem ação', () => {
    expect(acaoDaLinha({ ...daReserva, idReserva: null })).toBeNull();
    expect(acaoDaLinha({ ...daLocacao, idLocacao: null })).toBeNull();
  });
});

describe('textoColunaReserva (o código vem pronto do servidor)', () => {
  it('mostra o codigoReserva como veio, nos itens das duas origens', () => {
    expect(textoColunaReserva(daReserva)).toBe('AV-00031');
    expect(textoColunaReserva(daLocacao)).toBe('AV-00037');
  });

  it('item de locação sem codigoReserva mostra o número da locação', () => {
    expect(textoColunaReserva({ ...daLocacao, codigoReserva: null, idLocacao: 9 })).toBe('Locação nº 9');
  });
});

describe('ehStatusConhecido (só status com rótulo do domínio viram etiqueta)', () => {
  it('reconhece os status de reserva e de locação', () => {
    expect(ehStatusConhecido('CONFIRMADA')).toBe(true);
    expect(ehStatusConhecido('EM_ANDAMENTO')).toBe(true);
  });

  it('recusa valor fora do domínio', () => {
    expect(ehStatusConhecido('ZZZ')).toBe(false);
    expect(ehStatusConhecido('constructor')).toBe(false);
  });
});
