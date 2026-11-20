import { useId, useState } from 'react';
import { Botao, EstadoErro, EstadoVazio } from '@/components/ui';
import { useDebounce } from '@/hooks/useDebounce';
import { useTituloDocumento } from '@/hooks/useTituloDocumento';
import { AvisoFotoCapaFrota } from '@/features/frota/componentes/AvisoFotoCapaFrota';
import { obterMensagemErro } from '@/lib/erros';
import { aoFalharFotoCapa, criarControleNovaConsultaFoto, primeiraMensagemErroFotoCapa } from '@/lib/fotosVeiculo';
import {
  MENSAGEM_MANUTENCAO_ERRO_LISTA,
  MENSAGEM_MANUTENCAO_HISTORICO_DESCRICAO,
  MENSAGEM_MANUTENCAO_HISTORICO_TITULO,
  MENSAGEM_MANUTENCAO_LIMPAR_FILTROS,
  MENSAGEM_MANUTENCAO_LISTA_VAZIA,
  MENSAGEM_MANUTENCAO_TENTAR_DE_NOVO,
  MENSAGEM_MANUTENCAO_TITULO,
} from '@/lib/mensagens';
import type { ManutencaoListaRetorno } from '@/types/manutencao';
import { CabecalhoManutencao } from '@/features/manutencao/componentes/CabecalhoManutencao';
import { BarraFiltrosManutencao } from '@/features/manutencao/componentes/BarraFiltrosManutencao';
import { FaixaMetricasManutencao } from '@/features/manutencao/componentes/FaixaMetricasManutencao';
import { ListaAlertasRevisao } from '@/features/manutencao/componentes/ListaAlertasRevisao';
import { ModalAbrirOrdem } from '@/features/manutencao/componentes/ModalAbrirOrdem';
import { ModalEncerrarOrdem } from '@/features/manutencao/componentes/ModalEncerrarOrdem';
import { RodapeOrdensManutencao } from '@/features/manutencao/componentes/RodapeOrdensManutencao';
import { TabelaOrdensManutencao } from '@/features/manutencao/componentes/TabelaOrdensManutencao';
import {
  ATRASO_BUSCA_MANUTENCAO_MS,
  ESTADO_FILTROS_MANUTENCAO_INICIAL,
  alterarFiltrosManutencao,
  montarFiltroManutencao,
  temFiltroManutencaoAplicado,
  type EstadoFiltrosManutencao,
  type MudancaFiltrosManutencao,
} from '@/features/manutencao/listagem/filtrosManutencao';
import {
  useAlertasRevisao,
  useListarManutencoes,
  useResumoManutencao,
} from '@/features/manutencao/hooks/useConsultasManutencao';

/**
 * P8-FE-01 · Manutenção da frota (tela 14, desenho 14_back_office_manuten_o_da_frota). Cabeçalho com Abrir ordem, alertas
 * de revisão em cartões, métricas (OS Ativas e Custo Mês), filtros, tabela paginada e os modais de abrir e de encerrar.
 * Cada bloco tem o seu próprio estado de carregando, vazio e erro; um erro num bloco não derruba os outros.
 */
export default function ManutencaoFrotaPagina() {
  useTituloDocumento(MENSAGEM_MANUTENCAO_TITULO);
  const idHistorico = useId();
  const [estado, setEstado] = useState<EstadoFiltrosManutencao>(ESTADO_FILTROS_MANUTENCAO_INICIAL);
  /** A busca só chega à consulta depois da pausa de digitação. */
  const buscaAplicada = useDebounce(estado.busca, ATRASO_BUSCA_MANUTENCAO_MS);
  const filtro = montarFiltroManutencao(estado, buscaAplicada);
  const lista = useListarManutencoes(filtro);
  const alertas = useAlertasRevisao();
  const resumo = useResumoManutencao();
  /** Modal aberto: veículo pré-escolhido (ou nulo) para abrir uma ordem; ordem escolhida para encerrar. */
  const [abrindoPara, setAbrindoPara] = useState<{ idVeiculo: number | null } | null>(null);
  const [encerrando, setEncerrando] = useState<ManutencaoListaRetorno | null>(null);
  /** Uma nova consulta por visita quando uma capa falha (ver `aoFalharFotoCapa`), uma para cada bloco. */
  const [controleFotoAlertas] = useState(criarControleNovaConsultaFoto);
  const [controleFotoLista] = useState(criarControleNovaConsultaFoto);

  const dadosLista = lista.data;
  const listaComErro = lista.isError && dadosLista === undefined;
  const alertasErro = alertas.isError && alertas.data === undefined ? obterMensagemErro(alertas.error) : null;
  const resumoErro = resumo.isError && resumo.data === undefined;
  const filtroAplicado = temFiltroManutencaoAplicado(estado);
  const erroCapaLista = primeiraMensagemErroFotoCapa(dadosLista?.itens ?? []);

  function mudar(mudanca: MudancaFiltrosManutencao) {
    setEstado((anterior) => alterarFiltrosManutencao(anterior, mudanca));
  }

  function atualizar() {
    void lista.refetch();
    void alertas.refetch();
    void resumo.refetch();
  }

  return (
    <div className="flex flex-col gap-8">
      <CabecalhoManutencao aoAbrirOrdem={() => setAbrindoPara({ idVeiculo: null })} />

      <ListaAlertasRevisao
        carregando={alertas.isPending}
        mensagemErro={alertasErro}
        tentando={alertas.isFetching}
        alertas={alertas.data}
        aoTentarNovamente={() => void alertas.refetch()}
        aoAbrirOrdem={(idVeiculo) => setAbrindoPara({ idVeiculo })}
        aoVerHistorico={(idVeiculo) => mudar({ tipo: 'veiculo', idVeiculo })}
        aoFalharFotoCapa={() => aoFalharFotoCapa(controleFotoAlertas, () => void alertas.refetch())}
      />

      <section aria-labelledby={idHistorico} className="flex flex-col gap-4">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
          <div className="flex flex-col gap-1">
            <h2 id={idHistorico} className="text-nome-veiculo font-semibold text-texto-desenho">
              {MENSAGEM_MANUTENCAO_HISTORICO_TITULO}
            </h2>
            <p className="text-apoio text-texto-apoio">{MENSAGEM_MANUTENCAO_HISTORICO_DESCRICAO}</p>
          </div>
          <FaixaMetricasManutencao
            carregando={resumo.isPending}
            erro={resumoErro}
            tentando={resumo.isFetching}
            resumo={resumo.data}
            aoTentarNovamente={() => void resumo.refetch()}
          />
        </div>

        <BarraFiltrosManutencao
          busca={estado.busca}
          aoMudarBusca={(busca) => mudar({ tipo: 'busca', busca })}
          tipo={estado.tipo}
          aoMudarTipo={(valor) => mudar({ tipo: 'tipo', valor })}
          situacao={estado.situacao}
          aoMudarSituacao={(valor) => mudar({ tipo: 'situacao', valor })}
          podeLimpar={filtroAplicado}
          aoLimpar={() => mudar({ tipo: 'limpar' })}
          aoAtualizar={atualizar}
          atualizando={lista.isFetching || alertas.isFetching || resumo.isFetching}
        />

        <div className="flex flex-col overflow-hidden rounded-cartao bg-branco shadow-sm">
          {listaComErro ? (
            <EstadoErro
              titulo={MENSAGEM_MANUTENCAO_ERRO_LISTA}
              mensagem={obterMensagemErro(lista.error)}
              aoTentarNovamente={() => void lista.refetch()}
              tentando={lista.isFetching}
              rotuloTentarNovamente={MENSAGEM_MANUTENCAO_TENTAR_DE_NOVO}
            />
          ) : (
            <>
              {erroCapaLista !== null && <AvisoFotoCapaFrota mensagem={erroCapaLista} />}
              <TabelaOrdensManutencao
                itens={dadosLista?.itens ?? []}
                carregando={lista.isPending}
                aoFalharFotoCapa={() => aoFalharFotoCapa(controleFotoLista, () => void lista.refetch())}
                aoEncerrar={setEncerrando}
                vazio={
                  filtroAplicado ? (
                    <EstadoVazio
                      titulo={MENSAGEM_MANUTENCAO_LISTA_VAZIA}
                      acao={
                        <Botao variante="contorno" onClick={() => mudar({ tipo: 'limpar' })}>
                          {MENSAGEM_MANUTENCAO_LIMPAR_FILTROS}
                        </Botao>
                      }
                    />
                  ) : (
                    <EstadoVazio titulo={MENSAGEM_MANUTENCAO_LISTA_VAZIA} />
                  )
                }
              />
              {dadosLista !== undefined && (
                <RodapeOrdensManutencao
                  pagina={estado.pagina}
                  tamanhoPagina={estado.tamanhoPagina}
                  total={dadosLista.total}
                  desabilitada={lista.isFetching}
                  aoMudarPagina={(pagina) => mudar({ tipo: 'pagina', pagina })}
                  aoMudarLinhas={(tamanhoPagina) => mudar({ tipo: 'linhas', tamanhoPagina })}
                />
              )}
            </>
          )}
        </div>
      </section>

      {abrindoPara !== null && (
        <ModalAbrirOrdem idVeiculoInicial={abrindoPara.idVeiculo} aoFechar={() => setAbrindoPara(null)} />
      )}
      {encerrando !== null && <ModalEncerrarOrdem ordem={encerrando} aoFechar={() => setEncerrando(null)} />}
    </div>
  );
}
