import type { Ref } from 'react';
import { BotaoLink, Icone } from '@/components/ui';
import {
  RECUPERACAO_CONFIRMACAO_BOTAO_VOLTAR,
  RECUPERACAO_CONFIRMACAO_MENSAGEM,
  RECUPERACAO_CONFIRMACAO_TITULO,
  RECUPERACAO_CONFIRMACAO_VALIDADE,
} from '@/lib/mensagens';
import { CAMINHOS } from '@/routes/caminhos';
import type { EstadoRecuperacao } from './estadoRecuperacao';

export interface ConfirmacaoRecuperacaoProps {
  /** Título recebe o foco logo após o envio. A mensagem (não o cartão) é a região que o leitor anuncia. */
  tituloRef: Ref<HTMLHeadingElement>;
  /** Estado para "Voltar ao login": mesmo perfil marcado e mesmo caminho de retorno. */
  estadoVoltar: EstadoRecuperacao;
}

/**
 * Confirmação depois do 202. O texto é o mesmo em qualquer resposta: a tela nunca diz se a conta existe.
 */
export function ConfirmacaoRecuperacao({ tituloRef, estadoVoltar }: ConfirmacaoRecuperacaoProps) {
  return (
    <div className="flex flex-col">
      <h1 ref={tituloRef} tabIndex={-1} className="text-titulo text-navy focus:outline-none">
        {RECUPERACAO_CONFIRMACAO_TITULO}
      </h1>

      <div className="mt-6 flex items-start gap-3 rounded-md border border-steel bg-steel-claro p-4 text-corpo-denso text-navy">
        <Icone nome="mail" tamanho={20} className="mt-0.5 text-steel" />
        <div className="flex flex-col gap-2">
          <output className="block">{RECUPERACAO_CONFIRMACAO_MENSAGEM}</output>
          <p>{RECUPERACAO_CONFIRMACAO_VALIDADE}</p>
        </div>
      </div>

      <BotaoLink to={CAMINHOS.entrar} state={estadoVoltar} larguraTotal className="mt-8">
        {RECUPERACAO_CONFIRMACAO_BOTAO_VOLTAR}
      </BotaoLink>
    </div>
  );
}
