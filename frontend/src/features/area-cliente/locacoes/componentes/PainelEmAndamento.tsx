import type { ReactNode } from 'react';
import { BotaoLink, EstadoErro, EstadoVazio, Esqueleto, Icone, Paginacao } from '@/components/ui';
import { AvisoFotosReservas } from '@/features/area-cliente/reservas/componentes/AvisoFotosReservas';
import { CartaoLocacaoAtiva } from '@/features/area-cliente/locacoes/componentes/CartaoLocacaoAtiva';
import { SecaoUltimasLocacoes } from '@/features/area-cliente/locacoes/componentes/SecaoUltimasLocacoes';
import { mensagemErroLista, TAMANHO_PAGINA_LOCACOES } from '@/features/area-cliente/locacoes/regrasLocacoes';
import { primeiraMensagemErroFotoCapa } from '@/lib/fotosVeiculo';
import { MENSAGEM_CARREGANDO } from '@/lib/mensagens';
import {
  mensagemLocacoesCabecalhoAtivas,
  MENSAGEM_LOCACOES_ERRO_TITULO,
  MENSAGEM_LOCACOES_RODAPE_ITENS,
  MENSAGEM_LOCACOES_VAZIO_ANDAMENTO_DESCRICAO,
  MENSAGEM_LOCACOES_VAZIO_ANDAMENTO_TITULO,
  MENSAGEM_LOCACOES_VER_RESERVAS,
} from '@/lib/mensagensLocacoesCliente';
import { MENSAGEM_RESERVAS_ERRO_ATUALIZACAO } from '@/lib/mensagensReservas';
import { CAMINHOS } from '@/routes/caminhos';
import type { RespostaLocacoes } from '@/types/locacoesCliente';

/** Estado de uma lista de locações (uma aba), como a página entrega aos painéis. */
export interface EstadoListaLocacoes {
  /** Última resposta conhecida (pode ser de uma consulta anterior da mesma aba). */
  dados: RespostaLocacoes | undefined;
  /** Não há nenhum dado ainda: mostra o esqueleto. */
  carregando: boolean;
  /** Erro da última consulta; undefined/null quando não há erro. */
  erro: unknown;
  /** Consulta em andamento (nova tentativa, troca de página). */
  atualizando: boolean;
  aoTentarNovamente: () => void;
}

export interface PainelEmAndamentoProps {
  estado: EstadoListaLocacoes;
  pagina: number;
  aoMudarPagina: (pagina: number) => void;
  /** Foto de capa que falhou em um cartão: a tela refaz a consulta uma vez. */
  aoFalharFotoCapa: () => void;
  /** Leva para a aba Histórico (botão "Ver todas" da seção de últimas locações). */
  aoVerHistorico: () => void;
}

/**
 * Aba Em andamento (desenho 27): a locação ativa com o cronômetro, a paginação, a seção "Últimas locações do
 * histórico" e os estados de carregando, vazio e erro. Erro com dado já na tela mantém o dado e avisa.
 */
export function PainelEmAndamento({
  estado,
  pagina,
  aoMudarPagina,
  aoFalharFotoCapa,
  aoVerHistorico,
}: PainelEmAndamentoProps) {
  const { dados, carregando, erro, atualizando, aoTentarNovamente } = estado;
  /** A lista falhou e não há dado: o cartão de erro desta aba é o único aviso (sem um segundo cartão de erro). */
  const falhouSemDados = !carregando && Boolean(erro) && !dados;

  let conteudo: ReactNode;
  if (carregando) {
    conteudo = (
      <div aria-busy="true" className="overflow-hidden rounded-2xl bg-branco p-4 shadow-xl lg:p-6">
        <div className="grid gap-8 xl:grid-cols-12">
          <div className="flex flex-col gap-4 xl:col-span-5">
            <Esqueleto className="h-5 w-40" />
            <Esqueleto className="aspect-[16/10] w-full" />
            <Esqueleto className="h-16 w-full" />
          </div>
          <div className="flex flex-col gap-4 xl:col-span-7">
            <Esqueleto className="h-6 w-56" />
            <Esqueleto className="h-24 w-full" />
            <Esqueleto className="h-40 w-full" />
          </div>
        </div>
        <span className="sr-only">{MENSAGEM_CARREGANDO}</span>
      </div>
    );
  } else if (falhouSemDados) {
    conteudo = (
      <section className="overflow-hidden rounded-cartao bg-branco shadow-cartao">
        <EstadoErro
          titulo={MENSAGEM_LOCACOES_ERRO_TITULO}
          mensagem={mensagemErroLista(erro)}
          aoTentarNovamente={aoTentarNovamente}
          tentando={atualizando}
        />
      </section>
    );
  } else if (!dados || dados.total === 0) {
    conteudo = (
      <section className="overflow-hidden rounded-cartao bg-branco shadow-cartao">
        <EstadoVazio
          titulo={MENSAGEM_LOCACOES_VAZIO_ANDAMENTO_TITULO}
          descricao={MENSAGEM_LOCACOES_VAZIO_ANDAMENTO_DESCRICAO}
          icone="directions_car"
          acao={
            <BotaoLink to={CAMINHOS.minhasReservas} variante="contorno">
              {MENSAGEM_LOCACOES_VER_RESERVAS}
            </BotaoLink>
          }
        />
      </section>
    );
  } else {
    const erroFotoCapa = primeiraMensagemErroFotoCapa(dados.itens);
    conteudo = (
      <div aria-busy={atualizando} className="flex flex-col gap-6">
        <h2 className="flex items-center gap-2 text-rotulo font-semibold uppercase tracking-wider text-texto-apoio">
          <Icone nome="radar" tamanho={16} className="text-frota-oficina" />
          {mensagemLocacoesCabecalhoAtivas(dados.total)}
        </h2>
        {erro ? (
          <p role="alert" className="rounded-cartao bg-status-perigo-fundo px-4 py-3 text-corpo-denso text-status-perigo">
            {MENSAGEM_RESERVAS_ERRO_ATUALIZACAO}
          </p>
        ) : null}
        {erroFotoCapa !== null && <AvisoFotosReservas mensagem={erroFotoCapa} />}

        {dados.itens.map((locacao) => (
          <CartaoLocacaoAtiva key={locacao.idLocacao} locacao={locacao} aoFalharFotoCapa={aoFalharFotoCapa} />
        ))}

        {dados.total > TAMANHO_PAGINA_LOCACOES && (
          <div className="overflow-hidden rounded-cartao bg-branco shadow-cartao">
            <Paginacao
              pagina={pagina}
              tamanhoPagina={TAMANHO_PAGINA_LOCACOES}
              total={dados.total}
              aoMudarPagina={aoMudarPagina}
              desabilitada={atualizando}
              rotuloItens={MENSAGEM_LOCACOES_RODAPE_ITENS}
            />
          </div>
        )}
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-2">
      {conteudo}
      {/* Com a lista em erro (sem dado), a seção não é montada: não aparece um segundo cartão de erro. */}
      {!falhouSemDados && <SecaoUltimasLocacoes aoVerTodas={aoVerHistorico} />}
    </div>
  );
}
