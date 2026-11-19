import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { STATUS_VEICULO } from '@/types/enums';
import { EtiquetaStatusFrota } from './EtiquetaStatusFrota';

describe('EtiquetaStatusFrota', () => {
  it('sempre mostra o texto da situação, nos seis status', () => {
    const textos = {
      DISPONIVEL: 'Disponível',
      RESERVADO: 'Reservado',
      LOCADO: 'Locado',
      MANUTENCAO: 'Em manutenção',
      VENDIDO: 'Vendido',
      BAIXADO: 'Baixado',
    };
    for (const status of STATUS_VEICULO) {
      expect(renderToStaticMarkup(<EtiquetaStatusFrota status={status} />)).toContain(textos[status]);
    }
  });

  it('LOCADO em azul-marinho e MANUTENCAO em laranja, como no desenho 11', () => {
    expect(renderToStaticMarkup(<EtiquetaStatusFrota status="LOCADO" />)).toContain('bg-steel-suave text-navy');
    expect(renderToStaticMarkup(<EtiquetaStatusFrota status="MANUTENCAO" />)).toContain('bg-frota-oficina-fundo text-frota-oficina');
  });

  it('o ponto de cor é decorativo (o texto já diz a situação)', () => {
    expect(renderToStaticMarkup(<EtiquetaStatusFrota status="DISPONIVEL" />)).toContain('aria-hidden="true"');
  });
});
