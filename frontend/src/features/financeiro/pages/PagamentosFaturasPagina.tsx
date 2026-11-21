import { useRef, useState } from 'react';
import { EstadoErro } from '@/components/ui';
import { CabecalhoPagamentosCliente } from '@/features/financeiro/cliente/componentes/CabecalhoPagamentosCliente';
import { BarraFiltrosPagamentos } from '@/features/financeiro/cliente/componentes/BarraFiltrosPagamentos';
import { CartoesResumoPagamentos } from '@/features/financeiro/cliente/componentes/CartoesResumoPagamentos';
import { FaixaPendenciaPagamentos } from '@/features/financeiro/cliente/componentes/FaixaPendenciaPagamentos';
import { GavetaPixPagamento } from '@/features/financeiro/cliente/componentes/GavetaPixPagamento';
import { TabelaPagamentosCliente } from '@/features/financeiro/cliente/componentes/TabelaPagamentosCliente';
import {
  aplicarMudancaFiltro,
  cobrancaParaGaveta,
  destinoBotaoPagarResumo,
  FILTRO_INICIAL_PAGAMENTOS_CLIENTE,
  type MudancaFiltroPagamentos,
  opcoesPeriodo,
  temFiltroAplicado,
  ultimaPaginaPagamentos,
} from '@/features/financeiro/cliente/regrasPagamentosCliente';
import {
  useBuscarUnicaPendencia,
  useMeusPagamentos,
  useResumoMeusPagamentos,
} from '@/features/financeiro/hooks/usePagamentosCliente';
import { useTituloDocumento } from '@/hooks/useTituloDocumento';
import {
  MENSAGEM_PAGAMENTOS_ERRO_RESUMO,
  MENSAGEM_PAGAMENTOS_SEM_PENDENTES,
  MENSAGEM_PAGAMENTOS_TITULO_PAGINA,
} from '@/lib/mensagens';
import { obterMensagemErro } from '@/lib/erros';
import type { FiltroMeusPagamentos, MeuPagamento } from '@/types/pagamentos';

/**
 * P7-FE-02 · Pagamentos e Faturas do cliente (tela 30, RF-36). Mostra só as cobranças do próprio cliente (o servidor
 * decide quais). Os botões de pagar abrem a gaveta de PIX (P7-FE-02): uma cobrança pendente abre a gaveta dela; mais de
 * uma leva à aba Pendentes.
 */
export default function PagamentosFaturasPagina() {
  useTituloDocumento(MENSAGEM_PAGAMENTOS_TITULO_PAGINA);
  const [filtro, setFiltro] = useState<FiltroMeusPagamentos>(FILTRO_INICIAL_PAGAMENTOS_CLIENTE);
  /** Ano usado no seletor enquanto o resumo não chegou (o ano do servidor vale depois). Fixado ao abrir a tela. */
  const [anoLocal] = useState(() => new Date().getFullYear());
  /** Cobrança com a gaveta de PIX aberta; nulo com a gaveta fechada. */
  const [cobrancaAberta, setCobrancaAberta] = useState<MeuPagamento | null>(null);
  /** Busca da cobrança única (Pagar Fatura ou Regularizar com uma pendente só). */
  const [buscandoUnica, setBuscandoUnica] = useState(false);
  const [erroUnica, setErroUnica] = useState<unknown>(null);
  /** Aviso quando a busca da cobrança única voltou sem cobrança pagável (ela foi paga no meio do caminho). */
  const [avisoUnica, setAvisoUnica] = useState<string | null>(null);
  /** Botão que abriu a gaveta: o foco volta a ele quando a gaveta fecha. */
  const refGatilho = useRef<HTMLElement | null>(null);
  const buscarUnicaPendencia = useBuscarUnicaPendencia();
  const consultaResumo = useResumoMeusPagamentos(filtro.periodo);
  const consulta = useMeusPagamentos(filtro);
  const resumo = consultaResumo.data ?? null;
  const dados = consulta.data;

  // Página além da última (ex.: a última cobrança saiu da página): volta para a última existente.
  // Feito durante o render, como o React indica para estado derivado. A consulta sem dados não corrige nada.
  if (dados !== undefined) {
    const ultima = ultimaPaginaPagamentos(dados.total, filtro.tamanhoPagina);
    if (filtro.pagina > ultima) setFiltro((atual) => ({ ...atual, pagina: ultima }));
  }

  function mudar(mudanca: MudancaFiltroPagamentos) {
    setFiltro((atual) => aplicarMudancaFiltro(atual, mudanca));
  }

  function abrirGaveta(pagamento: MeuPagamento, gatilho: HTMLElement) {
    refGatilho.current = gatilho;
    setCobrancaAberta(pagamento);
  }

  function fecharGaveta() {
    setCobrancaAberta(null);
  }

  /** Pagar Fatura e Regularizar: pela quantidade de pendentes do resumo (ver destinoBotaoPagarResumo). */
  async function pagarPendencias(gatilho: HTMLElement) {
    const destino = destinoBotaoPagarResumo(resumo?.quantidadePendentes ?? 0);
    if (destino === 'ABA_PENDENTES') {
      mudar({ tipo: 'aba', aba: 'PENDENTES' });
      return;
    }
    if (destino !== 'COBRANCA_UNICA') return;

    setBuscandoUnica(true);
    setErroUnica(null);
    setAvisoUnica(null);
    try {
      const unica = cobrancaParaGaveta(await buscarUnicaPendencia());
      if (unica !== null) {
        abrirGaveta(unica, gatilho);
      } else {
        // Sem cobrança pagável (foi paga depois do resumo): recarrega o resumo e a lista e avisa, para a tela não ficar muda.
        setAvisoUnica(MENSAGEM_PAGAMENTOS_SEM_PENDENTES);
        void consultaResumo.refetch();
        void consulta.refetch();
      }
    } catch (erro: unknown) {
      setErroUnica(erro);
    } finally {
      setBuscandoUnica(false);
    }
  }

  const anoOpcoes = resumo?.anoExercicio ?? anoLocal;
  const resumoComErro = consultaResumo.isError && resumo === null;

  return (
    <div className="w-full">
      {resumo !== null && (
        <FaixaPendenciaPagamentos
          quantidadePendentes={resumo.quantidadePendentes}
          totalPendenteStr={resumo.totalPendenteStr}
          aoPagar={(gatilho) => void pagarPendencias(gatilho)}
          buscando={buscandoUnica}
        />
      )}

      <div className="mx-auto flex w-full max-w-7xl flex-col gap-8 px-4 py-8 sm:px-6 lg:px-8">
        <CabecalhoPagamentosCliente />

        {erroUnica !== null && (
          <p role="alert" className="text-corpo-denso font-medium text-status-perigo">
            {obterMensagemErro(erroUnica)}
          </p>
        )}
        {avisoUnica !== null && (
          <output className="block text-corpo-denso font-medium text-texto-apoio">{avisoUnica}</output>
        )}

        {resumoComErro ? (
          <div className="overflow-hidden rounded-cartao bg-branco shadow-sm">
            <EstadoErro
              titulo={MENSAGEM_PAGAMENTOS_ERRO_RESUMO}
              mensagem={obterMensagemErro(consultaResumo.error)}
              aoTentarNovamente={() => void consultaResumo.refetch()}
              tentando={consultaResumo.isFetching}
            />
          </div>
        ) : (
          <CartoesResumoPagamentos
            resumo={resumo}
            aoRegularizar={(gatilho) => void pagarPendencias(gatilho)}
            buscando={buscandoUnica}
          />
        )}

        <section className="flex flex-col gap-8" aria-label={MENSAGEM_PAGAMENTOS_TITULO_PAGINA}>
          <BarraFiltrosPagamentos
            aba={filtro.aba}
            aoMudarAba={(aba) => mudar({ tipo: 'aba', aba })}
            periodo={filtro.periodo}
            aoMudarPeriodo={(periodo) => mudar({ tipo: 'periodo', periodo })}
            opcoesPeriodo={opcoesPeriodo(anoOpcoes)}
            quantidadePendentes={resumo?.quantidadeAbaPendentes ?? null}
            quantidadePagos={resumo?.quantidadeAbaPagos ?? null}
          />

          <TabelaPagamentosCliente
            itens={dados?.itens ?? []}
            carregando={consulta.isPending}
            erro={consulta.error}
            total={dados?.total ?? 0}
            pagina={filtro.pagina}
            tamanhoPagina={filtro.tamanhoPagina}
            atualizando={consulta.isFetching}
            comFiltro={temFiltroAplicado(filtro)}
            aoMudarPagina={(pagina) => mudar({ tipo: 'pagina', pagina })}
            aoTentarNovamente={() => void consulta.refetch()}
            aoPagarCobranca={abrirGaveta}
          />
        </section>
      </div>

      {cobrancaAberta !== null && (
        <GavetaPixPagamento
          key={cobrancaAberta.idPagamento}
          pagamento={cobrancaAberta}
          aoFechar={fecharGaveta}
          refGatilho={refGatilho}
        />
      )}
    </div>
  );
}
