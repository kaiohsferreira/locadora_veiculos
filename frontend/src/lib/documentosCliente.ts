/**
 * Documentos do cliente (tela 21): regras puras, sem React e sem API.
 * A conferência do navegador é só conveniência: o backend decide em cada envio (400 para o que ele recusa).
 * O arquivo é enviado como veio: nada aqui reduz, regrava nem tira dados da imagem.
 */
import { somenteDigitos } from '@/lib/formatters';
import {
  MENSAGEM_DOCUMENTO_JA_ENVIADOS,
  MENSAGEM_DOCUMENTO_MODO_SO_ENVIAR,
  MENSAGEM_DOCUMENTO_RECUSA_FORMATO,
  MENSAGEM_DOCUMENTO_RECUSA_TAMANHO,
  MENSAGEM_DOCUMENTO_RECUSA_VAZIO,
} from '@/lib/mensagensCadastro';
import {
  TIPOS_DOCUMENTO_CLIENTE,
  type DocumentoClienteGravado,
  type TipoConteudoDocumento,
  type TipoDocumentoCliente,
} from '@/types/documentosCliente';

/** Limite do arquivo (contrato: 10.485.760 bytes, o mesmo das fotos). */
export const TAMANHO_MAXIMO_DOCUMENTO_BYTES = 10_485_760;

/** Quantidade de bytes do começo do arquivo que a conferência lê (a maior assinatura tem 8). */
export const TAMANHO_CABECALHO_DOCUMENTO_BYTES = 8;

/** Tipos declarados aceitos (o backend recusa qualquer outro com 400). */
export const TIPOS_DOCUMENTO_ACEITOS: readonly string[] = ['application/pdf', 'image/jpeg', 'image/png'];

/** Arquivo escolhido por tipo de documento (só na memória do navegador). */
export type ArquivosDocumento = Partial<Record<TipoDocumentoCliente, File>>;

/** Motivo da recusa no navegador (a mensagem vem de `mensagemRecusaDocumento`). */
export type MotivoRecusaDocumento = 'vazio' | 'tamanho' | 'formato';

/** Assinaturas dos primeiros bytes de cada formato aceito. */
const ASSINATURAS: readonly { tipo: TipoConteudoDocumento; bytes: readonly number[] }[] = [
  { tipo: 'image/jpeg', bytes: [0xff, 0xd8, 0xff] },
  { tipo: 'image/png', bytes: [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a] },
  // "%PDF-"
  { tipo: 'application/pdf', bytes: [0x25, 0x50, 0x44, 0x46, 0x2d] },
];

/** Tipo do conteúdo pelos primeiros bytes, ou null quando não é JPEG, PNG nem PDF. */
export function detectarTipoDocumento(cabecalho: Uint8Array): TipoConteudoDocumento | null {
  for (const assinatura of ASSINATURAS) {
    const combina = assinatura.bytes.every((byte, indice) => cabecalho[indice] === byte);
    if (cabecalho.length >= assinatura.bytes.length && combina) return assinatura.tipo;
  }
  return null;
}

/**
 * Confere o arquivo: vazio, tamanho (até 10 MB), tipo declarado (PDF, JPG ou PNG) e primeiros bytes que
 * batem com o tipo declarado. Devolve o primeiro motivo de recusa, ou null quando passa.
 */
export function validarArquivoDocumento(
  arquivo: { type: string; size: number },
  cabecalho: Uint8Array,
): MotivoRecusaDocumento | null {
  if (arquivo.size === 0 || cabecalho.length === 0) return 'vazio';
  if (arquivo.size > TAMANHO_MAXIMO_DOCUMENTO_BYTES) return 'tamanho';
  if (!TIPOS_DOCUMENTO_ACEITOS.includes(arquivo.type)) return 'formato';
  const conteudo = detectarTipoDocumento(cabecalho);
  if (conteudo === null || conteudo !== arquivo.type) return 'formato';
  return null;
}

/** Lê os primeiros bytes do arquivo e confere. Não reduz nem regrava nada. */
export async function conferirArquivoDocumento(arquivo: File): Promise<MotivoRecusaDocumento | null> {
  const cabecalho = new Uint8Array(await arquivo.slice(0, TAMANHO_CABECALHO_DOCUMENTO_BYTES).arrayBuffer());
  return validarArquivoDocumento(arquivo, cabecalho);
}

/** Texto da recusa, em português. */
export function mensagemRecusaDocumento(motivo: MotivoRecusaDocumento): string {
  switch (motivo) {
    case 'vazio':
      return MENSAGEM_DOCUMENTO_RECUSA_VAZIO;
    case 'tamanho':
      return MENSAGEM_DOCUMENTO_RECUSA_TAMANHO;
    case 'formato':
      return MENSAGEM_DOCUMENTO_RECUSA_FORMATO;
  }
}

/** Só imagem tem prévia; PDF mostra ícone, nome e tamanho. */
export function ehImagemDocumento(tipo: string): boolean {
  return tipo === 'image/jpeg' || tipo === 'image/png';
}

/**
 * CPF mascarado para a tela ("***.482.918-**", só os três grupos do meio). Vazio quando o valor não tem
 * 11 dígitos.
 */
export function mascararCpf(cpf: string): string {
  const digitos = somenteDigitos(cpf);
  if (digitos.length !== 11) return '';
  return `***.${digitos.slice(3, 6)}.${digitos.slice(6, 9)}-**`;
}

/** Ordem em que os documentos são enviados e aparecem (CNH antes do comprovante). */
export const ORDEM_DOCUMENTOS: readonly TipoDocumentoCliente[] = TIPOS_DOCUMENTO_CLIENTE;

/**
 * Documentos com arquivo escolhido que ainda não subiram: o arquivo na memória é diferente do que foi enviado
 * (troca de arquivo conta como pendente). Na ordem da tela.
 */
export function documentosPendentes(
  arquivos: ArquivosDocumento,
  enviados: ArquivosDocumento,
): TipoDocumentoCliente[] {
  return ORDEM_DOCUMENTOS.filter((tipo) => {
    const arquivo = arquivos[tipo];
    return arquivo !== undefined && arquivo !== enviados[tipo];
  });
}

/**
 * Documentos que faltam para o envio: sem arquivo escolhido e sem documento já gravado no servidor.
 * Na ordem da tela.
 */
export function documentosFaltando(
  arquivos: ArquivosDocumento,
  noServidor: readonly TipoDocumentoCliente[],
): TipoDocumentoCliente[] {
  return ORDEM_DOCUMENTOS.filter((tipo) => arquivos[tipo] === undefined && !noServidor.includes(tipo));
}

/** Tipos que já têm documento gravado, para `documentosFaltando`. */
export function tiposNoServidor(
  documentos: Partial<Record<TipoDocumentoCliente, DocumentoClienteGravado>>,
): TipoDocumentoCliente[] {
  return ORDEM_DOCUMENTOS.filter((tipo) => documentos[tipo] !== undefined);
}

/** Aviso e botão do modo só enviar (cliente logado, conta já criada). */
export interface SituacaoModoSoEnviar {
  /** Texto do aviso no topo da tela. */
  aviso: string;
  /** Se o botão "Enviar documentos" aparece. */
  mostrarBotao: boolean;
}

/**
 * Modo só enviar: com os dois documentos já gravados, o aviso passa a dizer que já foram enviados (o aviso de
 * "faltam documentos" seria falso) e o botão só aparece quando há arquivo novo escolhido (substituição).
 * Com algum documento faltando, aviso e botão ficam como antes.
 */
export function situacaoModoSoEnviar(
  noServidor: readonly TipoDocumentoCliente[],
  pendentes: readonly TipoDocumentoCliente[],
): SituacaoModoSoEnviar {
  const todosNoServidor = ORDEM_DOCUMENTOS.every((tipo) => noServidor.includes(tipo));
  return {
    aviso: todosNoServidor ? MENSAGEM_DOCUMENTO_JA_ENVIADOS : MENSAGEM_DOCUMENTO_MODO_SO_ENVIAR,
    mostrarBotao: !todosNoServidor || pendentes.length > 0,
  };
}

/** Cópia do mapa sem a chave do tipo (sem deixar `undefined` para trás). */
export function semTipo<V>(
  mapa: Partial<Record<TipoDocumentoCliente, V>>,
  tipo: TipoDocumentoCliente,
): Partial<Record<TipoDocumentoCliente, V>> {
  const copia: Partial<Record<TipoDocumentoCliente, V>> = { ...mapa };
  delete copia[tipo];
  return copia;
}

/** Estados do cartão de documento (um por tipo). Cada um tem texto ou ícone com texto alternativo. */
export type EstadoCartaoDocumento = 'vazio' | 'selecionado' | 'enviando' | 'enviado' | 'erro';

export interface EntradaEstadoCartao {
  /** Há arquivo escolhido que ainda não subiu. */
  selecionado: boolean;
  /** Há documento gravado no servidor (e não há troca pendente). */
  enviado: boolean;
  /** O envio deste tipo está em andamento. */
  enviando: boolean;
  /** O último envio deste tipo falhou. */
  falhou: boolean;
}

/** Estado do cartão: enviando > erro > selecionado > enviado > vazio. */
export function estadoCartaoDocumento(entrada: EntradaEstadoCartao): EstadoCartaoDocumento {
  if (entrada.enviando) return 'enviando';
  if (entrada.falhou) return 'erro';
  if (entrada.selecionado) return 'selecionado';
  if (entrada.enviado) return 'enviado';
  return 'vazio';
}

/** Percentual de 0 a 100, sem valor inválido (NaN ou fora da faixa). */
export function percentualProgresso(valor: number): number {
  if (!Number.isFinite(valor)) return 0;
  return Math.min(100, Math.max(0, Math.round(valor)));
}
