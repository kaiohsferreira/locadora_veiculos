import { useEffect, useState, type ReactNode } from 'react';
import { Outlet, useLocation } from 'react-router-dom';
import { useListarVeiculos } from '@/features/frota/hooks/useListarVeiculos';
import { criarAgendadorBusca } from '@/features/frota/listagem/agendarBusca';
import { ContextoFiltrosFrota, type FiltrosListagemFrotaValor } from '@/features/frota/listagem/ContextoFiltrosFrota';
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

export interface ProvedorFiltrosFrotaProps {
  /** Conteúdo (ex.: nos testes). Sem ele, o provedor mostra a rota filha (as telas 11 e 12). */
  children?: ReactNode;
}

/**
 * Filtros da frota (busca, categoria, status, página e linhas) para as duas telas da lista: Painel (11) e Lista (12).
 * É a rota-pai sem caminho que envolve só `/back-office/frota` e `/back-office/frota/veiculos`; novo veículo e edição
 * ficam fora dela. Trocar de tela mantém os filtros e volta para a página 1. A consulta da lista fica aqui porque a
 * correção de página depende da resposta que a tela mostra.
 */
export function ProvedorFiltrosFrota({ children }: ProvedorFiltrosFrotaProps) {
  const { pathname } = useLocation();
  const [estado, setEstado] = useState<EstadoListagemFrota>(ESTADO_LISTAGEM_FROTA_INICIAL);
  /** Caminho da tela anterior: ao trocar de tela (Painel | Lista), a página volta para 1 e os filtros ficam. */
  const [telaAnterior, setTelaAnterior] = useState(pathname);
  if (telaAnterior !== pathname) {
    setTelaAnterior(pathname);
    setEstado((anterior) => alterarListagemFrota(anterior, { tipo: 'tela' }));
  }
  /**
   * Termo da busca que vai para a consulta (aparado; ver `termoBuscaFrota`). Digitar espera a pausa (uma consulta por
   * pausa, não por tecla); quando o termo muda e entra na consulta, a página volta para 1. Trocar pílula, categoria,
   * página ou linhas leva o termo que está na tela para a consulta na hora.
   */
  const [buscaConsulta, setBuscaConsulta] = useState(termoBuscaFrota(ESTADO_FILTROS_FROTA_INICIAL.busca));
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

  // Página além da última (ex.: a lista encolheu): volta para a última existente. Só com resposta da consulta atual; com
  // a anterior (enquanto a nova carrega) não se corrige nada. Feito durante o render, como o React indica para estado
  // derivado; não há efeito nem laço, pois a página passa a caber.
  const paginaCorrigida = paginaAposResposta(estado.pagina, dados, !consulta.isPlaceholderData);
  if (paginaCorrigida !== estado.pagina) {
    setEstado((anterior) => ({ ...anterior, pagina: paginaCorrigida }));
  }

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

  function mudarLinhas(tamanhoPagina: number) {
    setBuscaConsulta(termoDigitado);
    setEstado((anterior) => alterarListagemFrota(anterior, { tipo: 'linhas', tamanhoPagina }));
  }

  function limparFiltros() {
    setBuscaConsulta(ESTADO_FILTROS_FROTA_INICIAL.busca);
    setEstado((anterior) => ({ ...anterior, filtros: ESTADO_FILTROS_FROTA_INICIAL, pagina: 1 }));
  }

  const valor: FiltrosListagemFrotaValor = {
    estado,
    consulta,
    termoDigitado,
    temFiltroNaLista,
    rodape,
    digitarBusca,
    mudarPagina,
    mudarFiltro,
    mudarLinhas,
    limparFiltros,
  };

  return <ContextoFiltrosFrota.Provider value={valor}>{children ?? <Outlet />}</ContextoFiltrosFrota.Provider>;
}
