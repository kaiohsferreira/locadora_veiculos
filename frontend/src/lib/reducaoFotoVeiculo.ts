/**
 * Redução da foto no navegador antes do envio: JPEG com lado maior de até 1600 px, fundo branco e qualidade 0,90.
 * Quem não puder reduzir (navegador sem suporte, cabeçalho ilegível, falha em qualquer passo) envia o original,
 * como hoje. O servidor valida e recodifica o que chegar, reduzido ou não. Só `criarAmbienteReducaoDoNavegador`
 * toca o navegador: o resto recebe o ambiente de fora, para o teste rodar sem canvas.
 */
import { dimensoesOrientadas, type CabecalhoFoto } from '@/lib/cabecalhoFoto';
import type { DimensoesFoto } from '@/lib/fotosVeiculo';

/** Espelho de ConstantsValues.LadoMaximoFotoVeiculoPixels no servidor. */
export const LADO_MAXIMO_FOTO_ENVIADA_PIXELS = 1600;
export const QUALIDADE_JPEG_FOTO_ENVIADA = 0.9;
/** Opção resizeQuality do createImageBitmap: sem ela o navegador pode reamostrar mal (placa e texto serrilhados). */
export const QUALIDADE_REDIMENSIONAMENTO_FOTO = 'high' as const;
export const COR_FUNDO_FOTO_ENVIADA = '#ffffff';
export const TIPO_FOTO_ENVIADA = 'image/jpeg';
/** Diferença mínima entre vermelho e azul (0 a 255) para a sonda de orientação considerar o pixel colorido. */
export const LIMIAR_DIFERENCA_COR_SONDA = 64;

/** Imagem de teste A (PNG 2x2) para a sonda de opções. Gerada no teste, sem arquivo de foto real no repositório. */
export const IMAGEM_TESTE_SONDA_A: Uint8Array<ArrayBuffer> = new Uint8Array([
  0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a, 0x00, 0x00, 0x00, 0x0d, 0x49, 0x48, 0x44, 0x52,
  0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x02, 0x08, 0x02, 0x00, 0x00, 0x00, 0xfd, 0xd4, 0x9a,
  0x73, 0x00, 0x00, 0x00, 0x14, 0x49, 0x44, 0x41, 0x54, 0x78, 0x9c, 0x63, 0xf8, 0xcf, 0xc0, 0xc0,
  0x00, 0xc2, 0x0c, 0xff, 0xff, 0xff, 0xff, 0x0f, 0x00, 0x1f, 0xee, 0x05, 0xfb, 0xb0, 0xb8, 0x97,
  0xc9, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4e, 0x44, 0xae, 0x42, 0x60, 0x82,
]);

/**
 * Imagem de teste B (JPEG 16x8, 4:4:4, com Exif de orientação 6): metade esquerda vermelha, direita azul. Com a
 * orientação aplicada fica 8x16, vermelha em cima e azul embaixo. Conferida por decodificação (cores e dimensões).
 */
export const IMAGEM_TESTE_SONDA_B: Uint8Array<ArrayBuffer> = new Uint8Array([
  0xff, 0xd8, 0xff, 0xe1, 0x00, 0x22, 0x45, 0x78, 0x69, 0x66, 0x00, 0x00, 0x49, 0x49, 0x2a, 0x00,
  0x08, 0x00, 0x00, 0x00, 0x01, 0x00, 0x12, 0x01, 0x03, 0x00, 0x01, 0x00, 0x00, 0x00, 0x06, 0x00,
  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xff, 0xdb, 0x00, 0x43, 0x00, 0x01, 0x01, 0x01, 0x01, 0x01,
  0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
  0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
  0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
  0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0xff, 0xc0, 0x00, 0x11, 0x08,
  0x00, 0x08, 0x00, 0x10, 0x03, 0x01, 0x11, 0x00, 0x02, 0x11, 0x00, 0x03, 0x11, 0x00, 0xff, 0xc4,
  0x00, 0x16, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
  0x00, 0x00, 0x00, 0x09, 0x0a, 0x0b, 0xff, 0xc4, 0x00, 0x14, 0x10, 0x01, 0x00, 0x00, 0x00, 0x00,
  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xff, 0xda, 0x00, 0x0c,
  0x03, 0x01, 0x00, 0x02, 0x00, 0x03, 0x00, 0x00, 0x3f, 0x00, 0x0b, 0xe1, 0x4e, 0x7f, 0x80, 0x87,
  0x55, 0x41, 0x35, 0xf7, 0xff, 0xd9,
]);

/** Suporte do navegador à redução, detectado uma vez por carga da página. */
export interface SuporteReducao {
  reduz: boolean;
  aplicaOrientacao: boolean;
}

export type MotivoSemReducao =
  | 'navegador_sem_suporte'
  | 'sem_cabecalho'
  | 'tipo_divergente'
  | 'orientacao_desconhecida'
  | 'orientacao_sem_suporte'
  | 'jpeg_pequeno';

export interface DecisaoEnviarOriginal {
  acao: 'enviar_original';
  motivo: MotivoSemReducao;
}

export interface DecisaoReduzir {
  acao: 'reduzir';
  largura: number;
  altura: number;
  redimensiona: boolean;
}

export type DecisaoPreparo = DecisaoEnviarOriginal | DecisaoReduzir;

/** Foto pronta para o envio: a reduzida (`reduzida` verdadeiro) ou o próprio original. */
export interface FotoPreparada {
  arquivo: File;
  reduzida: boolean;
}

/** Parte do ImageBitmap usada aqui (largura, altura e liberação da memória). */
export interface BitmapDecodificado {
  width: number;
  height: number;
  close(): void;
}

/** Chamadas ao navegador, atrás de uma interface para o teste injetar uma versão falsa. */
export interface AmbienteReducao {
  criarBitmap(arquivo: Blob, opcoes: ImageBitmapOptions): Promise<BitmapDecodificado>;
  codificarJpeg(
    bitmap: BitmapDecodificado,
    largura: number,
    altura: number,
    corFundo: string,
    qualidade: number,
  ): Promise<Blob | null>;
  lerCorDoPixel(bitmap: BitmapDecodificado, x: number, y: number): [number, number, number] | null;
  criarArquivo(partes: BlobPart[], nome: string, opcoes: FilePropertyBag): File;
}

/** Contexto de desenho usado na redução (canvas comum ou offscreen). */
type ContextoDesenho = Pick<CanvasRenderingContext2D, 'fillStyle' | 'fillRect' | 'drawImage'>;
/** Contexto de leitura de pixels (um canvas de 1x1). */
type ContextoCor = Pick<CanvasRenderingContext2D, 'drawImage' | 'getImageData'>;

function suporteSemRecurso(): SuporteReducao {
  return { reduz: false, aplicaOrientacao: false };
}

function enviarOriginal(motivo: MotivoSemReducao): DecisaoEnviarOriginal {
  return { acao: 'enviar_original', motivo };
}

/** Lado escalado, arredondado para o inteiro mais próximo (metade para cima) e nunca menor que 1. */
function escalarLado(lado: number, ladoMaximo: number, maiorLado: number): number {
  const arredondado = Math.floor((2 * lado * ladoMaximo + maiorLado) / (2 * maiorLado));
  return Math.max(1, arredondado);
}

/**
 * Dimensões de saída: mantém a proporção, deixa o lado maior em até `ladoMaximo`, nunca amplia e nenhum lado sai
 * menor que 1. Espelho de RegrasVeiculoFoto.CalcularDimensoesDeSaida no servidor.
 */
export function calcularDimensoesReduzidas(largura: number, altura: number, ladoMaximo: number): DimensoesFoto {
  for (const valor of [largura, altura, ladoMaximo]) {
    if (!Number.isInteger(valor) || valor < 1) throw new RangeError();
  }
  const maiorLado = Math.max(largura, altura);
  if (maiorLado <= ladoMaximo) return { largura, altura };
  return {
    largura: escalarLado(largura, ladoMaximo, maiorLado),
    altura: escalarLado(altura, ladoMaximo, maiorLado),
  };
}

function formatoEsperadoDoTipo(tipo: string): CabecalhoFoto['formato'] | null {
  if (tipo === 'image/jpeg') return 'jpeg';
  if (tipo === 'image/png') return 'png';
  return null;
}

/**
 * Escolhe entre reduzir e enviar o original, na ordem: sem suporte, sem cabeçalho, tipo divergente, orientação
 * desconhecida, orientação sem suporte, JPEG pequeno (o servidor recodifica e tira o EXIF de qualquer jeito).
 * PNG sempre passa pela redução, mesmo pequeno (vira JPEG sobre fundo branco).
 */
export function decidirPreparo(
  tipo: string,
  cabecalho: CabecalhoFoto | null,
  suporte: SuporteReducao,
): DecisaoPreparo {
  if (!suporte.reduz) return enviarOriginal('navegador_sem_suporte');
  if (cabecalho === null) return enviarOriginal('sem_cabecalho');
  if (cabecalho.formato !== formatoEsperadoDoTipo(tipo)) return enviarOriginal('tipo_divergente');
  if (cabecalho.orientacao === null) return enviarOriginal('orientacao_desconhecida');
  if (cabecalho.orientacao !== 1 && !suporte.aplicaOrientacao) return enviarOriginal('orientacao_sem_suporte');
  const maiorLadoOriginal = Math.max(cabecalho.largura, cabecalho.altura);
  if (tipo === 'image/jpeg' && maiorLadoOriginal <= LADO_MAXIMO_FOTO_ENVIADA_PIXELS) {
    return enviarOriginal('jpeg_pequeno');
  }
  const orientadas = dimensoesOrientadas(cabecalho);
  const saida = calcularDimensoesReduzidas(orientadas.largura, orientadas.altura, LADO_MAXIMO_FOTO_ENVIADA_PIXELS);
  return {
    acao: 'reduzir',
    largura: saida.largura,
    altura: saida.altura,
    redimensiona: Math.max(orientadas.largura, orientadas.altura) > LADO_MAXIMO_FOTO_ENVIADA_PIXELS,
  };
}

/** Nome do arquivo reduzido: extensão trocada por .jpg; sem nome, "foto.jpg". O servidor não usa o nome. */
export function nomeDoArquivoReduzido(nome: string): string {
  const base = nome.replace(/\.[^.]*$/, '');
  return `${base.length > 0 ? base : 'foto'}.jpg`;
}

/** Reduz a foto com o ambiente. Devolve nulo quando o resultado não serve (o chamador envia o original). */
async function reduzirFoto(
  arquivo: File,
  decisao: DecisaoReduzir,
  ambiente: AmbienteReducao,
): Promise<File | null> {
  const opcoes: ImageBitmapOptions = {
    imageOrientation: 'from-image',
    resizeQuality: QUALIDADE_REDIMENSIONAMENTO_FOTO,
  };
  if (decisao.redimensiona) {
    opcoes.resizeWidth = decisao.largura;
    opcoes.resizeHeight = decisao.altura;
  }
  const bitmap = await ambiente.criarBitmap(arquivo, opcoes);
  try {
    if (bitmap.width !== decisao.largura || bitmap.height !== decisao.altura) return null;
    const jpeg = await ambiente.codificarJpeg(
      bitmap,
      decisao.largura,
      decisao.altura,
      COR_FUNDO_FOTO_ENVIADA,
      QUALIDADE_JPEG_FOTO_ENVIADA,
    );
    if (jpeg === null || jpeg.size === 0 || jpeg.type !== TIPO_FOTO_ENVIADA || jpeg.size >= arquivo.size) return null;
    return ambiente.criarArquivo([jpeg], nomeDoArquivoReduzido(arquivo.name), {
      type: TIPO_FOTO_ENVIADA,
      lastModified: arquivo.lastModified,
    });
  } finally {
    bitmap.close();
  }
}

/**
 * Prepara a foto para o envio. Nunca rejeita: qualquer falha, em qualquer passo, devolve o original. Quando a
 * decisão é reduzir, o bitmap é sempre fechado.
 */
export async function prepararFotoParaEnvio(
  arquivo: File,
  cabecalho: CabecalhoFoto | null,
  suporte: SuporteReducao,
  ambiente: AmbienteReducao | null,
): Promise<FotoPreparada> {
  const original: FotoPreparada = { arquivo, reduzida: false };
  try {
    const decisao = decidirPreparo(arquivo.type, cabecalho, suporte);
    if (decisao.acao === 'enviar_original' || ambiente === null) return original;
    const reduzido = await reduzirFoto(arquivo, decisao, ambiente);
    return reduzido === null ? original : { arquivo: reduzido, reduzida: true };
  } catch {
    return original;
  }
}

/** Guarda a promessa da primeira detecção: a sonda roda uma vez por carga da página. */
export function memorizarSuporteReducao(detectar: () => Promise<SuporteReducao>): () => Promise<SuporteReducao> {
  let promessa: Promise<SuporteReducao> | null = null;
  return () => {
    if (promessa === null) promessa = detectar();
    return promessa;
  };
}

/** Registra a leitura de cada campo das opções: o navegador que não lê um campo não reduz. */
function registrarLeitura(
  opcoes: ImageBitmapOptions,
  campo: string,
  valor: string | number,
  lidos: Set<string>,
): void {
  Object.defineProperty(opcoes, campo, {
    enumerable: true,
    get: () => {
      lidos.add(campo);
      return valor;
    },
  });
}

/** Sonda 1: o navegador lê as quatro opções e entrega o tamanho pedido. */
async function sondaOpcoes(ambiente: AmbienteReducao): Promise<boolean> {
  const lidos = new Set<string>();
  const opcoes: ImageBitmapOptions = {};
  registrarLeitura(opcoes, 'imageOrientation', 'from-image', lidos);
  registrarLeitura(opcoes, 'resizeWidth', 1, lidos);
  registrarLeitura(opcoes, 'resizeHeight', 1, lidos);
  registrarLeitura(opcoes, 'resizeQuality', QUALIDADE_REDIMENSIONAMENTO_FOTO, lidos);
  const bitmap = await ambiente.criarBitmap(new Blob([IMAGEM_TESTE_SONDA_A], { type: 'image/png' }), opcoes);
  try {
    return lidos.size === 4 && bitmap.width === 1 && bitmap.height === 1;
  } finally {
    bitmap.close();
  }
}

/**
 * Sonda 2: as MESMAS opções do caminho real (orientação e redimensionamento juntos). A imagem B é 16x8 com
 * orientação 6: o resultado 4x8 tem vermelho em cima e azul embaixo só se o navegador girar antes de reduzir.
 */
async function sondaOrientacao(ambiente: AmbienteReducao): Promise<boolean> {
  const opcoes: ImageBitmapOptions = {
    imageOrientation: 'from-image',
    resizeWidth: 4,
    resizeHeight: 8,
    resizeQuality: QUALIDADE_REDIMENSIONAMENTO_FOTO,
  };
  const bitmap = await ambiente.criarBitmap(new Blob([IMAGEM_TESTE_SONDA_B], { type: 'image/jpeg' }), opcoes);
  try {
    if (bitmap.width !== 4 || bitmap.height !== 8) return false;
    const cima = ambiente.lerCorDoPixel(bitmap, 1, 1);
    const baixo = ambiente.lerCorDoPixel(bitmap, 1, 6);
    if (cima === null || baixo === null) return false;
    const vermelhoEmCima = cima[0] - cima[2] >= LIMIAR_DIFERENCA_COR_SONDA;
    const azulEmBaixo = baixo[2] - baixo[0] >= LIMIAR_DIFERENCA_COR_SONDA;
    return vermelhoEmCima && azulEmBaixo;
  } finally {
    bitmap.close();
  }
}

/**
 * Detecta o que o navegador faz. Sem ambiente, ou em qualquer erro, nada é reduzido. `aplicaOrientacao` só vale
 * quando `reduz` vale. Nunca rejeita.
 */
export async function detectarSuporteReducao(ambiente: AmbienteReducao | null): Promise<SuporteReducao> {
  if (ambiente === null) return suporteSemRecurso();
  try {
    if (!(await sondaOpcoes(ambiente))) return suporteSemRecurso();
    return { reduz: true, aplicaOrientacao: await sondaOrientacao(ambiente) };
  } catch {
    return suporteSemRecurso();
  }
}

/**
 * Verdadeiro quando a tela é um OffscreenCanvas. O identificador global só é lido quando o navegador o tem: sem ele
 * (Safari anterior ao 16.4) o `instanceof` lançaria ReferenceError e a redução nunca chegaria ao canvas comum.
 */
export function ehOffscreenCanvas(
  tela: OffscreenCanvas | HTMLCanvasElement,
  navegadorTemOffscreen: boolean,
): tela is OffscreenCanvas {
  return navegadorTemOffscreen && tela instanceof OffscreenCanvas;
}

function criarCanvas(largura: number, altura: number): OffscreenCanvas | HTMLCanvasElement {
  if (typeof OffscreenCanvas === 'function') return new OffscreenCanvas(largura, altura);
  const canvas = document.createElement('canvas');
  canvas.width = largura;
  canvas.height = altura;
  return canvas;
}

/** Libera a memória do canvas: zerar o tamanho descarta o bitmap sem esperar o coletor de lixo. */
function liberarTela(tela: OffscreenCanvas | HTMLCanvasElement): void {
  tela.width = 0;
  tela.height = 0;
}

/** Fundo branco primeiro, depois a imagem no tamanho de saída. */
function desenharComFundo(
  contexto: ContextoDesenho,
  bitmap: ImageBitmap,
  largura: number,
  altura: number,
  corFundo: string,
): void {
  contexto.fillStyle = corFundo;
  contexto.fillRect(0, 0, largura, altura);
  contexto.drawImage(bitmap, 0, 0, largura, altura);
}

async function codificarJpegNoNavegador(
  bitmap: BitmapDecodificado,
  largura: number,
  altura: number,
  corFundo: string,
  qualidade: number,
): Promise<Blob | null> {
  if (!(bitmap instanceof ImageBitmap)) return null;
  const tela = criarCanvas(largura, altura);
  try {
    if (ehOffscreenCanvas(tela, typeof OffscreenCanvas === 'function')) {
      const contexto = tela.getContext('2d');
      if (contexto === null) return null;
      desenharComFundo(contexto, bitmap, largura, altura, corFundo);
      return await tela.convertToBlob({ type: TIPO_FOTO_ENVIADA, quality: qualidade });
    }
    const contexto = tela.getContext('2d');
    if (contexto === null) return null;
    desenharComFundo(contexto, bitmap, largura, altura, corFundo);
    return await new Promise<Blob | null>((resolver) => {
      tela.toBlob((blob) => resolver(blob), TIPO_FOTO_ENVIADA, qualidade);
    });
  } finally {
    liberarTela(tela);
  }
}

function lerCorDoPixelNoNavegador(
  bitmap: BitmapDecodificado,
  x: number,
  y: number,
): [number, number, number] | null {
  if (!(bitmap instanceof ImageBitmap)) return null;
  let tela: OffscreenCanvas | HTMLCanvasElement | null = null;
  try {
    tela = criarCanvas(1, 1);
    const contexto: ContextoCor | null = tela.getContext('2d');
    if (contexto === null) return null;
    contexto.drawImage(bitmap, x, y, 1, 1, 0, 0, 1, 1);
    const dados = contexto.getImageData(0, 0, 1, 1).data;
    return [dados[0], dados[1], dados[2]];
  } catch {
    return null;
  } finally {
    if (tela !== null) liberarTela(tela);
  }
}

/**
 * Ambiente real do navegador, ou nulo quando falta createImageBitmap ou canvas. Único ponto que toca o navegador
 * (sem teste de unidade: é coberto pelo roteiro de QA no navegador).
 */
export function criarAmbienteReducaoDoNavegador(): AmbienteReducao | null {
  if (typeof createImageBitmap !== 'function') return null;
  if (typeof OffscreenCanvas !== 'function' && typeof document === 'undefined') return null;
  return {
    criarBitmap: (arquivo, opcoes) => createImageBitmap(arquivo, opcoes),
    codificarJpeg: codificarJpegNoNavegador,
    lerCorDoPixel: lerCorDoPixelNoNavegador,
    criarArquivo: (partes, nome, opcoes) => new File(partes, nome, opcoes),
  };
}
