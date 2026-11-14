import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { IndicadorEtapasDesenhoLocadora } from './IndicadorEtapasDesenhoLocadora';

describe('IndicadorEtapasDesenhoLocadora (desenhos 37 e 38)', () => {
  it('na etapa 2 (desenho 37): 1 concluída, 2 em andamento, 3 pendente, com a etapa atual marcada', () => {
    const marcado = renderToStaticMarkup(<IndicadorEtapasDesenhoLocadora etapaAtual={2} />);

    expect(marcado.match(/aria-current="step"/g)).toHaveLength(1);
    expect(marcado).toContain('Concluído');
    expect(marcado).toContain('Em andamento');
    expect(marcado).toContain('Pendente');
    expect(marcado).toContain('Dados da empresa');
    expect(marcado).toContain('Responsável e acesso');
    expect(marcado).toContain('Confirmação');
  });

  it('na etapa 3 (desenho 38): a confirmação é a etapa atual e aparece concluída, sem "Em andamento"', () => {
    const marcado = renderToStaticMarkup(<IndicadorEtapasDesenhoLocadora etapaAtual={3} />);

    expect(marcado.match(/aria-current="step"/g)).toHaveLength(1);
    expect(marcado).toContain('Concluído');
    expect(marcado).not.toContain('Em andamento');
    expect(marcado).not.toContain('Pendente');
  });

  it('é uma lista nomeada para leitores de tela', () => {
    expect(renderToStaticMarkup(<IndicadorEtapasDesenhoLocadora etapaAtual={2} />)).toContain(
      '<ol aria-label="Etapas do cadastro"',
    );
  });
});
