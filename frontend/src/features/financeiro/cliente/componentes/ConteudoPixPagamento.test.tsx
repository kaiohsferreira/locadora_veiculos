import { describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MENSAGEM_ERRO_API_PADRAO } from '@/lib/mensagens';
import { ErroApi } from '@/types/api';
import type { CobrancaPix, MeuPagamento } from '@/types/pagamentos';
import {
  ConteudoPixErro,
  ConteudoPixExpirado,
  ConteudoPixGerando,
  ConteudoPixPronto,
  OrigemCobrancaPix,
  ResumoCobrancaPix,
} from './ConteudoPixPagamento';
import { termosCortadosPresentes } from './termosCortadosPix';

const PAGAMENTO: MeuPagamento = {
  idPagamento: 77,
  tipo: 'DIARIA',
  valor: 395.6,
  valorStr: 'R$ 395,60',
  dataVencimento: '2026-11-22',
  dataVencimentoStr: '22/11/2026',
  dataPagamentoStr: null,
  diasParaVencer: 2,
  situacao: 'PENDENTE',
  forma: null,
  idLocacao: 123,
  idVenda: null,
  veiculoDescricao: 'Jeep Renegade Longitude',
};

const COBRANCA: CobrancaPix = {
  idPagamento: 77,
  forma: 'PIX',
  valor: 395.6,
  valorStr: 'R$ 395,60',
  qrCodeBase64: 'iVBORw0KGgoAAAANSUhEUgAAAMgAAADICAIAAAAiOjnJ',
  copiaECola: '00020126580014BR.GOV.BCB.PIX0136FAKE-COPIA-PAG-77',
  expiraEmStr: '20/11/2026 15:30:00',
  segundosParaExpirar: 900,
};

describe('ResumoCobrancaPix (bloco de valor da gaveta)', () => {
  it('mostra valor e vencimento curto, com as cores de destaque da tela 30', () => {
    const html = renderToStaticMarkup(<ResumoCobrancaPix pagamento={PAGAMENTO} />);
    expect(html).toContain('Valor da Fatura');
    expect(html).toContain('R$ 395,60');
    expect(html).toContain('Venc. 22/11');
    expect(html).toContain('text-frota-oficina');
    expect(html).toContain('bg-frota-oficina-fundo');
    expect(html).toContain('text-pagamento-texto-pendente');
  });

  it('a origem e o número da cobrança ficam fora deste bloco (vão para o cabeçalho)', () => {
    const html = renderToStaticMarkup(<ResumoCobrancaPix pagamento={PAGAMENTO} />);
    expect(html).not.toContain('Locação nº');
    expect(html).not.toContain('Cobrança nº');
  });

  it('não mostra nenhum dos textos cortados pela decisão P4', () => {
    const html = renderToStaticMarkup(<ResumoCobrancaPix pagamento={PAGAMENTO} />);
    expect(termosCortadosPresentes(html)).toEqual([]);
  });
});

describe('OrigemCobrancaPix (linha abaixo do título da gaveta)', () => {
  it('locação: mostra só "Locação nº", sem o número da cobrança (não existe no desenho nem no contrato)', () => {
    const html = renderToStaticMarkup(<OrigemCobrancaPix pagamento={PAGAMENTO} />);
    expect(html).toContain('Locação nº 123');
    expect(html).not.toContain('Cobrança nº');
  });

  it('venda aparece como Venda nº, sem linha de locação e sem o número da cobrança', () => {
    const html = renderToStaticMarkup(
      <OrigemCobrancaPix pagamento={{ ...PAGAMENTO, idLocacao: null, idVenda: 45 }} />,
    );
    expect(html).toContain('Venda nº 45');
    expect(html).not.toContain('Locação nº');
    expect(html).not.toContain('Cobrança nº');
  });

  it('sem locação nem venda, não mostra nenhum texto de cobrança', () => {
    const html = renderToStaticMarkup(
      <OrigemCobrancaPix pagamento={{ ...PAGAMENTO, idLocacao: null, idVenda: null }} />,
    );
    expect(html).not.toContain('Locação nº');
    expect(html).not.toContain('Venda nº');
    expect(html).not.toContain('Cobrança nº');
  });

  it('não mostra nenhum dos textos cortados pela decisão P4', () => {
    const html = renderToStaticMarkup(<OrigemCobrancaPix pagamento={PAGAMENTO} />);
    expect(termosCortadosPresentes(html)).toEqual([]);
  });
});

describe('ConteudoPixGerando', () => {
  it('mostra o texto de gerando com aviso de status', () => {
    const html = renderToStaticMarkup(<ConteudoPixGerando />);
    expect(html).toContain('Gerando seu PIX...');
    expect(html).toContain('<output');
  });

  it('não mostra nenhum dos textos cortados pela decisão P4', () => {
    const html = renderToStaticMarkup(<ConteudoPixGerando />);
    expect(termosCortadosPresentes(html)).toEqual([]);
  });
});

describe('ConteudoPixPronto (QR, contagem e copia e cola)', () => {
  it('mostra o QR como imagem PNG a partir do base64 da resposta, com o texto alternativo aprovado', () => {
    const html = renderToStaticMarkup(<ConteudoPixPronto cobranca={COBRANCA} aoGerarNovo={vi.fn()} />);
    expect(html).toContain(`src="data:image/png;base64,${COBRANCA.qrCodeBase64}"`);
    expect(html).toContain('alt="QR Code para pagamento via PIX"');
  });

  it('a caixa do QR segue o desenho: raio de 8 px (token rounded-cartao, o xl do desenho) com sombra interna', () => {
    const html = renderToStaticMarkup(<ConteudoPixPronto cobranca={COBRANCA} aoGerarNovo={vi.fn()} />);
    expect(html).toMatch(/<div class="rounded-cartao bg-steel-fundo p-4 shadow-inner">/);
    expect(html).not.toContain('rounded-xl');
  });

  it('começa a contagem em segundosParaExpirar (900 vira 15:00) e diz que o código expira', () => {
    const html = renderToStaticMarkup(<ConteudoPixPronto cobranca={COBRANCA} aoGerarNovo={vi.fn()} />);
    expect(html).toContain('Escaneie com o app do seu Banco');
    expect(html).toContain('O código expira em');
    expect(html).toContain('15:00');
    expect(html).toContain('text-frota-oficina');
  });

  it('a contagem inicial respeita o valor do servidor, sem depender do relógio', () => {
    const html = renderToStaticMarkup(
      <ConteudoPixPronto cobranca={{ ...COBRANCA, segundosParaExpirar: 61 }} aoGerarNovo={vi.fn()} />,
    );
    expect(html).toContain('01:01');
  });

  it('mostra o copia e cola somente leitura com o botão Copiar, sem o feedback no início', () => {
    const html = renderToStaticMarkup(<ConteudoPixPronto cobranca={COBRANCA} aoGerarNovo={vi.fn()} />);
    expect(html).toContain(`value="${COBRANCA.copiaECola}"`);
    expect(html).toContain('readonly=""');
    expect(html).toContain('aria-label="Código PIX copia e cola"');
    expect(html).toContain('Copiar');
    expect(html).not.toContain('Código copiado');
  });

  it('sem validade (zero segundos) já mostra o estado expirado, sem QR', () => {
    const html = renderToStaticMarkup(
      <ConteudoPixPronto cobranca={{ ...COBRANCA, segundosParaExpirar: 0 }} aoGerarNovo={vi.fn()} />,
    );
    expect(html).toContain('O código PIX expirou.');
    expect(html).toContain('Gerar novo código');
    expect(html).not.toContain('data:image/png;base64');
  });

  it('não mostra nenhum dos textos cortados pela decisão P4', () => {
    const html = renderToStaticMarkup(<ConteudoPixPronto cobranca={COBRANCA} aoGerarNovo={vi.fn()} />);
    expect(termosCortadosPresentes(html)).toEqual([]);
  });
});

describe('ConteudoPixExpirado', () => {
  it('diz que o código expirou e oferece gerar outro', () => {
    const html = renderToStaticMarkup(<ConteudoPixExpirado aoGerarNovo={vi.fn()} />);
    expect(html).toContain('O código PIX expirou.');
    expect(html).toContain('Gerar novo código');
    expect(html).not.toContain('<img');
  });

  it('não mostra nenhum dos textos cortados pela decisão P4', () => {
    const html = renderToStaticMarkup(<ConteudoPixExpirado aoGerarNovo={vi.fn()} />);
    expect(termosCortadosPresentes(html)).toEqual([]);
  });
});

describe('ConteudoPixErro', () => {
  it('mostra a mensagem amigável do erro da API e o botão Tentar novamente', () => {
    const html = renderToStaticMarkup(
      <ConteudoPixErro
        erro={new ErroApi({ status: 409, mensagem: 'Este pagamento já foi pago. Não é preciso gerar um PIX.' })}
        aoTentarNovamente={vi.fn()}
      />,
    );
    expect(html).toContain('role="alert"');
    expect(html).toContain('Este pagamento já foi pago. Não é preciso gerar um PIX.');
    expect(html).toContain('Tentar novamente');
  });

  it('erro que não é da API vira a mensagem padrão, sem o texto técnico', () => {
    const html = renderToStaticMarkup(
      <ConteudoPixErro erro={new Error('Falha interna: stack trace 0xBAD')} aoTentarNovamente={vi.fn()} />,
    );
    expect(html).toContain(MENSAGEM_ERRO_API_PADRAO);
    expect(html).not.toContain('stack trace');
  });

  it('não mostra nenhum dos textos cortados pela decisão P4', () => {
    const html = renderToStaticMarkup(
      <ConteudoPixErro erro={new Error('Falha')} aoTentarNovamente={vi.fn()} />,
    );
    expect(termosCortadosPresentes(html)).toEqual([]);
  });
});
