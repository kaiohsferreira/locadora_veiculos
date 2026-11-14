import { BotaoLink } from '@/components/ui';
import { formatarData } from '@/lib/formatters';
import {
  MENSAGEM_PAINEL_BOTAO_CADASTRAR_CABECALHO,
  MENSAGEM_PAINEL_NOME_SISTEMA,
  MENSAGEM_PAINEL_TITULO_PAGINA,
  mensagemPainelPeriodoAnunciado,
  mensagemPainelResumoDaOperacao,
} from '@/lib/mensagens';
import { CAMINHOS } from '@/routes/caminhos';
import type { PeriodoVisaoGeral } from '@/types/painel';
import { rotuloPeriodo } from '@/features/visao-geral/periodo';
import { SeletorPeriodo } from '@/features/visao-geral/componentes/SeletorPeriodo';

export interface CabecalhoVisaoProps {
  /** Nome fantasia da locadora da sessão. */
  nomeLocadora: string | undefined;
  /** Saudação com o primeiro nome, já pela hora local. */
  saudacao: string;
  /** Data de hoje, para o subtítulo. */
  dataHoje: Date;
  periodo: PeriodoVisaoGeral;
  aoMudarPeriodo: (periodo: PeriodoVisaoGeral) => void;
}

/**
 * Cabeçalho da tela 39: rótulo com a locadora, saudação, data de hoje, seletor de período e o botão de cadastrar veículo.
 * Fica montado durante a troca de período (só os números são refeitos).
 */
export function CabecalhoVisao({ nomeLocadora, saudacao, dataHoje, periodo, aoMudarPeriodo }: CabecalhoVisaoProps) {
  return (
    <header className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
      <div className="flex min-w-0 flex-col">
        <p className="mb-1 flex flex-wrap items-center gap-2 text-legenda font-semibold uppercase tracking-wide text-texto-suave">
          <span>{nomeLocadora || MENSAGEM_PAINEL_NOME_SISTEMA}</span>
          <span aria-hidden="true">·</span>
          <span>{MENSAGEM_PAINEL_TITULO_PAGINA}</span>
        </p>
        <h1 className="text-titulo text-navy">{saudacao}</h1>
        <p className="mt-1 text-corpo-denso text-texto-suave">{mensagemPainelResumoDaOperacao(formatarData(dataHoje))}</p>
      </div>

      <div className="flex flex-wrap items-center gap-3">
        <SeletorPeriodo periodo={periodo} aoMudar={aoMudarPeriodo} />
        <BotaoLink to={CAMINHOS.frotaNovoVeiculo}>{MENSAGEM_PAINEL_BOTAO_CADASTRAR_CABECALHO}</BotaoLink>
      </div>

      <output className="sr-only">{mensagemPainelPeriodoAnunciado(rotuloPeriodo(periodo))}</output>
    </header>
  );
}
