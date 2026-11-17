import { describe, expect, it } from 'vitest';
import { ErroApi } from '@/types/api';
import type { FotoDoVeiculo, ItemFilaFoto, SituacaoItemFila } from '@/types/fotosVeiculo';
import {
  fotosParaExibir,
  formatarContadorFotos,
  LADO_MAXIMO_FOTO_PIXELS,
  MAXIMO_FOTOS_POR_VEICULO,
  mensagemErroEnvioFoto,
  mensagemRecusaFoto,
  ordenarFotos,
  primeiraFoto,
  primeiraMensagemErroFotoCapa,
  proximoDaFila,
  resumoFila,
  TAMANHO_MAXIMO_FOTO_BYTES,
  TOTAL_MAXIMO_PIXELS_FOTO,
  urlDaImagem,
  urlImagemExibida,
  validarArquivoFoto,
  validarDimensoesFoto,
  veiculoMudouDesdeAEscolha,
  vagasRestantes,
} from './fotosVeiculo';
import {
  MENSAGEM_FOTOS_ERRO_ARMAZENAMENTO,
  MENSAGEM_FOTOS_ERRO_DIMENSAO,
  MENSAGEM_FOTOS_ERRO_ENVIO,
  MENSAGEM_FOTOS_ERRO_TAMANHO,
  MENSAGEM_FOTOS_ERRO_TIPO,
} from './mensagensFotosVeiculo';

function foto(idVeiculoFoto: number, ordem: number): FotoDoVeiculo {
  return {
    idVeiculoFoto,
    ordem,
    url: `https://armazenamento.exemplo/${idVeiculoFoto}`,
    urlMiniatura: null,
    expiraEm: '2026-11-16T13:15:00.0000000+00:00',
  };
}

function item(chave: number, situacao: SituacaoItemFila): ItemFilaFoto {
  return { chave, nome: `foto-${chave}.jpg`, previa: `blob:${chave}`, situacao, percentual: 0, erro: null, foto: null };
}

describe('validarArquivoFoto', () => {
  it('aceita JPEG e PNG', () => {
    expect(validarArquivoFoto({ type: 'image/jpeg', size: 1000 })).toBeNull();
    expect(validarArquivoFoto({ type: 'image/png', size: 1000 })).toBeNull();
  });

  it('recusa outro tipo, como GIF ou PDF', () => {
    expect(validarArquivoFoto({ type: 'image/gif', size: 1000 })).toBe('tipo');
    expect(validarArquivoFoto({ type: 'application/pdf', size: 1000 })).toBe('tipo');
  });

  it('aceita até 10 MiB e recusa a partir de 1 byte acima', () => {
    expect(validarArquivoFoto({ type: 'image/png', size: TAMANHO_MAXIMO_FOTO_BYTES })).toBeNull();
    expect(validarArquivoFoto({ type: 'image/png', size: TAMANHO_MAXIMO_FOTO_BYTES + 1 })).toBe('tamanho');
  });
});

describe('validarDimensoesFoto', () => {
  it('aceita imagens comuns', () => {
    expect(validarDimensoesFoto(4000, 3000)).toBeNull();
  });

  it('aceita o lado máximo e o total máximo exatos', () => {
    expect(validarDimensoesFoto(LADO_MAXIMO_FOTO_PIXELS, 2048)).toBeNull();
    expect(validarDimensoesFoto(4096, 4096)).toBeNull();
  });

  it('recusa lado acima de 8192 px, mesmo com pouca área', () => {
    expect(validarDimensoesFoto(LADO_MAXIMO_FOTO_PIXELS + 1, 100)).toBe('dimensao');
  });

  it('recusa total de pixels acima de 16.777.216', () => {
    expect(validarDimensoesFoto(5000, 4000)).toBe('dimensao');
    expect(validarDimensoesFoto(4096, 4097)).toBe('dimensao');
    expect(TOTAL_MAXIMO_PIXELS_FOTO).toBe(16_777_216);
  });
});

describe('vagasRestantes', () => {
  it('conta o teto menos as gravadas e as da fila', () => {
    expect(vagasRestantes(0, 0)).toBe(MAXIMO_FOTOS_POR_VEICULO);
    expect(vagasRestantes(10, 4)).toBe(1);
    expect(vagasRestantes(15, 0)).toBe(0);
  });

  it('nunca fica negativo', () => {
    expect(vagasRestantes(10, 6)).toBe(0);
    expect(vagasRestantes(16, 0)).toBe(0);
  });
});

describe('veiculoMudouDesdeAEscolha', () => {
  it('no cadastro (sem id ao escolher) o id que chega depois não descarta as fotos', () => {
    expect(veiculoMudouDesdeAEscolha(null, null)).toBe(false);
    expect(veiculoMudouDesdeAEscolha(null, 15)).toBe(false);
  });

  it('com o mesmo veículo, não descarta', () => {
    expect(veiculoMudouDesdeAEscolha(7, 7)).toBe(false);
  });

  it('com outro id, ou com o veículo sumido depois da escolha, descarta', () => {
    expect(veiculoMudouDesdeAEscolha(7, 8)).toBe(true);
    expect(veiculoMudouDesdeAEscolha(7, null)).toBe(true);
  });
});

describe('formatarContadorFotos', () => {
  it('mostra o total sobre o teto', () => {
    expect(formatarContadorFotos(4)).toBe('4/15 Fotos');
    expect(formatarContadorFotos(0)).toBe('0/15 Fotos');
  });
});

describe('ordenarFotos e primeiraFoto', () => {
  it('ordena por ordem e, no empate, pelo id, sem alterar a lista recebida', () => {
    const entrada = [foto(9, 2), foto(4, 1), foto(3, 2)];
    const ordenadas = ordenarFotos(entrada);

    expect(ordenadas.map((f) => f.idVeiculoFoto)).toEqual([4, 3, 9]);
    expect(entrada.map((f) => f.idVeiculoFoto)).toEqual([9, 4, 3]);
  });

  it('a capa é a primeira na ordem; sem foto, nula', () => {
    expect(primeiraFoto([foto(9, 3), foto(4, 1)])?.idVeiculoFoto).toBe(4);
    expect(primeiraFoto([])).toBeNull();
  });
});

describe('fotosParaExibir', () => {
  function itemConcluido(chave: number, fotoGravada: FotoDoVeiculo): ItemFilaFoto {
    return { chave, nome: `f${chave}.png`, previa: `blob:${chave}`, situacao: 'concluida', percentual: 100, erro: null, foto: fotoGravada };
  }

  function itemAguardando(chave: number): ItemFilaFoto {
    return { chave, nome: `f${chave}.png`, previa: `blob:${chave}`, situacao: 'aguardando', percentual: 0, erro: null, foto: null };
  }

  it('sem lote, mostra só as gravadas da lista, em ordem', () => {
    const exibidas = fotosParaExibir([foto(3, 2), foto(1, 1)], []);

    expect(exibidas.map((f) => f.idVeiculoFoto)).toEqual([1, 3]);
  });

  it('a foto concluída que a lista ainda não trouxe entra na exibição, na ordem certa', () => {
    const exibidas = fotosParaExibir([foto(1, 1)], [itemConcluido(1, foto(2, 2)), itemAguardando(2)]);

    expect(exibidas.map((f) => f.idVeiculoFoto)).toEqual([1, 2]);
  });

  it('a foto concluída que a lista já trouxe não aparece duas vezes', () => {
    const exibidas = fotosParaExibir([foto(1, 1), foto(2, 2)], [itemConcluido(1, foto(2, 2))]);

    expect(exibidas.map((f) => f.idVeiculoFoto)).toEqual([1, 2]);
  });

  it('item em envio ou com erro não entra: só o concluído tem foto gravada', () => {
    const exibidas = fotosParaExibir([], [
      { ...itemAguardando(1), situacao: 'enviando', percentual: 30 },
      { ...itemAguardando(2), situacao: 'erro', erro: 'falhou' },
    ]);

    expect(exibidas).toEqual([]);
  });

  it('não altera as listas recebidas', () => {
    const gravadas = [foto(2, 2)];
    const itens = [itemConcluido(1, foto(1, 1))];

    fotosParaExibir(gravadas, itens);

    expect(gravadas.map((f) => f.idVeiculoFoto)).toEqual([2]);
    expect(itens).toHaveLength(1);
  });
});

describe('urlDaImagem e urlImagemExibida', () => {
  it('usa a miniatura quando existe e a imagem comum quando não', () => {
    expect(urlDaImagem({ url: 'https://a/grande', urlMiniatura: null })).toBe('https://a/grande');
    expect(urlDaImagem({ url: 'https://a/grande', urlMiniatura: 'https://a/mini' })).toBe('https://a/mini');
  });

  it('sem URL, a imagem não tem o que mostrar', () => {
    expect(urlImagemExibida(null, null)).toBeNull();
    expect(urlImagemExibida(undefined, null)).toBeNull();
    expect(urlImagemExibida('', null)).toBeNull();
  });

  it('a URL que já falhou (ex.: vencida) cai no ícone; uma URL nova volta a ser usada', () => {
    expect(urlImagemExibida('https://a/1', null)).toBe('https://a/1');
    expect(urlImagemExibida('https://a/1', 'https://a/1')).toBeNull();
    expect(urlImagemExibida('https://a/2', 'https://a/1')).toBe('https://a/2');
  });
});

describe('proximoDaFila', () => {
  it('devolve o primeiro aguardando quando nada está enviando', () => {
    const itens = [item(1, 'concluida'), item(2, 'erro'), item(3, 'aguardando'), item(4, 'aguardando')];
    expect(proximoDaFila(itens)?.chave).toBe(3);
  });

  it('não devolve nada enquanto uma foto está enviando, nem quando não há aguardando', () => {
    expect(proximoDaFila([item(1, 'enviando'), item(2, 'aguardando')])).toBeNull();
    expect(proximoDaFila([item(1, 'erro'), item(2, 'concluida')])).toBeNull();
    expect(proximoDaFila([])).toBeNull();
  });
});

describe('resumoFila', () => {
  it('conta o lote em andamento: gravadas, em envio e aguardando; erro fica fora do total do lote', () => {
    const resumo = resumoFila([item(1, 'concluida'), item(2, 'enviando'), item(3, 'aguardando'), item(4, 'erro')]);

    expect(resumo).toEqual({ emEnvio: true, atual: 2, total: 3, naFila: 3, concluidas: 1 });
  });

  it('sem fila, não há envio e o lote está vazio', () => {
    expect(resumoFila([])).toEqual({ emEnvio: false, atual: 1, total: 0, naFila: 0, concluidas: 0 });
  });
});

describe('mensagemRecusaFoto', () => {
  it('traduz cada motivo para o texto do desenho ou da proposta', () => {
    expect(mensagemRecusaFoto('tipo')).toBe(MENSAGEM_FOTOS_ERRO_TIPO);
    expect(mensagemRecusaFoto('tamanho')).toBe(MENSAGEM_FOTOS_ERRO_TAMANHO);
    expect(mensagemRecusaFoto('dimensao')).toBe(MENSAGEM_FOTOS_ERRO_DIMENSAO);
  });
});

describe('mensagemErroEnvioFoto', () => {
  it('erro que não veio da API vira o texto de envio', () => {
    expect(mensagemErroEnvioFoto(new Error('falha inesperada'))).toBe(MENSAGEM_FOTOS_ERRO_ENVIO);
    expect(mensagemErroEnvioFoto(undefined)).toBe(MENSAGEM_FOTOS_ERRO_ENVIO);
  });

  it('502 (armazenamento indisponível) tem texto próprio', () => {
    const erro = new ErroApi({ status: 502, mensagem: 'Mensagem do servidor.' });
    expect(mensagemErroEnvioFoto(erro)).toBe(MENSAGEM_FOTOS_ERRO_ARMAZENAMENTO);
  });

  it('400, 404, 409, 429, falha de rede e tempo esgotado usam a mensagem que o cliente HTTP já filtrou', () => {
    for (const status of [400, 404, 409, 429, 0]) {
      const erro = new ErroApi({ status, mensagem: `Mensagem do status ${status}.` });
      expect(mensagemErroEnvioFoto(erro)).toBe(`Mensagem do status ${status}.`);
    }
  });
});

describe('primeiraMensagemErroFotoCapa', () => {
  it('sem nenhum erro de capa, devolve nulo (lista vazia, respostas sem o campo e erro nulo)', () => {
    expect(primeiraMensagemErroFotoCapa([])).toBeNull();
    expect(primeiraMensagemErroFotoCapa([{}, { erroFotoCapa: null }])).toBeNull();
  });

  it('ignora texto vazio ou só com espaços', () => {
    expect(primeiraMensagemErroFotoCapa([{ erroFotoCapa: '' }, { erroFotoCapa: '   ' }])).toBeNull();
    expect(primeiraMensagemErroFotoCapa([{ erroFotoCapa: '  ' }, { erroFotoCapa: 'Falhou.' }])).toBe('Falhou.');
  });

  it('devolve a primeira mensagem, na ordem da lista, como o servidor mandou', () => {
    expect(
      primeiraMensagemErroFotoCapa([{}, { erroFotoCapa: 'Primeira.' }, { erroFotoCapa: 'Segunda.' }]),
    ).toBe('Primeira.');
  });
});
