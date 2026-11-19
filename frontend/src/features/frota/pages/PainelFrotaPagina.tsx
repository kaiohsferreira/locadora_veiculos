import { useEffect, useState } from 'react';
import { Botao, BotaoLink, EstadoErro, Icone } from '@/components/ui';
import { useTituloDocumento } from '@/hooks/useTituloDocumento';
import { obterMensagemErro } from '@/lib/erros';
import { aoFalharFotoCapa, criarControleNovaConsultaFoto, primeiraMensagemErroFotoCapa } from '@/lib/fotosVeiculo';
import {
  MENSAGEM_FROTA_BOTAO_CADASTRAR,
  MENSAGEM_FROTA_DESCRICAO,
  MENSAGEM_FROTA_ERRO_LISTA,
  MENSAGEM_FROTA_ERRO_RESUMO,
  MENSAGEM_FROTA_KICKER,
  MENSAGEM_FROTA_TENTAR_NOVAMENTE,
  MENSAGEM_FROTA_TITULO_PAGINA,
  mensagemVeiculoRotuloCategoria,
} from '@/lib/mensagens';
import { CAMINHOS } from '@/routes/caminhos';
import { PERIODO_PADRAO_VISAO_GERAL } from '@/types/painel';
import { usePainelVisaoGeral } from '@/features/visao-geral/hooks/usePainelVisaoGeral';
import { contarPilulasFrota, montarCartoesFrota } from '@/features/frota/cartoes/cartoesFrota';
import { CartoesFrota } from '@/features/frota/componentes/CartoesFrota';
import { AvisoFotoCapaFrota } from '@/features/frota/componentes/AvisoFotoCapaFrota';
import { EstadoVazioFrota } from '@/features/frota/componentes/EstadoVazioFrota';
import { FiltrosListagemFrota } from '@/features/frota/componentes/FiltrosListagemFrota';
import { PilulasStatusFrota } from '@/features/frota/componentes/PilulasStatusFrota';
import { RodapeListagemFrota } from '@/features/frota/componentes/RodapeListagemFrota';
import { TabelaVeiculos } from '@/features/frota/componentes/TabelaVeiculos';
import { useCategorias } from '@/features/frota/hooks/useCategorias';
import { useListarVeiculos } from '@/features/frota/hooks/useListarVeiculos';
import {
  alterarListagemFrota,
  ATRASO_BUSCA_FROTA_MS,
  ESTADO_FILTROS_FROTA_INICIAL,
  ESTADO_LISTAGEM_FROTA_INICIAL,
  estadoRodapeFrota,
  filtroMudou,
  montarFiltroListagemFrota,
  paginaAposAplicarBusca,
  paginaAposResposta,
  temFiltroFrotaAplicado,
  temFiltroNaListaMostrada,
  termoBuscaFrota,
  type EstadoFiltrosFrota,
  type EstadoListagemFrota,
} from '@/features/frota/listagem/parametrosListagemFrota';
import { criarAgendadorBusca } from '@/features/frota/listagem/agendarBusca';

/**
 * P1-FE-01 · Gestão de Frota (tela 11). Cartões com os números e os percentuais da frota em operação, pílulas de
 * situação com a contagem, busca e categoria, e a tabela paginada da frota. Etapa 1 (leitura): sem troca de status
 * e sem ações em lote.
 */
export default function PainelFrotaPagina() {
  useTituloDocumento(MENSAGEM_FROTA_TITULO_PAGINA);

  const [estado, setEstado] = useState<EstadoListagemFrota>(ESTADO_LISTAGEM_FROTA_INICIAL);
  /**
   * Termo da busca que vai para a consulta (aparado; ver `termoBuscaFrota`). Digitar espera a pausa (uma consulta por
   * pausa, não por tecla); quando o termo muda e entra na consulta, a página volta para 1. Trocar pílula, categoria,
   * página ou linhas leva o termo que está na tela para a consulta na hora.
   */
  const [buscaConsulta, setBuscaConsulta] = useState(termoBuscaFrota(ESTADO_FILTROS_FROTA_INICIAL.busca));
  /** Uma nova consulta por visita quando uma capa falha (ver `aoFalharFotoCapa`). */
  const [controleFoto] = useState(criarControleNovaConsultaFoto);
  /** Se a última resposta recebida (a que a tabela mostra) veio de uma consulta com filtro. */
  const [comFiltroNaUltimaResposta, setComFiltroNaUltimaResposta] = useState(false);

  const termoDigitado = termoBuscaFrota(estado.filtros.busca);

  useEffect(() => {
    if (buscaConsulta === termoDigitado) return undefined;
    const agendador = criarAgendadorBusca((termo) => {
      // A página volta para 1 só se o termo que a API recebe mudou (ver paginaAposAplicarBusca).
      setEstado((anterior) => ({ ...anterior, pagina: paginaAposAplicarBusca(buscaConsulta, termo, anterior.pagina) }));
      setBuscaConsulta(termo);
    }, ATRASO_BUSCA_FROTA_MS);
    agendador.agendar(termoDigitado);
    return () => agendador.cancelar();
  }, [termoDigitado, buscaConsulta]);

  const consulta = useListarVeiculos(
    montarFiltroListagemFrota(estado.pagina, estado.tamanhoPagina, { ...estado.filtros, busca: buscaConsulta }),
  );
  const dados = consulta.data;
  const painel = usePainelVisaoGeral(PERIODO_PADRAO_VISAO_GERAL);
  const categorias = useCategorias();

  // Página além da última (ex.: a lista encolheu): volta para a última existente. Só com resposta da consulta atual; com
  // a anterior (enquanto a nova carrega) não se corrige nada. Feito durante o render, como o React indica para estado
  // derivado; não há efeito nem laço, pois a página passa a caber.
  const paginaCorrigida = paginaAposResposta(estado.pagina, dados, !consulta.isPlaceholderData);
  if (paginaCorrigida !== estado.pagina) {
    setEstado((anterior) => ({ ...anterior, pagina: paginaCorrigida }));
  }

  const cartoes = painel.isPending ? undefined : montarCartoesFrota(painel.data);
  const contagens = contarPilulasFrota(painel.data?.frotaPorSituacao);
  const opcoesCategoria = (categorias.data?.itens ?? []).map((categoria) => ({
    valor: String(categoria.idCategoria),
    rotulo: mensagemVeiculoRotuloCategoria(categoria.nome, categoria.descricao),
  }));
  // Com ou sem filtro: o filtro que vale é o da consulta (a busca que foi à API), não o texto digitado. Enquanto a
  // próxima resposta carrega, a tabela mostra a anterior, e o texto segue o filtro dela.
  const temFiltro = temFiltroFrotaAplicado({ ...estado.filtros, busca: buscaConsulta });
  if (dados !== undefined && !consulta.isPlaceholderData && comFiltroNaUltimaResposta !== temFiltro) {
    setComFiltroNaUltimaResposta(temFiltro);
  }
  const temFiltroNaLista = temFiltroNaListaMostrada(temFiltro, comFiltroNaUltimaResposta, consulta.isPlaceholderData);
  const rodape = estadoRodapeFrota(estado, dados);

  /** Digitar só muda o texto: a página volta para 1 junto com a busca, quando ela entra na consulta (após a pausa). */
  function digitarBusca(busca: string) {
    setEstado((anterior) => ({ ...anterior, filtros: { ...anterior.filtros, busca } }));
  }

  /**
   * Trocar de página também leva a busca que está na tela para a consulta. Se essa busca ainda não tinha entrado na
   * consulta, a página vai para 1 (o filtro mudou); senão, a página escolhida vale.
   */
  function mudarPagina(pagina: number) {
    const paginaDestino = paginaAposAplicarBusca(buscaConsulta, termoDigitado, pagina);
    setBuscaConsulta(termoDigitado);
    setEstado((anterior) => ({ ...anterior, pagina: paginaDestino }));
  }

  function mudarFiltro(parcial: Partial<EstadoFiltrosFrota>) {
    // Escolher o filtro que já está marcado não é mudança: a página e a consulta ficam como estão.
    if (!filtroMudou(estado.filtros, parcial)) return;
    // Pílula e categoria não esperam a pausa da busca: a consulta já sai com a busca que está na tela.
    setBuscaConsulta(termoDigitado);
    setEstado((anterior) => alterarListagemFrota(anterior, { tipo: 'filtro', parcial }));
  }

  function limparFiltros() {
    setBuscaConsulta(ESTADO_FILTROS_FROTA_INICIAL.busca);
    setEstado((anterior) => ({ ...anterior, filtros: ESTADO_FILTROS_FROTA_INICIAL, pagina: 1 }));
  }

  function aoFalharFoto() {
    aoFalharFotoCapa(controleFoto, () => void consulta.refetch());
  }

  const erroCapa = primeiraMensagemErroFotoCapa(dados?.itens ?? []);

  return (
    <div className="flex flex-col gap-6">
      <header className="flex flex-col gap-4 md:flex-row md:items-end md:justify-between">
        <div className="flex flex-col">
          <p className="mb-1 text-micro uppercase tracking-wider text-texto-apoio">
            <span className="font-semibold text-orange-acao">{MENSAGEM_FROTA_KICKER}</span>
          </p>
          <h1 className="text-titulo-acesso font-bold tracking-tight text-texto-desenho">
            {MENSAGEM_FROTA_TITULO_PAGINA}
          </h1>
          <p className="mt-0.5 text-corpo-denso text-texto-apoio">{MENSAGEM_FROTA_DESCRICAO}</p>
        </div>
        <div className="flex shrink-0 items-center gap-2 self-start md:self-auto">
          <BotaoLink to={CAMINHOS.frotaNovoVeiculo} icone="add" variante="frota">
            {MENSAGEM_FROTA_BOTAO_CADASTRAR}
          </BotaoLink>
        </div>
      </header>

      {painel.isError && (
        <output className="flex flex-wrap items-center justify-between gap-2 rounded-cartao bg-status-perigo-fundo px-4 py-3 text-corpo-denso text-status-perigo">
          <span className="flex items-center gap-2">
            <Icone nome="error" tamanho={18} />
            <span>{MENSAGEM_FROTA_ERRO_RESUMO}</span>
          </span>
          <Botao
            variante="contorno"
            tamanho="pequeno"
            carregando={painel.isFetching}
            onClick={() => void painel.refetch()}
          >
            {MENSAGEM_FROTA_TENTAR_NOVAMENTE}
          </Botao>
        </output>
      )}

      <CartoesFrota cartoes={cartoes} />

      {/* Filtros e tabela ficam a 16px um do outro, como no desenho. */}
      <div className="flex flex-col gap-4">
        <section className="flex flex-col gap-4 rounded-cartao bg-branco p-4 shadow-sm">
          <PilulasStatusFrota
            valorSelecionado={estado.filtros.status}
            contagens={contagens}
            aoEscolher={(status) => mudarFiltro({ status })}
          />
          <FiltrosListagemFrota
            busca={estado.filtros.busca}
            aoMudarBusca={digitarBusca}
            opcoesCategoria={opcoesCategoria}
            idCategoria={estado.filtros.idCategoria}
            aoMudarCategoria={(idCategoria) => mudarFiltro({ idCategoria })}
          />
        </section>

        <section className="flex flex-col overflow-hidden rounded-cartao bg-branco shadow-sm">
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
                vazio={<EstadoVazioFrota comFiltro={temFiltroNaLista} aoLimpar={limparFiltros} />}
              />
              {rodape && (
                <RodapeListagemFrota
                  pagina={rodape.pagina}
                  tamanhoPagina={rodape.tamanhoPagina}
                  total={rodape.total}
                  desabilitada={consulta.isFetching}
                  aoMudarPagina={mudarPagina}
                  aoMudarLinhas={(tamanhoPagina) => {
                    setBuscaConsulta(termoDigitado);
                    setEstado((anterior) => alterarListagemFrota(anterior, { tipo: 'linhas', tamanhoPagina }));
                  }}
                />
              )}
            </>
          )}
        </section>
      </div>
    </div>
  );
}
