import { useRef, useState } from 'react';
import { EstadoErro } from '@/components/ui';
import { useTituloDocumento } from '@/hooks/useTituloDocumento';
import { obterMensagemErro } from '@/lib/erros';
import { aoFalharFotoCapa, criarControleNovaConsultaFoto, primeiraMensagemErroFotoCapa } from '@/lib/fotosVeiculo';
import { MENSAGEM_FROTA_ERRO_LISTA, MENSAGEM_FROTA_TITULO_PAGINA } from '@/lib/mensagens';
import { usePermissoes } from '@/routes/autenticacao/usePermissoes';
import { podeTrocarStatusVeiculo } from '@/features/frota/status/menuOperacional';
import { useTrocaStatusFrota } from '@/features/frota/hooks/useTrocaStatusFrota';
import { PERIODO_PADRAO_VISAO_GERAL } from '@/types/painel';
import { usePainelVisaoGeral } from '@/features/visao-geral/hooks/usePainelVisaoGeral';
import { contarPilulasFrota, montarCartoesFrota } from '@/features/frota/cartoes/cartoesFrota';
import { AvisoFotoCapaFrota } from '@/features/frota/componentes/AvisoFotoCapaFrota';
import { AvisoResumoFrota } from '@/features/frota/componentes/AvisoResumoFrota';
import { CabecalhoFrota } from '@/features/frota/componentes/CabecalhoFrota';
import { CartoesFrota } from '@/features/frota/componentes/CartoesFrota';
import { ConfirmacaoBaixaFrota } from '@/features/frota/componentes/ConfirmacaoBaixaFrota';
import { EstadoVazioFrota } from '@/features/frota/componentes/EstadoVazioFrota';
import { FiltrosListagemFrota } from '@/features/frota/componentes/FiltrosListagemFrota';
import { PilulasStatusFrota } from '@/features/frota/componentes/PilulasStatusFrota';
import { RodapeListagemFrota } from '@/features/frota/componentes/RodapeListagemFrota';
import { TabelaVeiculos } from '@/features/frota/componentes/TabelaVeiculos';
import { useCategorias } from '@/features/frota/hooks/useCategorias';
import { useFiltrosFrota } from '@/features/frota/listagem/useFiltrosFrota';
import { montarOpcoesCategoriaFrota } from '@/features/frota/listagem/opcoesListagemFrota';

/**
 * P1-FE-01 · Gestão de Frota (tela 11). Cartões com os números e os percentuais da frota em operação, pílulas de
 * situação com a contagem, busca e categoria, e a tabela paginada da frota. Etapa 2: menu de três pontos em cada linha
 * para trocar o status (só GESTOR_FROTA e ADMINISTRADOR; a baixa pede confirmação). Sem ações em lote.
 * Os filtros vêm do ProvedorFiltrosFrota da rota; a troca de status e a baixa, de useTrocaStatusFrota (a tela 12 usa o mesmo).
 */
export default function PainelFrotaPagina() {
  useTituloDocumento(MENSAGEM_FROTA_TITULO_PAGINA);
  const { cargo } = usePermissoes();
  /** Área da tabela (seção): destino do foco depois da troca de status. */
  const refAreaTabela = useRef<HTMLElement>(null);
  const troca = useTrocaStatusFrota(refAreaTabela);
  const filtros = useFiltrosFrota();
  const { estado, consulta, rodape, temFiltroNaLista } = filtros;
  const dados = consulta.data;
  const painel = usePainelVisaoGeral(PERIODO_PADRAO_VISAO_GERAL);
  const categorias = useCategorias();
  /** Uma nova consulta por visita quando uma capa falha (ver `aoFalharFotoCapa`). */
  const [controleFoto] = useState(criarControleNovaConsultaFoto);

  /** Só GESTOR_FROTA e ADMINISTRADOR veem o menu de três pontos (o servidor confere de novo). */
  const podeTrocar = podeTrocarStatusVeiculo(cargo);
  const cartoes = painel.isPending ? undefined : montarCartoesFrota(painel.data);
  const contagens = contarPilulasFrota(painel.data?.frotaPorSituacao);
  const opcoesCategoria = montarOpcoesCategoriaFrota(categorias.data?.itens);

  function aoFalharFoto() {
    aoFalharFotoCapa(controleFoto, () => void consulta.refetch());
  }

  const erroCapa = primeiraMensagemErroFotoCapa(dados?.itens ?? []);

  return (
    <div className="flex flex-col gap-6">
      <CabecalhoFrota />

      {painel.isError && (
        <AvisoResumoFrota carregando={painel.isFetching} aoTentarNovamente={() => void painel.refetch()} />
      )}

      <CartoesFrota cartoes={cartoes} />

      {/* Filtros e tabela ficam a 16px um do outro, como no desenho. */}
      <div className="flex flex-col gap-4">
        <section className="flex flex-col gap-4 rounded-cartao bg-branco p-4 shadow-sm">
          <PilulasStatusFrota
            valorSelecionado={estado.filtros.status}
            contagens={contagens}
            aoEscolher={(status) => filtros.mudarFiltro({ status })}
          />
          <FiltrosListagemFrota
            busca={estado.filtros.busca}
            aoMudarBusca={filtros.digitarBusca}
            opcoesCategoria={opcoesCategoria}
            idCategoria={estado.filtros.idCategoria}
            aoMudarCategoria={(idCategoria) => filtros.mudarFiltro({ idCategoria })}
          />
        </section>

        <section
          ref={refAreaTabela}
          tabIndex={-1}
          className="flex flex-col overflow-hidden rounded-cartao bg-branco shadow-sm focus:outline-none"
        >
          {consulta.isError && !dados ? (
            <EstadoErro
              titulo={MENSAGEM_FROTA_ERRO_LISTA}
              mensagem={obterMensagemErro(consulta.error)}
              aoTentarNovamente={() => void consulta.refetch()}
              tentando={consulta.isFetching}
            />
          ) : (
            <>
              {erroCapa !== null && <AvisoFotoCapaFrota mensagem={erroCapa} />}
              <TabelaVeiculos
                itens={dados?.itens ?? []}
                carregando={consulta.isPending}
                aoFalharFotoCapa={aoFalharFoto}
                categorias={categorias.data?.itens}
                vazio={<EstadoVazioFrota comFiltro={temFiltroNaLista} aoLimpar={filtros.limparFiltros} />}
                acoesStatus={
                  podeTrocar ? { idEmAndamento: troca.idEmAndamento, aoEscolher: troca.aoEscolherItem } : undefined
                }
              />
              {rodape && (
                <RodapeListagemFrota
                  pagina={rodape.pagina}
                  tamanhoPagina={rodape.tamanhoPagina}
                  total={rodape.total}
                  desabilitada={consulta.isFetching}
                  aoMudarPagina={filtros.mudarPagina}
                  aoMudarLinhas={filtros.mudarLinhas}
                />
              )}
            </>
          )}
        </section>
      </div>

      <ConfirmacaoBaixaFrota
        paraBaixa={troca.paraBaixa}
        pendente={troca.pendente}
        aoCancelar={troca.cancelarBaixa}
        aoConfirmar={troca.confirmarBaixa}
      />
    </div>
  );
}
