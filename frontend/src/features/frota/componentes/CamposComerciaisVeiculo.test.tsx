import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { valoresIniciaisCadastro } from '@/lib/conversaoVeiculo';
import { CamposComerciaisVeiculo } from './CamposComerciaisVeiculo';

const CATEGORIAS = [
  { valor: '1', rotulo: 'Grupo B - Econômico Hatch Compacto' },
  { valor: '2', rotulo: 'Grupo G - SUV Compacto Automático' },
];

function renderizar(): string {
  return renderToStaticMarkup(
    <CamposComerciaisVeiculo
      valores={valoresIniciaisCadastro()}
      erros={{}}
      aoMudar={() => undefined}
      opcoesCategoria={CATEGORIAS}
    />,
  );
}

describe('CamposComerciaisVeiculo', () => {
  it('mostra a seção "2. Parâmetros Comerciais e Tarifação", a etiqueta "Matriz de Receita" e os rótulos, em ordem', () => {
    const html = renderizar();
    const ordem = [
      '2. Parâmetros Comerciais e Tarifação',
      'Matriz de Receita',
      'Grupo Operacional / Categoria',
      'Status Inicial de Entrada',
      'Valor Base da Diária (Locação)',
      'Valor Referência Seminovos (Desmobilização)',
    ];

    let posicaoAnterior = -1;
    for (const texto of ordem) {
      const posicao = html.indexOf(texto);
      expect(posicao, texto).toBeGreaterThan(posicaoAnterior);
      posicaoAnterior = posicao;
    }
  });

  it('as categorias recebidas viram opções, e nenhuma vem marcada (sem "Grupo G" pré-marcado)', () => {
    const html = renderizar();

    expect(html).toContain('>Grupo G - SUV Compacto Automático</option>');
    expect(html).toContain('>Selecione</option>');
    expect(html).not.toContain('checked=""');
  });

  it('diária e valor de seminovo com prefixo "R$" e a máscara de valor em reais', () => {
    const html = renderizar();

    expect(html.match(/>R\$<\/span>/g)).toHaveLength(2);
    expect(html).toContain('inputMode="decimal"');
  });

  it('o status inicial aparece travado, com a única opção de disponível', () => {
    const html = renderizar();

    expect(html).toContain('<option value="DISPONIVEL" selected="">● Disponível para Locação Imediata</option>');
    expect(html).toContain('disabled=""');
    expect(html).not.toContain('Em Preparação');
    expect(html).not.toContain('Bloqueado Temporariamente');
  });

  it('não tem os textos de tarifa, FIPE nem o cartão de telemetria do desenho (decisão 5)', () => {
    const html = renderizar();

    expect(html).not.toContain('Tarifa padrão balcão');
    expect(html).not.toContain('Tabela FIPE');
    expect(html).not.toContain('Rastreador e Telemetria');
  });
});
