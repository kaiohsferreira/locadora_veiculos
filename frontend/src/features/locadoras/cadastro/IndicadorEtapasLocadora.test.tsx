import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { IndicadorEtapasLocadora } from './IndicadorEtapasLocadora';

describe('IndicadorEtapasLocadora', () => {
  it('na etapa 1, marca a etapa atual com aria-current e diz a situação em texto', () => {
    const marcado = renderToStaticMarkup(<IndicadorEtapasLocadora etapaAtual={1} />);

    expect(marcado).toContain('aria-current="step"');
    expect(marcado.match(/aria-current="step"/g)).toHaveLength(1);
    expect(marcado).toContain('Em preenchimento');
    expect(marcado).toContain('Aguardando');
    expect(marcado).toContain('Pendente');
    expect(marcado).toContain('Dados da empresa');
    expect(marcado).toContain('Responsável e acesso');
    expect(marcado).toContain('Confirmação');
  });

  it('na etapa 2, a 1 fica concluída e a 3 fica pendente', () => {
    const marcado = renderToStaticMarkup(<IndicadorEtapasLocadora etapaAtual={2} />);

    expect(marcado).toContain('Concluída');
    expect(marcado).toContain('Em preenchimento');
    expect(marcado.match(/aria-current="step"/g)).toHaveLength(1);
  });

  it('é uma lista nomeada para leitores de tela', () => {
    const marcado = renderToStaticMarkup(<IndicadorEtapasLocadora etapaAtual={1} />);

    expect(marcado).toContain('<ol aria-label="Etapas do cadastro"');
  });
});
