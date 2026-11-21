import { Esqueleto, Icone } from '@/components/ui';
import {
  MENSAGEM_FINANCEIRO_ROTULO,
  MENSAGEM_FINANCEIRO_SUBTITULO,
  MENSAGEM_FINANCEIRO_TITULO,
  mensagemFinanceiroPeriodo,
} from '@/lib/mensagens';

export interface CabecalhoPainelFinanceiroProps {
  /** Datas do período já formatadas pelo servidor (de e até). Ausentes enquanto carrega. */
  periodo?: { deStr: string; ateStr: string };
  /** Mostra o esqueleto da linha de período enquanto os indicadores não chegaram. */
  carregandoPeriodo: boolean;
}

/**
 * Cabeçalho da tela 09: rótulo, título e subtítulo do desenho, com a linha de período no lugar do seletor de datas (que foi
 * cortado: o período é o do mês corrente, pelo servidor).
 */
export function CabecalhoPainelFinanceiro({ periodo, carregandoPeriodo }: CabecalhoPainelFinanceiroProps) {
  return (
    <header className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
      <div className="flex max-w-3xl flex-col gap-1">
        <p className="text-micro uppercase tracking-wider text-texto-apoio">{MENSAGEM_FINANCEIRO_ROTULO}</p>
        <h1 className="text-titulo-documentos font-bold tracking-tight text-navy-profundo">{MENSAGEM_FINANCEIRO_TITULO}</h1>
        <p className="text-corpo-denso text-texto-apoio">{MENSAGEM_FINANCEIRO_SUBTITULO}</p>
      </div>

      {(periodo !== undefined || carregandoPeriodo) && (
        <div className="flex shrink-0 items-center gap-2 self-start rounded bg-branco px-3 py-2 shadow-sm lg:self-auto">
          <Icone nome="calendar_today" tamanho={18} className="text-texto-apoio" />
          {periodo !== undefined ? (
            <span className="text-apoio font-medium text-texto-desenho">
              {mensagemFinanceiroPeriodo(periodo.deStr, periodo.ateStr)}
            </span>
          ) : (
            <Esqueleto className="h-4 w-40" />
          )}
        </div>
      )}
    </header>
  );
}
