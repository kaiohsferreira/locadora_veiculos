/**
 * Fila de envio das fotos do veículo: uma foto por vez, em ordem. Sem React: o hook só liga esta classe ao
 * estado da tela (useSyncExternalStore). Erro numa foto não para as seguintes; "tentar de novo" é por foto;
 * nunca há nova tentativa automática. Ao encerrar, cancela o envio em curso e libera todas as prévias.
 */
import { PERCENTUAL_MAXIMO, proximoDaFila, vagasRestantes } from '@/lib/fotosVeiculo';
import type { FotoDoVeiculo, ItemFilaFoto } from '@/types/fotosVeiculo';

/** Arquivo aceito para a fila, com a sua pré-visualização local. */
export interface ArquivoParaFila {
  arquivo: File;
  previa: string;
}

/** Dependências da fila: a chamada à API e o que o navegador precisa fazer (liberar prévias, traduzir erros). */
export interface OpcoesFilaEnvioFotos {
  enviar(
    idVeiculo: number,
    arquivo: File,
    aoProgredir: (percentual: number) => void,
    sinal: AbortSignal,
  ): Promise<FotoDoVeiculo>;
  liberarPrevia(previa: string): void;
  mensagemDeErro(erro: unknown): string;
  /**
   * Chamado quando a fila termina um lote com envios (atualiza a lista da locadora). A fila só tira as fotos
   * gravadas do lote depois que esta promessa resolve, para a lista não piscar.
   */
  aoConcluirLote(): Promise<void>;
}

interface RegistroFila {
  item: ItemFilaFoto;
  arquivo: File;
}

export class FilaEnvioFotos {
  private readonly opcoes: OpcoesFilaEnvioFotos;
  private registros: RegistroFila[] = [];
  private instantaneo: readonly ItemFilaFoto[] = [];
  private idVeiculo: number | null = null;
  private encerrada = false;
  private processando = false;
  private controleAtual: AbortController | null = null;
  private proximaChave = 1;
  private readonly ouvintes = new Set<() => void>();

  constructor(opcoes: OpcoesFilaEnvioFotos) {
    this.opcoes = opcoes;
  }

  /** Assina mudanças da fila. Devolve a função que cancela a assinatura. */
  readonly inscrever = (ouvinte: () => void): (() => void) => {
    this.ouvintes.add(ouvinte);
    return () => {
      this.ouvintes.delete(ouvinte);
    };
  };

  /** Foto da fila em instante fixo: a mesma referência até a próxima mudança. */
  readonly obterInstantaneo = (): readonly ItemFilaFoto[] => this.instantaneo;

  /**
   * Informa o veículo. Sem veículo (cadastro) as fotos ficam na fila sem enviar; ao receber o id, o envio começa.
   * Trocar o veículo (de um id para outro, ou para nenhum) esvazia a fila: as fotos esperando ou com erro são do
   * veículo anterior e não podem ir para o novo.
   */
  definirVeiculo(idVeiculo: number | null): void {
    if (this.encerrada) return;
    const trocouDeVeiculo = this.idVeiculo !== null && this.idVeiculo !== idVeiculo;
    if (trocouDeVeiculo) this.esvaziar();
    this.idVeiculo = idVeiculo;
    if (idVeiculo !== null) void this.processar();
  }

  /**
   * Põe as fotos na fila até o limite de vagas (teto menos gravadas menos as já na fila). Devolve as que não
   * couberam: essas prévias são liberadas aqui.
   */
  adicionar(novos: readonly ArquivoParaFila[], gravadas: number): ArquivoParaFila[] {
    const recusados: ArquivoParaFila[] = [];
    for (const novo of novos) {
      if (this.encerrada || vagasRestantes(gravadas, this.contarNaFila()) === 0) {
        this.opcoes.liberarPrevia(novo.previa);
        recusados.push(novo);
        continue;
      }
      this.registros.push({
        arquivo: novo.arquivo,
        item: {
          chave: this.proximaChave,
          nome: novo.arquivo.name,
          previa: novo.previa,
          situacao: 'aguardando',
          percentual: 0,
          erro: null,
          foto: null,
        },
      });
      this.proximaChave += 1;
    }
    this.publicar();
    void this.processar();
    return recusados;
  }

  /** Volta uma foto com erro para a fila. Só age sobre item em erro. */
  tentarDeNovo(chave: number): void {
    const registro = this.localizar(chave);
    if (registro === undefined || registro.item.situacao !== 'erro') return;
    this.atualizar(chave, { situacao: 'aguardando', percentual: 0, erro: null });
    void this.processar();
  }

  /** Tira da fila uma foto que não está enviando nem gravada. A prévia é liberada. */
  remover(chave: number): void {
    const indice = this.registros.findIndex((registro) => registro.item.chave === chave);
    if (indice < 0) return;
    const situacao = this.registros[indice].item.situacao;
    if (situacao === 'enviando' || situacao === 'concluida') return;
    const [removido] = this.registros.splice(indice, 1);
    this.opcoes.liberarPrevia(removido.item.previa);
    this.publicar();
  }

  /**
   * Tira do lote a foto já gravada com este id (ela foi removida no servidor enquanto o lote rodava). Só age sobre
   * item concluído com esse id; a prévia é liberada e a miniatura deixa de aparecer na hora.
   */
  descartarGravada(idVeiculoFoto: number): void {
    const indice = this.registros.findIndex(
      (registro) =>
        registro.item.situacao === 'concluida' && registro.item.foto?.idVeiculoFoto === idVeiculoFoto,
    );
    if (indice < 0) return;
    const [descartado] = this.registros.splice(indice, 1);
    this.opcoes.liberarPrevia(descartado.item.previa);
    this.publicar();
  }

  /** Tira do lote as fotos já gravadas (depois de a lista ser atualizada). Libera as prévias. */
  limparConcluidas(): void {
    const concluidas = this.registros.filter((registro) => registro.item.situacao === 'concluida');
    if (concluidas.length === 0) return;
    this.registros = this.registros.filter((registro) => registro.item.situacao !== 'concluida');
    concluidas.forEach((registro) => this.opcoes.liberarPrevia(registro.item.previa));
    this.publicar();
  }

  /** Reabre a fila depois de um encerramento (ex.: o StrictMode desmonta e monta de novo a tela). */
  ativar(): void {
    this.encerrada = false;
  }

  /** Encerra a fila (desmontagem da tela): cancela o envio em curso, libera as prévias e para de avisar. */
  encerrar(): void {
    if (this.encerrada) return;
    this.encerrada = true;
    this.controleAtual?.abort();
    this.registros.forEach((registro) => this.opcoes.liberarPrevia(registro.item.previa));
    this.registros = [];
    this.publicar();
    this.ouvintes.clear();
  }

  /** Cancela o envio em curso e tira da fila todas as fotos (troca de veículo). A fila segue ativa. */
  private esvaziar(): void {
    this.controleAtual?.abort();
    this.registros.forEach((registro) => this.opcoes.liberarPrevia(registro.item.previa));
    this.registros = [];
    this.publicar();
  }

  private async processar(): Promise<void> {
    if (this.processando || this.encerrada || this.idVeiculo === null) return;
    this.processando = true;
    let houveEnvio = false;
    try {
      let proximo = proximoDaFila(this.instantaneo);
      while (proximo !== null && !this.encerrada && this.idVeiculo !== null) {
        houveEnvio = true;
        const chave = proximo.chave;
        const idVeiculo = this.idVeiculo;
        const registro = this.localizar(chave);
        if (registro === undefined) break;

        const controle = new AbortController();
        this.controleAtual = controle;
        this.atualizar(chave, { situacao: 'enviando', percentual: 0, erro: null });
        try {
          const foto = await this.opcoes.enviar(
            idVeiculo,
            registro.arquivo,
            (percentual) => {
              if (!controle.signal.aborted) this.atualizar(chave, { percentual });
            },
            controle.signal,
          );
          if (this.encerrada || controle.signal.aborted) break;
          this.atualizar(chave, { situacao: 'concluida', percentual: PERCENTUAL_MAXIMO, erro: null, foto });
        } catch (erro: unknown) {
          if (this.encerrada || controle.signal.aborted) break;
          this.atualizar(chave, { situacao: 'erro', percentual: 0, erro: this.opcoes.mensagemDeErro(erro) });
        } finally {
          this.controleAtual = null;
        }
        proximo = proximoDaFila(this.instantaneo);
      }
    } finally {
      this.processando = false;
    }
    if (houveEnvio && !this.encerrada) {
      await this.opcoes.aoConcluirLote();
      this.limparConcluidas();
    }
    // Fotos adicionadas enquanto este lote ainda rodava (ex.: depois de trocar o veículo) esperaram o fim daqui.
    if (!this.encerrada && this.idVeiculo !== null && proximoDaFila(this.instantaneo) !== null) {
      void this.processar();
    }
  }

  private localizar(chave: number): RegistroFila | undefined {
    return this.registros.find((registro) => registro.item.chave === chave);
  }

  private contarNaFila(): number {
    return this.registros.filter((registro) => registro.item.situacao !== 'concluida').length;
  }

  private atualizar(chave: number, alteracao: Partial<ItemFilaFoto>): void {
    const registro = this.localizar(chave);
    if (registro === undefined) return;
    registro.item = { ...registro.item, ...alteracao };
    this.publicar();
  }

  private publicar(): void {
    this.instantaneo = this.registros.map((registro) => registro.item);
    this.ouvintes.forEach((ouvinte) => ouvinte());
  }
}
