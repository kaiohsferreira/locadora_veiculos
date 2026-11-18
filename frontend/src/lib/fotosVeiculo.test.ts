import { describe, expect, it } from 'vitest';
import { ErroApi } from '@/types/api';
import type { FotoCapaVeiculo, FotoDoVeiculo, ItemFilaFoto, SituacaoItemFila } from '@/types/fotosVeiculo';
import {
  aoFalharFotoCapa,
  arquivoParaEnviar,
  criarControleNovaConsultaFoto,
  erroDaFotoGravada,
  erroRecusouArquivo,
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
  proximoParaPreparar,
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
  MENSAGEM_FOTOS_ERRO_CARREGAR,
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
    expect(formatarContadorFotos(15)).toBe('15/15 Fotos');
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

describe('urlDaImagem com url nula (foto gravada sem arquivo)', () => {
  it('url e miniatura nulas: nulo, sem imagem', () => {
    expect(urlDaImagem({ url: null, urlMiniatura: null })).toBeNull();
  });

  it('aceita a capa (url texto) e a foto gravada (url nula ou texto) sem conversão', () => {
    const capa: FotoCapaVeiculo = { url: 'https://a/capa', urlMiniatura: null, expiraEm: '2026-11-16T21:15:00.0000000+00:00' };
    const gravada: FotoDoVeiculo = foto(5, 1);
    const semArquivo: FotoDoVeiculo = { ...foto(6, 2), url: null, expiraEm: null, erroFoto: 'Texto do servidor.' };

    expect(urlDaImagem(capa)).toBe('https://a/capa');
    expect(urlDaImagem(gravada)).toBe('https://armazenamento.exemplo/5');
    expect(urlDaImagem(semArquivo)).toBeNull();
  });
});

describe('erroDaFotoGravada', () => {
  const TEXTO_SERVIDOR = 'Texto que o servidor mandou.';

  it('foto com url preenchida: nulo, mesmo que o servidor mande erroFoto', () => {
    expect(erroDaFotoGravada({ url: 'https://a/1', erroFoto: null })).toBeNull();
    expect(erroDaFotoGravada({ url: 'https://a/1', erroFoto: TEXTO_SERVIDOR })).toBeNull();
  });

  it('foto sem url com erroFoto: o texto do servidor', () => {
    expect(erroDaFotoGravada({ url: null, erroFoto: TEXTO_SERVIDOR })).toBe(TEXTO_SERVIDOR);
  });

  it('o texto do servidor sai sem espaços nas pontas', () => {
    expect(erroDaFotoGravada({ url: null, erroFoto: `   ${TEXTO_SERVIDOR}  ` })).toBe(TEXTO_SERVIDOR);
  });

  it('foto sem url e erroFoto nulo, ausente ou só com espaços: o texto de lista que já existe', () => {
    expect(erroDaFotoGravada({ url: null, erroFoto: null })).toBe(MENSAGEM_FOTOS_ERRO_CARREGAR);
    expect(erroDaFotoGravada({ url: null })).toBe(MENSAGEM_FOTOS_ERRO_CARREGAR);
    expect(erroDaFotoGravada({ url: null, erroFoto: '   ' })).toBe(MENSAGEM_FOTOS_ERRO_CARREGAR);
  });

  it('url vazia conta como sem url', () => {
    expect(erroDaFotoGravada({ url: '', erroFoto: TEXTO_SERVIDOR })).toBe(TEXTO_SERVIDOR);
    expect(erroDaFotoGravada({ url: '' })).toBe(MENSAGEM_FOTOS_ERRO_CARREGAR);
  });

  it('aceita a foto do tipo FotoDoVeiculo, com erroFoto opcional', () => {
    const semArquivo: FotoDoVeiculo = { ...foto(7, 1), url: null, expiraEm: null, erroFoto: TEXTO_SERVIDOR };
    const semCampo: FotoDoVeiculo = { ...foto(8, 2), url: null, expiraEm: null };

    expect(erroDaFotoGravada(semArquivo)).toBe(TEXTO_SERVIDOR);
    expect(erroDaFotoGravada(semCampo)).toBe(MENSAGEM_FOTOS_ERRO_CARREGAR);
  });
});

describe('criarControleNovaConsultaFoto', () => {
  it('a primeira falha pede nova consulta; a segunda não', () => {
    const controle = criarControleNovaConsultaFoto();

    expect(controle.registrarFalha()).toBe(true);
    expect(controle.registrarFalha()).toBe(false);
  });

  it('várias imagens que falham juntas geram uma consulta só', () => {
    const controle = criarControleNovaConsultaFoto();
    const pedidos = [1, 2, 3, 4, 5].map(() => controle.registrarFalha());

    expect(pedidos.filter(Boolean)).toHaveLength(1);
    expect(pedidos[0]).toBe(true);
  });

  it('depois da consulta, URL nova volta a ser usada; se falhar de novo, não pede outra consulta (fica o ícone)', () => {
    const controle = criarControleNovaConsultaFoto();
    const urlVencida = 'https://armazenamento.exemplo/fotos/1?expirou';
    const urlNova = 'https://armazenamento.exemplo/fotos/1?nova';

    expect(controle.registrarFalha()).toBe(true);
    expect(urlImagemExibida(urlVencida, urlVencida)).toBeNull();
    expect(urlImagemExibida(urlNova, urlVencida)).toBe(urlNova);
    expect(controle.registrarFalha()).toBe(false);
    expect(urlImagemExibida(urlNova, urlNova)).toBeNull();
  });

  it('controles diferentes (visitas diferentes) têm contagem própria', () => {
    expect(criarControleNovaConsultaFoto().registrarFalha()).toBe(true);
    expect(criarControleNovaConsultaFoto().registrarFalha()).toBe(true);
  });
});

describe('aoFalharFotoCapa', () => {
  it('duas falhas chamam recarregar uma vez só; a terceira não chama', () => {
    const controle = criarControleNovaConsultaFoto();
    let chamadas = 0;
    const recarregar = (): void => {
      chamadas += 1;
    };

    aoFalharFotoCapa(controle, recarregar);
    aoFalharFotoCapa(controle, recarregar);
    expect(chamadas).toBe(1);

    aoFalharFotoCapa(controle, recarregar);
    expect(chamadas).toBe(1);
  });

  it('a consulta refeita traz URL nova; se essa URL também falhar, não há nova consulta (sem laço)', () => {
    const controle = criarControleNovaConsultaFoto();
    let chamadas = 0;
    const recarregar = (): void => {
      chamadas += 1;
    };
    const urlVencida = 'https://armazenamento.exemplo/fotos/1?expirou';
    const urlNova = 'https://armazenamento.exemplo/fotos/1?nova';

    aoFalharFotoCapa(controle, recarregar);
    expect(chamadas).toBe(1);
    expect(urlImagemExibida(urlNova, urlVencida)).toBe(urlNova);

    aoFalharFotoCapa(controle, recarregar);
    expect(chamadas).toBe(1);
    expect(urlImagemExibida(urlNova, urlNova)).toBeNull();
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

describe('arquivoParaEnviar', () => {
  const original = new File(['original'], 'a.png', { type: 'image/png' });
  const reduzido = new File(['reduzido'], 'a.jpg', { type: 'image/jpeg' });

  it('sem reduzido: o original', () => {
    expect(arquivoParaEnviar({ original, preparado: null, reenviarOriginal: false })).toBe(original);
  });

  it('com reduzido: o reduzido', () => {
    expect(arquivoParaEnviar({ original, preparado: reduzido, reenviarOriginal: false })).toBe(reduzido);
  });

  it('com reduzido e reenviarOriginal: o original', () => {
    expect(arquivoParaEnviar({ original, preparado: reduzido, reenviarOriginal: true })).toBe(original);
  });
});

describe('proximoParaPreparar (antecipação de uma foto)', () => {
  const semPreparo = new Set<number>();

  it('fila vazia: nulo', () => {
    expect(proximoParaPreparar([], semPreparo, null)).toBeNull();
  });

  it('preparação em curso: nulo, mesmo com candidato', () => {
    expect(proximoParaPreparar([item(1, 'aguardando')], semPreparo, 3)).toBeNull();
  });

  it('primeiro aguardando não preparado, sem foto enviando: a chave dele', () => {
    expect(proximoParaPreparar([item(1, 'aguardando'), item(2, 'aguardando')], semPreparo, null)).toBe(1);
  });

  it('primeiro aguardando não preparado, com foto enviando antes dele: a chave dele', () => {
    expect(proximoParaPreparar([item(1, 'enviando'), item(2, 'aguardando')], semPreparo, null)).toBe(2);
  });

  it('primeiro aguardando já preparado: nulo, mesmo com o segundo não preparado', () => {
    expect(proximoParaPreparar([item(1, 'aguardando'), item(2, 'aguardando')], new Set([1]), null)).toBeNull();
  });

  it('erro e concluída não contam como candidato', () => {
    expect(proximoParaPreparar([item(1, 'concluida'), item(2, 'erro'), item(3, 'aguardando')], semPreparo, null)).toBe(3);
    expect(proximoParaPreparar([item(1, 'erro'), item(2, 'concluida')], semPreparo, null)).toBeNull();
  });

  it('item que voltou de erro para aguardando e é o primeiro da ordem: a chave dele', () => {
    const itens = [item(1, 'aguardando'), item(2, 'aguardando')];
    expect(proximoParaPreparar(itens, semPreparo, null)).toBe(1);
    expect(proximoParaPreparar(itens, new Set([1]), null)).toBeNull();
  });
});

describe('erroRecusouArquivo', () => {
  it('só o 400 do servidor (o arquivo foi recusado) é verdadeiro', () => {
    expect(erroRecusouArquivo(new ErroApi({ status: 400, mensagem: 'Arquivo recusado.' }))).toBe(true);
  });

  it('409, 429, 500, 502, erro comum e cancelamento são falsos', () => {
    for (const status of [409, 429, 500, 502]) {
      expect(erroRecusouArquivo(new ErroApi({ status, mensagem: 'Erro.' }))).toBe(false);
    }
    expect(erroRecusouArquivo(new TypeError('Failed to fetch'))).toBe(false);
    expect(erroRecusouArquivo(new DOMException('cancelado', 'AbortError'))).toBe(false);
  });
});
