import { useState } from 'react';
import { EstadoErro } from '@/components/ui';
import { CabecalhoPagamentosCliente } from '@/features/financeiro/cliente/componentes/CabecalhoPagamentosCliente';
import { BarraFiltrosPagamentos } from '@/features/financeiro/cliente/componentes/BarraFiltrosPagamentos';
import { CartoesResumoPagamentos } from '@/features/financeiro/cliente/componentes/CartoesResumoPagamentos';
import { FaixaPendenciaPagamentos } from '@/features/financeiro/cliente/componentes/FaixaPendenciaPagamentos';
import { TabelaPagamentosCliente } from '@/features/financeiro/cliente/componentes/TabelaPagamentosCliente';
import {
  aplicarMudancaFiltro,
  FILTRO_INICIAL_PAGAMENTOS_CLIENTE,
  type MudancaFiltroPagamentos,
  opcoesPeriodo,
  temFiltroAplicado,
  ultimaPaginaPagamentos,
} from '@/features/financeiro/cliente/regrasPagamentosCliente';
import { useMeusPagamentos, useResumoMeusPagamentos } from '@/features/financeiro/hooks/usePagamentosCliente';
import { useTituloDocumento } from '@/hooks/useTituloDocumento';
import { MENSAGEM_PAGAMENTOS_ERRO_RESUMO, MENSAGEM_PAGAMENTOS_TITULO_PAGINA } from '@/lib/mensagens';
import { obterMensagemErro } from '@/lib/erros';
import type { FiltroMeusPagamentos } from '@/types/pagamentos';

/**
 * P7-FE-02 · Pagamentos e Faturas do cliente (tela 30, RF-36). Mostra só as cobranças do próprio cliente (o servidor
 * decide quais). Os botões de pagar ficam desligados nesta entrega (semana 4).
 */
export default function PagamentosFaturasPagina() {
  useTituloDocumento(MENSAGEM_PAGAMENTOS_TITULO_PAGINA);
  const [filtro, setFiltro] = useState<FiltroMeusPagamentos>(FILTRO_INICIAL_PAGAMENTOS_CLIENTE);
  /** Ano usado no seletor enquanto o resumo não chegou (o ano do servidor vale depois). Fixado ao abrir a tela. */
  const [anoLocal] = useState(() => new Date().getFullYear());
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

  const anoOpcoes = resumo?.anoExercicio ?? anoLocal;
  const resumoComErro = consultaResumo.isError && resumo === null;

  return (
    <div className="w-full">
      {resumo !== null && (
        <FaixaPendenciaPagamentos
          quantidadePendentes={resumo.quantidadePendentes}
          totalPendenteStr={resumo.totalPendenteStr}
        />
      )}

      <div className="mx-auto flex w-full max-w-7xl flex-col gap-8 px-4 py-8 sm:px-6 lg:px-8">
        <CabecalhoPagamentosCliente />

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
          <CartoesResumoPagamentos resumo={resumo} />
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
          />
        </section>
      </div>
    </div>
  );
}
