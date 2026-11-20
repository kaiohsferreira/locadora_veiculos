import type { ReactElement } from 'react';
import { describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { AvisoProvider } from '@/components/ui';
import type { ManutencaoListaRetorno } from '@/types/manutencao';
import { ModalAbrirOrdem } from './ModalAbrirOrdem';
import { ModalEncerrarOrdem } from './ModalEncerrarOrdem';

// Os modais só montam a marcação: as consultas e a mutação viram dublês (nenhum teste chama a API).
vi.mock('@/api/client', () => ({
  api: { get: vi.fn(), post: vi.fn(), patch: vi.fn() },
}));
vi.mock('@/features/manutencao/hooks/useVeiculosParaOrdem', () => ({
  useVeiculosParaOrdem: () => ({ data: undefined, isError: false, error: null, isSuccess: false }),
}));
vi.mock('@/features/manutencao/hooks/useAbrirManutencao', () => ({
  useAbrirManutencao: () => ({ mutate: vi.fn(), isPending: false }),
}));
vi.mock('@/features/manutencao/hooks/useEncerrarManutencao', () => ({
  useEncerrarManutencao: () => ({ mutate: vi.fn(), isPending: false }),
}));

const ORDEM: ManutencaoListaRetorno = {
  idManutencao: 8841,
  idVeiculo: 7,
  placa: 'PUL2C55',
  marca: 'Fiat',
  modelo: 'Pulse Drive 1.3',
  fotoCapa: null,
  erroFotoCapa: null,
  tipo: 'PREVENTIVA',
  dataEntrada: '2026-11-12',
  dataEntradaStr: '12/11/2026',
  dataSaida: null,
  dataSaidaStr: '',
  kmRegistrado: 30120,
  oficina: 'Oficina Central AutoVia CGH',
  descricao: null,
  custo: null,
  custoStr: '',
  aberta: true,
};

function renderizar(marcado: ReactElement): string {
  return renderToStaticMarkup(<AvisoProvider>{marcado}</AvisoProvider>);
}

describe('ModalAbrirOrdem', () => {
  const marcado = renderizar(<ModalAbrirOrdem idVeiculoInicial={7} aoFechar={vi.fn()} />);

  it('tem o título, os campos do desenho e os dois botões', () => {
    expect(marcado).toContain('Abrir ordem de manutenção');
    for (const rotulo of ['Veículo', 'Tipo', 'Oficina', 'Descrição', 'Data de entrada']) {
      expect(marcado).toContain(`>${rotulo}`);
    }
    expect(marcado).toContain('>Cancelar<');
    expect(marcado).toContain('>Abrir ordem<');
  });

  it('o tipo tem só Preventiva e Corretiva', () => {
    expect(marcado).toContain('>Preventiva<');
    expect(marcado).toContain('>Corretiva<');
  });

  it('não pede quilometragem (o servidor usa a do veículo)', () => {
    expect(marcado).not.toContain('Quilometragem');
  });

  it('limita a oficina e a descrição, e a data de entrada não passa de hoje', () => {
    expect(marcado).toMatch(/maxLength="100"/);
    expect(marcado).toMatch(/maxLength="200"/);
    expect(marcado).toMatch(/type="date"[^>]*max="\d{4}-\d{2}-\d{2}"/);
  });
});

describe('ModalEncerrarOrdem', () => {
  const marcado = renderizar(<ModalEncerrarOrdem ordem={ORDEM} aoFechar={vi.fn()} />);

  it('tem o título com o número da ordem e os três campos', () => {
    expect(marcado).toContain('Encerrar ordem #8841');
    expect(marcado).toContain('Quilometragem na saída');
    expect(marcado).toContain('Data de saída');
    expect(marcado).toContain('Custo (R$)');
  });

  it('tem Encerrar ordem e Cancelar', () => {
    expect(marcado).toContain('>Encerrar ordem<');
    expect(marcado).toContain('>Cancelar<');
  });

  it('a quilometragem é obrigatória (asterisco e aria-required)', () => {
    expect(marcado).toMatch(/aria-required="true"[^>]*name="kmAtual"|name="kmAtual"[^>]*aria-required="true"/);
  });

  it('o custo usa a máscara de reais e aceita a casa decimal no teclado', () => {
    expect(marcado).toContain('inputMode="decimal"');
  });

  it('não tem o botão de arquivo nem o de editar (cortados pelo P7)', () => {
    expect(marcado).not.toContain('Laudo');
    expect(marcado).not.toContain('Editar OS');
  });
});
