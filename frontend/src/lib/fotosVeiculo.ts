/**
 * Funções puras das fotos do veículo: conferência antes do envio (só conveniência; o servidor decide),
 * vagas, ordem, capa, imagem exibida, próximo da fila e tradução dos erros da API.
 */
import type { FotoCapaVeiculo, FotoDoVeiculo, ItemFilaFoto } from '@/types/fotosVeiculo';
import { ErroApi } from '@/types/api';
import {
  MENSAGEM_FOTOS_ERRO_ARMAZENAMENTO,
  MENSAGEM_FOTOS_ERRO_DIMENSAO,
  MENSAGEM_FOTOS_ERRO_ENVIO,
  MENSAGEM_FOTOS_ERRO_TAMANHO,
  MENSAGEM_FOTOS_ERRO_TIPO,
  mensagemContadorFotos,
} from '@/lib/mensagensFotosVeiculo';

/** Teto de fotos ativas por veículo (espelho de ConstantsValues.MaximoFotosPorVeiculo do contrato). */
export const MAXIMO_FOTOS_POR_VEICULO = 15;
/** 10 MiB: teto de cada arquivo (espelho de ConstantsArmazenamento.TamanhoMaximoFotoBytes). */
export const TAMANHO_MAXIMO_FOTO_BYTES = 10_485_760;
/** Tipos de arquivo aceitos. */
export const TIPOS_DE_FOTO_ACEITOS: readonly string[] = ['image/jpeg', 'image/png'];
/** Maior lado aceito, em pixels. */
export const LADO_MAXIMO_FOTO_PIXELS = 8192;
/** Total de pixels aceito (largura x altura). */
export const TOTAL_MAXIMO_PIXELS_FOTO = 16_777_216;
/** Percentual que representa o envio completo. */
export const PERCENTUAL_MAXIMO = 100;

/** Status HTTP do armazenamento indisponível (502): tem texto próprio; os demais usam a mensagem do servidor. */
const STATUS_FALHA_INTEGRACAO = 502;

export type MotivoRecusaFoto = 'tipo' | 'tamanho' | 'dimensao';

/** Parte do arquivo usada na conferência (um File real ou um objeto com a mesma forma). */
export type ArquivoFotoConferido = Pick<File, 'type' | 'size'>;

/** Dimensões da imagem lidas da pré-visualização. */
export interface DimensoesFoto {
  largura: number;
  altura: number;
}

/** Tipo e tamanho do arquivo. Devolve o motivo da recusa ou nulo quando passa. */
export function validarArquivoFoto(arquivo: ArquivoFotoConferido): MotivoRecusaFoto | null {
  if (!TIPOS_DE_FOTO_ACEITOS.includes(arquivo.type)) return 'tipo';
  if (arquivo.size > TAMANHO_MAXIMO_FOTO_BYTES) return 'tamanho';
  return null;
}

/** Lado maior e total de pixels. Devolve o motivo da recusa ou nulo quando passa. */
export function validarDimensoesFoto(largura: number, altura: number): MotivoRecusaFoto | null {
  const ladoMaior = Math.max(largura, altura);
  if (ladoMaior > LADO_MAXIMO_FOTO_PIXELS) return 'dimensao';
  if (largura * altura > TOTAL_MAXIMO_PIXELS_FOTO) return 'dimensao';
  return null;
}

/** Quantas fotos ainda cabem: o teto menos as gravadas menos as que estão na fila. Nunca negativo. */
export function vagasRestantes(gravadas: number, naFila: number): number {
  return Math.max(0, MAXIMO_FOTOS_POR_VEICULO - gravadas - naFila);
}

/**
 * Vale descartar as fotos escolhidas quando o veículo mudou durante a análise. Só conta se havia veículo ao escolher:
 * no cadastro (sem id) as fotos ficam na fila e entram quando o id chega, então o id que surge depois não descarta.
 */
export function veiculoMudouDesdeAEscolha(idAoEscolher: number | null, idAgora: number | null): boolean {
  return idAoEscolher !== null && idAoEscolher !== idAgora;
}

/** "4/15 Fotos". */
export function formatarContadorFotos(total: number): string {
  return mensagemContadorFotos(total, MAXIMO_FOTOS_POR_VEICULO);
}

/** Ordena por ordem crescente e, no empate, pelo id. Não altera a lista recebida. */
export function ordenarFotos(fotos: readonly FotoDoVeiculo[]): FotoDoVeiculo[] {
  return [...fotos].sort((a, b) => a.ordem - b.ordem || a.idVeiculoFoto - b.idVeiculoFoto);
}

/**
 * Fotos gravadas que o bloco mostra: as da lista do veículo mais as que já foram gravadas neste lote e ainda não
 * estão na lista (a lista só é atualizada no fim do lote). Sem repetir a mesma foto; em ordem de exibição.
 */
export function fotosParaExibir(gravadas: readonly FotoDoVeiculo[], itens: readonly ItemFilaFoto[]): FotoDoVeiculo[] {
  const idsNaLista = new Set(gravadas.map((foto) => foto.idVeiculoFoto));
  const concluidasForaDaLista = itens.flatMap((item) =>
    item.situacao === 'concluida' && item.foto !== null && !idsNaLista.has(item.foto.idVeiculoFoto) ? [item.foto] : [],
  );
  return ordenarFotos([...gravadas, ...concluidasForaDaLista]);
}

/** Primeira foto na ordem de exibição (a capa), ou nulo quando não há foto. */
export function primeiraFoto(fotos: readonly FotoDoVeiculo[]): FotoDoVeiculo | null {
  const ordenadas = ordenarFotos(fotos);
  return ordenadas.length > 0 ? ordenadas[0] : null;
}

/** Miniatura quando existir; senão a imagem comum. */
export function urlDaImagem(foto: Pick<FotoCapaVeiculo, 'url' | 'urlMiniatura'>): string {
  return foto.urlMiniatura ?? foto.url;
}

/**
 * URL que a imagem deve tentar abrir. Sem URL, ou quando a URL já falhou (ex.: vencida), devolve nulo:
 * a tela mostra o ícone de carro no lugar da foto.
 */
export function urlImagemExibida(url: string | null | undefined, urlComFalha: string | null): string | null {
  if (!url) return null;
  return url === urlComFalha ? null : url;
}

/**
 * Primeira mensagem de erro de foto de capa entre os itens de uma lista (erroFotoCapa do contrato reserva), ou nulo.
 * Texto vazio ou só com espaços não conta. A tela mostra uma única mensagem, a que o servidor mandou, sem texto novo.
 */
export function primeiraMensagemErroFotoCapa(itens: readonly { erroFotoCapa?: string | null }[]): string | null {
  const primeiro = itens.find((item) => (item.erroFotoCapa ?? '').trim() !== '');
  return primeiro?.erroFotoCapa ?? null;
}

/**
 * Próximo item a enviar: o primeiro "aguardando", só quando nenhum está "enviando".
 * Erro não entra de novo sozinho; foto concluída nunca volta.
 */
export function proximoDaFila(itens: readonly ItemFilaFoto[]): ItemFilaFoto | null {
  if (itens.some((item) => item.situacao === 'enviando')) return null;
  return itens.find((item) => item.situacao === 'aguardando') ?? null;
}

/** Números do lote em andamento: quantas fotos já foram gravadas, quantas faltam e se há envio agora. */
export interface ResumoFila {
  emEnvio: boolean;
  /** Posição da foto em envio dentro do lote (1 a total). */
  atual: number;
  /** Fotos do lote: gravadas, em envio e aguardando (erro fica de fora). */
  total: number;
  /** Fotos ainda não gravadas na fila (aguardando, enviando e com erro). */
  naFila: number;
  concluidas: number;
}

/** Resumo da fila a partir dos itens. Fotos concluídas ficam no lote até a lista ser atualizada. */
export function resumoFila(itens: readonly ItemFilaFoto[]): ResumoFila {
  const concluidas = itens.filter((item) => item.situacao === 'concluida').length;
  const enviando = itens.filter((item) => item.situacao === 'enviando').length;
  const aguardando = itens.filter((item) => item.situacao === 'aguardando').length;
  const erros = itens.filter((item) => item.situacao === 'erro').length;
  return {
    emEnvio: enviando > 0,
    atual: concluidas + 1,
    total: concluidas + enviando + aguardando,
    naFila: enviando + aguardando + erros,
    concluidas,
  };
}

/** Texto da recusa de um arquivo, pelo motivo. */
export function mensagemRecusaFoto(motivo: MotivoRecusaFoto): string {
  switch (motivo) {
    case 'tipo':
      return MENSAGEM_FOTOS_ERRO_TIPO;
    case 'tamanho':
      return MENSAGEM_FOTOS_ERRO_TAMANHO;
    case 'dimensao':
      return MENSAGEM_FOTOS_ERRO_DIMENSAO;
  }
}

/**
 * Texto do erro de um envio. Erro que não é da API (sem resposta, cancelado ou inesperado) vira o texto
 * de envio. Armazenamento indisponível (502) tem texto próprio. Os demais (400, 404, 409, 429, 500,
 * falha de rede e tempo esgotado) usam a mensagem que o cliente HTTP já filtrou.
 */
export function mensagemErroEnvioFoto(erro: unknown): string {
  if (!(erro instanceof ErroApi)) return MENSAGEM_FOTOS_ERRO_ENVIO;
  if (erro.status === STATUS_FALHA_INTEGRACAO) return MENSAGEM_FOTOS_ERRO_ARMAZENAMENTO;
  return erro.mensagem;
}
