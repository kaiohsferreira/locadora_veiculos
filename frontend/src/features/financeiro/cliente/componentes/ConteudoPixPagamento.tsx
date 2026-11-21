import { useEffect, useRef, useState } from 'react';
import { Botao, Icone } from '@/components/ui';
import { formatarDiaEMes } from '@/lib/formatters';
import { obterMensagemErro } from '@/lib/erros';
import {
  MENSAGEM_PIX_CAMPO_CODIGO,
  MENSAGEM_PIX_CODIGO_COPIADO,
  MENSAGEM_PIX_CONTAGEM_ANTES,
  MENSAGEM_PIX_COPIAR,
  MENSAGEM_PIX_ESCANEIE,
  MENSAGEM_PIX_EXPIROU,
  MENSAGEM_PIX_GERANDO,
  MENSAGEM_PIX_GERAR_NOVO,
  MENSAGEM_PIX_QR_ALT,
  MENSAGEM_PIX_TENTAR_NOVAMENTE,
  MENSAGEM_PIX_VALOR_FATURA,
  mensagemPixVencimentoCurto,
} from '@/lib/mensagens';
import {
  formatarContagemPix,
  pixExpirou,
  proximaContagemPix,
  rotuloOrigem,
} from '@/features/financeiro/cliente/regrasPagamentosCliente';
import type { CobrancaPix, MeuPagamento } from '@/types/pagamentos';

/** Tempo que o "Código copiado" fica na tela. */
const TEMPO_FEEDBACK_COPIA_MS = 2000;

/** Um passo da contagem regressiva. */
const INTERVALO_CONTAGEM_MS = 1000;

/**
 * Bloco de valor da gaveta (desenho: "Valor da Fatura" e o selo "Venc. dd/MM"). Valor e selo usam as cores de destaque da
 * tela 30 (frota-oficina, o `secondary` do desenho).
 */
export function ResumoCobrancaPix({ pagamento }: { pagamento: MeuPagamento }) {
  return (
    <div className="flex items-center justify-between gap-4 rounded-cartao bg-steel-fundo p-4">
      <div className="flex flex-col">
        <span className="text-micro font-semibold uppercase text-texto-apoio">{MENSAGEM_PIX_VALOR_FATURA}</span>
        <span className="text-valor-destaque text-frota-oficina">{pagamento.valorStr}</span>
      </div>
      <span className="rounded bg-frota-oficina-fundo px-2.5 py-1 text-rotulo font-bold text-pagamento-texto-pendente">
        {mensagemPixVencimentoCurto(formatarDiaEMes(pagamento.dataVencimentoStr))}
      </span>
    </div>
  );
}

/**
 * Origem da cobrança, na linha abaixo do título da gaveta (desenho: #drawerSub, contrato pagamento-pix: "Locação nº" ou
 * "Venda nº"). Só aparece quando a cobrança tem locação ou venda. O número da cobrança não entra aqui: a tabela da tela
 * 30 é que o mostra.
 */
export function OrigemCobrancaPix({ pagamento }: { pagamento: MeuPagamento }) {
  const origem = rotuloOrigem(pagamento);
  return <div className="flex flex-col text-apoio text-texto-apoio">{origem && <span>{origem}</span>}</div>;
}

/** Estado gerando: enquanto o servidor responde a geração ou a reabertura. */
export function ConteudoPixGerando() {
  return (
    <output className="flex flex-col items-center gap-3 py-8 text-center">
      <span className="h-6 w-6 animate-spin rounded-full border-2 border-navy border-t-transparent" aria-hidden="true" />
      <span className="text-corpo-denso font-semibold text-navy">{MENSAGEM_PIX_GERANDO}</span>
    </output>
  );
}

export interface ConteudoPixExpiradoProps {
  aoGerarNovo: () => void;
}

/** Estado expirado: a contagem chegou a zero. "Gerar novo código" chama a mesma rota, que gera outro PIX. */
export function ConteudoPixExpirado({ aoGerarNovo }: ConteudoPixExpiradoProps) {
  return (
    <div className="flex flex-col items-center gap-4 py-6 text-center">
      <span className="flex h-12 w-12 items-center justify-center rounded-full bg-status-perigo-fundo text-status-perigo">
        <Icone nome="timer_off" tamanho={24} />
      </span>
      <p className="text-corpo font-semibold text-navy">{MENSAGEM_PIX_EXPIROU}</p>
      <Botao variante="primario" icone="refresh" onClick={aoGerarNovo}>
        {MENSAGEM_PIX_GERAR_NOVO}
      </Botao>
    </div>
  );
}

export interface ConteudoPixErroProps {
  erro: unknown;
  aoTentarNovamente: () => void;
}

/** Estado erro: a mensagem vem de obterMensagemErro (nunca o texto técnico). */
export function ConteudoPixErro({ erro, aoTentarNovamente }: ConteudoPixErroProps) {
  return (
    <div role="alert" className="flex flex-col items-center gap-3 py-6 text-center">
      <span className="flex h-12 w-12 items-center justify-center rounded-full bg-status-perigo-fundo text-status-perigo">
        <Icone nome="error" tamanho={24} />
      </span>
      <p className="max-w-xs text-corpo-denso text-texto-suave">{obterMensagemErro(erro)}</p>
      <Botao variante="contorno" icone="refresh" onClick={aoTentarNovamente}>
        {MENSAGEM_PIX_TENTAR_NOVAMENTE}
      </Botao>
    </div>
  );
}

export interface ConteudoPixProntoProps {
  cobranca: CobrancaPix;
  aoGerarNovo: () => void;
}

/**
 * Estado pronto: QR, contagem regressiva e copia e cola. A contagem começa em `segundosParaExpirar` (vindo do servidor) e
 * desce um segundo por vez. Quando chega a zero, o próprio componente mostra o estado expirado. A validade não depende
 * do relógio do navegador: só a contagem na tela.
 */
export function ConteudoPixPronto({ cobranca, aoGerarNovo }: ConteudoPixProntoProps) {
  const [restante, setRestante] = useState<number>(cobranca.segundosParaExpirar);
  const [copiado, setCopiado] = useState(false);
  const campoRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (pixExpirou(restante)) return undefined;
    const temporizador = window.setTimeout(
      () => setRestante((atual) => proximaContagemPix(atual)),
      INTERVALO_CONTAGEM_MS,
    );
    return () => window.clearTimeout(temporizador);
  }, [restante]);

  useEffect(() => {
    if (!copiado) return undefined;
    const temporizador = window.setTimeout(() => setCopiado(false), TEMPO_FEEDBACK_COPIA_MS);
    return () => window.clearTimeout(temporizador);
  }, [copiado]);

  if (pixExpirou(restante)) return <ConteudoPixExpirado aoGerarNovo={aoGerarNovo} />;

  async function copiar() {
    try {
      await navigator.clipboard.writeText(cobranca.copiaECola);
      setCopiado(true);
    } catch {
      // Sem acesso à área de transferência: seleciona o código para o usuário copiar com o teclado.
      campoRef.current?.select();
    }
  }

  return (
    <div className="flex flex-col items-center gap-4 py-2 text-center">
      <div className="rounded-cartao bg-steel-fundo p-4 shadow-inner">
        <img
          src={`data:image/png;base64,${cobranca.qrCodeBase64}`}
          alt={MENSAGEM_PIX_QR_ALT}
          width={192}
          height={192}
          className="h-48 w-48 rounded-cartao bg-branco"
        />
      </div>
      <div className="flex flex-col gap-1">
        <p className="text-corpo-denso font-semibold text-navy">{MENSAGEM_PIX_ESCANEIE}</p>
        <p className="text-apoio text-texto-apoio">
          {MENSAGEM_PIX_CONTAGEM_ANTES}{' '}
          <strong className="text-dado-tabular font-semibold text-frota-oficina">{formatarContagemPix(restante)}</strong>
        </p>
      </div>
      <div className="flex w-full items-center gap-1 rounded bg-steel-fundo p-1.5">
        <input
          ref={campoRef}
          type="text"
          readOnly
          value={cobranca.copiaECola}
          aria-label={MENSAGEM_PIX_CAMPO_CODIGO}
          onFocus={(evento) => evento.currentTarget.select()}
          className="min-w-0 flex-1 truncate bg-transparent px-2 text-apoio text-texto-apoio focus:outline-none"
        />
        <button
          type="button"
          onClick={() => void copiar()}
          className="inline-flex shrink-0 items-center gap-1 rounded bg-navy px-3 py-1.5 text-corpo-denso font-semibold text-branco transition-colors hover:bg-navy-claro"
        >
          <Icone nome="content_copy" tamanho={16} />
          {MENSAGEM_PIX_COPIAR}
        </button>
      </div>
      <output className="block min-h-5 text-apoio font-medium text-status-sucesso">
        {copiado ? MENSAGEM_PIX_CODIGO_COPIADO : ''}
      </output>
    </div>
  );
}
