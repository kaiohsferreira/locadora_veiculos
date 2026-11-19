/**
 * Chamadas dos documentos do cliente (contrato cliente-documento v1): enviar (POST multipart com os campos
 * "tipo" e "arquivo") e listar (GET, só o dono). As telas acessam estas funções pelos hooks do TanStack Query.
 */
import { api } from './client';
import type { DocumentoClienteGravado, RespostaDocumentoCliente, TipoDocumentoCliente } from '@/types/documentosCliente';

/** Percentual máximo do progresso do envio. */
const PERCENTUAL_MAXIMO = 100;

/** Nome do campo do tipo e do arquivo no formulário (contrato: campos "tipo" e "arquivo"). */
export const CAMPO_TIPO_DOCUMENTO = 'tipo';
export const CAMPO_ARQUIVO_DOCUMENTO = 'arquivo';

/** Tempo limite próprio do envio: 10 MB em rede lenta não cabem nos 30 s padrão do cliente. */
export const TEMPO_LIMITE_ENVIO_DOCUMENTO_MS = 120_000;

/** Opções do envio: progresso de 0 a 100 e cancelamento. */
export interface OpcoesEnvioDocumento {
  aoProgredir?: (percentual: number) => void;
  sinal?: AbortSignal;
}

function caminhoDocumentos(idCliente: number): string {
  return `/api/clientes/${idCliente}/documentos`;
}

/** Tira o link de leitura e o prazo dele: a tela e o cache só recebem o que mostram. */
function semLinkDeLeitura(documento: RespostaDocumentoCliente): DocumentoClienteGravado {
  return {
    idClienteDocumento: documento.idClienteDocumento,
    tipo: documento.tipo,
    nomeArquivo: documento.nomeArquivo,
    tipoConteudo: documento.tipoConteudo,
    tamanhoBytes: documento.tamanhoBytes,
    dataEnvio: documento.dataEnvio,
  };
}

/** Envia (ou substitui) o documento do tipo informado (201 com o documento gravado, sem o link). */
export async function enviarDocumentoCliente(
  idCliente: number,
  tipo: TipoDocumentoCliente,
  arquivo: File,
  opcoes: OpcoesEnvioDocumento = {},
): Promise<DocumentoClienteGravado> {
  const formulario = new FormData();
  formulario.append(CAMPO_TIPO_DOCUMENTO, tipo);
  formulario.append(CAMPO_ARQUIVO_DOCUMENTO, arquivo, arquivo.name);

  const resposta = await api.post<RespostaDocumentoCliente>(caminhoDocumentos(idCliente), formulario, {
    timeout: TEMPO_LIMITE_ENVIO_DOCUMENTO_MS,
    signal: opcoes.sinal,
    onUploadProgress: (evento) => {
      const total = evento.total ?? 0;
      if (total > 0 && opcoes.aoProgredir) {
        opcoes.aoProgredir(Math.round((evento.loaded * PERCENTUAL_MAXIMO) / total));
      }
    },
  });
  return semLinkDeLeitura(resposta.data);
}

/** Documentos ativos do cliente (no máximo um por tipo), sem o link. Resposta que não é lista vira lista vazia. */
export async function listarDocumentosCliente(idCliente: number): Promise<DocumentoClienteGravado[]> {
  const resposta = await api.get<unknown>(caminhoDocumentos(idCliente));
  return Array.isArray(resposta.data) ? (resposta.data as RespostaDocumentoCliente[]).map(semLinkDeLeitura) : [];
}
