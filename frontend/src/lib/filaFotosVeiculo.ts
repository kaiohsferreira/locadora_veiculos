/**
 * Fila de envio das fotos do veículo: uma foto por vez, em ordem. Sem React: o hook só liga esta classe ao
 * estado da tela (useSyncExternalStore). Erro numa foto não para as seguintes; "tentar de novo" é por foto;
 * nunca há nova tentativa automática. Ao encerrar, cancela o envio em curso e libera todas as prévias.
 * Antes de enviar, cada foto é preparada (reduzida no navegador ou a própria original), uma por vez.
 */
import type { CabecalhoFoto } from '@/lib/cabecalhoFoto';
import type { FotoPreparada } from '@/lib/reducaoFotoVeiculo';
import { arquivoParaEnviar, PERCENTUAL_MAXIMO, proximoDaFila, proximoParaPreparar, vagasRestantes } from '@/lib/fotosVeiculo';
import type { FotoDoVeiculo, ItemFilaFoto } from '@/types/fotosVeiculo';

/** Arquivo aceito para a fila, com a sua pré-visualização local e o cabeçalho lido (nulo quando ilegível). */
export interface ArquivoParaFila {
  arquivo: File;
  previa: string;
  cabecalho: CabecalhoFoto | null;
}

/** Dependências da fila: a chamada à API e o que o navegador precisa fazer (preparar, prévias, erros). */
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
  /** Prepara a foto antes do envio (reduzida ou a própria original). Nunca rejeita. */
  preparar(arquivo: File, cabecalho: CabecalhoFoto | null): Promise<FotoPreparada>;
  /** Cria a prévia (URL de objeto) da foto reduzida. */
  criarPrevia(arquivo: Blob): string;
  /** Verdadeiro quando o servidor recusou o arquivo enviado (400). */
  recusaDoArquivo(erro: unknown): boolean;
}

interface RegistroFila {
  item: ItemFilaFoto;
  /** Arquivo escolhido pela pessoa. */
  original: File;
  cabecalho: CabecalhoFoto | null;
  /** Arquivo reduzido, quando a preparação reduziu a foto. */
  preparado: File | null;
  /** Verdadeiro depois que a preparação terminou (reduzida ou não). */
  jaPreparado: boolean;
  /** Verdadeiro depois de o servidor recusar o arquivo reduzido: o próximo envio usa o original. */
  reenviarOriginal: boolean;
}

/** Preparação em curso (no máximo uma): a chave da foto e a promessa que termina quando ela fica pronta. */
interface PreparoEmCurso {
  chave: number;
  promessa: Promise<void>;
}

export class FilaEnvioFotos {
  private readonly opcoes: OpcoesFilaEnvioFotos;
  private registros: RegistroFila[] = [];
  private instantaneo: readonly ItemFilaFoto[] = [];
  private idVeiculo: number | null = null;
  private encerrada = false;
  private processando = false;
  private preparoEmCurso: PreparoEmCurso | null = null;
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
        original: novo.arquivo,
        cabecalho: novo.cabecalho,
        preparado: null,
        jaPreparado: false,
        reenviarOriginal: false,
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

  /**
   * Volta uma foto com erro para a fila. Só age sobre item em erro. O arquivo reenviado é o mesmo já preparado
   * (sem preparar de novo), salvo depois de recusa do servidor ao arquivo reduzido: aí vai o original.
   */
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

  /**
   * Laço de envio. Para cada foto da vez: (1) pega o próximo da fila; (2) se ainda não foi preparada, espera a
   * preparação em curso (ou começa a dela) e (3) confere de novo quem é o próximo, porque a foto pode ter sido
   * removida ou uma anterior pode ter voltado para aguardando durante o preparo: nesse caso não envia esta agora e
   * volta ao passo 1; (4) envia o arquivo certo; (5) logo depois de marcar "enviando", antecipa o preparo da próxima;
   * (6) grava o resultado (201 sai da fila; erro guarda a mensagem).
   */
  private async processar(): Promise<void> {
    if (this.processando || this.encerrada || this.idVeiculo === null) return;
    this.processando = true;
    let houveEnvio = false;
    try {
      let proximo: ItemFilaFoto | null = proximoDaFila(this.instantaneo);
      while (proximo !== null && !this.encerrada && this.idVeiculo !== null) {
        const chave: number = proximo.chave;
        const idVeiculo: number = this.idVeiculo;
        const registro = this.localizar(chave);
        if (registro === undefined) break;

        if (!registro.jaPreparado) {
          await this.aguardarPreparo(registro);
          if (this.encerrada || this.idVeiculo !== idVeiculo) break;
          const agora = proximoDaFila(this.instantaneo);
          if (agora === null || agora.chave !== chave) {
            proximo = agora;
            continue;
          }
        }

        houveEnvio = true;
        const arquivo = arquivoParaEnviar(registro);
        const eraPreparado = arquivo !== registro.original;
        const controle = new AbortController();
        this.controleAtual = controle;
        this.atualizar(chave, { situacao: 'enviando', percentual: 0, erro: null });
        this.anteciparPreparo();
        try {
          const foto = await this.opcoes.enviar(
            idVeiculo,
            arquivo,
            (percentual) => {
              if (!controle.signal.aborted) this.atualizar(chave, { percentual });
            },
            controle.signal,
          );
          if (this.encerrada || controle.signal.aborted) break;
          this.atualizar(chave, { situacao: 'concluida', percentual: PERCENTUAL_MAXIMO, erro: null, foto });
        } catch (erro: unknown) {
          if (this.encerrada || controle.signal.aborted) break;
          if (eraPreparado && this.opcoes.recusaDoArquivo(erro)) registro.reenviarOriginal = true;
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

  /**
   * Antes de enviar uma foto ainda não preparada: espera a preparação em curso (uma por vez) e, se a própria foto
   * continua na fila sem preparo, começa a preparação dela e espera.
   */
  private async aguardarPreparo(registro: RegistroFila): Promise<void> {
    while (this.preparoEmCurso !== null) {
      await this.preparoEmCurso.promessa;
    }
    if (!registro.jaPreparado && this.localizar(registro.item.chave) === registro) {
      await this.comecarPreparo(registro);
    }
  }

  /**
   * Antecipação de UMA foto (V5): logo depois de a foto da vez ir para "enviando", prepara a próxima a ser enviada
   * enquanto a atual sobe. Nada começa enquanto outra preparação está em curso.
   */
  private anteciparPreparo(): void {
    const preparadas = new Set<number>(
      this.registros.filter((registro) => registro.jaPreparado).map((registro) => registro.item.chave),
    );
    const chave = proximoParaPreparar(this.instantaneo, preparadas, this.preparoEmCurso?.chave ?? null);
    if (chave === null) return;
    const registro = this.localizar(chave);
    if (registro === undefined) return;
    void this.comecarPreparo(registro);
  }

  /**
   * Começa a preparação de uma foto. A promessa nunca rejeita (falha vira o original, em prepararSeguro). Quando
   * termina, deixa de ser a preparação em curso.
   */
  private comecarPreparo(registro: RegistroFila): Promise<void> {
    const promessa: Promise<void> = this.preparar(registro).catch((): void => undefined);
    const marca: PreparoEmCurso = { chave: registro.item.chave, promessa };
    this.preparoEmCurso = marca;
    void promessa.then((): void => {
      if (this.preparoEmCurso === marca) this.preparoEmCurso = null;
    });
    return promessa;
  }

  /**
   * Prepara uma foto. O resultado é descartado se a foto saiu da fila enquanto preparava (removida, fila
   * esvaziada ou encerrada): nenhuma prévia nova é criada. Se a foto foi reduzida, a prévia passa a ser a
   * reduzida e só então a antiga é liberada.
   */
  private async preparar(registro: RegistroFila): Promise<void> {
    const resultado = await this.prepararSeguro(registro);
    if (this.encerrada || this.localizar(registro.item.chave) !== registro) return;
    const previaNova = resultado.reduzida ? this.criarPreviaSegura(resultado.arquivo) : null;
    registro.jaPreparado = true;
    if (previaNova === null) {
      registro.preparado = null;
      return;
    }
    registro.preparado = resultado.arquivo;
    const previaAntiga = registro.item.previa;
    registro.item = { ...registro.item, previa: previaNova };
    this.opcoes.liberarPrevia(previaAntiga);
    this.publicar();
  }

  /** Prévia da foto reduzida. Falha ao criá-la devolve nulo: a foto segue com o original. */
  private criarPreviaSegura(arquivo: File): string | null {
    try {
      return this.opcoes.criarPrevia(arquivo);
    } catch {
      return null;
    }
  }

  /** Preparação que nunca rejeita: falha vira o original. */
  private async prepararSeguro(registro: RegistroFila): Promise<FotoPreparada> {
    try {
      return await this.opcoes.preparar(registro.original, registro.cabecalho);
    } catch {
      return { arquivo: registro.original, reduzida: false };
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
