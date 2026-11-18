import { describe, expect, it } from 'vitest';
import { MENSAGEM_RESERVAS_ABA_HISTORICO, MENSAGEM_RESERVAS_HISTORICO_TITULO } from '@/lib/mensagensReservas';
import { semTextosAprovadosDoDesenho, TERMOS_SEM_LASTRO } from '@/features/area-cliente/reservas/fixturasReservas';

function termosProibidosEncontrados(html: string): string[] {
  const limpo = semTextosAprovadosDoDesenho(html).toLowerCase();
  return TERMOS_SEM_LASTRO.filter((termo) => limpo.includes(termo.toLowerCase()));
}

describe('semTextosAprovadosDoDesenho', () => {
  it('um uso de "Locações" fora dos dois textos aprovados continua sendo pego', () => {
    const html = '<h2>Minhas Locações</h2><span>Histórico de Locações</span>';
    expect(semTextosAprovadosDoDesenho(html)).toContain('Minhas Locações');
    expect(termosProibidosEncontrados(html)).toEqual(['Locações']);
  });

  it('um HTML só com a aba e o título aprovados passa limpo', () => {
    const html = `<span>${MENSAGEM_RESERVAS_ABA_HISTORICO}</span><h2>${MENSAGEM_RESERVAS_HISTORICO_TITULO}</h2>`;
    expect(semTextosAprovadosDoDesenho(html)).not.toContain('Locações');
    expect(termosProibidosEncontrados(html)).toEqual([]);
  });

  it('retira o título inteiro e não deixa resto de "Recentes" nem de "Histórico"', () => {
    expect(semTextosAprovadosDoDesenho(MENSAGEM_RESERVAS_HISTORICO_TITULO)).toBe('');
    expect(semTextosAprovadosDoDesenho(MENSAGEM_RESERVAS_ABA_HISTORICO)).toBe('');
  });
});
