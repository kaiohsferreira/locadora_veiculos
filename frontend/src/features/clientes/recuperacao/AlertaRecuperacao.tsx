import type { Ref } from 'react';
import { Icone } from '@/components/ui';

export interface AlertaRecuperacaoProps {
  /** Mensagem do alerta geral, ou null quando não há erro geral. */
  mensagem: string | null;
  /** Ref do alerta para o foco depois do erro (o alerta só existe quando há mensagem). */
  alertaRef?: Ref<HTMLDivElement>;
}

/** Alerta geral da tela (erros que não são de um campo). Ícone e texto, sem depender só da cor. */
export function AlertaRecuperacao({ mensagem, alertaRef }: AlertaRecuperacaoProps) {
  if (mensagem === null) return null;
  return (
    <div
      ref={alertaRef}
      role="alert"
      tabIndex={-1}
      className="flex items-start gap-2 rounded-md border border-status-perigo bg-status-perigo-fundo p-3 text-corpo-denso font-medium text-status-perigo focus:outline-none focus-visible:ring-2 focus-visible:ring-status-perigo/30"
    >
      <Icone nome="error" tamanho={20} className="mt-0.5" />
      <p>{mensagem}</p>
    </div>
  );
}
