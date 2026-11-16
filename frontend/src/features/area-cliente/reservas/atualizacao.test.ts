import { describe, expect, it } from 'vitest';
import { descreverAtualizacao } from '@/features/area-cliente/reservas/atualizacao';

const AGORA = new Date(2026, 10, 16, 9, 0);

describe('descreverAtualizacao', () => {
  it('no mesmo dia mostra "hoje às HH:mm"', () => {
    const momento = new Date(2026, 10, 16, 8, 35).getTime();
    expect(descreverAtualizacao(momento, AGORA)).toBe('Atualizado hoje às 08:35');
  });

  it('em outro dia mostra a data completa e a hora', () => {
    const momento = new Date(2026, 10, 15, 8, 35).getTime();
    expect(descreverAtualizacao(momento, AGORA)).toBe('Atualizado em 15/11/2026 às 08:35');
  });

  it('nunca carregou (dataUpdatedAt zero) devolve null, para a faixa sumir', () => {
    expect(descreverAtualizacao(0, AGORA)).toBeNull();
  });
});
