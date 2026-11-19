import { Botao, Esqueleto, Icone } from '@/components/ui';
import { formatarTamanhoArquivo } from '@/lib/formatters';
import {
  MENSAGEM_VERIFICACAO_DOCS_ERRO,
  MENSAGEM_VERIFICACAO_DOC_BAIXAR_PDF,
  MENSAGEM_VERIFICACAO_DOC_SEM_ENVIO,
  MENSAGEM_VERIFICACAO_DOC_SEM_LINK,
  MENSAGEM_VERIFICACAO_SECAO_DOCUMENTOS,
  MENSAGEM_VERIFICACAO_TENTAR_NOVAMENTE,
  mensagemVerificacaoAbrirDocumento,
  mensagemVerificacaoEnviadoEm,
  mensagemVerificacaoImagemDocumento,
} from '@/lib/mensagens';
import type { DocumentoVerificacao } from '@/types/verificacaoCadastros';
import {
  documentoDoTipo,
  ORDEM_DOCUMENTOS_VERIFICACAO,
  rotuloTipoDocumentoVerificacao,
  situacaoVisualDocumento,
  type SituacaoVisualDocumento,
} from './regras';

/** Situação da consulta dos documentos no painel. */
export type EstadoConsultaDocumentos =
  | { situacao: 'carregando' }
  | { situacao: 'erro' }
  | { situacao: 'ok'; documentos: readonly DocumentoVerificacao[] };

export interface DocumentosVerificacaoProps {
  estado: EstadoConsultaDocumentos;
  aoTentarNovamente: () => void;
}

/**
 * Documentos do cliente no painel (tela 18): um cartão por tipo, na ordem da tela. Imagem (JPG ou PNG) aparece pelo
 * link temporário e abre em nova aba; PDF vira link para baixar; item sem link aparece sem imagem e sem link. O link não
 * sai do painel: a consulta não fica em cache (ver `useDocumentosVerificacao`).
 */
export function DocumentosVerificacao({ estado, aoTentarNovamente }: DocumentosVerificacaoProps) {
  return (
    <section className="flex flex-col gap-3">
      <h3 className="text-legenda font-semibold uppercase tracking-wide text-contorno">
        {MENSAGEM_VERIFICACAO_SECAO_DOCUMENTOS}
      </h3>

      {estado.situacao === 'carregando' && (
        <div className="grid grid-cols-1 gap-3">
          <Esqueleto className="h-32 w-full" />
          <Esqueleto className="h-20 w-full" />
        </div>
      )}

      {estado.situacao === 'erro' && (
        <div role="alert" className="flex flex-col items-start gap-2 rounded bg-status-perigo-fundo p-3">
          <p className="text-corpo-denso text-status-perigo">{MENSAGEM_VERIFICACAO_DOCS_ERRO}</p>
          <Botao variante="contorno" tamanho="pequeno" icone="refresh" onClick={aoTentarNovamente}>
            {MENSAGEM_VERIFICACAO_TENTAR_NOVAMENTE}
          </Botao>
        </div>
      )}

      {estado.situacao === 'ok' &&
        ORDEM_DOCUMENTOS_VERIFICACAO.map((tipo) => (
          <CartaoDocumentoVerificacao
            key={tipo}
            rotulo={rotuloTipoDocumentoVerificacao(tipo)}
            documento={documentoDoTipo(estado.documentos, tipo)}
          />
        ))}
    </section>
  );
}

interface CartaoDocumentoVerificacaoProps {
  rotulo: string;
  documento: DocumentoVerificacao | undefined;
}

/** Um documento: nome do tipo, arquivo, tamanho, data de envio e a forma de abrir (imagem, PDF ou sem link). */
function CartaoDocumentoVerificacao({ rotulo, documento }: CartaoDocumentoVerificacaoProps) {
  const situacao: SituacaoVisualDocumento = situacaoVisualDocumento(documento);
  const url = documento?.url ?? null;

  if (documento === undefined) {
    return (
      <article className="flex flex-col gap-1 rounded bg-branco p-3 shadow-cartao">
        <span className="font-semibold text-texto-desenho">{rotulo}</span>
        <p className="text-corpo-denso text-texto-apoio">{MENSAGEM_VERIFICACAO_DOC_SEM_ENVIO}</p>
      </article>
    );
  }

  return (
    <article className="flex flex-col gap-2 rounded bg-branco p-3 shadow-cartao">
      <div className="flex items-start justify-between gap-2">
        <span className="font-semibold text-texto-desenho">{rotulo}</span>
        <span className="shrink-0 text-legenda text-texto-apoio">{formatarTamanhoArquivo(documento.tamanhoBytes)}</span>
      </div>
      <p className="truncate text-legenda text-texto-apoio" title={documento.nomeArquivo}>
        {documento.nomeArquivo}
      </p>
      <p className="text-legenda text-texto-apoio">{mensagemVerificacaoEnviadoEm(documento.dataEnvio)}</p>

      {url !== null && situacao === 'imagem' && (
        <a
          href={url}
          target="_blank"
          rel="noopener noreferrer"
          aria-label={mensagemVerificacaoAbrirDocumento(rotulo)}
          className="block overflow-hidden rounded bg-steel-fundo focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-orange-acao"
        >
          <img
            src={url}
            alt={mensagemVerificacaoImagemDocumento(rotulo)}
            loading="lazy"
            className="max-h-72 w-full object-contain"
          />
        </a>
      )}

      {url !== null && situacao === 'pdf' && (
        <a
          href={url}
          target="_blank"
          rel="noopener noreferrer"
          aria-label={mensagemVerificacaoAbrirDocumento(rotulo)}
          className="inline-flex w-fit items-center gap-2 rounded bg-steel-fundo px-3 py-2 text-corpo-denso font-semibold text-navy transition-colors hover:bg-steel-alto focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-orange-acao"
        >
          <Icone nome="picture_as_pdf" tamanho={18} />
          {MENSAGEM_VERIFICACAO_DOC_BAIXAR_PDF}
        </a>
      )}

      {situacao === 'sem-link' && (
        <p className="text-corpo-denso text-texto-apoio">{MENSAGEM_VERIFICACAO_DOC_SEM_LINK}</p>
      )}
    </article>
  );
}
