import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ATRASO_BUSCA_FROTA_MS } from './parametrosListagemFrota';
import { criarAgendadorBusca } from './agendarBusca';

describe('criarAgendadorBusca (relógio falso)', () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('aplica o valor só depois do atraso, e não antes', () => {
    const aplicar = vi.fn();
    const agendador = criarAgendadorBusca(aplicar, ATRASO_BUSCA_FROTA_MS);

    agendador.agendar('Onix');
    vi.advanceTimersByTime(ATRASO_BUSCA_FROTA_MS - 1);
    expect(aplicar).not.toHaveBeenCalled();

    vi.advanceTimersByTime(1);
    expect(aplicar).toHaveBeenCalledTimes(1);
    expect(aplicar).toHaveBeenCalledWith('Onix');
  });

  it('uma tecla nova dentro do atraso cancela a anterior: só a última vale', () => {
    const aplicar = vi.fn();
    const agendador = criarAgendadorBusca(aplicar, ATRASO_BUSCA_FROTA_MS);

    agendador.agendar('O');
    vi.advanceTimersByTime(200);
    agendador.agendar('On');
    vi.advanceTimersByTime(200);
    agendador.agendar('Onix');
    vi.advanceTimersByTime(ATRASO_BUSCA_FROTA_MS);

    expect(aplicar).toHaveBeenCalledTimes(1);
    expect(aplicar).toHaveBeenCalledWith('Onix');
  });

  it('cancelar antes do atraso impede a consulta', () => {
    const aplicar = vi.fn();
    const agendador = criarAgendadorBusca(aplicar, ATRASO_BUSCA_FROTA_MS);

    agendador.agendar('Corolla');
    agendador.cancelar();
    vi.advanceTimersByTime(ATRASO_BUSCA_FROTA_MS * 3);

    expect(aplicar).not.toHaveBeenCalled();
  });

  it('depois de aplicada, uma nova espera funciona de novo', () => {
    const aplicar = vi.fn();
    const agendador = criarAgendadorBusca(aplicar, ATRASO_BUSCA_FROTA_MS);

    agendador.agendar('A');
    vi.advanceTimersByTime(ATRASO_BUSCA_FROTA_MS);
    agendador.agendar('B');
    vi.advanceTimersByTime(ATRASO_BUSCA_FROTA_MS);

    expect(aplicar.mock.calls).toEqual([['A'], ['B']]);
  });

  it('cancelar sem espera pendente não faz nada', () => {
    const aplicar = vi.fn();
    const agendador = criarAgendadorBusca(aplicar, ATRASO_BUSCA_FROTA_MS);

    expect(() => agendador.cancelar()).not.toThrow();
    vi.advanceTimersByTime(ATRASO_BUSCA_FROTA_MS);
    expect(aplicar).not.toHaveBeenCalled();
  });
});
