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

  it('os cartões mobile mostram status, código e valor antes dos dados do veículo', () => {
    const html = renderizar({ estado: { dados: COM_DADOS } });
    const cartoes = html.match(/<ul[^>]*aria-label="Histórico de reservas"[^>]*>([\s\S]*?)<\/ul>/)?.[1] ?? '';
    expect(cartoes).toMatch(/Cancelada.*AV-00017.*—.*Jeep Renegade Longitude.*18\/10\/2026 a 21\/10\/2026/);
    expect(cartoes).toContain('h-12 w-16');
    expect(cartoes).toContain('São Paulo - SP •');
  });

  it('o cartão mobile mostra a cidade e o total de diárias quando falta a placa', () => {
    const reserva = { ...RESERVA_HISTORICO, placa: '' };
    const html = renderizar({ estado: { dados: { ...COM_DADOS, itens: [reserva] } } });
    const cartoes = html.match(/<ul[^>]*aria-label="Histórico de reservas"[^>]*>([\s\S]*?)<\/ul>/)?.[1] ?? '';
    expect(cartoes).toContain('São Paulo - SP • 3 diárias');
    expect(cartoes).not.toContain('Placa ');
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

  it('cartão do celular: o código sai pequeno e cinza (desenho 28)', () => {
    const html = renderizar({ estado: { dados: COM_DADOS } });
    const cartoes = html.match(/<ul[^>]*aria-label="Histórico de reservas"[^>]*>([\s\S]*?)<\/ul>/)?.[1] ?? '';
    expect(cartoes).toMatch(/<span class="text-micro text-texto-apoio">AV-00017<\/span>/);
  });

  it('cartão do celular: o valor da concluída fica em negrito e navy, sem riscado (desenho 28)', () => {
    const reserva = { ...RESERVA_HISTORICO, status: 'CONCLUIDA' as const };
    const html = renderizar({ estado: { dados: { ...COM_DADOS, itens: [reserva] } } });
    const cartoes = html.match(/<ul[^>]*aria-label="Histórico de reservas"[^>]*>([\s\S]*?)<\/ul>/)?.[1] ?? '';
    expect(cartoes).toMatch(/<span class="ml-auto text-corpo-denso font-bold tabular-nums text-navy">—<\/span>/);
  });

  it('cartão do celular: o valor da cancelada é pequeno, cinza (texto-apoio) e riscado, como no desenho 28', () => {
    const html = renderizar({ estado: { dados: COM_DADOS } });
    const cartoes = html.match(/<ul[^>]*aria-label="Histórico de reservas"[^>]*>([\s\S]*?)<\/ul>/)?.[1] ?? '';
    expect(cartoes).toMatch(/<span class="ml-auto text-micro font-medium tabular-nums text-texto-apoio line-through">—<\/span>/);
    expect(cartoes).not.toMatch(/<span class="ml-auto text-corpo-denso font-bold[^"]*">—<\/span>/);
  });

  it('cartão do celular: a reserva cancelada tem chip com as cores do desenho 28 e ícone; na tabela (computador) segue cinza', () => {
    const html = renderizar({ estado: { dados: COM_DADOS } });
    const cartoes = html.match(/<ul[^>]*aria-label="Histórico de reservas"[^>]*>([\s\S]*?)<\/ul>/)?.[1] ?? '';
    const tabela = html.match(/<table\b[^>]*>([\s\S]*?)<\/table>/)?.[1] ?? '';
    expect(cartoes).toContain('bg-manutencao-alerta-trilho text-manutencao-alerta-escuro');
    expect(cartoes).not.toContain('bg-status-perigo-fundo');
    expect(cartoes).toContain('cancel');
    expect(tabela).not.toContain('bg-manutencao-alerta-trilho');
    expect(tabela).not.toContain('bg-status-perigo-fundo');
    expect(tabela).toContain('bg-steel-suave');
  });

  it('cartão do celular: reserva cancelada tem 85% de opacidade e o valor riscado (desenho 28)', () => {
    const html = renderizar({ estado: { dados: COM_DADOS } });
    const cartoes = html.match(/<ul[^>]*aria-label="Histórico de reservas"[^>]*>([\s\S]*?)<\/ul>/)?.[1] ?? '';
    expect(cartoes).toContain('class="space-y-2 opacity-85"');
    expect(cartoes).toMatch(/<span class="ml-auto [^"]*line-through[^"]*">—<\/span>/);
  });

  it('cartão do celular: reserva concluída não usa o chip vermelho, nem opacidade, nem valor riscado', () => {
    const reserva = { ...RESERVA_HISTORICO, status: 'CONCLUIDA' as const };
    const html = renderizar({ estado: { dados: { ...COM_DADOS, itens: [reserva] } } });
    const cartoes = html.match(/<ul[^>]*aria-label="Histórico de reservas"[^>]*>([\s\S]*?)<\/ul>/)?.[1] ?? '';
    expect(cartoes).not.toContain('bg-manutencao-alerta-trilho');
    expect(cartoes).not.toContain('bg-status-perigo-fundo');
    expect(cartoes).toContain('bg-steel-suave text-status-neutro');
    expect(cartoes).toContain('class="space-y-2"');
    expect(cartoes).not.toContain('opacity-85');
    expect(cartoes).not.toContain('line-through');
  });

  it('cartão do celular sem cidade, com placa: mostra "Placa XXX" (sem as diárias)', () => {
    const reserva = { ...RESERVA_HISTORICO, locadoraCidade: '', locadoraUf: '' };
    const html = renderizar({ estado: { dados: { ...COM_DADOS, itens: [reserva] } } });
    const cartoes = html.match(/<ul[^>]*aria-label="Histórico de reservas"[^>]*>([\s\S]*?)<\/ul>/)?.[1] ?? '';
    expect(cartoes).toContain('Placa BRA2E19');
    expect(cartoes).not.toContain('diárias</p>');
  });

  it('cartão do celular: a última linha junta a cidade e a placa com o prefixo "Placa "', () => {
    const html = renderizar({ estado: { dados: COM_DADOS } });
    const cartoes = html.match(/<ul[^>]*aria-label="Histórico de reservas"[^>]*>([\s\S]*?)<\/ul>/)?.[1] ?? '';
    expect(cartoes).toContain('São Paulo - SP • Placa BRA2E19');
  });

  it('foto do cartão do celular: com capa mostra a miniatura (quando existe) ou a imagem; sem capa, o ícone de carro', () => {
    const URL_IMAGEM = 'https://armazenamento.exemplo/bucket/veiculos/7/fotos/abc.jpg?assinatura=teste';
    const URL_MINIATURA = 'https://armazenamento.exemplo/bucket/veiculos/7/fotos/abc-mini.jpg?assinatura=teste';
    const cartoesDe = (itens: typeof COM_DADOS.itens): string => {
      const html = renderizar({ estado: { dados: { ...COM_DADOS, itens } } });
      return html.match(/<ul[^>]*aria-label="Histórico de reservas"[^>]*>([\s\S]*?)<\/ul>/)?.[1] ?? '';
    };

    const comMiniatura = cartoesDe([
      { ...RESERVA_HISTORICO, fotoCapa: { url: URL_IMAGEM, urlMiniatura: URL_MINIATURA, expiraEm: '2026-11-16T21:15:00' } },
    ]);
    expect(comMiniatura).toContain(`src="${URL_MINIATURA}"`);
    expect(comMiniatura).not.toContain(`src="${URL_IMAGEM}"`);

    const comImagem = cartoesDe([
      { ...RESERVA_HISTORICO, fotoCapa: { url: URL_IMAGEM, urlMiniatura: null, expiraEm: '2026-11-16T21:15:00' } },
    ]);
    expect(comImagem).toContain(`src="${URL_IMAGEM}"`);

    const semCapa = cartoesDe([{ ...RESERVA_HISTORICO, fotoCapa: null }]);
    expect(semCapa).not.toContain('<img');
    expect(semCapa).toContain('directions_car');
  });

  it('nenhum termo sem lastro do desenho aparece no histórico', () => {
    const html = renderizar({ estado: { dados: COM_DADOS } });
    const limpo = semTextosAprovadosDoDesenho(html).toLowerCase();
    for (const termo of TERMOS_SEM_LASTRO) {
      expect(limpo, `termo proibido: ${termo}`).not.toContain(termo.toLowerCase());
    }
  });
});
