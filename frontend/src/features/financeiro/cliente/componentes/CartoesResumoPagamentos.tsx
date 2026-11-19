import { Esqueleto, Icone } from '@/components/ui';
import {
  MENSAGEM_PAGAMENTOS_CARTAO_LIQUIDADO,
  MENSAGEM_PAGAMENTOS_CARTAO_PENDENTES,
  MENSAGEM_PAGAMENTOS_REGULARIZAR,
  MENSAGEM_PAGAMENTOS_SEM_PENDENTES,
  mensagemCobrancasQuitadas,
  mensagemExercicio,
  mensagemFaturasAguardandoRegularizacao,
  MENSAGEM_PAGAMENTOS_BOTAO_EM_BREVE,
} from '@/lib/mensagens';
import type { ResumoMeusPagamentos } from '@/types/pagamentos';
import { BotaoPagarDesligado } from './BotaoPagarDesligado';

export interface CartoesResumoPagamentosProps {
  /** Resumo já carregado; nulo enquanto carrega. */
  resumo: ResumoMeusPagamentos | null;
}

/** Os dois cartões do topo: total liquidado no ano e cobranças pendentes (desenho, sem o cartão de forma padrão). */
export function CartoesResumoPagamentos({ resumo }: CartoesResumoPagamentosProps) {
  const carregando = resumo === null;
  const temPendentes = resumo !== null && resumo.quantidadePendentes > 0;

  return (
    <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
      <article className="group relative flex flex-col justify-between overflow-hidden rounded-cartao bg-branco p-6 shadow-sm transition-shadow hover:shadow-md">
        <div aria-hidden="true" className="pointer-events-none absolute -right-6 -top-6 h-24 w-24 rounded-xl bg-steel-alto/40 blur-xl transition-transform group-hover:scale-125" />
        <div>
          <div className="mb-2 flex items-center justify-between">
            <span className="text-rotulo font-semibold uppercase tracking-wider text-texto-apoio">
              {MENSAGEM_PAGAMENTOS_CARTAO_LIQUIDADO}
            </span>
            <span className="flex h-8 w-8 items-center justify-center rounded bg-steel-alto text-navy-profundo">
              <Icone nome="verified" tamanho={20} />
            </span>
          </div>
          {carregando ? (
            <Esqueleto className="h-10 w-48" />
          ) : (
            <p className="text-titulo-hero font-bold tracking-tight text-navy-profundo">{resumo.totalLiquidadoAnoStr}</p>
          )}
        </div>
        <div className="mt-4 flex items-center justify-between pt-4 text-apoio text-texto-apoio">
          {carregando ? (
            <Esqueleto className="h-4 w-40" />
          ) : (
            <>
              <span className="flex items-center gap-1.5 font-medium text-navy-profundo">
                <span className="h-2 w-2 rounded-full bg-pagamento-sucesso-ponto" aria-hidden="true" />
                {mensagemCobrancasQuitadas(resumo.quantidadeQuitadasAno)}
              </span>
              <span className="text-dado-tabular text-texto-apoio">{mensagemExercicio(resumo.anoExercicio)}</span>
            </>
          )}
        </div>
      </article>

      <article className="group relative flex flex-col justify-between overflow-hidden rounded-cartao bg-branco p-6 shadow-sm transition-shadow hover:shadow-md">
        <div aria-hidden="true" className="absolute left-0 right-0 top-0 h-1 bg-frota-oficina" />
        <div aria-hidden="true" className="pointer-events-none absolute -right-6 -top-6 h-24 w-24 rounded-xl bg-frota-oficina-fundo/40 blur-xl transition-transform group-hover:scale-125" />
        <div>
          <div className="mb-2 flex items-center justify-between">
            <span className="flex items-center gap-1 text-rotulo font-bold uppercase tracking-wider text-frota-oficina">
              <Icone nome="warning" tamanho={16} />
              {MENSAGEM_PAGAMENTOS_CARTAO_PENDENTES}
            </span>
            <span className="flex h-8 w-8 items-center justify-center rounded bg-frota-oficina-fundo text-pagamento-texto-pendente">
              <Icone nome="notifications_active" tamanho={20} />
            </span>
          </div>
          {carregando ? (
            <Esqueleto className="h-10 w-48" />
          ) : (
            <p className="text-titulo-hero font-bold tracking-tight text-frota-oficina">{resumo.totalPendenteStr}</p>
          )}
        </div>
        <div className="mt-4 flex items-center justify-between gap-2 pt-4 text-apoio">
          {carregando ? (
            <Esqueleto className="h-4 w-56" />
          ) : (
            <>
              <span className={temPendentes ? 'font-semibold text-frota-oficina' : 'font-medium text-texto-apoio'}>
                {temPendentes
                  ? mensagemFaturasAguardandoRegularizacao(resumo.quantidadePendentes)
                  : MENSAGEM_PAGAMENTOS_SEM_PENDENTES}
              </span>
              {temPendentes && (
                <BotaoPagarDesligado variante="cartao" rotulo={MENSAGEM_PAGAMENTOS_REGULARIZAR} />
              )}
            </>
          )}
        </div>
        {temPendentes && <p className="mt-2 text-apoio text-texto-apoio">{MENSAGEM_PAGAMENTOS_BOTAO_EM_BREVE}</p>}
      </article>
    </div>
  );
}
