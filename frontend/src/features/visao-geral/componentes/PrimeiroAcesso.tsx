import { BotaoLink, Icone } from '@/components/ui';
import {
  MENSAGEM_PAINEL_BOTAO_CADASTRAR_PASSO,
  MENSAGEM_PAINEL_CONFIGURACAO_INICIAL,
  MENSAGEM_PAINEL_PASSO_CONCLUIDO,
  MENSAGEM_PAINEL_PASSO_PROXIMO,
  MENSAGEM_PAINEL_PRIMEIROS_PASSOS,
  MENSAGEM_PAINEL_PRIMEIROS_PASSOS_DESCRICAO,
  MENSAGEM_PAINEL_PROGRESSO_ATIVACAO,
  mensagemPainelBoasVindas,
  mensagemPainelEtapaEmAndamento,
  mensagemPainelFaltamPassos,
  mensagemPainelProgresso,
} from '@/lib/mensagens';
import { CAMINHOS } from '@/routes/caminhos';
import { etapaEmAndamento, type PassoAtivacao, type ProgressoAtivacao } from '@/features/visao-geral/primeirosPassos';

export interface FaixaBoasVindasProps {
  nomeLocadora: string | undefined;
  progresso: ProgressoAtivacao;
}

/** Faixa escura de boas-vindas com o nome da locadora e o progresso da ativação (desenho da tela 40). */
export function FaixaBoasVindas({ nomeLocadora, progresso }: FaixaBoasVindasProps) {
  return (
    <section
      aria-labelledby="titulo-boas-vindas"
      className="flex flex-col gap-6 rounded-cartao bg-navy p-6 text-branco shadow-cartao md:flex-row md:items-center md:justify-between"
    >
      <div className="flex min-w-0 max-w-2xl flex-col gap-2">
        <span className="inline-flex w-fit items-center gap-2 rounded-full bg-orange/20 px-3 py-1 text-legenda font-semibold uppercase tracking-wide text-orange-claro">
          <span className="h-1.5 w-1.5 rounded-full bg-orange" aria-hidden="true" />
          {MENSAGEM_PAINEL_CONFIGURACAO_INICIAL}
        </span>
        <h1 id="titulo-boas-vindas" className="text-titulo text-branco">
          {mensagemPainelBoasVindas(nomeLocadora)}
        </h1>
        <p className="text-corpo text-steel-palido">{mensagemPainelFaltamPassos(progresso.restantes)}</p>
      </div>

      <div className="flex w-full min-w-0 flex-col gap-2 rounded-cartao bg-navy-escuro/60 p-4 md:w-auto md:min-w-[240px]">
        <div className="flex items-center justify-between gap-2 text-legenda font-semibold">
          <span className="text-steel-palido">{MENSAGEM_PAINEL_PROGRESSO_ATIVACAO}</span>
          <span className="text-branco">{mensagemPainelProgresso(progresso.concluidos, progresso.total)}</span>
        </div>
        <progress
          aria-label={MENSAGEM_PAINEL_PROGRESSO_ATIVACAO}
          max={progresso.total}
          value={progresso.concluidos}
          className="h-2.5 w-full appearance-none overflow-hidden rounded-full border-0 bg-navy-claro [&::-moz-progress-bar]:bg-orange [&::-webkit-progress-bar]:rounded-full [&::-webkit-progress-bar]:bg-navy-claro [&::-webkit-progress-value]:rounded-full [&::-webkit-progress-value]:bg-orange"
        />
      </div>
    </section>
  );
}

export interface PrimeirosPassosProps {
  passos: readonly PassoAtivacao[];
}

/** Cartão "Primeiros passos" com os dois passos e o botão do próximo (cadastro do veículo). */
export function PrimeirosPassos({ passos }: PrimeirosPassosProps) {
  const etapa = etapaEmAndamento(passos);
  return (
    <section aria-labelledby="titulo-primeiros-passos" className="flex flex-col gap-4 rounded-cartao bg-branco p-6 shadow-cartao">
      <div className="flex flex-wrap items-start justify-between gap-2">
        <div className="flex min-w-0 flex-col">
          <h2 id="titulo-primeiros-passos" className="text-secao text-navy">
            {MENSAGEM_PAINEL_PRIMEIROS_PASSOS}
          </h2>
          <p className="mt-1 text-corpo-denso text-texto-suave">{MENSAGEM_PAINEL_PRIMEIROS_PASSOS_DESCRICAO}</p>
        </div>
        <span className="rounded bg-steel-suave px-2 py-0.5 text-legenda font-semibold uppercase tracking-wide text-navy">
          {mensagemPainelEtapaEmAndamento(etapa)}
        </span>
      </div>

      <ol className="grid gap-4 md:grid-cols-2">
        {passos.map((passo) => (
          <PassoItem key={passo.id} passo={passo} />
        ))}
      </ol>
    </section>
  );
}

interface PassoItemProps {
  passo: PassoAtivacao;
}

/** Um passo: número, situação, título, descrição e, no próximo passo, o botão de ação. */
function PassoItem({ passo }: PassoItemProps) {
  const concluido = passo.situacao === 'concluido';
  return (
    <li
      className={`flex flex-col justify-between gap-4 rounded-cartao p-4 ${
        concluido ? 'bg-steel-fundo' : 'bg-branco ring-2 ring-orange'
      }`}
    >
      <div className="flex flex-col gap-2">
        <div className="flex items-center justify-between gap-2">
          <span
            className={`flex h-7 w-7 items-center justify-center rounded-full text-corpo-denso font-semibold ${
              concluido ? 'bg-branco text-navy shadow-cartao' : 'bg-orange text-branco'
            }`}
          >
            {passo.numero}
          </span>
          {concluido ? (
            <span className="inline-flex items-center gap-1 text-legenda font-semibold text-status-sucesso">
              <Icone nome="check_circle" tamanho={16} preenchido />
              {MENSAGEM_PAINEL_PASSO_CONCLUIDO}
            </span>
          ) : (
            <span className="rounded bg-orange-claro px-2 py-0.5 text-legenda font-semibold text-orange-escuro">
              {MENSAGEM_PAINEL_PASSO_PROXIMO}
            </span>
          )}
        </div>
        <h3 className="text-corpo-denso font-semibold text-navy">{passo.titulo}</h3>
        <p className="text-corpo-denso text-texto-suave">{passo.descricao}</p>
      </div>
      {!concluido && (
        <BotaoLink to={CAMINHOS.frotaNovoVeiculo} larguraTotal icone="add">
          {MENSAGEM_PAINEL_BOTAO_CADASTRAR_PASSO}
        </BotaoLink>
      )}
    </li>
  );
}
