import type { ReactNode, Ref } from 'react';
import { Icone } from '@/components/ui';

export interface AlertaGoogleProps {
  /** Texto do alerta, já convertido por `obterMensagemErro` ou vindo de `@/lib/mensagens`. */
  mensagem: string;
  /** Ref para o foco: o alerta recebe o foco quando aparece. */
  alertaRef?: Ref<HTMLDivElement>;
  /** Ações extras (ex.: botão "Voltar ao login"). */
  children?: ReactNode;
}

/** Alerta de erro do login com Google. Ícone e texto, sem depender só da cor. */
export function AlertaGoogle({ mensagem, alertaRef, children }: AlertaGoogleProps) {
  return (
    <div
      ref={alertaRef}
      role="alert"
      tabIndex={-1}
      className="flex flex-col gap-3 rounded-md border border-status-perigo bg-status-perigo-fundo p-3 text-corpo-denso font-medium text-status-perigo focus:outline-none focus-visible:ring-2 focus-visible:ring-status-perigo/30"
    >
      <div className="flex items-start gap-2">
        <Icone nome="error" tamanho={20} className="mt-0.5" />
        <p>{mensagem}</p>
      </div>
      {children}
    </div>
  );
}
