import { describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter } from 'react-router-dom';
import { AvisoProvider } from '@/components/ui';
import { PainelReservasAtivas } from '@/features/area-cliente/reservas/componentes/PainelReservasAtivas';
import type { EstadoListaReservas } from '@/features/area-cliente/reservas/estadoLista';
import {
  RESERVA_ATIVA,
  RESERVA_CONFIRMADA,
  semTextosAprovadosDoDesenho,
  TERMOS_SEM_LASTRO,
} from '@/features/area-cliente/reservas/fixturasReservas';
import { ErroApi } from '@/types/api';
import type { RespostaReservas } from '@/types/reservas';

function estado(parcial: Partial<EstadoListaReservas>): EstadoListaReservas {
  return {
    dados: undefined,
    carregando: false,
    erro: null,
    atualizando: false,
    aoTentarNovamente: vi.fn(),
    ...parcial,
  };
}

function renderizar(parcial: Partial<EstadoListaReservas>): string {
  return renderToStaticMarkup(
    <MemoryRouter>
      <AvisoProvider>
        <PainelReservasAtivas
          estado={estado(parcial)}
          pagina={1}
          aoMudarPagina={vi.fn()}
          nomeCondutor="Marina Costa"
          aoCancelar={vi.fn()}
        />
      </AvisoProvider>
    </MemoryRouter>,
  );
}

const COM_DADOS: RespostaReservas = {
  itens: [RESERVA_ATIVA, RESERVA_CONFIRMADA],
  total: 2,
  pagina: 1,
  tamanhoPagina: 5,
};

describe('PainelReservasAtivas', () => {
  it('carregando: esqueleto ocupado, sem cartão de verdade', () => {
    const html = renderizar({ carregando: true });
    expect(html).toContain('aria-busy="true"');
    expect(html).not.toContain('AV-00042');
  });

  it('lista vazia: título do vazio das ativas e botão para buscar um veículo', () => {
    const html = renderizar({ dados: { itens: [], total: 0, pagina: 1, tamanhoPagina: 5 } });
    expect(html).toContain('Você não tem reservas ativas.');
    expect(html).toContain('Buscar um veículo');
    expect(html).toContain('href="/"');
  });

  it('erro sem dado: título da tela, a mensagem do ErroApi (já amigável) e Tentar novamente', () => {
    const html = renderizar({ erro: new ErroApi({ status: 500, mensagem: 'Falha de conexão com o servidor.' }) });
    expect(html).toContain('Não foi possível carregar suas reservas.');
    expect(html).toContain('Falha de conexão com o servidor.');
    expect(html).toContain('Tentar novamente');
  });

  it('erro com dado já na tela: mantém os cartões e avisa que podem estar desatualizados', () => {
    const html = renderizar({ dados: COM_DADOS, erro: new ErroApi({ status: 0, mensagem: 'Falha de conexão.' }) });
    expect(html).toContain('AV-00042');
    expect(html).toContain('Os dados abaixo podem estar desatualizados.');
    expect(html).toContain('role="alert"');
  });

  it('cartão: código, marca e modelo, placa formatada, datas sem hora, valor e o botão Cancelar reserva', () => {
    // O Intl põe espaço não separável entre "R$" e o valor; a tela mostra isso assim, e o teste normaliza.
    const html = renderizar({ dados: COM_DADOS }).replace(/ /g, ' ');
    expect(html).toContain('AV-00042');
    expect(html).toContain('Jeep Renegade Longitude');
    expect(html).toContain('BRA2E19');
    expect(html).toContain('14/11/2026');
    expect(html).toContain('18/11/2026');
    expect(html).not.toMatch(/14\/11\/2026 às/);
    expect(html).toContain('R$ 1.146,16');
    expect(html).toContain('4 diárias × R$ 286,54');
    expect(html).toContain('Cancelar reserva');
    expect(html).toContain('Criada em 02/11/2026 14:22');
  });

  it('mostra o status com o rótulo do enum (Ativa e Confirmada)', () => {
    const html = renderizar({ dados: COM_DADOS });
    expect(html).toContain('Ativa');
    expect(html).toContain('Confirmada');
  });

  it('o rodapé de paginação resume as reservas da aba', () => {
    const html = renderizar({ dados: COM_DADOS });
    expect(html).toContain('Paginação');
    expect(html).toContain('reservas');
  });

  it('nenhum termo sem lastro do desenho aparece na aba de ativas', () => {
    const html = renderizar({ dados: COM_DADOS });
    const limpo = semTextosAprovadosDoDesenho(html).toLowerCase();
    for (const termo of TERMOS_SEM_LASTRO) {
      expect(limpo, `termo proibido: ${termo}`).not.toContain(termo.toLowerCase());
    }
  });
});
