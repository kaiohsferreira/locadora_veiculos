/** Documentos do cliente (contrato cliente-documento v1): CNH e comprovante de residência. */

/** Tipos aceitos pelo backend (enum TipoDocumentoCliente, gravado como texto). */
export const TIPOS_DOCUMENTO_CLIENTE = ['CNH', 'COMPROVANTE_RESIDENCIA'] as const;
export type TipoDocumentoCliente = (typeof TIPOS_DOCUMENTO_CLIENTE)[number];

/** Tipo de conteúdo decidido pelo backend a partir dos primeiros bytes do arquivo. */
export type TipoConteudoDocumento = 'image/jpeg' | 'image/png' | 'application/pdf';

/**
 * Documento ativo do cliente como o backend devolve (ReturnClienteDocumentoVO). `url` é o link temporário de
 * leitura e `expiraEm` o prazo dele. Estes dois campos NÃO entram na tela nem no cache: a camada de API tira
 * (ver `DocumentoClienteGravado`).
 */
export interface RespostaDocumentoCliente {
  idClienteDocumento: number;
  tipo: TipoDocumentoCliente;
  nomeArquivo: string;
  tipoConteudo: TipoConteudoDocumento;
  tamanhoBytes: number;
  /** Data e hora do envio, da hora do servidor, no formato dd/MM/yyyy HH:mm (StaticMethods.FormateDateTimeToPTBR). */
  dataEnvio: string;
  url: string | null;
  expiraEm: string | null;
}

/** Documento gravado que a tela usa: sem o link de leitura nem o prazo dele (o front não guarda o link). */
export type DocumentoClienteGravado = Omit<RespostaDocumentoCliente, 'url' | 'expiraEm'>;
