import { describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MENSAGENS_DEVOLUCAO as M } from '@/lib/mensagensDevolucao';
import type { ContextoDevolucao } from '@/types/devolucao';
import { CaixaConfirmacaoEncerramento } from './CaixaConfirmacaoEncerramento';
import { falhaDoEncerramento, desfechoCaucaoPrevisto } from '../regras/regrasDevolucao';
import { ErroApi } from '@/types/api';

const contexto: ContextoDevolucao = {
  locacao: { idLocacao: 42, idReserva: 8, idVeiculo: 3, kmSaida: 1000, status: 'EM_ANDAMENTO', valorCaucao: 1200 },
  entrada: null, avariasEntrada: [], comparacao: null,
  placa: 'ABC1D23', marca: 'Fiat', modelo: 'Toro', diariasPrevistas: 2, valorDiaria: 100, valorDiariaStr: 'R$ 100,00',
  totalDiarias: 200, totalDiariasStr: 'R$ 200,00', totalExtras: 160, totalExtrasStr: 'R$ 160,00', valorTotalPrevisto: 360,
  valorTotalPrevistoStr: 'R$ 360,00', idConfiguracao: 3, versaoEsperada: 7, caucao: { situacao: 'PENDENTE', acao: null },
};
const desfecho = desfechoCaucaoPrevisto(1200, 160);

function renderizar(parcial: Partial<Parameters<typeof CaixaConfirmacaoEncerramento>[0]> = {}): string {
  return renderToStaticMarkup(
    <CaixaConfirmacaoEncerramento
      contexto={contexto}
      desfecho={desfecho}
      enviando={false}
      falha={null}
      aoConfirmar={vi.fn()}
      aoCancelar={vi.fn()}
      {...parcial}
    />,
  );
}

describe('caixa de confirmação do encerramento (decisão D1, anexo tela 06)', () => {
  it('mostra o título, as três linhas do total do servidor e a frase da caução', () => {
    const html = renderizar();
    expect(html).toContain(M.caixa.titulo);
    expect(html).toContain(M.caixa.diarias(2, 'R$ 100,00', 'R$ 200,00'));
    expect(html).toContain(M.caixa.extras('R$ 160,00'));
    expect(html).toContain(M.caixa.total('R$ 360,00'));
    expect(html).toContain(desfecho.frase);
    expect(html).toContain(M.caixa.confirmo);
  });

  it('o botão de confirmar começa desligado: a marcação é obrigatória', () => {
    const html = renderizar();
    expect(html).toMatch(new RegExp(`<button[^>]*disabled=""[^>]*>(?:(?!</button>).)*${M.caixa.confirmar}`));
    expect(html).toContain('aria-required="true"');
    expect(html).toContain(M.caixa.cancelar);
  });

  it('o botão de confirmar desligado parece desligado: opacidade e cursor de desabilitado', () => {
    const html = renderizar();
    const botao = html.match(new RegExp(`<button[^>]*>(?:(?!</button>).)*${M.caixa.confirmar}`))?.[0] ?? '';
    expect(botao).toContain('disabled=""');
    expect(botao).toContain('disabled:opacity-60');
    expect(botao).toContain('disabled:cursor-not-allowed');
  });

  it('durante o envio: aviso de encerramento, região ocupada e os botões travados', () => {
    const html = renderizar({ enviando: true });
    expect(html).toContain(M.encerrando);
    expect(html).toContain('aria-busy="true"');
    expect(html).toMatch(new RegExp(`<button[^>]*disabled=""[^>]*>(?:(?!</button>).)*${M.caixa.cancelar}`));
  });

  it('502 (ou queda de conexão): mensagem do servidor, complemento e Tentar de novo', () => {
    const falha = falhaDoEncerramento(new ErroApi({ status: 502, mensagem: 'Não foi possível consultar a caução no provedor agora.' }));
    const html = renderizar({ falha });
    expect(html).toContain('Não foi possível consultar a caução no provedor agora.');
    expect(html).toContain(M.complementoFalha);
    expect(html).toContain(M.tentarDeNovo);
    expect(html).toContain('role="alert"');
  });

  it('409: mensagem do servidor e o complemento de dados atualizados, sem botão de tentar de novo', () => {
    const falha = falhaDoEncerramento(new ErroApi({ status: 409, mensagem: 'O total mudou desde a confirmação.' }));
    const html = renderizar({ falha });
    expect(html).toContain('O total mudou desde a confirmação.');
    expect(html).toContain(M.complemento409);
    expect(html).not.toContain(M.tentarDeNovo);
  });

  it('502 com Tentar de novo começa desligado, porque a marcação ainda não foi feita', () => {
    const falha = falhaDoEncerramento(new ErroApi({ status: 502, mensagem: 'Provedor fora.' }));
    const html = renderizar({ falha });
    expect(html).toMatch(new RegExp(`<button[^>]*disabled=""[^>]*>(?:(?!</button>).)*${M.tentarDeNovo}`));
  });

  it('o título é focável por script (tabindex -1), para receber o foco ao abrir a caixa', () => {
    const html = renderizar();
    expect(html).toMatch(/<h3[^>]*tabindex="-1"[^>]*>/);
    expect(html).toContain('id="caixa-encerramento"');
  });
});
