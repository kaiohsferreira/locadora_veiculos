import { useEffect, useMemo, useRef, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { Botao, EstadoErro, EstadoVazio, Esqueleto, Icone, Paginacao } from '@/components/ui';
import { useAoFalharFotoCapa } from '@/features/area-cliente/hooks/useAoFalharFotoCapa';
import { CartaoSeminovo } from '@/features/vendas/catalogo/CartaoSeminovo';
import { escreverFiltroNoEndereco, lerFiltroDoEndereco, paginaCorrigida } from '@/features/vendas/catalogo/enderecoCatalogo';
import { EtiquetasFiltros } from '@/features/vendas/catalogo/EtiquetasFiltros';
import {
  CAMPOS_FILTRO_VAZIOS,
  camposParaFiltro,
  contarFiltrosAtivos,
  etiquetasFiltro,
  filtroParaCampos,
  filtrosValidos,
  removerFiltro,
  validarCamposFiltro,
  type CamposFiltro,
  type ChaveFiltroCatalogo,
} from '@/features/vendas/catalogo/filtrosCatalogo';
import { FormularioFiltros } from '@/features/vendas/catalogo/FormularioFiltros';
import { useCatalogoSeminovos } from '@/features/vendas/hooks/useCatalogoSeminovos';
import { useTituloDocumento } from '@/hooks/useTituloDocumento';
import { obterMensagemErro } from '@/lib/erros';
import {
  CATALOGO_BOTAO_FILTROS,
  CATALOGO_BOTAO_LIMPAR,
  CATALOGO_ORDEM_MENOR_PRECO,
  CATALOGO_ORDENADO_POR,
  CATALOGO_ROTULO_ESTOQUE,
  CATALOGO_ROTULO_ITENS,
  CATALOGO_ROTULO_LISTA,
  CATALOGO_SUBTITULO,
  CATALOGO_TITULO_DOCUMENTO,
  CATALOGO_TITULO_PAGINA,
  CATALOGO_VAZIO_FILTRADO_TEXTO,
  CATALOGO_VAZIO_FILTRADO_TITULO,
  CATALOGO_VAZIO_TEXTO,
  CATALOGO_VAZIO_TITULO,
  textoEmDestaque,
  textoTotalEstoque,
} from '@/lib/mensagensCatalogo';
import type { FiltroCatalogo } from '@/types/vendas';

/** Quantos cartões cinzas aparecem enquanto a página carrega. */
const CARTOES_CARREGANDO = 6;

/** Classes da grade do desenho: 1 cartão por linha no celular, 2 em tela média e 3 em tela larga. */
const CLASSES_GRADE = 'grid grid-cols-1 gap-6 md:grid-cols-2 lg:grid-cols-3';

/** Id do painel dos filtros, para o botão "Filtros Avançados" dizer o que abre. */
const ID_PAINEL_FILTROS = 'catalogo-filtros-avancados';

/** Grade de cartões cinzas enquanto a página carrega. */
function GradeCarregando() {
  return (
    <div className={CLASSES_GRADE} aria-busy="true">
      {Array.from({ length: CARTOES_CARREGANDO }, (_, indice) => (
        <div key={indice} className="flex flex-col gap-3 rounded-cartao bg-branco p-4 shadow-cartao">
          <Esqueleto className="h-56 w-full" />
          <Esqueleto className="h-5 w-3/4" />
          <Esqueleto className="h-4 w-1/2" />
          <Esqueleto className="h-9 w-full" />
        </div>
      ))}
    </div>
  );
}

/** Faixa de destaque do desenho 24: título, subtítulo e o total de veículos à venda. */
function FaixaDestaque({ total }: { total: number | undefined }) {
  return (
    <section className="w-full bg-navy">
      <div className="mx-auto flex min-h-[280px] w-full max-w-7xl flex-col justify-center gap-4 px-4 pb-12 pt-8 sm:px-6 lg:px-8">
        <div className="flex max-w-2xl flex-col gap-2">
          <h1 className="text-titulo font-bold tracking-tight text-branco">{CATALOGO_TITULO_PAGINA}</h1>
          <p className="text-corpo text-steel-palido">{CATALOGO_SUBTITULO}</p>
        </div>
        <div className="grid max-w-2xl grid-cols-3 gap-4 pt-1">
          <div className="rounded-lg bg-navy-claro p-2">
            {total === undefined ? (
              <Esqueleto className="h-7 w-16" />
            ) : (
              <p className="text-secao font-bold text-branco tabular-nums" data-testid="total-estoque">
                {textoTotalEstoque(total)}
              </p>
            )}
            <p className="text-legenda uppercase text-steel-neblina">{CATALOGO_ROTULO_ESTOQUE}</p>
          </div>
        </div>
      </div>
    </section>
  );
}

/**
 * P6-FE-01 · Catálogo de seminovos (RF-26), desenho 24_cat_logo_de_seminovos_web_desktop. Faixa de destaque com o total,
 * barra com "Filtros Avançados" (marca, preço, ano e km), "N veículos em destaque · Ordenado por: Menor Preço", grade de
 * 3 colunas e paginação do servidor (20 por página). Os filtros e a página ficam no endereço; página além da última volta
 * para a última. Cada cartão leva à proposta de compra.
 *
 * Fora do desenho por não existirem no sistema: faixa de promoções, selos da faixa de destaque, "Itens inspecionados" e
 * "Histórico de sinistro", busca por modelo ou placa, pátio, pílulas de categoria com contagem, faixa do pátio, "Vantagens
 * Exclusivas", avaliação do usado e o cartão do showroom.
 */
export default function CatalogoSeminovosPagina() {
  useTituloDocumento(CATALOGO_TITULO_DOCUMENTO);
  const [parametros, setParametros] = useSearchParams();
  const chaveEndereco = parametros.toString();
  const filtro = useMemo(() => lerFiltroDoEndereco(new URLSearchParams(chaveEndereco)), [chaveEndereco]);

  const consulta = useCatalogoSeminovos(filtro);
  const aoFalharFoto = useAoFalharFotoCapa(() => {
    void consulta.refetch();
  });

  const [campos, setCampos] = useState<CamposFiltro>(() => filtroParaCampos(filtro));
  const [painelAberto, setPainelAberto] = useState(false);
  const topoDaLista = useRef<HTMLElement>(null);

  // O endereço manda: ao voltar pelo navegador ou tirar uma etiqueta, os campos mostram o filtro que está valendo.
  // Ajuste durante a renderização (padrão do React para estado derivado de uma mudança), sem efeito.
  const [enderecoDosCampos, setEnderecoDosCampos] = useState(chaveEndereco);
  if (enderecoDosCampos !== chaveEndereco) {
    setEnderecoDosCampos(chaveEndereco);
    setCampos(filtroParaCampos(filtro));
  }

  const erros = validarCamposFiltro(campos);
  const etiquetas = etiquetasFiltro(filtro);
  const filtrosAtivos = contarFiltrosAtivos(filtro);
  const dados = consulta.data;

  // Página além da última (endereço antigo ou carros que saíram): vai para a última página que existe.
  const paginaCerta = dados && !consulta.isPlaceholderData ? paginaCorrigida(filtro.pagina, dados.total, dados.tamanhoPagina) : null;
  useEffect(() => {
    if (paginaCerta !== null) setParametros(escreverFiltroNoEndereco({ ...filtro, pagina: paginaCerta }), { replace: true });
  }, [paginaCerta, filtro, setParametros]);

  function irPara(novoFiltro: FiltroCatalogo) {
    setParametros(escreverFiltroNoEndereco(novoFiltro));
  }

  function mudarCampo(campo: ChaveFiltroCatalogo, valor: string) {
    setCampos((anteriores) => ({ ...anteriores, [campo]: valor }));
  }

  function aplicar() {
    if (!filtrosValidos(erros)) return;
    irPara(camposParaFiltro(campos));
  }

  function limpar() {
    setCampos(CAMPOS_FILTRO_VAZIOS);
    setParametros(new URLSearchParams());
  }

  function alternarPainel() {
    if (!painelAberto) setCampos(filtroParaCampos(filtro));
    setPainelAberto(!painelAberto);
  }

  function mudarPagina(pagina: number) {
    irPara({ ...filtro, pagina });
    topoDaLista.current?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  let lista;
  if (consulta.isError) {
    lista = (
      <EstadoErro
        mensagem={obterMensagemErro(consulta.error)}
        aoTentarNovamente={() => void consulta.refetch()}
        tentando={consulta.isFetching}
      />
    );
  } else if (dados === undefined || paginaCerta !== null) {
    lista = <GradeCarregando />;
  } else if (dados.itens.length === 0) {
    lista =
      filtrosAtivos > 0 ? (
        <EstadoVazio
          icone="search_off"
          titulo={CATALOGO_VAZIO_FILTRADO_TITULO}
          descricao={CATALOGO_VAZIO_FILTRADO_TEXTO}
          acao={
            <Botao variante="contorno" onClick={limpar}>
              {CATALOGO_BOTAO_LIMPAR}
            </Botao>
          }
        />
      ) : (
        <EstadoVazio icone="directions_car" titulo={CATALOGO_VAZIO_TITULO} descricao={CATALOGO_VAZIO_TEXTO} />
      );
  } else {
    lista = (
      <div className="flex flex-col gap-8">
        <ul
          className={`${CLASSES_GRADE} transition-opacity ${consulta.isPlaceholderData ? 'opacity-60' : ''}`}
          aria-busy={consulta.isPlaceholderData}
          aria-label={CATALOGO_ROTULO_LISTA}
        >
          {dados.itens.map((veiculo) => (
            <li key={veiculo.idVeiculo}>
              <CartaoSeminovo veiculo={veiculo} aoFalharFoto={aoFalharFoto} />
            </li>
          ))}
        </ul>
        <div className="rounded-cartao bg-branco shadow-cartao">
          <Paginacao
            pagina={dados.pagina}
            tamanhoPagina={dados.tamanhoPagina}
            total={dados.total}
            aoMudarPagina={mudarPagina}
            desabilitada={consulta.isPlaceholderData}
            rotuloItens={CATALOGO_ROTULO_ITENS}
          />
        </div>
      </div>
    );
  }

  const mostrarResumo = dados !== undefined && !consulta.isError && paginaCerta === null;

  return (
    <div className="flex w-full flex-col">
      <FaixaDestaque total={consulta.isError ? undefined : dados?.total} />

      <div className="mx-auto -mt-6 flex w-full max-w-7xl flex-col gap-8 px-4 pb-8 sm:px-6 lg:px-8">
        <section className="relative rounded-cartao bg-branco p-6 shadow-elevado" aria-label={CATALOGO_BOTAO_FILTROS}>
          <div className="flex flex-col items-stretch gap-4 lg:flex-row lg:items-center lg:justify-end">
            <button
              type="button"
              onClick={alternarPainel}
              aria-expanded={painelAberto}
              aria-controls={ID_PAINEL_FILTROS}
              className="flex h-11 items-center justify-center gap-2 rounded-lg bg-navy-claro px-6 text-corpo-denso font-semibold text-branco hover:bg-navy"
            >
              <Icone nome="tune" tamanho={18} />
              {filtrosAtivos > 0 ? `${CATALOGO_BOTAO_FILTROS} (${filtrosAtivos})` : CATALOGO_BOTAO_FILTROS}
            </button>
          </div>

          <div id={ID_PAINEL_FILTROS} hidden={!painelAberto} className="mt-6 border-t border-borda pt-6">
            <FormularioFiltros idBase="filtro" campos={campos} erros={erros} aoMudar={mudarCampo} aoAplicar={aplicar} aoLimpar={limpar} />
          </div>

          <div className="-mx-6 -mb-6 mt-4 flex flex-wrap items-center justify-between gap-2 rounded-b-cartao bg-steel-fundo px-6 py-2">
            <EtiquetasFiltros etiquetas={etiquetas} aoRemover={(chave) => irPara(removerFiltro(filtro, chave))} />
            {mostrarResumo && (
              <p className="ml-auto flex items-center gap-2 text-corpo-denso text-contorno" aria-live="polite">
                <span className="font-semibold text-texto" data-testid="em-destaque">
                  {textoEmDestaque(dados.itens.length)}
                </span>
                <span className="h-1 w-1 rounded-full bg-contorno-suave" aria-hidden="true" />
                <span>
                  {CATALOGO_ORDENADO_POR} <strong className="font-semibold text-texto">{CATALOGO_ORDEM_MENOR_PRECO}</strong>
                </span>
              </p>
            )}
          </div>
        </section>

        <section ref={topoDaLista} className="flex min-w-0 scroll-mt-24 flex-col gap-4" aria-label={CATALOGO_ROTULO_LISTA}>
          {lista}
        </section>
      </div>
    </div>
  );
}
