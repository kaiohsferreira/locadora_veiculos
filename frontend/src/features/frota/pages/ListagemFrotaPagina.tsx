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
import { montarResumoCartoesFrota } from '@/features/frota/cartoes/cartoesResumoFrota';
import { AvisoFotoCapaFrota } from '@/features/frota/componentes/AvisoFotoCapaFrota';
import { AvisoResumoFrota } from '@/features/frota/componentes/AvisoResumoFrota';
import { BarraFiltrosListagemFrota } from '@/features/frota/componentes/BarraFiltrosListagemFrota';
import { CabecalhoFrota } from '@/features/frota/componentes/CabecalhoFrota';
import { CartoesResumoFrota } from '@/features/frota/componentes/CartoesResumoFrota';
import { ConfirmacaoBaixaFrota } from '@/features/frota/componentes/ConfirmacaoBaixaFrota';
import { EstadoVazioFrota } from '@/features/frota/componentes/EstadoVazioFrota';
import { RodapeListagemFrota } from '@/features/frota/componentes/RodapeListagemFrota';
import { TabelaListagemVeiculos } from '@/features/frota/componentes/TabelaListagemVeiculos';
import { useCategorias } from '@/features/frota/hooks/useCategorias';
import { useFiltrosFrota } from '@/features/frota/listagem/useFiltrosFrota';
import { montarOpcoesCategoriaFrota } from '@/features/frota/listagem/opcoesListagemFrota';
import { temFiltroFrotaAplicado } from '@/features/frota/listagem/parametrosListagemFrota';

/**
 * P1-FE-02 · Listagem da frota (tela 12, desenho 12_back_office_gest_o_de_frota_2). Cartões do resumo, barra de busca,
 * categoria e status, a tabela paginada e o rodapé. Mesmas consultas, filtros e troca de status da tela 11 (Painel), que
 * fica na mesma rota-pai de filtros: trocar de tela mantém a busca, a categoria e o status.
 */
export default function ListagemFrotaPagina() {
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
  const resumo = montarResumoCartoesFrota(painel.data);
  const opcoesCategoria = montarOpcoesCategoriaFrota(categorias.data?.itens);
  const podeLimpar = temFiltroFrotaAplicado(estado.filtros);

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

      <CartoesResumoFrota carregando={painel.isPending} resumo={resumo} />

      <BarraFiltrosListagemFrota
        busca={estado.filtros.busca}
        aoMudarBusca={filtros.digitarBusca}
        opcoesCategoria={opcoesCategoria}
        idCategoria={estado.filtros.idCategoria}
        aoMudarCategoria={(idCategoria) => filtros.mudarFiltro({ idCategoria })}
        status={estado.filtros.status}
        aoMudarStatus={(status) => filtros.mudarFiltro({ status })}
        podeLimpar={podeLimpar}
        aoLimpar={filtros.limparFiltros}
      />

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
            <TabelaListagemVeiculos
              itens={dados?.itens ?? []}
              carregando={consulta.isPending}
              aoFalharFotoCapa={aoFalharFoto}
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

      <ConfirmacaoBaixaFrota
        paraBaixa={troca.paraBaixa}
        pendente={troca.pendente}
        aoCancelar={troca.cancelarBaixa}
        aoConfirmar={troca.confirmarBaixa}
      />
    </div>
  );
}
