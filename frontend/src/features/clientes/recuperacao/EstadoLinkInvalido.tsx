import type { Ref } from 'react';
import { BotaoLink, Icone } from '@/components/ui';
import {
  REDEFINICAO_BOTAO_NOVO_LINK,
  REDEFINICAO_LINK_INVALIDO_MENSAGEM,
  REDEFINICAO_LINK_INVALIDO_TITULO,
} from '@/lib/mensagens';
import { CAMINHOS } from '@/routes/caminhos';

export interface EstadoLinkInvalidoProps {
  /** Título recebe o foco quando o link deixa de valer no envio. */
  tituloRef: Ref<HTMLHeadingElement>;
}

/**
 * Estado sem formulário: o link não tem token válido (ausente, expirado, usado ou recusado pela API).
 * O mesmo texto para todos os casos, para não revelar o motivo.
 */
export function EstadoLinkInvalido({ tituloRef }: EstadoLinkInvalidoProps) {
  return (
    <div className="flex flex-col items-start gap-4">
      <span className="flex h-12 w-12 items-center justify-center rounded-full bg-status-perigo-fundo text-status-perigo">
        <Icone nome="link_off" tamanho={24} />
      </span>
      <h1 ref={tituloRef} tabIndex={-1} className="text-titulo text-navy focus:outline-none">
        {REDEFINICAO_LINK_INVALIDO_TITULO}
      </h1>
      <p className="text-corpo text-texto-suave">{REDEFINICAO_LINK_INVALIDO_MENSAGEM}</p>
      <BotaoLink to={CAMINHOS.recuperarSenha} larguraTotal icone="refresh" className="mt-4">
        {REDEFINICAO_BOTAO_NOVO_LINK}
      </BotaoLink>
    </div>
  );
}
