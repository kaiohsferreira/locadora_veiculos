import { EstadoErro, Esqueleto, Icone } from '@/components/ui';
import {
  MENSAGEM_CARREGANDO,
  MENSAGEM_FINANCEIRO_CARTAO_A_RECEBER,
  MENSAGEM_FINANCEIRO_CARTAO_EM_ATRASO,
  MENSAGEM_FINANCEIRO_CARTAO_RECEBIDO,
  MENSAGEM_FINANCEIRO_CARTAO_TICKET,
  MENSAGEM_FINANCEIRO_LINHA_TICKET,
} from '@/lib/mensagens';
import {
  textoLinhaAReceber,
  textoLinhaEmAtraso,
  textoLinhaRecebido,
} from '@/features/financeiro/painel/regrasPainelFinanceiro';
import type { RespostaIndicadoresFinanceiros } from '@/types/painelFinanceiro';

export interface CartoesIndicadoresFinanceirosProps {
  indicadores: RespostaIndicadoresFinanceiros | undefined;
  carregando: boolean;
  /** Erro sem dados para mostrar. */
  erro: boolean;
  mensagemErro: string | null;
  tentando: boolean;
  aoTentarNovamente: () => void;
}

/**
 * Os quatro cartões do desenho (Recebido, A receber, Em atraso e Ticket médio), com a mesma estrutura: título e valor à
 * esquerda, ícone num quadrado à direita, e a linha de baixo. O cartão de atraso leva o tom de alerta do desenho.
 */
export function CartoesIndicadoresFinanceiros({
  indicadores,
  carregando,
  erro,
  mensagemErro,
  tentando,
  aoTentarNovamente,
}: CartoesIndicadoresFinanceirosProps) {
  if (erro) {
    return (
      <div className="rounded-cartao bg-branco shadow-sm">
        <EstadoErro mensagem={mensagemErro ?? undefined} aoTentarNovamente={aoTentarNovamente} tentando={tentando} />
      </div>
    );
  }

  return (
    <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4" aria-busy={carregando || undefined}>
      {carregando && (
        <output className="sr-only">{MENSAGEM_CARREGANDO}</output>
      )}

      <CartaoIndicador
        titulo={MENSAGEM_FINANCEIRO_CARTAO_RECEBIDO}
        icone="account_balance_wallet"
        valor={indicadores?.recebidoStr}
        carregando={carregando}
        linha={indicadores ? textoLinhaRecebido(indicadores.quantidadeRecebido) : undefined}
      />
      <CartaoIndicador
        titulo={MENSAGEM_FINANCEIRO_CARTAO_A_RECEBER}
        icone="pending_actions"
        valor={indicadores?.aReceberStr}
        carregando={carregando}
        linha={indicadores ? textoLinhaAReceber(indicadores.quantidadeAReceber) : undefined}
        destaqueLinha
      />
      <CartaoIndicador
        titulo={MENSAGEM_FINANCEIRO_CARTAO_EM_ATRASO}
        icone="warning"
        tom="atraso"
        valor={indicadores?.emAtrasoStr}
        carregando={carregando}
        linha={indicadores ? textoLinhaEmAtraso(indicadores.quantidadeEmAtraso) : undefined}
      />
      <CartaoIndicador
        titulo={MENSAGEM_FINANCEIRO_CARTAO_TICKET}
        icone="price_change"
        valor={indicadores?.ticketMedioStr}
        carregando={carregando}
        linha={indicadores ? MENSAGEM_FINANCEIRO_LINHA_TICKET : undefined}
      />
    </div>
  );
}

interface CartaoIndicadorProps {
  titulo: string;
  icone: string;
  tom?: 'padrao' | 'atraso';
  valor: string | undefined;
  carregando: boolean;
  linha: string | undefined;
  /** Linha em seminegrito navy (o número de títulos do cartão A receber, como no desenho). */
  destaqueLinha?: boolean;
}

/** Um cartão de indicador. Enquanto carrega, mostra esqueletos no valor e na linha de baixo. */
function CartaoIndicador({ titulo, icone, tom = 'padrao', valor, carregando, linha, destaqueLinha = false }: CartaoIndicadorProps) {
  const atraso = tom === 'atraso';
  return (
    <article
      className="relative flex flex-col justify-between overflow-hidden rounded-cartao bg-branco p-4 shadow-sm"
    >
      {atraso && (
        <div
          aria-hidden="true"
          className="pointer-events-none absolute right-0 top-0 h-24 w-24 rounded-full bg-manutencao-alerta-trilho/30 blur-xl"
        />
      )}

      <div className="relative flex items-start justify-between gap-2">
        <div className="flex min-w-0 flex-col">
          <p
            className={`flex items-center gap-1 text-micro uppercase tracking-wider ${
              atraso ? 'font-semibold text-manutencao-alerta' : 'text-texto-apoio'
            }`}
          >
            {atraso && <span aria-hidden="true" className="h-1.5 w-1.5 rounded-full bg-manutencao-alerta" />}
            {titulo}
          </p>
          {valor !== undefined ? (
            <p
              className={`mt-1 text-marca font-bold tracking-tight ${atraso ? 'text-manutencao-alerta' : 'text-navy-profundo'}`}
            >
              {valor}
            </p>
          ) : (
            carregando && <Esqueleto className="mt-2 h-6 w-32" />
          )}
        </div>
        <span
          className={`shrink-0 rounded p-2 ${
            atraso ? 'bg-manutencao-alerta-trilho text-manutencao-alerta' : 'bg-steel-fundo text-navy-profundo'
          }`}
        >
          <Icone nome={icone} tamanho={22} />
        </span>
      </div>

      <div
        className={`relative mt-4 rounded p-2.5 pt-3 ${atraso ? 'bg-manutencao-alerta-trilho/20' : 'bg-steel-fundo/50'}`}
      >
        {linha !== undefined ? (
          <p className={`text-micro ${atraso ? 'font-medium text-manutencao-alerta' : destaqueLinha ? 'font-semibold text-navy-profundo' : 'text-texto-apoio'}`}>{linha}</p>
        ) : (
          carregando && <Esqueleto className="h-3 w-3/4" />
        )}
      </div>
    </article>
  );
}
