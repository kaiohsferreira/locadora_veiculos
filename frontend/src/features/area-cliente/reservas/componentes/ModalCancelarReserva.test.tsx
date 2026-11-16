import { describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { ModalCancelarReserva } from '@/features/area-cliente/reservas/componentes/ModalCancelarReserva';
import { RESERVA_ATIVA } from '@/features/area-cliente/reservas/fixturasReservas';

function renderizar(reserva: typeof RESERVA_ATIVA | null, pendente: boolean): string {
  return renderToStaticMarkup(
    <ModalCancelarReserva reserva={reserva} pendente={pendente} aoConfirmar={vi.fn()} aoFechar={vi.fn()} />,
  );
}

describe('ModalCancelarReserva', () => {
  it('aberto: título com o código, descrição e os dois botões', () => {
    const html = renderizar(RESERVA_ATIVA, false);
    expect(html).toContain('Cancelar a reserva AV-00042?');
    expect(html).toContain('A reserva deixa de valer e não pode ser reativada.');
    expect(html).toContain('Manter reserva');
    expect(html).toContain('Cancelar reserva');
    expect(html).toContain('aria-labelledby');
  });

  it('fechado (sem reserva): não renderiza o diálogo', () => {
    const html = renderizar(null, false);
    expect(html).not.toContain('Cancelar a reserva');
    expect(html).not.toContain('<dialog');
  });

  it('cancelamento em andamento: o botão de confirmar fica ocupado e o de manter desabilitado', () => {
    const html = renderizar(RESERVA_ATIVA, true);
    expect(html).toContain('aria-busy="true"');
    expect(html).toMatch(/<button[^>]*disabled[^>]*>Manter reserva/);
  });
});
