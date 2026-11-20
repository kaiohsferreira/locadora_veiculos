import { describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { AvisoProvider } from '@/components/ui';
import { PainelHistoricoReservas } from '@/features/area-cliente/reservas/componentes/PainelHistoricoReservas';
import type { EstadoListaReservas } from '@/features/area-cliente/reservas/estadoLista';
import {
  RESERVA_HISTORICO,
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

interface OpcoesPainel {
  estado?: Partial<EstadoListaReservas>;
  buscaAplicada?: string;
}

function renderizar({ estado: parcial = {}, buscaAplicada = '' }: OpcoesPainel): string {
  return renderToStaticMarkup(
    <AvisoProvider>
      <PainelHistoricoReservas
        estado={estado(parcial)}
        pagina={1}
        aoMudarPagina={vi.fn()}
        buscaDigitada={buscaAplicada}
        buscaAplicada={buscaAplicada}
        aoMudarBusca={vi.fn()}
        aoEnviarBusca={vi.fn()}
        aoLimparBusca={vi.fn()}
      />
    </AvisoProvider>,
  );
}

const COM_DADOS: RespostaReservas = {
  itens: [RESERVA_HISTORICO],
  total: 3,
  pagina: 1,
  tamanhoPagina: 10,
};

describe('PainelHistoricoReservas', () => {
  it('tabela com 6 colunas e sem coluna de ações', () => {
    const html = renderizar({ estado: { dados: COM_DADOS } });
    const tabela = html.match(/<table\b[^>]*>([\s\S]*?)<\/table>/)?.[1] ?? '';
    expect(tabela.match(/<th /g)?.length).toBe(6);
    expect(tabela).toContain('Reserva / data');
    expect(tabela).toContain('Veículo e categoria');
    expect(tabela).toContain('Locadora');
    expect(tabela).toContain('Período');
    expect(tabela).toContain('Valor estimado');
    expect(tabela).toContain('Status');
    expect(tabela).not.toContain('Ações');
  });

  it('a linha mostra código, período com datas, diárias, locadora, status e "—" quando o valor é nulo', () => {
    const html = renderizar({ estado: { dados: COM_DADOS } });
    const tabela = html.match(/<table\b[^>]*>([\s\S]*?)<\/table>/)?.[1] ?? '';
    expect(tabela).toContain('AV-00017');
    expect(tabela).toContain('18/10/2026 a 21/10/2026');
    expect(tabela).toContain('3 diárias');
    expect(tabela).toContain('Jeep Renegade Longitude');
    expect(tabela).toContain('SUV Compacto Especial');
    expect(tabela).toContain('Locadora Centro');
    expect(tabela).toContain('São Paulo - SP');
    expect(tabela).toContain('Cancelada');
    expect(tabela).toContain('—');
  });

  it('os cartões agrupam os mesmos seis campos em uma lista com nome acessível', () => {
    const html = renderizar({ estado: { dados: COM_DADOS } });
    const cartoes = html.match(/<ul[^>]*aria-label="Histórico de reservas"[^>]*>([\s\S]*?)<\/ul>/)?.[1];
    expect(cartoes).toContain('<li');
    const campos = Array.from(
      cartoes?.matchAll(/<dt[^>]*>([^<]+)<\/dt><dd>([\s\S]*?)<\/dd>/g) ?? [],
      ([, rotulo, valor]) => ({ rotulo, valor }),
    );
    expect(campos).toEqual([
      { rotulo: 'Reserva / data', valor: expect.stringMatching(/AV-00017.*18\/10\/2026 a 21\/10\/2026/) },
      { rotulo: 'Veículo e categoria', valor: expect.stringMatching(/Jeep Renegade Longitude.*SUV Compacto Especial/) },
      { rotulo: 'Locadora', valor: expect.stringMatching(/Locadora Centro.*São Paulo - SP/) },
      { rotulo: 'Período', valor: expect.stringContaining('3 diárias') },
      { rotulo: 'Valor estimado', valor: expect.stringContaining('—') },
      { rotulo: 'Status', valor: expect.stringContaining('Cancelada') },
    ]);
  });

  it('a busca aparece com o rótulo, o placeholder e o botão Filtrar', () => {
    const html = renderizar({ estado: { dados: COM_DADOS } });
    expect(html).toContain('Filtrar por código ou modelo');
    expect(html).toContain('placeholder="Filtrar por código ou modelo..."');
    expect(html).toContain('Filtrar');
  });

  it('carregando: linhas de esqueleto e área ocupada', () => {
    const html = renderizar({ estado: { carregando: true } });
    expect(html).toContain('aria-busy="true"');
    expect(html).not.toContain('AV-00017');
  });

  it('vazio sem busca: título do histórico vazio e descrição', () => {
    const html = renderizar({ estado: { dados: { itens: [], total: 0, pagina: 1, tamanhoPagina: 10 } } });
    expect(html).toContain('Nenhuma reserva no histórico.');
    expect(html).toContain('Reservas concluídas e canceladas aparecem aqui.');
    expect(html).not.toContain('Limpar busca');
  });

  it('vazio com busca: "Nenhuma reserva encontrada." e o botão Limpar busca', () => {
    const html = renderizar({
      estado: { dados: { itens: [], total: 0, pagina: 1, tamanhoPagina: 10 } },
      buscaAplicada: 'AV-99999',
    });
    expect(html).toContain('Nenhuma reserva encontrada.');
    expect(html).toContain('Limpar busca');
  });

  it('erro sem dado: EstadoErro com Tentar novamente', () => {
    const html = renderizar({ estado: { erro: new ErroApi({ status: 0, mensagem: 'Falha de conexão.' }) } });
    expect(html).toContain('Não foi possível carregar suas reservas.');
    expect(html).toContain('Tentar novamente');
  });

  it('paginação de 10 em 10 com o resumo das reservas', () => {
    const html = renderizar({ estado: { dados: COM_DADOS } });
    expect(html).toContain('Exibindo');
    expect(html).toContain('reservas');
  });

  it('nenhum termo sem lastro do desenho aparece no histórico', () => {
    const html = renderizar({ estado: { dados: COM_DADOS } });
    const limpo = semTextosAprovadosDoDesenho(html).toLowerCase();
    for (const termo of TERMOS_SEM_LASTRO) {
      expect(limpo, `termo proibido: ${termo}`).not.toContain(termo.toLowerCase());
    }
  });
});
