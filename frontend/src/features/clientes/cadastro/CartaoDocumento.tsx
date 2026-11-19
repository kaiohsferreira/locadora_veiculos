import { useEffect, useId, useRef, useState, type ChangeEvent } from 'react';
import { Botao, Icone } from '@/components/ui';
import {
  ehImagemDocumento,
  estadoCartaoDocumento,
  mensagemRecusaDocumento,
  percentualProgresso,
  type EstadoCartaoDocumento,
  type MotivoRecusaDocumento,
} from '@/lib/documentosCliente';
import {
  MENSAGEM_DOCUMENTO_ERRO_ENVIO,
  MENSAGEM_DOCUMENTO_ENVIADO_HA,
  MENSAGEM_DOCUMENTO_ESCOLHER_ARQUIVO,
  MENSAGEM_DOCUMENTO_PROGRESSO,
  MENSAGEM_DOCUMENTO_SELECIONAR,
  MENSAGEM_DOCUMENTO_SELO_ENVIADO,
  MENSAGEM_DOCUMENTO_SELO_ENVIANDO,
  MENSAGEM_DOCUMENTO_SELO_ERRO,
  MENSAGEM_DOCUMENTO_SELO_SELECIONADO,
  MENSAGEM_DOCUMENTO_SUBSTITUIR,
  MENSAGEM_DOCUMENTO_TENTAR_DE_NOVO,
  MENSAGEM_DOCUMENTO_VAZIO,
} from '@/lib/mensagensCadastro';
import { formatarTamanhoArquivo, formatarTempoDecorrido } from '@/lib/formatters';
import type { DocumentoClienteGravado, TipoDocumentoCliente } from '@/types/documentosCliente';

/** Tipos de arquivo que o seletor oferece (sem `capture`: nada de câmera direta). */
const ACCEPT_DOCUMENTO = 'application/pdf,image/jpeg,image/png';

export interface CartaoDocumentoProps {
  tipo: TipoDocumentoCliente;
  titulo: string;
  /** Linha de formatos aceitos, como no desenho. */
  formatos: string;
  /** Arquivo escolhido que ainda não subiu (pendente). */
  arquivo: File | null;
  /** Documento já gravado no servidor (sem troca pendente). Sem o link de leitura: a tela não o usa. */
  enviado: DocumentoClienteGravado | null;
  enviando: boolean;
  progresso: number;
  /** O último envio deste tipo falhou. */
  falhou: boolean;
  /** Motivo da última escolha recusada no navegador. */
  recusa: MotivoRecusaDocumento | null;
  desabilitado?: boolean;
  /** Hora de referência para "Enviado há X" (padrão: a hora em que o cartão apareceu). Os testes passam uma data fixa. */
  agora?: Date;
  aoEscolher: (arquivo: File) => void;
  aoTentarNovamente: () => void;
}

interface SeloEstado {
  icone: string;
  texto: string;
  classes: string;
}

const SELOS: Readonly<Record<Exclude<EstadoCartaoDocumento, 'vazio'>, SeloEstado>> = {
  selecionado: {
    icone: 'task_alt',
    texto: MENSAGEM_DOCUMENTO_SELO_SELECIONADO,
    classes: 'bg-steel-suave text-navy',
  },
  enviando: {
    icone: 'sync',
    texto: MENSAGEM_DOCUMENTO_SELO_ENVIANDO,
    classes: 'bg-status-pendente-fundo text-status-pendente',
  },
  enviado: {
    icone: 'check_circle',
    texto: MENSAGEM_DOCUMENTO_SELO_ENVIADO,
    classes: 'bg-documento-fundo text-documento',
  },
  erro: {
    icone: 'error',
    texto: MENSAGEM_DOCUMENTO_SELO_ERRO,
    classes: 'bg-status-perigo-fundo text-status-perigo',
  },
};

/** Ícone do arquivo pelo tipo: imagem ou PDF (sem prévia). */
function iconeDoArquivo(tipo: string): string {
  return ehImagemDocumento(tipo) ? 'image' : 'picture_as_pdf';
}

export interface AreaPreviaProps {
  /** Link temporário do navegador da imagem escolhida, ou null (PDF, ou arquivo ainda não escolhido). */
  previa: string | null;
  tipoConteudo: string;
}

/** Área da prévia: a imagem escolhida, ou o ícone do arquivo quando não há prévia (PDF). */
export function AreaPrevia({ previa, tipoConteudo }: AreaPreviaProps) {
  return (
    <div className="flex h-48 items-center justify-center overflow-hidden rounded-sm bg-branco">
      {previa !== null ? (
        <img src={previa} alt="" className="h-full w-full object-contain" />
      ) : (
        <Icone nome={iconeDoArquivo(tipoConteudo)} tamanho={48} className="text-status-pendente" />
      )}
    </div>
  );
}

/**
 * Cartão de um documento do cliente (CNH ou comprovante). Mostra o estado com texto e ícone, a prévia de
 * imagem (só do arquivo escolhido na memória; o link do servidor nunca é usado como prévia) e o seletor
 * de arquivo, que abre pelo botão. Sem arrastar e soltar.
 */
export function CartaoDocumento({
  tipo,
  titulo,
  formatos,
  arquivo,
  enviado,
  enviando,
  progresso,
  falhou,
  recusa,
  desabilitado = false,
  agora,
  aoEscolher,
  aoTentarNovamente,
}: CartaoDocumentoProps) {
  const idBase = useId();
  const idTitulo = `${idBase}-titulo`;
  const entradaRef = useRef<HTMLInputElement>(null);
  // Hora de referência do "Enviado há X": fixada quando o cartão aparece (não muda a cada render).
  const [horaDaTela] = useState(() => new Date());

  // A prévia é um link temporário do navegador (createObjectURL), só para imagem escolhida. Criada num efeito
  // e revogada ao trocar o arquivo e ao sair da tela (com o StrictMode, o efeito roda duas vezes e a última
  // URL criada é a que fica no estado).
  const [previa, setPrevia] = useState<{ arquivo: File; url: string } | null>(null);
  useEffect(() => {
    if (arquivo === null || !ehImagemDocumento(arquivo.type)) return;
    const url = URL.createObjectURL(arquivo);
    // Sincroniza com um recurso externo (o link do navegador). Quando o arquivo muda, a prévia antiga
    // deixa de valer (ver `previaUrl`) e a URL antiga é revogada no cleanup.
    // oxlint-disable-next-line react/set-state-in-effect
    setPrevia({ arquivo, url });
    return () => URL.revokeObjectURL(url);
  }, [arquivo]);
  const previaUrl = previa !== null && previa.arquivo === arquivo ? previa.url : null;

  const estado = estadoCartaoDocumento({
    selecionado: arquivo !== null,
    enviado: enviado !== null,
    enviando,
    falhou,
  });
  const selo = estado === 'vazio' ? null : SELOS[estado];
  const ocupado = enviando || desabilitado;

  // Nome e tamanho do que aparece na linha: o arquivo escolhido, ou o gravado no servidor.
  const nome = arquivo?.name ?? enviado?.nomeArquivo ?? '';
  const tamanho = arquivo?.size ?? enviado?.tamanhoBytes ?? null;
  const tipoConteudo = arquivo?.type ?? enviado?.tipoConteudo ?? '';
  const tempoEnvio =
    enviado !== null && arquivo === null ? formatarTempoDecorrido(enviado.dataEnvio, agora ?? horaDaTela) : null;

  function aoMudarEntrada(evento: ChangeEvent<HTMLInputElement>) {
    const escolhido = evento.target.files?.[0];
    // Zera o campo: escolher de novo o mesmo arquivo também dispara a mudança.
    evento.target.value = '';
    if (escolhido !== undefined) aoEscolher(escolhido);
  }

  function abrirSeletor() {
    entradaRef.current?.click();
  }

  return (
    <section
      aria-labelledby={idTitulo}
      data-estado={estado}
      className="flex min-w-0 flex-col rounded-lg bg-branco p-6 shadow-cartao"
    >
      <div className="mb-1 flex flex-wrap items-center justify-between gap-2">
        <div className="flex min-w-0 items-center gap-2">
          <Icone
            nome={tipo === 'CNH' ? 'id_card' : 'home_pin'}
            tamanho={22}
            className="shrink-0 text-status-pendente"
          />
          <h2 id={idTitulo} className="min-w-0 text-titulo-md text-navy">
            {titulo}
          </h2>
        </div>
        {selo !== null && (
          <span className={`inline-flex items-center gap-1 rounded-sm px-2 py-0.5 text-legenda font-semibold ${selo.classes}`}>
            <Icone nome={selo.icone} tamanho={14} />
            {selo.texto}
          </span>
        )}
      </div>

      <p className="mb-4 text-legenda text-status-neutro">{formatos}</p>

      {recusa !== null && (
        <p role="alert" className="mb-4 flex items-start gap-2 rounded-sm bg-status-perigo-fundo p-2 text-corpo-denso text-status-perigo">
          <Icone nome="error" tamanho={16} className="mt-0.5 shrink-0" />
          <span>{mensagemRecusaDocumento(recusa)}</span>
        </p>
      )}

      <div className="flex flex-1 flex-col gap-4 rounded-lg bg-steel-fundo p-4">
        {estado === 'vazio' ? (
          <div className="flex min-h-[10rem] flex-col items-center justify-center gap-3 text-center">
            <Icone nome="upload_file" tamanho={32} className="text-status-neutro" />
            <p className="text-corpo-denso text-status-neutro">{MENSAGEM_DOCUMENTO_VAZIO}</p>
            <Botao
              variante="contorno"
              icone="folder_open"
              onClick={abrirSeletor}
              disabled={ocupado}
              className="!h-11 w-full sm:w-auto"
            >
              {MENSAGEM_DOCUMENTO_SELECIONAR}
            </Botao>
          </div>
        ) : (
          <>
            <AreaPrevia previa={previaUrl} tipoConteudo={tipoConteudo} />

            <div className="flex flex-col gap-2 rounded-lg bg-branco p-2 shadow-sm sm:flex-row sm:items-center sm:justify-between">
              <div className="flex min-w-0 items-center gap-2">
                <Icone nome={iconeDoArquivo(tipoConteudo)} tamanho={24} className="shrink-0 text-status-pendente" />
                <div className="flex min-w-0 flex-col">
                  <span className="truncate text-corpo-denso font-semibold text-navy" title={nome}>
                    {nome}
                  </span>
                  <span className="text-micro text-status-neutro">
                    {tamanho !== null ? formatarTamanhoArquivo(tamanho) : ''}
                    {tempoEnvio !== null ? ` • ${MENSAGEM_DOCUMENTO_ENVIADO_HA(tempoEnvio)}` : ''}
                  </span>
                </div>
              </div>
              {!enviando && (
                <Botao
                  variante="fantasma"
                  icone="sync"
                  tamanho="pequeno"
                  onClick={abrirSeletor}
                  disabled={ocupado}
                  className="!h-11 w-full shrink-0 !bg-steel-suave !text-navy hover:!bg-steel-alto sm:w-auto"
                >
                  {MENSAGEM_DOCUMENTO_SUBSTITUIR}
                </Botao>
              )}
            </div>

            {enviando && (
              <progress
                aria-label={MENSAGEM_DOCUMENTO_PROGRESSO}
                max={100}
                value={percentualProgresso(progresso)}
                className="h-1.5 w-full appearance-none overflow-hidden rounded-full bg-steel-linha [&::-moz-progress-bar]:bg-status-pendente [&::-webkit-progress-bar]:bg-steel-linha [&::-webkit-progress-value]:bg-status-pendente"
              />
            )}

            {falhou && !enviando && (
              <p className="flex items-start gap-2 text-corpo-denso text-status-perigo">
                <Icone nome="error" tamanho={16} className="mt-0.5 shrink-0" />
                <span>{MENSAGEM_DOCUMENTO_ERRO_ENVIO}</span>
              </p>
            )}

            {falhou && !enviando && (
              <div className="flex flex-col gap-2 sm:flex-row sm:justify-end">
                <Botao
                  variante="primario"
                  icone="refresh"
                  onClick={aoTentarNovamente}
                  disabled={desabilitado}
                  className="!h-11 w-full sm:w-auto"
                >
                  {MENSAGEM_DOCUMENTO_TENTAR_DE_NOVO}
                </Botao>
              </div>
            )}
          </>
        )}
      </div>

      <input
        ref={entradaRef}
        type="file"
        accept={ACCEPT_DOCUMENTO}
        tabIndex={-1}
        aria-label={`${MENSAGEM_DOCUMENTO_ESCOLHER_ARQUIVO}: ${titulo}`}
        className="sr-only"
        disabled={ocupado}
        onChange={aoMudarEntrada}
      />
    </section>
  );
}
