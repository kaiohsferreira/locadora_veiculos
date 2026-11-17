import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { SeletorStatusInicialVeiculo } from './SeletorStatusInicialVeiculo';

describe('SeletorStatusInicialVeiculo (travado em disponível)', () => {
  const html = renderToStaticMarkup(<SeletorStatusInicialVeiculo />);

  it('tem o rótulo do desenho e uma única opção, disponível para locação imediata', () => {
    expect(html).toContain('Status Inicial de Entrada');
    expect(html.match(/<option/g)).toHaveLength(1);
    expect(html).toContain('● Disponível para Locação Imediata');
  });

  it('não oferece "Selecione" nem as outras três opções do desenho', () => {
    expect(html).not.toContain('>Selecione</option>');
    expect(html).not.toContain('Em Preparação');
    expect(html).not.toContain('Vistoria Cautelar');
    expect(html).not.toContain('Bloqueado Temporariamente');
  });

  it('fica desabilitado e com o destaque laranja do desenho', () => {
    expect(html).toContain('disabled=""');
    expect(html).toContain('font-semibold text-status-pendente');
  });
});
