import { Esqueleto } from '@/components/ui';
import { formatarNumero, VALOR_AUSENTE } from '@/lib/formatters';
import { MENSAGEM_VERIFICACAO_CARTAO_PENDENTES, MENSAGEM_VERIFICACAO_CARTAO_UNIDADE } from '@/lib/mensagens';

export interface CartaoPendentesVerificacaoProps {
  /** Total de cadastros pendentes, ou nulo enquanto carrega ou quando a consulta falha. */
  total: number | null;
  carregando: boolean;
}

/**
 * Cartão "Cadastros Pendentes" (tela 18): o total da fila com status pendente. Os outros cartões do desenho (tempo médio,
 * aprovados hoje e OCR) não existem nesta entrega.
 */
export function CartaoPendentesVerificacao({ total, carregando }: CartaoPendentesVerificacaoProps) {
  return (
    <section className="relative flex flex-col justify-between overflow-hidden rounded-cartao bg-branco p-4 shadow-cartao">
      <div
        aria-hidden="true"
        className="pointer-events-none absolute -right-3 -top-3 h-16 w-16 rounded-full bg-orange-acao/10"
      />
      <h2 className="relative mb-2 text-legenda font-semibold uppercase tracking-wider text-texto-apoio">
        {MENSAGEM_VERIFICACAO_CARTAO_PENDENTES}
      </h2>
      {carregando ? (
        <Esqueleto className="h-10 w-24" />
      ) : (
        <div className="relative flex items-baseline gap-2">
          <span className="text-titulo-hero font-bold text-navy-profundo">
            {total === null ? VALOR_AUSENTE : formatarNumero(total)}
          </span>
          <span className="text-corpo-denso text-texto-apoio">{MENSAGEM_VERIFICACAO_CARTAO_UNIDADE}</span>
        </div>
      )}
    </section>
  );
}
