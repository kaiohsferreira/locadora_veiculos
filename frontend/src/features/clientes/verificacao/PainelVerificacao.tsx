import type { ReactNode } from 'react';
import { Icone } from '@/components/ui';
import { MENSAGEM_VERIFICACAO_PAINEL_FECHAR, MENSAGEM_VERIFICACAO_PAINEL_TITULO } from '@/lib/mensagens';

export interface PainelVerificacaoProps {
  /** Nome do cliente aberto (abaixo do título). Sem cliente, o painel mostra só o título. */
  nomeCliente: string | null;
  aoFechar: () => void;
  children: ReactNode;
}

/**
 * Moldura do painel lateral da tela 18: título "Auditoria Cadastral", nome do cliente, botão de fechar e o conteúdo.
 * Fica fixo ao lado da tabela no desktop.
 */
export function PainelVerificacao({ nomeCliente, aoFechar, children }: PainelVerificacaoProps) {
  return (
    <aside
      aria-label={MENSAGEM_VERIFICACAO_PAINEL_TITULO}
      className="flex flex-col gap-4 rounded-cartao bg-branco p-4 shadow-cartao xl:sticky xl:top-20"
    >
      <header className="flex items-start justify-between gap-2 border-b border-steel-suave pb-3">
        <div className="flex min-w-0 flex-col">
          <h2 className="text-nome-veiculo text-navy-profundo">{MENSAGEM_VERIFICACAO_PAINEL_TITULO}</h2>
          {nomeCliente !== null && <p className="truncate text-corpo-denso text-texto-apoio">{nomeCliente}</p>}
        </div>
        <button
          type="button"
          onClick={aoFechar}
          aria-label={MENSAGEM_VERIFICACAO_PAINEL_FECHAR}
          title={MENSAGEM_VERIFICACAO_PAINEL_FECHAR}
          className="shrink-0 rounded-md p-1 text-texto-apoio transition-colors hover:bg-steel-fundo hover:text-navy-profundo focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-orange-acao"
        >
          <Icone nome="close" tamanho={20} />
        </button>
      </header>
      {children}
    </aside>
  );
}
