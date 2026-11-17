import { describe, expect, it, vi } from 'vitest';
import { ErroApi } from '@/types/api';
import type { FotoDoVeiculo } from '@/types/fotosVeiculo';
import { FilaEnvioFotos, type ArquivoParaFila, type OpcoesFilaEnvioFotos } from './filaFotosVeiculo';

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
  return nomes.map((nome) => ({ arquivo: arquivo(nome), previa: `blob:${nome}` }));
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

function criarOpcoes(enviar: OpcoesFilaEnvioFotos['enviar']) {
  const liberarPrevia = vi.fn<(previa: string) => void>();
  const aoConcluirLote = vi.fn<() => Promise<void>>(async () => undefined);
  const opcoes: OpcoesFilaEnvioFotos = {
    enviar,
    liberarPrevia,
    mensagemDeErro: (erro) => (erro instanceof ErroApi ? erro.mensagem : 'erro genérico'),
    aoConcluirLote,
  };
  return { opcoes, liberarPrevia, aoConcluirLote };
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
