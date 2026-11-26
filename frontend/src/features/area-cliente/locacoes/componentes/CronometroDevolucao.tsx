import { Icone } from '@/components/ui';
import {
  calcularContagemRegressiva,
  doisDigitos,
  locacaoEstaAtrasada,
} from '@/features/area-cliente/locacoes/regrasLocacoes';
import { VALOR_AUSENTE } from '@/lib/formatters';
import {
  mensagemLocacoesDentroDoPrazo,
  mensagemLocacoesFaltam,
  mensagemLocacoesProgresso,
  MENSAGEM_LOCACOES_CRONOMETRO_TITULO,
  MENSAGEM_LOCACOES_PRAZO_VENCIDO,
  MENSAGEM_LOCACOES_UNIDADES_CRONOMETRO,
} from '@/lib/mensagensLocacoesCliente';

export interface CronometroDevolucaoProps {
  /** Devolução prevista efetiva, como o servidor manda. A tela não recalcula a data. */
  devolucaoPrevistaEfetiva: string;
  /** Tolerância da categoria em minutos; nulo quando a categoria não tem política. */
  toleranciaAtrasoMinutos: number | null;
  /** Locação marcada como atrasada pelo servidor (status ATRASADA). */
  atrasadaServidor: boolean;
  /** Percentual da locação já percorrido (0 a 100); nulo quando não der para calcular. */
  progresso: number | null;
  /** Instante atual, vindo do relógio da tela (1 s). Recebido por prop para a marcação ser testável. */
  agora: Date;
}

interface BlocoCronometro {
  chave: keyof typeof MENSAGEM_LOCACOES_UNIDADES_CRONOMETRO;
  valor: string;
}

/**
 * Cronômetro da devolução (desenho 27): quatro blocos (dias, horas, minutos, segundos) e a barra de progresso. No
 * celular os rótulos são curtos (Dias, Hrs, Min, Seg). Quando o prazo passa, os blocos ficam em zero e a faixa avisa
 * que o atraso pode gerar cobrança extra. Não há aria-live: o tempo restante é lido só como texto único.
 */
export function CronometroDevolucao({
  devolucaoPrevistaEfetiva,
  toleranciaAtrasoMinutos,
  atrasadaServidor,
  progresso,
  agora,
}: CronometroDevolucaoProps) {
  const contagem = calcularContagemRegressiva(devolucaoPrevistaEfetiva, agora);
  const atrasada = locacaoEstaAtrasada(atrasadaServidor, contagem);

  const blocos: BlocoCronometro[] = contagem === null
    ? (['dias', 'horas', 'minutos', 'segundos'] as const).map((chave) => ({ chave, valor: VALOR_AUSENTE }))
    : [
        { chave: 'dias', valor: doisDigitos(contagem.dias) },
        { chave: 'horas', valor: doisDigitos(contagem.horas) },
        { chave: 'minutos', valor: doisDigitos(contagem.minutos) },
        { chave: 'segundos', valor: doisDigitos(contagem.segundos) },
      ];

  const textoLeitor = contagem === null || atrasada
    ? null
    : mensagemLocacoesFaltam(contagem.dias, contagem.horas, contagem.minutos);

  return (
    <div className="flex flex-col gap-3 rounded-xl bg-navy-profundo p-4 text-branco shadow-lg lg:p-6">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div className="flex items-center gap-2">
          <Icone nome="alarm" tamanho={20} className="text-orange-acao" />
          <span className="text-corpo-denso font-semibold uppercase tracking-wide text-steel-alto">
            {MENSAGEM_LOCACOES_CRONOMETRO_TITULO}
          </span>
        </div>
        {atrasada ? (
          <span className="inline-flex items-center gap-1.5 rounded-full bg-status-perigo-fundo px-2.5 py-1 text-legenda font-semibold text-status-perigo">
            <Icone nome="warning" tamanho={14} />
            {MENSAGEM_LOCACOES_PRAZO_VENCIDO}
          </span>
        ) : (
          <span className="inline-flex items-center gap-1.5 rounded-full bg-steel-alto/20 px-2.5 py-1 text-legenda text-branco">
            <span aria-hidden="true" className="h-1.5 w-1.5 rounded-full bg-pagamento-sucesso-ponto" />
            {mensagemLocacoesDentroDoPrazo(toleranciaAtrasoMinutos)}
          </span>
        )}
      </div>

      {textoLeitor !== null && <p className="sr-only">{textoLeitor}</p>}

      <div className="my-1 grid grid-cols-4 gap-2 text-center sm:gap-2">
        {blocos.map(({ chave, valor }) => {
          const unidade = MENSAGEM_LOCACOES_UNIDADES_CRONOMETRO[chave];
          const segundos = chave === 'segundos';
          return (
            <div
              key={chave}
              className={`flex flex-col items-center rounded-lg p-1.5 sm:p-3 ${
                segundos ? 'bg-navy/80 ring-1 ring-orange-acao/40' : 'bg-navy'
              }`}
            >
              <span
                className={`text-titulo-hero font-bold leading-none tracking-tight ${
                  segundos ? 'text-orange-acao' : 'text-branco'
                }`}
              >
                {valor}
              </span>
              <span className="mt-1 text-micro font-semibold uppercase text-steel-apagado">
                <span className="sm:hidden">{unidade.curto}</span>
                <span className="hidden sm:inline">{unidade.longo}</span>
              </span>
            </div>
          );
        })}
      </div>

      {progresso !== null && (
        <>
          <p className="sr-only">{mensagemLocacoesProgresso(progresso)}</p>
          <div aria-hidden="true" className="h-1.5 w-full overflow-hidden rounded-full bg-steel-linha/20">
            <div className="h-full rounded-full bg-orange-acao" style={{ width: `${progresso}%` }} />
          </div>
        </>
      )}
    </div>
  );
}
