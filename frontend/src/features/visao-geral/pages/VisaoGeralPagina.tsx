import { useState } from 'react';
import { EstadoErro } from '@/components/ui';
import { useTituloDocumento } from '@/hooks/useTituloDocumento';
import { obterMensagemErro } from '@/lib/erros';
import {
  MENSAGEM_PAINEL_CARREGANDO_RESUMO,
  MENSAGEM_PAINEL_ERRO_CARREGAR,
  MENSAGEM_PAINEL_TITULO_PAGINA,
} from '@/lib/mensagens';
import { useAutenticacao } from '@/routes/autenticacao/useAutenticacao';
import { PERIODO_PADRAO_VISAO_GERAL, type PeriodoVisaoGeral } from '@/types/painel';
import { CabecalhoVisao } from '@/features/visao-geral/componentes/CabecalhoVisao';
import { FaixaBoasVindas, PrimeirosPassos } from '@/features/visao-geral/componentes/PrimeiroAcesso';
import { IndicadoresVisao } from '@/features/visao-geral/componentes/IndicadoresVisao';
import { SituacaoFrota } from '@/features/visao-geral/componentes/SituacaoFrota';
import { usePainelVisaoGeral } from '@/features/visao-geral/hooks/usePainelVisaoGeral';
import { montarCartoesIndicadores } from '@/features/visao-geral/indicadores';
import { montarLegendaFrota } from '@/features/visao-geral/frota';
import { prepararPrimeiroAcesso } from '@/features/visao-geral/primeirosPassos';
import { montarSaudacao } from '@/features/visao-geral/saudacao';
import { escolherVariante } from '@/features/visao-geral/variante';

/**
 * P11-FE-01 · Visão geral da locadora. Tela 39 (completa) ou tela 40 (primeiro acesso), pela flag `primeiroAcesso` da
 * resposta. Trocar o período refaz só a consulta: o cabeçalho e o seletor ficam na tela.
 */
export default function VisaoGeralPagina() {
  useTituloDocumento(MENSAGEM_PAINEL_TITULO_PAGINA);
  const { usuario } = useAutenticacao();
  const [periodo, setPeriodo] = useState<PeriodoVisaoGeral>(PERIODO_PADRAO_VISAO_GERAL);
  /** Data e hora da abertura da tela: a saudação e a data não mudam a cada renderização. */
  const [agora] = useState(() => new Date());
  const consulta = usePainelVisaoGeral(periodo);
  const dados = consulta.data;
  const nomeLocadora = usuario?.empresaNomeFantasia;
  const carregandoResumo = consulta.isPending ? (
    <output className="sr-only">{MENSAGEM_PAINEL_CARREGANDO_RESUMO}</output>
  ) : null;

  // Tela 40: a flag vem da própria resposta. Com erro, a tela 39 (com o aviso) é a que mostra o erro e o período.
  if (dados !== undefined && !consulta.isError && escolherVariante(dados) === 'primeiro-acesso') {
    const { progresso, passos } = prepararPrimeiroAcesso(dados.primeiroAcesso);
    return (
      <div className="flex flex-col gap-6" aria-busy={consulta.isFetching || undefined}>
        <FaixaBoasVindas nomeLocadora={nomeLocadora} progresso={progresso} />
        <PrimeirosPassos passos={passos} />
        <IndicadoresVisao cartoes={montarCartoesIndicadores(dados, { primeiroAcesso: true })} />
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-6">
      <CabecalhoVisao
        nomeLocadora={nomeLocadora}
        saudacao={montarSaudacao(agora.getHours(), usuario?.nome)}
        dataHoje={agora}
        periodo={periodo}
        aoMudarPeriodo={setPeriodo}
      />
      {carregandoResumo}

      {consulta.isError ? (
        <section className="rounded-cartao bg-branco shadow-cartao">
          <EstadoErro
            titulo={MENSAGEM_PAINEL_ERRO_CARREGAR}
            mensagem={obterMensagemErro(consulta.error)}
            aoTentarNovamente={() => void consulta.refetch()}
            tentando={consulta.isFetching}
          />
        </section>
      ) : (
        <div className="flex flex-col gap-6" aria-busy={consulta.isFetching || undefined}>
          <IndicadoresVisao cartoes={dados ? montarCartoesIndicadores(dados) : undefined} />
          {/* Linha de 3 colunas no desktop: a situação da frota ocupa 1; a linha de receita (2 colunas) entra depois, nesta mesma linha. */}
          <div className="grid gap-6 lg:grid-cols-3">
            <div className="lg:col-span-1">
              <SituacaoFrota
                linhas={dados ? montarLegendaFrota(dados.frotaPorSituacao) : undefined}
                totalVeiculos={dados?.totalVeiculos}
              />
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
