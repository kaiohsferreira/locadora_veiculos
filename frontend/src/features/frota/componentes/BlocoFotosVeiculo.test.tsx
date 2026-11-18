import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { AvisoProvider } from '@/components/ui';
import { useFilaEnvioFotos, type FilaFotosVeiculo } from '@/features/frota/hooks/useFilaEnvioFotos';
import { useFotosVeiculo } from '@/features/frota/hooks/useFotosVeiculo';
import { useRemoverFotoVeiculo } from '@/features/frota/hooks/useRemoverFotoVeiculo';
import { ErroApi } from '@/types/api';
import type { FotoDoVeiculo, ItemFilaFoto, SituacaoItemFila } from '@/types/fotosVeiculo';
import { BlocoFotosVeiculo } from './BlocoFotosVeiculo';

// Renderização só no servidor (sem Testing Library): os hooks são trocados por dublês com o estado de cada cenário.
vi.mock('@/features/frota/hooks/useFotosVeiculo', () => ({
  useFotosVeiculo: vi.fn(),
}));
vi.mock('@/features/frota/hooks/useRemoverFotoVeiculo', () => ({
  useRemoverFotoVeiculo: vi.fn(),
}));
vi.mock('@/features/frota/hooks/useFilaEnvioFotos', () => ({
  useFilaEnvioFotos: vi.fn(),
}));

const consultaMock = vi.mocked(useFotosVeiculo);
const remocaoMock = vi.mocked(useRemoverFotoVeiculo);
const filaMock = vi.mocked(useFilaEnvioFotos);

function foto(idVeiculoFoto: number, ordem: number): FotoDoVeiculo {
  return {
    idVeiculoFoto,
    ordem,
    url: `https://a/${idVeiculoFoto}`,
    urlMiniatura: null,
    expiraEm: '2026-11-16T13:15:00.0000000+00:00',
  };
}

function fotos(quantidade: number): FotoDoVeiculo[] {
  return Array.from({ length: quantidade }, (_, indice) => foto(indice + 1, indice + 1));
}

function item(chave: number, situacao: SituacaoItemFila, parcial: Partial<ItemFilaFoto> = {}): ItemFilaFoto {
  return {
    chave,
    nome: `foto-${chave}.png`,
    previa: `blob:previa-${chave}`,
    situacao,
    percentual: 0,
    erro: null,
    foto: null,
    ...parcial,
  };
}

function consultaCom(parcial: Record<string, unknown>): ReturnType<typeof useFotosVeiculo> {
  return {
    data: undefined,
    error: null,
    isPending: false,
    isError: false,
    isSuccess: true,
    isFetching: false,
    refetch: vi.fn(),
    ...parcial,
  } as unknown as ReturnType<typeof useFotosVeiculo>;
}

function filaCom(itens: ItemFilaFoto[]): FilaFotosVeiculo {
  return {
    itens,
    adicionar: vi.fn(() => []),
    tentarDeNovo: vi.fn(),
    remover: vi.fn(),
    descartarGravada: vi.fn(),
  };
}

interface Cenario {
  idVeiculo: number | null;
  consulta?: ReturnType<typeof useFotosVeiculo>;
  itensFila?: ItemFilaFoto[];
}

function renderizar({ idVeiculo, consulta = consultaCom({ data: [] }), itensFila = [] }: Cenario): string {
  consultaMock.mockReturnValue(consulta);
  filaMock.mockReturnValue(filaCom(itensFila));
  remocaoMock.mockReturnValue({
    mutate: vi.fn(),
    reset: vi.fn(),
    isPending: false,
    isError: false,
  } as unknown as ReturnType<typeof useRemoverFotoVeiculo>);

  return renderToStaticMarkup(
    <AvisoProvider>
      <BlocoFotosVeiculo idVeiculo={idVeiculo} descricaoVeiculo="Jeep Renegade" />
    </AvisoProvider>,
  );
}

/** Zona de envio habilitada e desabilitada, pela marcação do botão nativo. */
const ZONA_HABILITADA = /<button type="button"(?![^>]*disabled)[^>]*aria-label="Carregar fotos do veículo"/;
const ZONA_DESABILITADA = /<button type="button" disabled=""[^>]*aria-label="Carregar fotos do veículo"/;

function contagem(texto: string, trecho: string): number {
  return texto.split(trecho).length - 1;
}

describe('BlocoFotosVeiculo: cadastro (sem id do veículo)', () => {
  beforeEach(() => {
    consultaMock.mockReset();
    filaMock.mockReset();
    remocaoMock.mockReset();
  });

  it('mostra o bloco do desenho 05 com o contador zerado, a zona de envio e o botão de adicionar', () => {
    const html = renderizar({ idVeiculo: null, consulta: consultaCom({ data: undefined, isPending: true, isSuccess: false }) });

    expect(html).toContain('Fotos do Veículo');
    expect(html).toContain('0/15 Fotos');
    expect(html).toContain('Arraste fotos aqui');
    expect(html).toContain('ou clique para selecionar do computador');
    expect(html).toContain('JPG ou PNG até 10MB por arquivo');
    expect(html).toContain('Nenhuma foto adicionada.');
    expect(html).toContain('Fotos Selecionadas');
    expect(html).toContain('+ Adicionar Mais Fotos');
    expect(html).toContain('aria-label="Carregar fotos do veículo"');
    expect(html).toMatch(ZONA_HABILITADA);
    expect(html).not.toContain('Carregando...');
  });

  it('o seletor de arquivos aceita só PNG e JPEG, em seleção múltipla', () => {
    const html = renderizar({ idVeiculo: null });

    expect(html).toContain('accept="image/png, image/jpeg"');
    expect(html).toContain('multiple=""');
  });

  it('fotos da fila sem veículo aparecem com "Aguardando envio" e sem o botão de remover gravada', () => {
    const html = renderizar({ idVeiculo: null, itensFila: [item(1, 'aguardando')] });

    expect(html).toContain('1/15 Fotos');
    expect(html).toContain('Aguardando envio');
    expect(html).toContain('aria-label="Remover imagem 1"');
  });
});

describe('BlocoFotosVeiculo: edição (com id do veículo)', () => {
  beforeEach(() => {
    consultaMock.mockReset();
    filaMock.mockReset();
    remocaoMock.mockReset();
  });

  it('lista as fotos gravadas na ordem de exibição, com o selo "Foto Principal" só na primeira', () => {
    const html = renderizar({
      idVeiculo: 7,
      consulta: consultaCom({ data: [foto(30, 3), foto(10, 1), foto(20, 2)] }),
    });

    expect(html.indexOf('https://a/10')).toBeLessThan(html.indexOf('https://a/20'));
    expect(html.indexOf('https://a/20')).toBeLessThan(html.indexOf('https://a/30'));
    expect(contagem(html, 'Foto Principal')).toBe(1);
    expect(html).toContain('3/15 Fotos');
    expect(html).toContain('aria-label="Remover imagem 1"');
    expect(html).toContain('aria-label="Remover imagem 3"');
  });

  it('foto gravada não mostra legenda nem estado: só a imagem e o botão de remover', () => {
    const html = renderizar({ idVeiculo: 7, consulta: consultaCom({ data: [foto(10, 1)] }) });

    expect(html).not.toContain('Aguardando envio');
    expect(html).not.toContain('Enviando');
    expect(html).not.toContain('Tentar de novo');
  });

  it('segue o desenho 05 no tom do selo, do ícone, do título e do zoom da foto no hover', () => {
    const html = renderizar({ idVeiculo: 7, consulta: consultaCom({ data: [foto(10, 1)] }) });

    expect(html).toContain('bg-status-pendente');
    expect(html).toContain('text-status-pendente');
    expect(html).not.toContain('bg-orange');
    expect(html).not.toContain('text-orange');
    expect(html).toMatch(/<h2 id="[^"]+" class="text-nome-veiculo[^"]*">Fotos do Veículo<\/h2>/);
    expect(html).toContain('group-hover:scale-105');
    expect(html).toContain('duration-300');
  });

  it('a zona de envio usa o contorno tracejado e o laranja de ação do desenho (zona e apoio)', () => {
    const html = renderizar({ idVeiculo: 7, consulta: consultaCom({ data: [foto(10, 1)] }) });

    expect(html).toContain('outline-contorno-suave');
    expect(html).toContain('hover:outline-orange-acao');
    expect(html).not.toContain('outline-borda-forte');
    expect(html).toMatch(/<p id="[^"]+" class="text-micro font-semibold uppercase tracking-wider text-contorno">Fotos Selecionadas<\/p>/);
    expect(html).toMatch(/<button[^>]*class="[^"]*text-texto-desenho[^"]*"[^>]*>(?:(?!<\/button>)[\s\S])*\+ Adicionar Mais Fotos<\/button>/);
  });

  it('o degradê é azul-marinho profundo: /80 na foto principal e /70 nas demais, como no desenho', () => {
    const html = renderizar({ idVeiculo: 7, consulta: consultaCom({ data: [foto(10, 1), foto(11, 2)] }) });

    expect(html).toContain('from-navy-profundo/80');
    expect(html).toContain('from-navy-profundo/70');
    expect(html).not.toContain('from-navy/');
  });

  it('com a lista carregando, mostra "Carregando..." e não libera o envio', () => {
    const html = renderizar({
      idVeiculo: 7,
      consulta: consultaCom({ data: undefined, isPending: true, isSuccess: false }),
    });

    expect(html).toContain('Carregando...');
    expect(html).toMatch(ZONA_DESABILITADA);
    expect(html).not.toContain('/15 Fotos');
  });

  it('com erro ao carregar, mostra a mensagem com "Tentar de novo" e bloqueia o envio', () => {
    const html = renderizar({
      idVeiculo: 7,
      consulta: consultaCom({
        data: undefined,
        isError: true,
        isSuccess: false,
        error: new ErroApi({ status: 500, mensagem: 'Ocorreu um erro inesperado no servidor. Tente novamente em instantes.' }),
      }),
    });

    expect(html).toContain('Não foi possível carregar as fotos do veículo.');
    expect(html).toContain('Tentar de novo');
    expect(html).toMatch(ZONA_DESABILITADA);
    expect(html).not.toContain('Nenhuma foto adicionada.');
  });
});

describe('BlocoFotosVeiculo: limite de 15 fotos', () => {
  beforeEach(() => {
    consultaMock.mockReset();
    filaMock.mockReset();
    remocaoMock.mockReset();
  });

  it('com 15 fotos, a zona e o botão de adicionar ficam desabilitados e a mensagem do limite aparece', () => {
    const html = renderizar({ idVeiculo: 7, consulta: consultaCom({ data: fotos(15) }) });

    expect(html).toContain('15/15 Fotos');
    expect(html).toContain('Limite de 15 fotos atingido. Remova uma foto para adicionar outra.');
    expect(html).toMatch(ZONA_DESABILITADA);
    expect(html).toMatch(/<button[^>]*disabled=""[^>]*>(?:(?!<\/button>)[\s\S])*\+ Adicionar Mais Fotos<\/button>/);
  });

  it('foto já gravada no lote, ainda fora da lista, conta no limite: 14 na lista mais 1 concluída bloqueiam', () => {
    const html = renderizar({
      idVeiculo: 7,
      consulta: consultaCom({ data: fotos(14) }),
      itensFila: [item(1, 'concluida', { foto: foto(15, 15) })],
    });

    expect(html).toContain('15/15 Fotos');
    expect(html).toMatch(ZONA_DESABILITADA);
    expect(html).toMatch(/<button[^>]*disabled=""[^>]*>(?:(?!<\/button>)[\s\S])*\+ Adicionar Mais Fotos<\/button>/);
  });

  it('a fila conta no limite: 14 gravadas com 1 na fila também bloqueiam', () => {
    const html = renderizar({
      idVeiculo: 7,
      consulta: consultaCom({ data: fotos(14) }),
      itensFila: [item(1, 'aguardando')],
    });

    expect(html).toContain('15/15 Fotos');
    expect(html).toMatch(ZONA_DESABILITADA);
  });

  it('com 14 fotos ainda há uma vaga e nenhum aviso de limite', () => {
    const html = renderizar({ idVeiculo: 7, consulta: consultaCom({ data: fotos(14) }) });

    expect(html).toContain('14/15 Fotos');
    expect(html).toMatch(ZONA_HABILITADA);
    expect(html).not.toContain('Limite de 15 fotos atingido');
  });
});

describe('BlocoFotosVeiculo: fila de envio', () => {
  beforeEach(() => {
    consultaMock.mockReset();
    filaMock.mockReset();
    remocaoMock.mockReset();
  });

  it('mostra o progresso da foto em envio, com barra acessível, e o progresso da fila', () => {
    const html = renderizar({
      idVeiculo: 7,
      consulta: consultaCom({ data: fotos(1) }),
      itensFila: [item(1, 'enviando', { percentual: 40 }), item(2, 'aguardando')],
    });

    expect(html).toContain('Enviando… 40%');
    expect(html).toContain('<progress');
    expect(html).toContain('value="40"');
    expect(html).toContain('Enviando foto 1 de 2');
    expect(html).toContain('Aguardando envio');
  });

  it('a foto em envio não mostra o botão de remover; a aguardando e a com erro mostram', () => {
    const html = renderizar({
      idVeiculo: 7,
      consulta: consultaCom({ data: fotos(1) }),
      itensFila: [item(1, 'enviando', { percentual: 10 }), item(2, 'aguardando'), item(3, 'erro', { erro: 'Não foi possível enviar esta foto.' })],
    });

    expect(html).not.toContain('aria-label="Remover imagem 2"');
    expect(html).toContain('aria-label="Remover imagem 3"');
    expect(html).toContain('aria-label="Remover imagem 4"');
  });

  it('foto com erro mostra a mensagem traduzida e "Tentar de novo"', () => {
    const html = renderizar({
      idVeiculo: 7,
      consulta: consultaCom({ data: [] }),
      itensFila: [item(1, 'erro', { erro: 'A foto passa de 10 MB.' })],
    });

    expect(html).toContain('A foto passa de 10 MB.');
    expect(html).toContain('Tentar de novo');
    expect(html).toContain('role="alert"');
  });

  it('foto já gravada no lote (concluída) não aparece duplicada e a barra de progresso some', () => {
    const html = renderizar({
      idVeiculo: 7,
      consulta: consultaCom({ data: fotos(1) }),
      itensFila: [item(1, 'concluida', { foto: foto(1, 1) })],
    });

    expect(contagem(html, '<progress')).toBe(0);
    expect(html).toContain('Foto enviada');
    expect(html).toContain('1/15 Fotos');
  });

  it('foto gravada no lote que a lista ainda não trouxe aparece na grade e conta no total', () => {
    const html = renderizar({
      idVeiculo: 7,
      consulta: consultaCom({ data: [] }),
      itensFila: [item(1, 'concluida', { foto: foto(1, 1) })],
    });

    expect(html).toContain('1/15 Fotos');
    expect(contagem(html, 'src="https://a/1"')).toBe(1);
    expect(html).toContain('aria-label="Remover imagem 1"');
    expect(html).not.toContain('Nenhuma foto adicionada.');
  });

  it('a foto concluída que a lista já trouxe não aparece duplicada', () => {
    const html = renderizar({
      idVeiculo: 7,
      consulta: consultaCom({ data: [foto(1, 1)] }),
      itensFila: [item(1, 'concluida', { foto: foto(1, 1) })],
    });

    expect(contagem(html, 'src="https://a/1"')).toBe(1);
    expect(html).toContain('1/15 Fotos');
  });

  it('no lote, as gravadas da lista e as concluídas ainda fora dela seguem a ordem de exibição', () => {
    const html = renderizar({
      idVeiculo: 7,
      consulta: consultaCom({ data: [foto(1, 1)] }),
      itensFila: [item(2, 'concluida', { foto: foto(2, 2) }), item(3, 'aguardando')],
    });

    expect(html.indexOf('https://a/1')).toBeLessThan(html.indexOf('https://a/2'));
    expect(html).toContain('3/15 Fotos');
    expect(html).toContain('Aguardando envio');
  });

  it('o texto de status mostra o progresso do lote e, sem fila, nada', () => {
    const emEnvio = renderizar({
      idVeiculo: 7,
      consulta: consultaCom({ data: [] }),
      itensFila: [item(1, 'enviando', { percentual: 0 })],
    });
    expect(emEnvio).toContain('Enviando foto 1 de 1');
    expect(emEnvio).toContain('<output');
  });
});

describe('BlocoFotosVeiculo: acessibilidade', () => {
  beforeEach(() => {
    consultaMock.mockReset();
    filaMock.mockReset();
    remocaoMock.mockReset();
  });

  it('o bloco tem título associado e a zona é um botão operável por teclado', () => {
    const html = renderizar({ idVeiculo: 7, consulta: consultaCom({ data: fotos(1) }) });

    expect(html).toMatch(/<section aria-labelledby="[^"]+"/);
    expect(html).toMatch(/<h2 id="[^"]+"[^>]*>Fotos do Veículo<\/h2>/);
    expect(html).toMatch(ZONA_HABILITADA);
  });

  it('o seletor escondido não recebe foco: a zona e o botão de adicionar fazem esse papel', () => {
    const html = renderizar({ idVeiculo: 7, consulta: consultaCom({ data: [] }) });

    expect(html).toMatch(/<input[^>]*aria-hidden="true"[^>]*tabindex="-1"|<input[^>]*tabindex="-1"[^>]*aria-hidden="true"/);
  });

  it('a miniatura da foto tem o texto alternativo com a descrição do veículo', () => {
    const html = renderizar({ idVeiculo: 7, consulta: consultaCom({ data: [foto(1, 1)] }) });

    expect(html).toContain('alt="Foto do veículo Jeep Renegade"');
  });

  it('o botão de remover fica visível no foco e em telas sem hover', () => {
    const html = renderizar({ idVeiculo: 7, consulta: consultaCom({ data: [foto(1, 1)] }) });

    expect(html).toContain('focus-visible:opacity-100');
    expect(html).toContain('[@media(hover:hover)]:opacity-0');
  });
});

describe('BlocoFotosVeiculo: largura e prévia do envio (1280 px com barra de rolagem)', () => {
  beforeEach(() => {
    consultaMock.mockReset();
    filaMock.mockReset();
    remocaoMock.mockReset();
  });

  it('o selo do contador fica numa linha só: sem quebra e sem encolher', () => {
    const html = renderizar({ idVeiculo: 7, consulta: consultaCom({ data: fotos(3) }) });

    expect(html).toMatch(/<span class="[^"]*shrink-0 whitespace-nowrap[^"]*rounded-full[^"]*">3\/15 Fotos<\/span>/);
  });

  it('a foto enviada mostra a prévia local por cima da imagem do servidor até ela carregar', () => {
    const html = renderizar({
      idVeiculo: 7,
      consulta: consultaCom({ data: fotos(1) }),
      itensFila: [item(2, 'concluida', { previa: 'blob:previa-2', foto: foto(1, 1) })],
    });

    expect(html).toContain('src="blob:previa-2"');
    expect(html).toContain('src="https://a/1"');
    expect(html.indexOf('src="https://a/1"')).toBeLessThan(html.indexOf('src="blob:previa-2"'));
  });

  it('a foto gravada sem envio neste lote não tem prévia', () => {
    const html = renderizar({ idVeiculo: 7, consulta: consultaCom({ data: fotos(2) }) });

    expect(html).not.toContain('blob:');
  });
});

/** Foto gravada sem arquivo nesta máquina: sem URL e sem validade; o texto do servidor vem em erroFoto, se houver. */
function fotoSemArquivo(idVeiculoFoto: number, ordem: number, erroFoto?: string): FotoDoVeiculo {
  return { idVeiculoFoto, ordem, url: null, urlMiniatura: null, expiraEm: null, ...(erroFoto === undefined ? {} : { erroFoto }) };
}

const TEXTO_FOTO_SEM_ARQUIVO = 'Não foi possível carregar esta foto. Se o problema continuar, remova a foto e envie de novo.';

describe('BlocoFotosVeiculo: foto gravada sem arquivo (erro no item, sem Tentar de novo)', () => {
  beforeEach(() => {
    consultaMock.mockReset();
    filaMock.mockReset();
    remocaoMock.mockReset();
  });

  it('lista com a do meio sem arquivo: três itens na ordem; o do meio sem imagem, com o texto do servidor e com remover', () => {
    const html = renderizar({
      idVeiculo: 7,
      consulta: consultaCom({ data: [foto(1, 1), fotoSemArquivo(2, 2, TEXTO_FOTO_SEM_ARQUIVO), foto(3, 3)] }),
    });

    expect(contagem(html, '<img')).toBe(2);
    expect(html.indexOf('src="https://a/1"')).toBeLessThan(html.indexOf(TEXTO_FOTO_SEM_ARQUIVO));
    expect(html.indexOf(TEXTO_FOTO_SEM_ARQUIVO)).toBeLessThan(html.indexOf('src="https://a/3"'));
    expect(html).toContain('aria-label="Remover imagem 2"');
    expect(html).toContain('aria-label="Remover imagem 3"');
    expect(html).toContain('3/15 Fotos');
  });

  it('o item sem arquivo não mostra "Tentar de novo" e o erro de lista não aparece; a zona de envio segue liberada', () => {
    const html = renderizar({
      idVeiculo: 7,
      consulta: consultaCom({ data: [foto(1, 1), fotoSemArquivo(2, 2, TEXTO_FOTO_SEM_ARQUIVO), foto(3, 3)] }),
    });

    expect(html).not.toContain('Tentar de novo');
    expect(html).not.toContain('Não foi possível carregar as fotos do veículo.');
    expect(html).toMatch(ZONA_HABILITADA);
  });

  it('primeira foto sem arquivo: continua sendo a principal, com o selo "Foto Principal"', () => {
    const html = renderizar({
      idVeiculo: 7,
      consulta: consultaCom({ data: [fotoSemArquivo(1, 1, TEXTO_FOTO_SEM_ARQUIVO), foto(2, 2)] }),
    });

    expect(contagem(html, 'Foto Principal')).toBe(1);
    expect(html.indexOf('Foto Principal')).toBeLessThan(html.indexOf(TEXTO_FOTO_SEM_ARQUIVO));
    expect(html).toContain('aria-label="Remover imagem 1"');
  });

  it('15 fotos com uma sem arquivo: contador 15/15, zona e botão de adicionar desabilitados', () => {
    const html = renderizar({
      idVeiculo: 7,
      consulta: consultaCom({ data: [...fotos(14), fotoSemArquivo(15, 15, TEXTO_FOTO_SEM_ARQUIVO)] }),
    });

    expect(html).toContain('15/15 Fotos');
    expect(html).toMatch(ZONA_DESABILITADA);
    expect(html).toMatch(/<button[^>]*disabled=""[^>]*>(?:(?!<\/button>)[\s\S])*\+ Adicionar Mais Fotos<\/button>/);
  });

  it('foto sem arquivo e sem erroFoto: mostra o texto de lista que já existe, no item', () => {
    const html = renderizar({
      idVeiculo: 7,
      consulta: consultaCom({ data: [fotoSemArquivo(1, 1)] }),
    });

    expect(contagem(html, 'Não foi possível carregar as fotos do veículo.')).toBe(1);
    expect(html).not.toContain('<img');
    expect(html).not.toContain('Tentar de novo');
  });
});

/** Tag de abertura do botão de remover de uma posição (o rótulo acessível identifica o botão). */
function tagDoBotaoRemover(html: string, posicao: number): string {
  const achado = new RegExp(`<button\\b[^>]*aria-label="Remover imagem ${posicao}"[^>]*>`).exec(html);
  if (achado === null) throw new Error(`botão de remover ${posicao} ausente`);
  return achado[0];
}

describe('BlocoFotosVeiculo: botão de remover por tipo de foto (visibilidade)', () => {
  beforeEach(() => {
    consultaMock.mockReset();
    filaMock.mockReset();
    remocaoMock.mockReset();
  });

  it('a foto gravada sem arquivo tem o botão de remover sempre visível, sem as classes que o escondem', () => {
    const html = renderizar({
      idVeiculo: 7,
      consulta: consultaCom({ data: [foto(1, 1), fotoSemArquivo(2, 2, TEXTO_FOTO_SEM_ARQUIVO), foto(3, 3)] }),
    });

    expect(tagDoBotaoRemover(html, 2)).not.toContain('opacity-0');
    expect(tagDoBotaoRemover(html, 2)).not.toContain('group-hover:opacity-100');
  });

  it('a foto gravada sem arquivo sem erroFoto (texto de lista) também tem o botão sempre visível', () => {
    const html = renderizar({ idVeiculo: 7, consulta: consultaCom({ data: [fotoSemArquivo(1, 1)] }) });

    expect(tagDoBotaoRemover(html, 1)).not.toContain('opacity-0');
    expect(tagDoBotaoRemover(html, 1)).not.toContain('group-hover:opacity-100');
  });

  it('as fotos gravadas com arquivo mantêm as classes que escondem o botão fora do hover', () => {
    const html = renderizar({
      idVeiculo: 7,
      consulta: consultaCom({ data: [foto(1, 1), fotoSemArquivo(2, 2, TEXTO_FOTO_SEM_ARQUIVO), foto(3, 3)] }),
    });

    for (const posicao of [1, 3]) {
      expect(tagDoBotaoRemover(html, posicao)).toContain('[@media(hover:hover)]:opacity-0');
      expect(tagDoBotaoRemover(html, posicao)).toContain('[@media(hover:hover)]:group-hover:opacity-100');
    }
  });

  it('o item em erro de envio da fila mantém as classes que escondem o botão', () => {
    const html = renderizar({
      idVeiculo: 7,
      consulta: consultaCom({ data: [] }),
      itensFila: [item(1, 'erro', { erro: 'A foto passa de 10 MB.' })],
    });

    expect(tagDoBotaoRemover(html, 1)).toContain('[@media(hover:hover)]:opacity-0');
    expect(tagDoBotaoRemover(html, 1)).toContain('[@media(hover:hover)]:group-hover:opacity-100');
  });
});
