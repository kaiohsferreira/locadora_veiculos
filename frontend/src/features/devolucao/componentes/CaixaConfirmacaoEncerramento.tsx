import { useEffect, useRef, useState } from 'react';
import { Botao } from '@/components/ui';
import { VALOR_AUSENTE } from '@/lib/formatters';
import { MENSAGENS_DEVOLUCAO as M } from '@/lib/mensagensDevolucao';
import type { ContextoDevolucao } from '@/types/devolucao';
import {
  deveDesmarcarAposFalha,
  podeConfirmarEncerramento,
  type DesfechoCaucaoPrevisto,
  type FalhaEncerramento,
} from '../regras/regrasDevolucao';

export interface CaixaConfirmacaoEncerramentoProps {
  contexto: ContextoDevolucao;
  desfecho: DesfechoCaucaoPrevisto;
  /** Envio do encerramento em curso: trava os botões e mostra o aviso de encerramento. */
  enviando: boolean;
  /** Falha do último envio, já traduzida pela tela. */
  falha: FalhaEncerramento | null;
  aoConfirmar: () => void;
  aoCancelar: () => void;
}

/**
 * Caixa de confirmação antes do encerramento (decisão D1; anexo, tela 06, NOVO aprovado). Mostra o total que vem do servidor
 * (nunca recalculado aqui), a frase da caução por caso e uma marcação obrigatória. Depois de um 409, a marcação é desfeita:
 * a pessoa precisa conferir os dados atualizados antes de confirmar de novo.
 */
export function CaixaConfirmacaoEncerramento({ contexto, desfecho, enviando, falha, aoConfirmar, aoCancelar }: CaixaConfirmacaoEncerramentoProps) {
  const [confirmado, setConfirmado] = useState(false);
  const [ultimaFalha, setUltimaFalha] = useState<FalhaEncerramento | null>(null);
  const tituloRef = useRef<HTMLHeadingElement>(null);
  // Reseta a marcação quando chega um 409 novo: ajuste durante a renderização, sem efeito.
  if (falha !== ultimaFalha) {
    setUltimaFalha(falha);
    if (deveDesmarcarAposFalha(falha)) setConfirmado(false);
  }
  // Ao abrir a caixa, o foco vai para o título: quem usa teclado ou leitor de tela entra no conteúdo novo.
  useEffect(() => {
    tituloRef.current?.focus();
  }, []);
  const temDados = contexto.valorTotalPrevistoStr !== null;
  const pronto = podeConfirmarEncerramento({ confirmado, enviando, temDados });
  return (
    <section aria-labelledby="caixa-encerramento" aria-busy={enviando} className="flex flex-col gap-4 rounded-lg border border-orange-acao/40 bg-offwhite p-4">
      <h3 ref={tituloRef} tabIndex={-1} id="caixa-encerramento" className="text-titulo-md font-bold text-navy-profundo outline-none">{M.caixa.titulo}</h3>
      <ul className="flex flex-col gap-2 text-corpo-denso text-navy-profundo">
        <li>{M.caixa.diarias(contexto.diariasPrevistas, contexto.valorDiariaStr, contexto.totalDiariasStr)}</li>
        <li>{M.caixa.extras(contexto.totalExtrasStr ?? VALOR_AUSENTE)}</li>
        <li className="font-bold">{M.caixa.total(contexto.valorTotalPrevistoStr ?? VALOR_AUSENTE)}</li>
      </ul>
      {desfecho.frase !== '' && <p className="rounded-lg bg-steel-suave p-3 text-apoio text-navy-profundo">{desfecho.frase}</p>}
      <label className="flex items-start gap-3 text-corpo-denso text-navy-profundo">
        <input
          type="checkbox"
          checked={confirmado}
          disabled={enviando}
          aria-required="true"
          onChange={event => setConfirmado(event.target.checked)}
          className="mt-1 h-4 w-4 shrink-0 accent-navy"
        />
        <span>{M.caixa.confirmo}</span>
      </label>
      {enviando && <output className="block text-apoio font-semibold text-navy-profundo">{M.encerrando}</output>}
      {falha && (
        <div role="alert" className="flex flex-col gap-2 text-corpo-denso text-status-perigo">
          <p>{falha.mensagem}</p>
          {falha.complemento !== null && <p>{falha.complemento}</p>}
          {falha.tentarDeNovo && (
            <div>
              <Botao variante="contorno" onClick={aoConfirmar} disabled={enviando || !confirmado}>{M.tentarDeNovo}</Botao>
            </div>
          )}
        </div>
      )}
      <div className="flex flex-wrap gap-3">
        {/* A variante do desenho (laranjaAcao) não tem opacidade no estado desabilitado: sem isso o botão parece habilitado (mesmo ajuste da tela 16). */}
        <Botao variante="laranjaAcao" carregando={enviando} disabled={!pronto} onClick={aoConfirmar} className="disabled:opacity-60">{M.caixa.confirmar}</Botao>
        <Botao variante="contorno" disabled={enviando} onClick={aoCancelar}>{M.caixa.cancelar}</Botao>
      </div>
    </section>
  );
}
