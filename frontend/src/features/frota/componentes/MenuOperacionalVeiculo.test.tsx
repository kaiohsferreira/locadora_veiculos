import { describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { montarMenuOperacional } from '@/features/frota/status/menuOperacional';
import { ConteudoMenuOperacional, MenuOperacionalVeiculo } from './MenuOperacionalVeiculo';

// O menu não chama a API: o cliente HTTP vira dublê (ele lê o navegador ao carregar, que não existe aqui).
vi.mock('@/api/client', () => ({
  api: { get: vi.fn(), post: vi.fn(), put: vi.fn(), patch: vi.fn() },
}));

const FRASE_FLUXO = 'O status atual do veículo só muda pela reserva ou pela locação.';
const FRASE_VENDIDO = 'Veículo vendido ou baixado não pode ter o status alterado.';

function conteudo(status: Parameters<typeof montarMenuOperacional>[0]): string {
  return renderToStaticMarkup(
    <ConteudoMenuOperacional placa="BRA2E19" menu={montarMenuOperacional(status)} aoEscolher={() => undefined} />,
  );
}

describe('ConteudoMenuOperacional: itens por status', () => {
  it('DISPONIVEL: Enviar para manutenção e Dar baixa, com o menu acessível pela placa', () => {
    const marcado = conteudo('DISPONIVEL');

    expect(marcado).toContain('role="menu"');
    expect(marcado).toContain('aria-label="Menu operacional do veículo BRA2E19"');
    expect(marcado).toContain('Enviar para manutenção');
    expect(marcado).toContain('Dar baixa');
    expect(marcado).not.toContain('Voltar para disponível');
    expect(marcado).not.toContain(FRASE_FLUXO);
  });

  it('MANUTENCAO: Voltar para disponível e Dar baixa', () => {
    const marcado = conteudo('MANUTENCAO');

    expect(marcado).toContain('Voltar para disponível');
    expect(marcado).toContain('Dar baixa');
    expect(marcado).not.toContain('Enviar para manutenção');
  });

  it('RESERVADO e LOCADO: sem item, só a frase do servidor', () => {
    for (const status of ['RESERVADO', 'LOCADO'] as const) {
      const marcado = conteudo(status);

      expect(marcado).not.toContain('role="menu"');
      expect(marcado).not.toContain('<button');
      expect(marcado).toContain(FRASE_FLUXO);
    }
  });

  it('VENDIDO e BAIXADO: sem item, só a frase de venda ou baixa', () => {
    for (const status of ['VENDIDO', 'BAIXADO'] as const) {
      const marcado = conteudo(status);

      expect(marcado).not.toContain('role="menu"');
      expect(marcado).toContain(FRASE_VENDIDO);
    }
  });

  it('o item Dar baixa usa a cor de perigo; os outros, a cor comum', () => {
    const marcado = conteudo('DISPONIVEL');
    const baixa = marcado.match(/<button[^>]*>Dar baixa<\/button>/)?.[0] ?? '';
    const manutencao = marcado.match(/<button[^>]*>Enviar para manutenção<\/button>/)?.[0] ?? '';

    expect(baixa).toContain('text-status-perigo');
    expect(manutencao).not.toContain('text-status-perigo');
  });
});

describe('MenuOperacionalVeiculo: botão de três pontos', () => {
  it('fechado: botão com o título do desenho, o nome com a placa e aria-expanded falso', () => {
    const marcado = renderToStaticMarkup(
      <MenuOperacionalVeiculo
        placa="BRA2E19"
        menu={montarMenuOperacional('DISPONIVEL')}
        aberto={false}
        desabilitado={false}
        aoAlternar={() => undefined}
        aoFechar={() => undefined}
        aoEscolher={() => undefined}
      />,
    );

    expect(marcado).toContain('title="Menu Operacional"');
    expect(marcado).toContain('aria-label="Menu operacional do veículo BRA2E19"');
    expect(marcado).toContain('aria-expanded="false"');
    expect(marcado).toContain('aria-haspopup="menu"');
    expect(marcado).toContain('h-8 w-8');
    expect(marcado).toContain('more_vert');
    expect(marcado).toContain('font-size:18px');
    expect(marcado).not.toContain('role="menu"');
    expect(marcado).not.toContain('disabled=""');
    expect(marcado).not.toContain('aria-disabled');
  });

  it('só frase (RESERVADO): sem aria-haspopup, pois não há menu para abrir', () => {
    const marcado = renderToStaticMarkup(
      <MenuOperacionalVeiculo
        placa="BRA2E19"
        menu={montarMenuOperacional('RESERVADO')}
        aberto={false}
        desabilitado={false}
        aoAlternar={() => undefined}
        aoFechar={() => undefined}
        aoEscolher={() => undefined}
      />,
    );

    expect(marcado).not.toContain('aria-haspopup');
  });

  it('desabilitado: o botão fica com aria-disabled (sem disabled, para manter o foco) enquanto há pedido em andamento', () => {
    const marcado = renderToStaticMarkup(
      <MenuOperacionalVeiculo
        placa="BRA2E19"
        menu={montarMenuOperacional('DISPONIVEL')}
        aberto={false}
        desabilitado
        aoAlternar={() => undefined}
        aoFechar={() => undefined}
        aoEscolher={() => undefined}
      />,
    );

    expect(marcado).toContain('aria-disabled="true"');
    expect(marcado).not.toContain('disabled=""');
  });
});
