import { describe, expect, it } from 'vitest';
import { STATUS_VEICULO } from '@/types/enums';
import type { StatusVeiculo } from '@/types/veiculos';
import {
  itemPedeConfirmacao,
  montarMenuOperacional,
  mensagemSucessoTrocaStatus,
  podeTrocarStatusVeiculo,
} from './menuOperacional';

const FRASE_FLUXO = 'O status atual do veículo só muda pela reserva ou pela locação.';
const FRASE_VENDIDO = 'Veículo vendido ou baixado não pode ter o status alterado.';

describe('montarMenuOperacional: itens por status', () => {
  it('DISPONIVEL: enviar para manutenção e dar baixa, nesta ordem', () => {
    const menu = montarMenuOperacional('DISPONIVEL');

    expect(menu.itens.map((item) => item.rotulo)).toEqual(['Enviar para manutenção', 'Dar baixa']);
    expect(menu.itens.map((item) => item.destino)).toEqual(['MANUTENCAO', 'BAIXADO']);
    expect(menu.frase).toBeNull();
  });

  it('MANUTENCAO: voltar para disponível e dar baixa', () => {
    const menu = montarMenuOperacional('MANUTENCAO');

    expect(menu.itens.map((item) => item.rotulo)).toEqual(['Voltar para disponível', 'Dar baixa']);
    expect(menu.itens.map((item) => item.destino)).toEqual(['DISPONIVEL', 'BAIXADO']);
    expect(menu.frase).toBeNull();
  });

  it('RESERVADO e LOCADO: nenhum item e a frase de fluxo', () => {
    for (const status of ['RESERVADO', 'LOCADO'] as const) {
      expect(montarMenuOperacional(status)).toEqual({ itens: [], frase: FRASE_FLUXO });
    }
  });

  it('VENDIDO e BAIXADO: nenhum item e a frase de venda ou baixa', () => {
    for (const status of ['VENDIDO', 'BAIXADO'] as const) {
      expect(montarMenuOperacional(status)).toEqual({ itens: [], frase: FRASE_VENDIDO });
    }
  });

  it('os seis status têm resposta: ou itens, ou frase (nenhum fica vazio)', () => {
    for (const status of STATUS_VEICULO) {
      const menu = montarMenuOperacional(status);
      expect(menu.itens.length > 0 || menu.frase !== null, status).toBe(true);
    }
  });

  it('só a baixa é perigosa; nenhum item leva a RESERVADO, LOCADO nem VENDIDO', () => {
    const itens = STATUS_VEICULO.flatMap((status) => montarMenuOperacional(status).itens);

    expect(itens.filter((item) => item.perigoso).map((item) => item.destino)).toEqual(['BAIXADO', 'BAIXADO']);
    expect(itens.some((item) => ['RESERVADO', 'LOCADO', 'VENDIDO'].includes(item.destino))).toBe(false);
  });
});

describe('podeTrocarStatusVeiculo: cargo', () => {
  it('GESTOR_FROTA e ADMINISTRADOR podem', () => {
    expect(podeTrocarStatusVeiculo('GESTOR_FROTA')).toBe(true);
    expect(podeTrocarStatusVeiculo('ADMINISTRADOR')).toBe(true);
  });

  it('os demais cargos não podem', () => {
    for (const cargo of ['ATENDENTE', 'VENDEDOR', 'FINANCEIRO', 'ADMINISTRATIVO'] as const) {
      expect(podeTrocarStatusVeiculo(cargo), cargo).toBe(false);
    }
  });

  it('sessão sem cargo (cliente ou sem sessão) não pode', () => {
    expect(podeTrocarStatusVeiculo(null)).toBe(false);
  });
});

describe('itemPedeConfirmacao: só a baixa', () => {
  it('Dar baixa pede confirmação; Enviar para manutenção e Voltar para disponível vão direto', () => {
    const itens = [...montarMenuOperacional('DISPONIVEL').itens, ...montarMenuOperacional('MANUTENCAO').itens];

    expect(itens.map((item) => itemPedeConfirmacao(item))).toEqual([false, true, false, true]);
  });
});

describe('montarMenuOperacional: status fora dos seis', () => {
  it('devolve menu vazio, sem quebrar a linha', () => {
    expect(montarMenuOperacional('XYZ' as StatusVeiculo)).toEqual({ itens: [], frase: null });
  });
});

describe('mensagemSucessoTrocaStatus', () => {
  it('um texto por ação', () => {
    expect(mensagemSucessoTrocaStatus('MANUTENCAO')).toBe('Veículo enviado para manutenção.');
    expect(mensagemSucessoTrocaStatus('DISPONIVEL')).toBe('Veículo de volta para disponível.');
    expect(mensagemSucessoTrocaStatus('BAIXADO')).toBe('Baixa registrada.');
  });
});
