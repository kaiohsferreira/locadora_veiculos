import {
  MENSAGEM_PAGAMENTOS_DESCRICAO,
  MENSAGEM_PAGAMENTOS_MIGALHA_ATUAL,
  MENSAGEM_PAGAMENTOS_MIGALHA_INICIO,
  MENSAGEM_PAGAMENTOS_NAVEGACAO_CAMINHO,
  MENSAGEM_PAGAMENTOS_TITULO_PAGINA,
} from '@/lib/mensagens';
import { Icone } from '@/components/ui';

/** Migalha de pão e título da página (desenho). */
export function CabecalhoPagamentosCliente() {
  return (
    <div className="flex flex-col gap-1">
      <nav aria-label={MENSAGEM_PAGAMENTOS_NAVEGACAO_CAMINHO} className="flex items-center gap-1 text-micro text-texto-apoio">
        <span>{MENSAGEM_PAGAMENTOS_MIGALHA_INICIO}</span>
        <Icone nome="chevron_right" tamanho={14} />
        <span className="font-semibold text-texto-desenho" aria-current="page">
          {MENSAGEM_PAGAMENTOS_MIGALHA_ATUAL}
        </span>
      </nav>
      <div className="pt-1">
        <h1 className="text-titulo-hero font-bold tracking-tight text-navy-profundo">{MENSAGEM_PAGAMENTOS_TITULO_PAGINA}</h1>
        <p className="mt-1 max-w-3xl text-corpo-denso text-texto-apoio">{MENSAGEM_PAGAMENTOS_DESCRICAO}</p>
      </div>
    </div>
  );
}
