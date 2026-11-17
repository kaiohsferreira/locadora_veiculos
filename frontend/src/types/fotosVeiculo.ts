/**
 * Fotos do veículo (contrato veiculo-foto). Os campos espelham ReturnVeiculoFotoVO e FotoCapaVeiculoVO.
 * Nenhum campo de id de locadora, de funcionário ou de chave do objeto existe aqui: a API não devolve.
 */

/** Foto gravada do veículo (201 do envio e cada item da lista). */
export interface FotoDoVeiculo {
  idVeiculoFoto: number;
  /** Maior ou igual a 1. A menor ordem ativa é a capa. */
  ordem: number;
  /** URL temporária (até 900 s). Não guardar: a lista traz outra. */
  url: string;
  /** Reservado para a miniatura da fatia completa: nesta fatia sempre nulo. */
  urlMiniatura: string | null;
  /** Instante de validade da URL, em UTC com fuso. */
  expiraEm: string;
}

/** Capa do veículo nas listas. Não tem id: nenhuma rota pública conhece idFoto. */
export interface FotoCapaVeiculo {
  url: string;
  urlMiniatura: string | null;
  expiraEm: string;
}

/** Situação de uma foto na fila de envio do bloco (só existe no navegador). */
export type SituacaoItemFila = 'aguardando' | 'enviando' | 'concluida' | 'erro';

/** Foto escolhida pelo usuário, do momento da escolha até ser gravada. */
export interface ItemFilaFoto {
  /** Identificador local da fila (não é o idVeiculoFoto). */
  chave: number;
  /** Nome do arquivo escolhido. Sempre tratado como texto, nunca como HTML. */
  nome: string;
  /** Pré-visualização local (URL de objeto do navegador). */
  previa: string;
  situacao: SituacaoItemFila;
  /** De 0 a 100, pelo progresso do envio. */
  percentual: number;
  /** Mensagem pronta para exibir quando a situação é "erro"; nula nos outros casos. */
  erro: string | null;
  /** Foto gravada, quando a situação é "concluida". */
  foto: FotoDoVeiculo | null;
}
