/**
 * Cabeçalho da foto (JPEG ou PNG) lido dos primeiros bytes do arquivo, sem decodificar a imagem: formato, largura,
 * altura e orientação (Exif do JPEG). Serve para conferir as dimensões antes de qualquer decodificação e para decidir
 * se a foto pode ser reduzida no navegador. Nunca lança: o que não se entende vira nulo.
 */
import type { DimensoesFoto } from '@/lib/fotosVeiculo';

/** Bytes lidos do início do arquivo: bastam para o cabeçalho e para o Exif da câmera. */
export const BYTES_LIDOS_DO_CABECALHO = 1_048_576;

/** Orientação do Exif: 1 é normal; de 5 a 8 a imagem sai girada (largura e altura trocam). */
export type OrientacaoFoto = 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8;

/** Dados do cabeçalho. Largura e altura como estão gravadas; orientação nula quando não foi possível saber. */
export interface CabecalhoFoto {
  formato: 'jpeg' | 'png';
  largura: number;
  altura: number;
  orientacao: OrientacaoFoto | null;
}

type LeituraExif = OrientacaoFoto | 'sem_marca' | 'malformado';

const ORIENTACOES: readonly OrientacaoFoto[] = [1, 2, 3, 4, 5, 6, 7, 8];
const ASSINATURA_PNG: readonly number[] = [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a];
/** "Exif" seguido de dois bytes zero, no início do segmento APP1. */
const ASSINATURA_EXIF: readonly number[] = [0x45, 0x78, 0x69, 0x66, 0x00, 0x00];
const MAIOR_DIMENSAO_PNG = 2_147_483_647;
const MARCADOR_APP1 = 0xe1;
const MARCADOR_INICIO_DADOS = 0xda;
const MARCADOR_FIM = 0xd9;
const TAG_ORIENTACAO = 0x0112;
const TIPO_SHORT = 3;

function temBytes(bytes: Uint8Array, posicao: number, quantidade: number): boolean {
  return posicao >= 0 && posicao + quantidade <= bytes.length;
}

/** Lê 16 bits. Quem chama confere os limites antes (temBytes). */
function lerU16(bytes: Uint8Array, posicao: number, littleEndian: boolean): number {
  const primeiro = bytes[posicao];
  const segundo = bytes[posicao + 1];
  return littleEndian ? primeiro + segundo * 256 : primeiro * 256 + segundo;
}

/** Lê 32 bits sem sinal. Quem chama confere os limites antes (temBytes). */
function lerU32(bytes: Uint8Array, posicao: number, littleEndian: boolean): number {
  const b0 = bytes[posicao];
  const b1 = bytes[posicao + 1];
  const b2 = bytes[posicao + 2];
  const b3 = bytes[posicao + 3];
  return littleEndian
    ? b0 + b1 * 256 + b2 * 65536 + b3 * 16777216
    : b0 * 16777216 + b1 * 65536 + b2 * 256 + b3;
}

function lerTexto(bytes: Uint8Array, posicao: number, quantidade: number): string {
  return String.fromCharCode(...bytes.subarray(posicao, posicao + quantidade));
}

function paraOrientacao(valor: number): OrientacaoFoto | null {
  return ORIENTACOES.find((orientacao) => orientacao === valor) ?? null;
}

function ehPng(bytes: Uint8Array): boolean {
  return temBytes(bytes, 0, ASSINATURA_PNG.length) && ASSINATURA_PNG.every((byte, indice) => bytes[indice] === byte);
}

function ehJpeg(bytes: Uint8Array): boolean {
  return temBytes(bytes, 0, 2) && bytes[0] === 0xff && bytes[1] === 0xd8;
}

function dimensaoPngValida(valor: number): boolean {
  return valor > 0 && valor <= MAIOR_DIMENSAO_PNG;
}

/**
 * PNG: primeiro bloco IHDR (comprimento 13) com largura e altura de 32 bits. A orientação é 1 quando o primeiro
 * IDAT aparece antes de qualquer eXIf; nula quando há eXIf antes ou quando o IDAT não cabe nos bytes lidos.
 */
function lerCabecalhoPng(bytes: Uint8Array): CabecalhoFoto | null {
  if (!temBytes(bytes, 0, 24)) return null;
  if (lerU32(bytes, 8, false) !== 13 || lerTexto(bytes, 12, 4) !== 'IHDR') return null;
  const largura = lerU32(bytes, 16, false);
  const altura = lerU32(bytes, 20, false);
  if (!dimensaoPngValida(largura) || !dimensaoPngValida(altura)) return null;
  return { formato: 'png', largura, altura, orientacao: orientacaoPng(bytes) };
}

function orientacaoPng(bytes: Uint8Array): OrientacaoFoto | null {
  let posicao = 8;
  while (temBytes(bytes, posicao, 8)) {
    const comprimento = lerU32(bytes, posicao, false);
    const tipo = lerTexto(bytes, posicao + 4, 4);
    if (tipo === 'eXIf') return null;
    if (tipo === 'IDAT') return 1;
    posicao += 12 + comprimento;
  }
  return null;
}

/** Marcadores de início de quadro (SOF): C4, C8 e CC não são quadros. */
function ehMarcadorQuadro(marcador: number): boolean {
  return (
    (marcador >= 0xc0 && marcador <= 0xc3) ||
    (marcador >= 0xc5 && marcador <= 0xc7) ||
    (marcador >= 0xc9 && marcador <= 0xcb) ||
    (marcador >= 0xcd && marcador <= 0xcf)
  );
}

function ehExif(bytes: Uint8Array, inicio: number, fim: number): boolean {
  return fim - inicio >= ASSINATURA_EXIF.length && ASSINATURA_EXIF.every((byte, indice) => bytes[inicio + indice] === byte);
}

/** Lê a marca de orientação (tag 0x0112, tipo 3, contagem 1) do primeiro diretório do cabeçalho TIFF do Exif. */
function lerOrientacaoExif(bytes: Uint8Array, tiff: number, fim: number): LeituraExif {
  if (tiff + 8 > fim) return 'malformado';
  const primeiroByte = bytes[tiff];
  const segundoByte = bytes[tiff + 1];
  let littleEndian: boolean;
  if (primeiroByte === 0x49 && segundoByte === 0x49) littleEndian = true;
  else if (primeiroByte === 0x4d && segundoByte === 0x4d) littleEndian = false;
  else return 'malformado';
  if (lerU16(bytes, tiff + 2, littleEndian) !== 42) return 'malformado';
  const diretorio = tiff + lerU32(bytes, tiff + 4, littleEndian);
  if (diretorio + 2 > fim) return 'malformado';
  const quantidade = lerU16(bytes, diretorio, littleEndian);
  if (diretorio + 2 + quantidade * 12 > fim) return 'malformado';
  for (let indice = 0; indice < quantidade; indice += 1) {
    const entrada = diretorio + 2 + indice * 12;
    if (lerU16(bytes, entrada, littleEndian) !== TAG_ORIENTACAO) continue;
    const tipo = lerU16(bytes, entrada + 2, littleEndian);
    const contagem = lerU32(bytes, entrada + 4, littleEndian);
    if (tipo !== TIPO_SHORT || contagem !== 1) return 'sem_marca';
    return paraOrientacao(lerU16(bytes, entrada + 8, littleEndian)) ?? 'malformado';
  }
  return 'sem_marca';
}

/**
 * JPEG: percorre os segmentos pelo comprimento (pulando bytes de preenchimento FF) até o quadro SOF. Antes dele,
 * cada Exif pode trazer a orientação; dois Exif que discordam, ou um malformado, deixam a orientação nula.
 */
function lerCabecalhoJpeg(bytes: Uint8Array): CabecalhoFoto | null {
  let orientacaoExif: OrientacaoFoto | null = null;
  let exifInvalido = false;
  let posicao = 2;
  while (posicao < bytes.length) {
    if (bytes[posicao] !== 0xff) return null;
    while (posicao < bytes.length && bytes[posicao] === 0xff) posicao += 1;
    if (posicao >= bytes.length) return null;
    const marcador = bytes[posicao];
    posicao += 1;
    if (marcador === 0x00 || marcador === MARCADOR_INICIO_DADOS || marcador === MARCADOR_FIM) return null;
    if (marcador === 0x01 || (marcador >= 0xd0 && marcador <= 0xd7)) continue;
    if (!temBytes(bytes, posicao, 2)) return null;
    const comprimento = lerU16(bytes, posicao, false);
    if (comprimento < 2 || !temBytes(bytes, posicao, comprimento)) return null;
    const inicio = posicao + 2;
    const fim = posicao + comprimento;
    if (ehMarcadorQuadro(marcador)) {
      if (comprimento < 8) return null;
      const altura = lerU16(bytes, inicio + 1, false);
      const largura = lerU16(bytes, inicio + 3, false);
      if (largura === 0 || altura === 0 || exifInvalido) return null;
      return { formato: 'jpeg', largura, altura, orientacao: orientacaoExif ?? 1 };
    }
    if (marcador === MARCADOR_APP1 && ehExif(bytes, inicio, fim)) {
      const leitura = lerOrientacaoExif(bytes, inicio + ASSINATURA_EXIF.length, fim);
      if (leitura === 'malformado') {
        exifInvalido = true;
      } else if (leitura !== 'sem_marca') {
        if (orientacaoExif !== null && orientacaoExif !== leitura) exifInvalido = true;
        orientacaoExif = leitura;
      }
    }
    posicao = fim;
  }
  return null;
}

/** Lê o cabeçalho de uma foto a partir dos seus bytes iniciais. Nunca lança: o que não se entende vira nulo. */
export function lerCabecalhoFoto(bytes: Uint8Array): CabecalhoFoto | null {
  try {
    if (ehPng(bytes)) return lerCabecalhoPng(bytes);
    if (ehJpeg(bytes)) return lerCabecalhoJpeg(bytes);
    return null;
  } catch {
    return null;
  }
}

/**
 * Lê só o início do arquivo (até BYTES_LIDOS_DO_CABECALHO bytes) e devolve o cabeçalho. Qualquer falha de leitura,
 * ou navegador sem `Blob.arrayBuffer`, devolve nulo. Nunca rejeita. `lerBytes` é injetável para o teste.
 */
export async function lerCabecalhoDoArquivo(
  arquivo: Blob,
  lerBytes: (parte: Blob) => Promise<ArrayBuffer> = (parte) => parte.arrayBuffer(),
): Promise<CabecalhoFoto | null> {
  try {
    const bytes = await lerBytes(arquivo.slice(0, BYTES_LIDOS_DO_CABECALHO));
    return lerCabecalhoFoto(new Uint8Array(bytes));
  } catch {
    return null;
  }
}

/** Largura e altura como aparecem depois da orientação do Exif (5 a 8 trocam). */
export function dimensoesOrientadas(cabecalho: CabecalhoFoto): DimensoesFoto {
  const girada = cabecalho.orientacao !== null && cabecalho.orientacao >= 5 && cabecalho.orientacao <= 8;
  return girada
    ? { largura: cabecalho.altura, altura: cabecalho.largura }
    : { largura: cabecalho.largura, altura: cabecalho.altura };
}
