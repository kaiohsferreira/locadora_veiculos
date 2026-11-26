import { describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { AbasReservas, type ContagemAba } from '@/features/area-cliente/reservas/componentes/AbasReservas';

function renderizar(contagens: Record<'ATIVAS' | 'HISTORICO', ContagemAba>, situacao: 'ATIVAS' | 'HISTORICO' = 'ATIVAS'): string {
  return renderToStaticMarkup(
    <AbasReservas
      situacao={situacao}
      aoMudar={vi.fn()}
      contagens={contagens}
      atualizacao={null}
      atualizando={false}
    />,
  );
}

/** Classes do contador (o span com o número) da aba cujo número é `numero`. */
function classesDoContador(html: string, numero: string): string[] {
  const achado = html.match(new RegExp(`<span class="([^"]*)">${numero}</span>`));
  return achado ? achado[1].split(' ') : [];
}

describe('AbasReservas: contador da aba ativa (desenho 28 no celular, desenho 29 no computador)', () => {
  it('aba ativa no celular: laranja exato do desenho 28 (frota-oficina) com texto branco', () => {
    const classes = classesDoContador(renderizar({ ATIVAS: 2, HISTORICO: 3 }), '2');
    expect(classes).toEqual(expect.arrayContaining(['bg-frota-oficina', 'text-branco', 'font-bold']));
  });

  it('aba ativa no computador: o laranja do desenho 29 (md:bg-orange-acao, #FE6A34), com texto branco', () => {
    const classes = classesDoContador(renderizar({ ATIVAS: 2, HISTORICO: 3 }), '2');
    expect(classes).toEqual(expect.arrayContaining(['md:bg-orange-acao', 'text-branco']));
    expect(classes).not.toContain('md:bg-orange');
    expect(classes).not.toContain('bg-orange');
  });

  it('aba inativa segue com o fundo steel-alto e o texto de apoio, sem o laranja', () => {
    const classes = classesDoContador(renderizar({ ATIVAS: 2, HISTORICO: 3 }), '3');
    expect(classes).toEqual(expect.arrayContaining(['bg-steel-alto', 'font-medium', 'text-status-neutro']));
    expect(classes).not.toContain('bg-frota-oficina');
  });

  it('quando a aba Histórico está ativa, o laranja do celular vai para ela', () => {
    const classes = classesDoContador(renderizar({ ATIVAS: 2, HISTORICO: 3 }, 'HISTORICO'), '3');
    expect(classes).toEqual(expect.arrayContaining(['bg-frota-oficina', 'md:bg-orange-acao']));
  });
});
