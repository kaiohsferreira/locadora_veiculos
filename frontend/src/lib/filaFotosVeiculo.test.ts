import { describe, expect, it, vi } from 'vitest';
import { ErroApi } from '@/types/api';
import type { FotoDoVeiculo } from '@/types/fotosVeiculo';
import type { CabecalhoFoto } from './cabecalhoFoto';
import { erroRecusouArquivo } from './fotosVeiculo';
import { FilaEnvioFotos, type ArquivoParaFila, type OpcoesFilaEnvioFotos } from './filaFotosVeiculo';
import type { FotoPreparada } from './reducaoFotoVeiculo';

interface ChamadaEnvio {
  idVeiculo: number;
  arquivo: File;
  aoProgredir: (percentual: number) => void;
  sinal: AbortSignal;
  resolver: (foto: FotoDoVeiculo) => void;
  rejeitar: (erro: unknown) => void;
}

function foto(idVeiculoFoto: number): FotoDoVeiculo {
  return {
    idVeiculoFoto,
    ordem: idVeiculoFoto,
    url: `https://armazenamento.exemplo/${idVeiculoFoto}`,
    urlMiniatura: null,
    expiraEm: '2026-11-16T13:15:00.0000000+00:00',
  };
}

function arquivo(nome: string): File {
  return new File(['conteudo'], nome, { type: 'image/png' });
}

function paraFila(...nomes: string[]): ArquivoParaFila[] {
  return nomes.map((nome) => ({ arquivo: arquivo(nome), previa: `blob:${nome}`, cabecalho: null }));
}

/** Preparação que devolve a própria foto (não reduzida): é o padrão dos testes anteriores à redução. */
function preparacaoDireta(arquivoOriginal: File): Promise<FotoPreparada> {
  return Promise.resolve({ arquivo: arquivoOriginal, reduzida: false });
}

/** Preparações controladas à mão: cada chamada fica pendente até o teste resolver ou rejeitar. */
function criarPreparacaoControlada() {
  const pendentes: Array<{
    arquivo: File;
    cabecalho: CabecalhoFoto | null;
    resolver: (foto: FotoPreparada) => void;
    rejeitar: (erro: unknown) => void;
  }> = [];
  const preparar = vi.fn(
    (arquivoOriginal: File, cabecalho: CabecalhoFoto | null) =>
      new Promise<FotoPreparada>((resolver, rejeitar) => {
        pendentes.push({ arquivo: arquivoOriginal, cabecalho, resolver, rejeitar });
      }),
  );
  return { preparar, pendentes };
}

/** Foto reduzida de teste: JPEG com o nome do original trocado. */
function reduzida(nome: string): File {
  return new File(['reduzido'], nome, { type: 'image/jpeg' });
}

/** Envios controlados à mão: cada chamada fica pendente até o teste resolver ou rejeitar. */
function criarEnvioControlado() {
  const chamadas: ChamadaEnvio[] = [];
  const enviar = vi.fn(
    (idVeiculo: number, arquivoEnviado: File, aoProgredir: (percentual: number) => void, sinal: AbortSignal) =>
      new Promise<FotoDoVeiculo>((resolver, rejeitar) => {
        chamadas.push({ idVeiculo, arquivo: arquivoEnviado, aoProgredir, sinal, resolver, rejeitar });
      }),
  );
  return { enviar, chamadas };
}

function criarOpcoes(
  enviar: OpcoesFilaEnvioFotos['enviar'],
  preparar: OpcoesFilaEnvioFotos['preparar'] = preparacaoDireta,
) {
  const liberarPrevia = vi.fn<(previa: string) => void>();
  const aoConcluirLote = vi.fn<() => Promise<void>>(async () => undefined);
  const criarPrevia = vi.fn((arquivoReduzido: File) => `blob:reduzido:${arquivoReduzido.name}`);
  const opcoes: OpcoesFilaEnvioFotos = {
    enviar,
    liberarPrevia,
    mensagemDeErro: (erro) => (erro instanceof ErroApi ? erro.mensagem : 'erro genérico'),
    aoConcluirLote,
    preparar,
    criarPrevia,
    recusaDoArquivo: erroRecusouArquivo,
  };
  return { opcoes, liberarPrevia, aoConcluirLote, criarPrevia };
}

/** Deixa as promessas e as trocas de estado da fila andarem até o fim da fila de microtarefas. */
function aguardar(): Promise<void> {
  return new Promise((resolver) => setTimeout(resolver, 0));
}

describe('FilaEnvioFotos: descartar foto gravada removida no servidor', () => {
  it('tira da fila o item concluído com o id da foto, libera a prévia e deixa os outros itens', async () => {
    const { enviar, chamadas } = criarEnvioControlado();
    const { opcoes, liberarPrevia } = criarOpcoes(enviar);
    const fila = new FilaEnvioFotos(opcoes);

    fila.definirVeiculo(7);
    fila.adicionar(paraFila('a.png', 'b.png'), 0);
    await aguardar();
    chamadas[0].resolver(foto(1));
    await aguardar();
    expect(fila.obterInstantaneo()[0].situacao).toBe('concluida');

    fila.descartarGravada(1);

    expect(fila.obterInstantaneo().map((item) => item.nome)).toEqual(['b.png']);
    expect(fila.obterInstantaneo()[0].situacao).toBe('enviando');
    expect(liberarPrevia).toHaveBeenCalledWith('blob:a.png');
  });

  it('id que não é de foto concluída na fila não faz nada e não libera prévia', async () => {
    const { enviar, chamadas } = criarEnvioControlado();
    const { opcoes, liberarPrevia } = criarOpcoes(enviar);
    const fila = new FilaEnvioFotos(opcoes);

    fila.definirVeiculo(7);
    fila.adicionar(paraFila('a.png'), 0);
    await aguardar();
    chamadas[0].resolver(foto(1));
    await aguardar();
    const antes = fila.obterInstantaneo();
    const liberacoesAntes = liberarPrevia.mock.calls.length;

    fila.descartarGravada(999);

    expect(fila.obterInstantaneo()).toBe(antes);
    expect(liberarPrevia.mock.calls.length).toBe(liberacoesAntes);
  });
});

describe('FilaEnvioFotos: envio sequencial', () => {
  it('envia uma foto por vez, em ordem, e só informa o fim do lote no último', async () => {
    const { enviar, chamadas } = criarEnvioControlado();
    const { opcoes, aoConcluirLote } = criarOpcoes(enviar);
    const fila = new FilaEnvioFotos(opcoes);

    fila.definirVeiculo(7);
    fila.adicionar(paraFila('a.png', 'b.png', 'c.png'), 0);
    await aguardar();

    expect(enviar).toHaveBeenCalledTimes(1);
    expect(chamadas[0].arquivo.name).toBe('a.png');

    chamadas[0].resolver(foto(1));
    await aguardar();
    expect(enviar).toHaveBeenCalledTimes(2);
    expect(chamadas[1].arquivo.name).toBe('b.png');

    chamadas[1].resolver(foto(2));
    await aguardar();
    chamadas[2].resolver(foto(3));
    await aguardar();

    expect(enviar).toHaveBeenCalledTimes(3);
    expect(enviar.mock.calls.every((args) => args[0] === 7)).toBe(true);
    expect(aoConcluirLote).toHaveBeenCalledTimes(1);
    expect(fila.obterInstantaneo()).toEqual([]);
  });

  it('as fotos gravadas do lote só saem depois que a atualização da lista termina (sem piscar)', async () => {
    const { enviar, chamadas } = criarEnvioControlado();
    let concluirAtualizacao: () => void = () => undefined;
    const aoConcluirLote = vi.fn<() => Promise<void>>(
      () => new Promise<void>((resolver) => { concluirAtualizacao = resolver; }),
    );
    const fila = new FilaEnvioFotos({ ...criarOpcoes(enviar).opcoes, aoConcluirLote });

    fila.definirVeiculo(7);
    fila.adicionar(paraFila('a.png'), 0);
    await aguardar();
    chamadas[0].resolver(foto(1));
    await aguardar();

    expect(fila.obterInstantaneo()).toHaveLength(1);
    expect(fila.obterInstantaneo()[0].situacao).toBe('concluida');

    concluirAtualizacao();
    await aguardar();
    expect(fila.obterInstantaneo()).toEqual([]);
  });

  it('nunca tem duas chamadas em voo ao mesmo tempo', async () => {
    const { enviar, chamadas } = criarEnvioControlado();
    const { opcoes } = criarOpcoes(enviar);
    const fila = new FilaEnvioFotos(opcoes);

    fila.definirVeiculo(7);
    fila.adicionar(paraFila('a.png', 'b.png'), 0);
    fila.adicionar(paraFila('c.png'), 0);
    await aguardar();

    expect(chamadas).toHaveLength(1);
    chamadas[0].resolver(foto(1));
    await aguardar();
    expect(chamadas).toHaveLength(2);
  });

  it('sem veículo, as fotos ficam na fila sem enviar; o envio começa quando o id chega', async () => {
    const { enviar } = criarEnvioControlado();
    const { opcoes } = criarOpcoes(enviar);
    const fila = new FilaEnvioFotos(opcoes);

    fila.adicionar(paraFila('a.png'), 0);
    await aguardar();
    expect(enviar).not.toHaveBeenCalled();
    expect(fila.obterInstantaneo()[0].situacao).toBe('aguardando');

    fila.definirVeiculo(9);
    await aguardar();
    expect(enviar).toHaveBeenCalledTimes(1);
    expect(enviar.mock.calls[0][0]).toBe(9);
  });
});

describe('FilaEnvioFotos: erros e nova tentativa', () => {
  it('erro numa foto não para as seguintes e guarda a mensagem traduzida', async () => {
    const { enviar, chamadas } = criarEnvioControlado();
    const { opcoes } = criarOpcoes(enviar);
    const fila = new FilaEnvioFotos(opcoes);

    fila.definirVeiculo(7);
    fila.adicionar(paraFila('a.png', 'b.png'), 0);
    await aguardar();

    chamadas[0].rejeitar(new ErroApi({ status: 502, mensagem: 'Armazenamento indisponível.' }));
    await aguardar();

    const [primeira, segunda] = fila.obterInstantaneo();
    expect(primeira.situacao).toBe('erro');
    expect(primeira.erro).toBe('Armazenamento indisponível.');
    expect(segunda.situacao).toBe('enviando');
    expect(enviar).toHaveBeenCalledTimes(2);
  });

  it('não tenta de novo sozinha: a foto com erro só volta com "tentar de novo"', async () => {
    const { enviar, chamadas } = criarEnvioControlado();
    const { opcoes, aoConcluirLote } = criarOpcoes(enviar);
    const fila = new FilaEnvioFotos(opcoes);

    fila.definirVeiculo(7);
    fila.adicionar(paraFila('a.png'), 0);
    await aguardar();
    chamadas[0].rejeitar(new ErroApi({ status: 429, mensagem: 'Muitas tentativas.' }));
    await aguardar();
    await aguardar();

    expect(enviar).toHaveBeenCalledTimes(1);
    expect(fila.obterInstantaneo()[0].situacao).toBe('erro');

    const chaveComErro = fila.obterInstantaneo()[0].chave;
    fila.tentarDeNovo(chaveComErro);
    await aguardar();
    expect(enviar).toHaveBeenCalledTimes(2);
    expect(chamadas[1].arquivo.name).toBe('a.png');

    chamadas[1].resolver(foto(5));
    await aguardar();
    expect(fila.obterInstantaneo()).toEqual([]);
    expect(aoConcluirLote).toHaveBeenCalledTimes(2);
  });

  it('tentar de novo só age sobre foto com erro', async () => {
    const { enviar } = criarEnvioControlado();
    const { opcoes } = criarOpcoes(enviar);
    const fila = new FilaEnvioFotos(opcoes);

    fila.adicionar(paraFila('a.png'), 0);
    const chave = fila.obterInstantaneo()[0].chave;
    fila.tentarDeNovo(chave);
    fila.tentarDeNovo(999);
    await aguardar();

    expect(fila.obterInstantaneo()[0].situacao).toBe('aguardando');
    expect(enviar).not.toHaveBeenCalled();
  });

  it('erro que não é da API ganha a mensagem do tradutor, nunca o texto técnico', async () => {
    const { enviar, chamadas } = criarEnvioControlado();
    const { opcoes } = criarOpcoes(enviar);
    const fila = new FilaEnvioFotos(opcoes);

    fila.definirVeiculo(7);
    fila.adicionar(paraFila('a.png'), 0);
    await aguardar();
    chamadas[0].rejeitar(new TypeError('Failed to fetch'));
    await aguardar();

    expect(fila.obterInstantaneo()[0].erro).toBe('erro genérico');
  });
});

describe('FilaEnvioFotos: vagas, remoção e progresso', () => {
  it('o que não cabe no teto é recusado e tem a prévia liberada', async () => {
    const { enviar } = criarEnvioControlado();
    const { opcoes, liberarPrevia } = criarOpcoes(enviar);
    const fila = new FilaEnvioFotos(opcoes);

    const recusados = fila.adicionar(paraFila('a.png', 'b.png'), 14);

    expect(fila.obterInstantaneo()).toHaveLength(1);
    expect(recusados.map((r) => r.arquivo.name)).toEqual(['b.png']);
    expect(liberarPrevia).toHaveBeenCalledWith('blob:b.png');
  });

  it('a fila conta contra o teto: com a vaga ocupada, a próxima foto é recusada', async () => {
    const { enviar } = criarEnvioControlado();
    const { opcoes } = criarOpcoes(enviar);
    const fila = new FilaEnvioFotos(opcoes);

    fila.adicionar(paraFila('a.png'), 14);
    const recusados = fila.adicionar(paraFila('c.png'), 14);

    expect(fila.obterInstantaneo()).toHaveLength(1);
    expect(recusados.map((r) => r.arquivo.name)).toEqual(['c.png']);
  });

  it('remove foto que não está enviando, libera a prévia e não mexe na que envia', async () => {
    const { enviar } = criarEnvioControlado();
    const { opcoes, liberarPrevia } = criarOpcoes(enviar);
    const fila = new FilaEnvioFotos(opcoes);

    fila.definirVeiculo(7);
    fila.adicionar(paraFila('a.png', 'b.png'), 0);
    await aguardar();
    const [enviando, aguardando] = fila.obterInstantaneo();

    fila.remover(enviando.chave);
    expect(fila.obterInstantaneo()).toHaveLength(2);

    fila.remover(aguardando.chave);
    expect(fila.obterInstantaneo().map((i) => i.nome)).toEqual(['a.png']);
    expect(liberarPrevia).toHaveBeenCalledWith('blob:b.png');
    expect(liberarPrevia).not.toHaveBeenCalledWith('blob:a.png');
  });

  it('atualiza o percentual da foto em envio', async () => {
    const { enviar, chamadas } = criarEnvioControlado();
    const { opcoes } = criarOpcoes(enviar);
    const fila = new FilaEnvioFotos(opcoes);

    fila.definirVeiculo(7);
    fila.adicionar(paraFila('a.png'), 0);
    await aguardar();
    chamadas[0].aoProgredir(40);

    expect(fila.obterInstantaneo()[0]).toMatchObject({ situacao: 'enviando', percentual: 40 });
  });

  it('limparConcluidas tira as fotos gravadas do lote e libera as prévias', async () => {
    const { enviar, chamadas } = criarEnvioControlado();
    const { opcoes, liberarPrevia } = criarOpcoes(enviar);
    const fila = new FilaEnvioFotos(opcoes);

    fila.definirVeiculo(7);
    fila.adicionar(paraFila('a.png'), 0);
    await aguardar();
    chamadas[0].resolver(foto(1));
    await aguardar();

    fila.limparConcluidas();

    expect(fila.obterInstantaneo()).toEqual([]);
    expect(liberarPrevia).toHaveBeenCalledWith('blob:a.png');
  });
});

describe('FilaEnvioFotos: troca de veículo', () => {
  it('trocar o veículo cancela o envio em curso, libera as prévias e não manda nada do veículo anterior ao novo', async () => {
    const { enviar, chamadas } = criarEnvioControlado();
    const { opcoes, liberarPrevia } = criarOpcoes(enviar);
    const fila = new FilaEnvioFotos(opcoes);

    fila.definirVeiculo(7);
    fila.adicionar(paraFila('a.png', 'b.png'), 0);
    await aguardar();

    fila.definirVeiculo(8);
    expect(chamadas[0].sinal.aborted).toBe(true);
    expect(liberarPrevia).toHaveBeenCalledWith('blob:a.png');
    expect(liberarPrevia).toHaveBeenCalledWith('blob:b.png');
    expect(fila.obterInstantaneo()).toEqual([]);

    chamadas[0].rejeitar(new DOMException('cancelado', 'AbortError'));
    await aguardar();
    expect(enviar).toHaveBeenCalledTimes(1);
    expect(fila.obterInstantaneo()).toEqual([]);
  });

  it('foto adicionada logo depois da troca vai para o veículo novo, só depois de o envio antigo terminar', async () => {
    const { enviar, chamadas } = criarEnvioControlado();
    const { opcoes } = criarOpcoes(enviar);
    const fila = new FilaEnvioFotos(opcoes);

    fila.definirVeiculo(7);
    fila.adicionar(paraFila('a.png'), 0);
    await aguardar();

    fila.definirVeiculo(8);
    fila.adicionar(paraFila('c.png'), 0);
    expect(enviar).toHaveBeenCalledTimes(1);

    chamadas[0].rejeitar(new DOMException('cancelado', 'AbortError'));
    await aguardar();

    expect(enviar).toHaveBeenCalledTimes(2);
    expect(enviar.mock.calls[1][0]).toBe(8);
    expect(chamadas[1].arquivo.name).toBe('c.png');
    expect(enviar.mock.calls.filter((args) => args[0] === 7)).toHaveLength(1);
  });

  it('a foto que estava enviando para o veículo 7 é cancelada na troca para o 9, e o 9 não recebe nada dela', async () => {
    const { enviar, chamadas } = criarEnvioControlado();
    const { opcoes, liberarPrevia } = criarOpcoes(enviar);
    const fila = new FilaEnvioFotos(opcoes);

    fila.adicionar(paraFila('a.png'), 0);
    fila.definirVeiculo(7);
    await aguardar();
    chamadas[0].resolver(foto(1));
    await aguardar();

    fila.adicionar(paraFila('b.png'), 0);
    await aguardar();
    expect(chamadas).toHaveLength(2);

    fila.definirVeiculo(9);
    expect(chamadas[1].sinal.aborted).toBe(true);
    expect(liberarPrevia).toHaveBeenCalledWith('blob:b.png');
    chamadas[1].rejeitar(new DOMException('cancelado', 'AbortError'));
    await aguardar();

    expect(enviar.mock.calls.map((args) => args[0])).toEqual([7, 7]);
    expect(fila.obterInstantaneo()).toEqual([]);
  });

  it('passar de nenhum veículo para um id não esvazia as fotos do cadastro', async () => {
    const { enviar } = criarEnvioControlado();
    const { opcoes, liberarPrevia } = criarOpcoes(enviar);
    const fila = new FilaEnvioFotos(opcoes);

    fila.adicionar(paraFila('a.png'), 0);
    fila.definirVeiculo(null);
    fila.definirVeiculo(5);
    await aguardar();

    expect(liberarPrevia).not.toHaveBeenCalledWith('blob:a.png');
    expect(enviar).toHaveBeenCalledTimes(1);
    expect(enviar.mock.calls[0][0]).toBe(5);
  });
});

describe('FilaEnvioFotos: encerramento e avisos', () => {
  it('encerrar cancela o envio em curso sem marcar erro, não envia mais nada e libera as prévias', async () => {
    const { enviar, chamadas } = criarEnvioControlado();
    const { opcoes, liberarPrevia, aoConcluirLote } = criarOpcoes(enviar);
    const fila = new FilaEnvioFotos(opcoes);

    fila.definirVeiculo(7);
    fila.adicionar(paraFila('a.png', 'b.png'), 0);
    await aguardar();

    fila.encerrar();
    expect(chamadas[0].sinal.aborted).toBe(true);
    chamadas[0].rejeitar(new DOMException('cancelado', 'AbortError'));
    await aguardar();

    expect(enviar).toHaveBeenCalledTimes(1);
    expect(fila.obterInstantaneo()).toEqual([]);
    expect(liberarPrevia).toHaveBeenCalledWith('blob:a.png');
    expect(liberarPrevia).toHaveBeenCalledWith('blob:b.png');
    expect(aoConcluirLote).not.toHaveBeenCalled();
  });

  it('depois de encerrada, nada mais entra na fila e as prévias recebidas são liberadas', () => {
    const { enviar } = criarEnvioControlado();
    const { opcoes, liberarPrevia } = criarOpcoes(enviar);
    const fila = new FilaEnvioFotos(opcoes);

    fila.encerrar();
    const recusados = fila.adicionar(paraFila('a.png'), 0);

    expect(fila.obterInstantaneo()).toEqual([]);
    expect(recusados).toHaveLength(1);
    expect(liberarPrevia).toHaveBeenCalledWith('blob:a.png');
  });

  it('ativar reabre a fila depois de um encerramento (o StrictMode desmonta e monta de novo)', async () => {
    const { enviar } = criarEnvioControlado();
    const { opcoes } = criarOpcoes(enviar);
    const fila = new FilaEnvioFotos(opcoes);

    fila.encerrar();
    fila.ativar();
    fila.definirVeiculo(7);
    fila.adicionar(paraFila('a.png'), 0);
    await aguardar();

    expect(enviar).toHaveBeenCalledTimes(1);
    expect(fila.obterInstantaneo()[0].situacao).toBe('enviando');
  });

  it('avisa quem está inscrito e para de avisar depois de cancelar a inscrição', () => {
    const { enviar } = criarEnvioControlado();
    const { opcoes } = criarOpcoes(enviar);
    const fila = new FilaEnvioFotos(opcoes);
    const ouvinte = vi.fn<() => void>();

    const cancelar = fila.inscrever(ouvinte);
    fila.adicionar(paraFila('a.png'), 0);
    expect(ouvinte).toHaveBeenCalled();

    const chamadasAntes = ouvinte.mock.calls.length;
    cancelar();
    fila.adicionar(paraFila('b.png'), 0);
    expect(ouvinte.mock.calls.length).toBe(chamadasAntes);
  });

  it('obterInstantaneo devolve a mesma referência enquanto nada muda', () => {
    const { enviar } = criarEnvioControlado();
    const { opcoes } = criarOpcoes(enviar);
    const fila = new FilaEnvioFotos(opcoes);
    fila.adicionar(paraFila('a.png'), 0);

    expect(fila.obterInstantaneo()).toBe(fila.obterInstantaneo());
  });
});

describe('FilaEnvioFotos: preparo antes do envio', () => {
  it('prepara a foto da vez, envia a reduzida e troca a prévia (a antiga é liberada uma vez)', async () => {
    const { enviar, chamadas } = criarEnvioControlado();
    const { preparar, pendentes } = criarPreparacaoControlada();
    const { opcoes, liberarPrevia, criarPrevia } = criarOpcoes(enviar, preparar);
    const fila = new FilaEnvioFotos(opcoes);

    fila.definirVeiculo(7);
    fila.adicionar(paraFila('a.png'), 0);
    await aguardar();

    expect(preparar).toHaveBeenCalledTimes(1);
    expect(enviar).not.toHaveBeenCalled();
    expect(fila.obterInstantaneo()[0]).toMatchObject({ situacao: 'aguardando', percentual: 0, erro: null });

    pendentes[0].resolver({ arquivo: reduzida('a.jpg'), reduzida: true });
    await aguardar();

    expect(criarPrevia).toHaveBeenCalledTimes(1);
    expect(criarPrevia.mock.calls[0][0].name).toBe('a.jpg');
    expect(fila.obterInstantaneo()[0].previa).toBe('blob:reduzido:a.jpg');
    expect(liberarPrevia.mock.calls.filter(([previa]) => previa === 'blob:a.png')).toHaveLength(1);
    expect(enviar).toHaveBeenCalledTimes(1);
    expect(chamadas[0].arquivo.name).toBe('a.jpg');
  });

  it('uma preparação por vez: a foto N+1 é preparada enquanto a N sobe, e a N+2 só começa quando a N+1 vira enviando', async () => {
    const { enviar, chamadas } = criarEnvioControlado();
    const { preparar, pendentes } = criarPreparacaoControlada();
    const { opcoes } = criarOpcoes(enviar, preparar);
    const fila = new FilaEnvioFotos(opcoes);

    fila.definirVeiculo(7);
    fila.adicionar(paraFila('a.png', 'b.png', 'c.png'), 0);
    await aguardar();
    expect(preparar).toHaveBeenCalledTimes(1);

    pendentes[0].resolver({ arquivo: arquivo('a.png'), reduzida: false });
    await aguardar();
    expect(enviar).toHaveBeenCalledTimes(1);
    expect(preparar).toHaveBeenCalledTimes(2);

    chamadas[0].resolver(foto(1));
    await aguardar();
    expect(enviar).toHaveBeenCalledTimes(1);
    expect(preparar).toHaveBeenCalledTimes(2);

    pendentes[1].resolver({ arquivo: arquivo('b.png'), reduzida: false });
    await aguardar();
    expect(enviar).toHaveBeenCalledTimes(2);
    expect(preparar).toHaveBeenCalledTimes(3);

    chamadas[1].resolver(foto(2));
    await aguardar();
    expect(enviar).toHaveBeenCalledTimes(2);
    pendentes[2].resolver({ arquivo: arquivo('c.png'), reduzida: false });
    await aguardar();
    chamadas[2].resolver(foto(3));
    await aguardar();

    expect(enviar.mock.calls.map((args) => args[1].name)).toEqual(['a.png', 'b.png', 'c.png']);
    expect(preparar).toHaveBeenCalledTimes(3);
  });

  it('ordem preservada com 5 fotos e preparacoes concluindo fora de ordem: a foto que volta de erro sobe antes, mesmo com o preparo da seguinte já pronto', async () => {
    const { enviar, chamadas } = criarEnvioControlado();
    const { preparar, pendentes } = criarPreparacaoControlada();
    const { opcoes, criarPrevia } = criarOpcoes(enviar, preparar);
    const fila = new FilaEnvioFotos(opcoes);

    fila.definirVeiculo(7);
    fila.adicionar(paraFila('a.png', 'b.png', 'c.png', 'd.png', 'e.png'), 0);
    await aguardar();
    pendentes[0].resolver({ arquivo: arquivo('a.png'), reduzida: false });
    await aguardar();
    chamadas[0].rejeitar(new ErroApi({ status: 500, mensagem: 'Falha interna.' }));
    await aguardar();
    expect(preparar).toHaveBeenCalledTimes(2);

    fila.tentarDeNovo(fila.obterInstantaneo()[0].chave);
    await aguardar();
    expect(enviar).toHaveBeenCalledTimes(1);

    pendentes[1].resolver({ arquivo: reduzida('b.jpg'), reduzida: true });
    await aguardar();
    expect(enviar).toHaveBeenCalledTimes(2);
    expect(chamadas[1].arquivo.name).toBe('a.png');
    expect(criarPrevia).toHaveBeenCalledTimes(1);

    chamadas[1].resolver(foto(1));
    await aguardar();
    expect(enviar).toHaveBeenCalledTimes(3);
    expect(chamadas[2].arquivo.name).toBe('b.jpg');
    expect(preparar).toHaveBeenCalledTimes(3);

    chamadas[2].resolver(foto(2));
    await aguardar();
    pendentes[2].resolver({ arquivo: arquivo('c.png'), reduzida: false });
    await aguardar();
    expect(enviar).toHaveBeenCalledTimes(4);
    expect(preparar).toHaveBeenCalledTimes(4);

    chamadas[3].resolver(foto(3));
    await aguardar();
    pendentes[3].resolver({ arquivo: arquivo('d.png'), reduzida: false });
    await aguardar();
    expect(enviar).toHaveBeenCalledTimes(5);
    expect(preparar).toHaveBeenCalledTimes(5);

    chamadas[4].resolver(foto(4));
    await aguardar();
    pendentes[4].resolver({ arquivo: arquivo('e.png'), reduzida: false });
    await aguardar();
    chamadas[5].resolver(foto(5));
    await aguardar();

    expect(enviar.mock.calls.map((args) => args[1].name)).toEqual(['a.png', 'a.png', 'b.jpg', 'c.png', 'd.png', 'e.png']);
    expect(preparar).toHaveBeenCalledTimes(5);
    expect(fila.obterInstantaneo()).toEqual([]);
  });

  it('foto antecipada removida durante o preparo: nada mais é enviado e nenhuma prévia nova é criada', async () => {
    const { enviar, chamadas } = criarEnvioControlado();
    const { preparar, pendentes } = criarPreparacaoControlada();
    const { opcoes, liberarPrevia, criarPrevia } = criarOpcoes(enviar, preparar);
    const fila = new FilaEnvioFotos(opcoes);

    fila.definirVeiculo(7);
    fila.adicionar(paraFila('a.png', 'b.png'), 0);
    await aguardar();
    pendentes[0].resolver({ arquivo: arquivo('a.png'), reduzida: false });
    await aguardar();
    expect(preparar).toHaveBeenCalledTimes(2);

    fila.remover(fila.obterInstantaneo()[1].chave);
    pendentes[1].resolver({ arquivo: reduzida('b.jpg'), reduzida: true });
    await aguardar();

    expect(criarPrevia).not.toHaveBeenCalled();
    expect(liberarPrevia.mock.calls.filter(([previa]) => previa === 'blob:b.png')).toHaveLength(1);

    chamadas[0].resolver(foto(1));
    await aguardar();
    expect(enviar).toHaveBeenCalledTimes(1);
    expect(fila.obterInstantaneo()).toEqual([]);
  });

  it('falha do preparo antecipado não derruba a fila: a foto segue com o próprio original', async () => {
    const { enviar, chamadas } = criarEnvioControlado();
    const { preparar, pendentes } = criarPreparacaoControlada();
    const { opcoes, criarPrevia } = criarOpcoes(enviar, preparar);
    const fila = new FilaEnvioFotos(opcoes);

    fila.definirVeiculo(7);
    fila.adicionar(paraFila('a.png', 'b.png', 'c.png'), 0);
    await aguardar();
    pendentes[0].resolver({ arquivo: arquivo('a.png'), reduzida: false });
    await aguardar();
    expect(preparar).toHaveBeenCalledTimes(2);

    pendentes[1].rejeitar(new Error('falha de preparo'));
    await aguardar();
    chamadas[0].resolver(foto(1));
    await aguardar();

    expect(enviar).toHaveBeenCalledTimes(2);
    expect(chamadas[1].arquivo.name).toBe('b.png');
    expect(chamadas[1].arquivo.type).toBe('image/png');
    expect(criarPrevia).not.toHaveBeenCalled();
    expect(preparar).toHaveBeenCalledTimes(3);
  });

  it('envia o próprio original quando a preparação devolve não reduzida, ou quando ela falha', async () => {
    const { enviar } = criarEnvioControlado();
    const original = arquivo('a.png');
    const fila = new FilaEnvioFotos(criarOpcoes(enviar).opcoes);
    fila.definirVeiculo(7);
    fila.adicionar([{ arquivo: original, previa: 'blob:a.png', cabecalho: null }], 0);
    await aguardar();
    expect(enviar.mock.calls[0][1]).toBe(original);

    const { enviar: enviarFalha } = criarEnvioControlado();
    const preparacaoFalha = vi.fn(async (): Promise<FotoPreparada> => {
      throw new Error('falha de preparo');
    });
    const original2 = arquivo('b.png');
    const filaFalha = new FilaEnvioFotos(criarOpcoes(enviarFalha, preparacaoFalha).opcoes);
    filaFalha.definirVeiculo(7);
    filaFalha.adicionar([{ arquivo: original2, previa: 'blob:b.png', cabecalho: null }], 0);
    await aguardar();
    expect(enviarFalha.mock.calls[0][1]).toBe(original2);
  });

  it('tentar de novo reenvia o mesmo arquivo reduzido, sem preparar outra vez', async () => {
    const { enviar, chamadas } = criarEnvioControlado();
    const { preparar, pendentes } = criarPreparacaoControlada();
    const { opcoes, criarPrevia } = criarOpcoes(enviar, preparar);
    const fila = new FilaEnvioFotos(opcoes);
    const arquivoReduzido = reduzida('a.jpg');

    fila.definirVeiculo(7);
    fila.adicionar(paraFila('a.png'), 0);
    await aguardar();
    pendentes[0].resolver({ arquivo: arquivoReduzido, reduzida: true });
    await aguardar();
    chamadas[0].rejeitar(new ErroApi({ status: 500, mensagem: 'Falha interna.' }));
    await aguardar();

    fila.tentarDeNovo(fila.obterInstantaneo()[0].chave);
    await aguardar();

    expect(preparar).toHaveBeenCalledTimes(1);
    expect(enviar).toHaveBeenCalledTimes(2);
    expect(chamadas[1].arquivo).toBe(arquivoReduzido);
    expect(criarPrevia).toHaveBeenCalledTimes(1);

    chamadas[1].resolver(foto(1));
    await aguardar();
    expect(fila.obterInstantaneo()).toEqual([]);
  });

  it('depois de 400 com o arquivo reduzido, tentar de novo reenvia o original', async () => {
    const { enviar, chamadas } = criarEnvioControlado();
    const { preparar, pendentes } = criarPreparacaoControlada();
    const { opcoes } = criarOpcoes(enviar, preparar);
    const fila = new FilaEnvioFotos(opcoes);
    const original = arquivo('a.png');

    fila.definirVeiculo(7);
    fila.adicionar([{ arquivo: original, previa: 'blob:a.png', cabecalho: null }], 0);
    await aguardar();
    pendentes[0].resolver({ arquivo: reduzida('a.jpg'), reduzida: true });
    await aguardar();
    chamadas[0].rejeitar(new ErroApi({ status: 400, mensagem: 'Arquivo recusado.' }));
    await aguardar();
    expect(fila.obterInstantaneo()[0].situacao).toBe('erro');

    fila.tentarDeNovo(fila.obterInstantaneo()[0].chave);
    await aguardar();

    expect(preparar).toHaveBeenCalledTimes(1);
    expect(chamadas[1].arquivo).toBe(original);
  });

  it('409 ou 429 com o arquivo reduzido não trocam o envio pelo original', async () => {
    const { enviar, chamadas } = criarEnvioControlado();
    const { preparar, pendentes } = criarPreparacaoControlada();
    const { opcoes } = criarOpcoes(enviar, preparar);
    const fila = new FilaEnvioFotos(opcoes);
    const arquivoReduzido = reduzida('a.jpg');

    fila.definirVeiculo(7);
    fila.adicionar(paraFila('a.png'), 0);
    await aguardar();
    pendentes[0].resolver({ arquivo: arquivoReduzido, reduzida: true });
    await aguardar();
    chamadas[0].rejeitar(new ErroApi({ status: 429, mensagem: 'Muitas tentativas.' }));
    await aguardar();

    fila.tentarDeNovo(fila.obterInstantaneo()[0].chave);
    await aguardar();

    expect(chamadas[1].arquivo).toBe(arquivoReduzido);
  });

  it('encerrar libera a prévia reduzida uma vez e a original uma vez, sem sobrar URL', async () => {
    const { enviar } = criarEnvioControlado();
    const { preparar, pendentes } = criarPreparacaoControlada();
    const { opcoes, liberarPrevia } = criarOpcoes(enviar, preparar);
    const fila = new FilaEnvioFotos(opcoes);

    fila.definirVeiculo(7);
    fila.adicionar(paraFila('a.png'), 0);
    await aguardar();
    pendentes[0].resolver({ arquivo: reduzida('a.jpg'), reduzida: true });
    await aguardar();

    fila.encerrar();

    const liberadas = liberarPrevia.mock.calls.map(([previa]) => previa);
    expect(liberadas.filter((previa) => previa === 'blob:a.png')).toHaveLength(1);
    expect(liberadas.filter((previa) => previa === 'blob:reduzido:a.jpg')).toHaveLength(1);
  });

  it('remover a foto durante a preparação: nada é enviado e nenhuma prévia nova é criada', async () => {
    const { enviar } = criarEnvioControlado();
    const { preparar, pendentes } = criarPreparacaoControlada();
    const { opcoes, liberarPrevia, criarPrevia } = criarOpcoes(enviar, preparar);
    const fila = new FilaEnvioFotos(opcoes);

    fila.definirVeiculo(7);
    fila.adicionar(paraFila('a.png'), 0);
    await aguardar();
    fila.remover(fila.obterInstantaneo()[0].chave);

    pendentes[0].resolver({ arquivo: reduzida('a.jpg'), reduzida: true });
    await aguardar();

    expect(enviar).not.toHaveBeenCalled();
    expect(criarPrevia).not.toHaveBeenCalled();
    expect(liberarPrevia.mock.calls.filter(([previa]) => previa === 'blob:a.png')).toHaveLength(1);
    expect(fila.obterInstantaneo()).toEqual([]);
  });

  it('trocar de veículo durante a preparação: nada do veículo anterior é enviado e a foto nova vai ao novo', async () => {
    const { enviar, chamadas } = criarEnvioControlado();
    const { preparar, pendentes } = criarPreparacaoControlada();
    const { opcoes, criarPrevia } = criarOpcoes(enviar, preparar);
    const fila = new FilaEnvioFotos(opcoes);

    fila.definirVeiculo(7);
    fila.adicionar(paraFila('a.png'), 0);
    await aguardar();
    fila.definirVeiculo(8);
    fila.adicionar(paraFila('c.png'), 0);
    await aguardar();

    pendentes[0].resolver({ arquivo: reduzida('a.jpg'), reduzida: true });
    await aguardar();
    expect(criarPrevia).not.toHaveBeenCalled();
    expect(enviar).not.toHaveBeenCalled();

    pendentes[1].resolver({ arquivo: arquivo('c.png'), reduzida: false });
    await aguardar();
    expect(enviar).toHaveBeenCalledTimes(1);
    expect(enviar.mock.calls[0][0]).toBe(8);
    expect(chamadas[0].arquivo.name).toBe('c.png');
  });

  it('encerrar durante a preparação: nada é enviado depois', async () => {
    const { enviar } = criarEnvioControlado();
    const { preparar, pendentes } = criarPreparacaoControlada();
    const { opcoes, criarPrevia } = criarOpcoes(enviar, preparar);
    const fila = new FilaEnvioFotos(opcoes);

    fila.definirVeiculo(7);
    fila.adicionar(paraFila('a.png'), 0);
    await aguardar();
    fila.encerrar();

    pendentes[0].resolver({ arquivo: reduzida('a.jpg'), reduzida: true });
    await aguardar();

    expect(enviar).not.toHaveBeenCalled();
    expect(criarPrevia).not.toHaveBeenCalled();
  });

  it('tentar de novo numa foto anterior durante o preparo da seguinte: a anterior sobe primeiro, sem ser preparada de novo e com a mesma prévia', async () => {
    const { enviar, chamadas } = criarEnvioControlado();
    const { preparar, pendentes } = criarPreparacaoControlada();
    const { opcoes } = criarOpcoes(enviar, preparar);
    const fila = new FilaEnvioFotos(opcoes);
    const reduzidaA = reduzida('a.jpg');

    fila.definirVeiculo(7);
    fila.adicionar(paraFila('a.png', 'b.png'), 0);
    await aguardar();
    pendentes[0].resolver({ arquivo: reduzidaA, reduzida: true });
    await aguardar();
    chamadas[0].rejeitar(new ErroApi({ status: 500, mensagem: 'Falha interna.' }));
    await aguardar();
    expect(preparar).toHaveBeenCalledTimes(2);

    const previaA = fila.obterInstantaneo()[0].previa;
    fila.tentarDeNovo(fila.obterInstantaneo()[0].chave);
    await aguardar();
    expect(enviar).toHaveBeenCalledTimes(1);

    pendentes[1].resolver({ arquivo: reduzida('b.jpg'), reduzida: true });
    await aguardar();
    expect(enviar).toHaveBeenCalledTimes(2);
    expect(chamadas[1].arquivo).toBe(reduzidaA);
    expect(fila.obterInstantaneo()[0].previa).toBe(previaA);

    chamadas[1].resolver(foto(1));
    await aguardar();
    expect(enviar).toHaveBeenCalledTimes(3);
    expect(chamadas[2].arquivo.name).toBe('b.jpg');
    expect(preparar).toHaveBeenCalledTimes(2);
  });

  it('remover a foto com erro libera a prévia reduzida', async () => {
    const { enviar, chamadas } = criarEnvioControlado();
    const { preparar, pendentes } = criarPreparacaoControlada();
    const { opcoes, liberarPrevia } = criarOpcoes(enviar, preparar);
    const fila = new FilaEnvioFotos(opcoes);

    fila.definirVeiculo(7);
    fila.adicionar(paraFila('a.png'), 0);
    await aguardar();
    pendentes[0].resolver({ arquivo: reduzida('a.jpg'), reduzida: true });
    await aguardar();
    chamadas[0].rejeitar(new ErroApi({ status: 500, mensagem: 'Falha interna.' }));
    await aguardar();

    fila.remover(fila.obterInstantaneo()[0].chave);

    expect(liberarPrevia.mock.calls.filter(([previa]) => previa === 'blob:reduzido:a.jpg')).toHaveLength(1);
    expect(fila.obterInstantaneo()).toEqual([]);
  });

  it('limpar as concluídas e trocar de veículo liberam a prévia reduzida', async () => {
    const { enviar, chamadas } = criarEnvioControlado();
    const { preparar, pendentes } = criarPreparacaoControlada();
    const { opcoes, liberarPrevia } = criarOpcoes(enviar, preparar);
    const fila = new FilaEnvioFotos(opcoes);

    fila.definirVeiculo(7);
    fila.adicionar(paraFila('a.png', 'b.png'), 0);
    await aguardar();
    pendentes[0].resolver({ arquivo: reduzida('a.jpg'), reduzida: true });
    await aguardar();
    chamadas[0].resolver(foto(1));
    await aguardar();
    fila.limparConcluidas();

    expect(liberarPrevia.mock.calls.filter(([previa]) => previa === 'blob:reduzido:a.jpg')).toHaveLength(1);

    fila.definirVeiculo(8);
    expect(liberarPrevia.mock.calls.filter(([previa]) => previa === 'blob:b.png')).toHaveLength(1);
  });
});
