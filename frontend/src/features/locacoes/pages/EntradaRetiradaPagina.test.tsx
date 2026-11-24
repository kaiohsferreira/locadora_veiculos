import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter } from 'react-router-dom';
import { ErroApi } from '@/types/api';
import type { ItemPainelOperacional, RespostaPainelOperacionalHoje } from '@/types/painelOperacional';
import { termosCortadosPresentesRetirada, termosDaCaucaoPresentesRetirada } from '../retirada/termosCortadosRetirada';
import { useRetiradasDeHoje } from '../hooks/useRetiradasDeHoje';
import EntradaRetiradaPagina from './EntradaRetiradaPagina';

vi.mock('../hooks/useRetiradasDeHoje', () => ({ useRetiradasDeHoje: vi.fn() }));

type Consulta = ReturnType<typeof useRetiradasDeHoje>;

const daReserva: ItemPainelOperacional = {
  origem: 'RESERVA', idReserva: 31, idLocacao: null, codigoReserva: 'AV-00031', dataHora: '24/11/2026 00:00',
  status: 'CONFIRMADA', idCliente: 5, nomeCliente: 'Ana Souza', idVeiculo: 2, placa: 'ABC1D23', marca: 'Jeep', modelo: 'Renegade',
};
const daLocacao: ItemPainelOperacional = {
  ...daReserva, origem: 'LOCACAO', idReserva: null, idLocacao: 37, codigoReserva: 'AV-00037', status: 'EM_ANDAMENTO',
  nomeCliente: 'Bruno Lima', placa: 'DEF2G34', marca: 'Fiat', modelo: 'Toro',
};
const RESPOSTA: RespostaPainelOperacionalHoje = { data: '2026-11-24', dataStr: '24/11/2026', retiradas: [daReserva, daLocacao], devolucoes: [] };

/** Abertura da tag do link com o endereço dado (o aria-label vem antes do href). */
function linkComEndereco(html: string, endereco: string): string {
  const posicao = html.indexOf(`href="${endereco}"`);
  return html.slice(html.lastIndexOf('<a ', posicao), html.indexOf('>', posicao) + 1);
}

function renderizar(parcial: Partial<Consulta>): string {
  vi.mocked(useRetiradasDeHoje).mockReturnValue({
    data: undefined, isPending: false, isError: false, isFetching: false, refetch: vi.fn(), ...parcial,
  } as unknown as Consulta);
  return renderToStaticMarkup(
    <MemoryRouter initialEntries={['/back-office/locacoes/retirada']}>
      <EntradaRetiradaPagina />
    </MemoryRouter>,
  );
}

describe('EntradaRetiradaPagina (página de entrada da retirada)', () => {
  beforeEach(() => vi.clearAllMocks());

  it('mostra o campo do código da reserva com a dica e o botão', () => {
    const html = renderizar({ data: RESPOSTA });
    expect(html).toContain('Código da reserva');
    expect(html).toContain('Exemplo: AV-00042 ou 42.');
    expect(html).toContain('Abrir retirada');
  });

  it('lista as duas origens: código do servidor e uma ação por linha, no passo certo', () => {
    const html = renderizar({ data: RESPOSTA });
    expect(html).toContain('Retiradas previstas para hoje (24/11/2026)');
    expect(html).toContain('AV-00031');
    expect(html).toContain('AV-00037');
    expect(html.match(/aria-label="Iniciar retirada /g)).toHaveLength(1);
    expect(html.match(/aria-label="Continuar para vistoria /g)).toHaveLength(1);
    expect(html).toContain('href="/back-office/locacoes/retirada/31"');
    expect(html).toContain('href="/back-office/locacoes/37/vistoria"');
  });

  it('cada ação tem nome para o leitor de tela com o texto visível e o código da linha', () => {
    const html = renderizar({ data: RESPOSTA });
    const retirada = linkComEndereco(html, '/back-office/locacoes/retirada/31');
    const vistoria = linkComEndereco(html, '/back-office/locacoes/37/vistoria');
    expect(retirada).toContain('aria-label="Iniciar retirada AV-00031"');
    expect(vistoria).toContain('aria-label="Continuar para vistoria AV-00037"');
    expect(html).toContain('>Iniciar retirada</a>');
    expect(html).toContain('>Continuar para vistoria</a>');
  });

  it('item de locação sem código mostra o número da locação na coluna Reserva e no nome da ação', () => {
    const html = renderizar({ data: { ...RESPOSTA, retiradas: [{ ...daLocacao, codigoReserva: null, idLocacao: 9 }] } });
    expect(html).toContain('Locação nº 9');
    expect(linkComEndereco(html, '/back-office/locacoes/9/vistoria')).toContain('aria-label="Continuar para vistoria Locação nº 9"');
  });

  it('linha sem referência: o link fica só com o texto visível, sem aria-label', () => {
    const html = renderizar({ data: { ...RESPOSTA, retiradas: [{ ...daReserva, codigoReserva: '' }] } });
    expect(linkComEndereco(html, '/back-office/locacoes/retirada/31')).not.toContain('aria-label');
    expect(html).toContain('>Iniciar retirada</a>');
  });

  it('lista vazia traz o texto de vazio e nenhuma ação', () => {
    const html = renderizar({ data: { ...RESPOSTA, retiradas: [] } });
    expect(html).toContain('Nenhuma retirada prevista para hoje.');
    expect(html).not.toContain('Iniciar retirada');
  });

  it('erro da consulta mostra a mensagem do servidor e Tentar novamente, sem a lista', () => {
    const html = renderizar({ isError: true, error: new ErroApi({ status: 403, mensagem: 'Sem permissão para ver as retiradas.' }) });
    expect(html).toContain('Sem permissão para ver as retiradas.');
    expect(html).toContain('Tentar novamente');
    expect(html).not.toContain('Iniciar retirada');
  });

  it('carregando não mostra linhas nem o texto de vazio', () => {
    const html = renderizar({ isPending: true });
    expect(html).toContain('aria-busy="true"');
    expect(html).not.toContain('Nenhuma retirada prevista para hoje.');
  });

  it('nenhum termo cortado do desenho aparece, com lista cheia ou vazia', () => {
    expect(termosCortadosPresentesRetirada(renderizar({ data: RESPOSTA }))).toEqual([]);
    expect(termosCortadosPresentesRetirada(renderizar({ data: { ...RESPOSTA, retiradas: [] } }))).toEqual([]);
    // A página de entrada não tem caução: os termos da caução (lista própria desde a v4 da tela 16) também não aparecem.
    expect(termosDaCaucaoPresentesRetirada(renderizar({ data: RESPOSTA }))).toEqual([]);
    expect(termosDaCaucaoPresentesRetirada(renderizar({ data: { ...RESPOSTA, retiradas: [] } }))).toEqual([]);
  });
});
