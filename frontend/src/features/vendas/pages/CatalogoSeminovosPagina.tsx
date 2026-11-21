import { useMemo, useRef, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { Botao, Cartao, EstadoErro, EstadoVazio, Esqueleto, Modal, Paginacao } from '@/components/ui';
import { useAoFalharFotoCapa } from '@/features/area-cliente/hooks/useAoFalharFotoCapa';
import { CartaoSeminovo } from '@/features/vendas/catalogo/CartaoSeminovo';
import { escreverFiltroNoEndereco, lerFiltroDoEndereco } from '@/features/vendas/catalogo/enderecoCatalogo';
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
  CATALOGO_ROTULO_ITENS,
  CATALOGO_SUBTITULO,
  CATALOGO_TITULO_FILTROS,
  CATALOGO_TITULO_PAGINA,
  CATALOGO_VAZIO_FILTRADO_TEXTO,
  CATALOGO_VAZIO_FILTRADO_TITULO,
  CATALOGO_VAZIO_TEXTO,
  CATALOGO_VAZIO_TITULO,
  textoTotalCatalogo,
} from '@/lib/mensagensCatalogo';
import type { FiltroCatalogo } from '@/types/vendas';

/** Quantos cartões cinzas aparecem enquanto a primeira página carrega. */
const CARTOES_CARREGANDO = 6;

/** Classes da grade: 1 cartão por linha no celular, 2 em tela média e 3 em tela larga. */
const CLASSES_GRADE = 'grid grid-cols-1 gap-6 sm:grid-cols-2 xl:grid-cols-3';

/** Grade de cartões cinzas enquanto a primeira página carrega. */
function GradeCarregando() {
  return (
    <div className={CLASSES_GRADE} aria-busy="true">
      {Array.from({ length: CARTOES_CARREGANDO }, (_, indice) => (
        <div key={indice} className="flex flex-col gap-3 rounded-cartao bg-branco p-4 shadow-cartao">
          <Esqueleto className="aspect-[16/10] w-full" />
          <Esqueleto className="h-5 w-3/4" />
          <Esqueleto className="h-4 w-1/2" />
          <Esqueleto className="h-9 w-full" />
        </div>
      ))}
    </div>
  );
}

/**
 * P6-FE-01 · Catálogo de seminovos (RF-26). Sem protótipo: segue o design system e o molde das telas prontas (filtros à
 * esquerda e grade à direita no computador; filtros numa janela no celular). Os filtros e a página ficam no endereço;
 * a paginação é do servidor (20 por página) e cada cartão leva à proposta de compra.
 */
export default function CatalogoSeminovosPagina() {
  useTituloDocumento(CATALOGO_TITULO_PAGINA);
  const [parametros, setParametros] = useSearchParams();
  const chaveEndereco = parametros.toString();
  const filtro = useMemo(() => lerFiltroDoEndereco(new URLSearchParams(chaveEndereco)), [chaveEndereco]);

  const consulta = useCatalogoSeminovos(filtro);
  const aoFalharFoto = useAoFalharFotoCapa(() => {
    void consulta.refetch();
  });

  const [campos, setCampos] = useState<CamposFiltro>(() => filtroParaCampos(filtro));
  const [janelaAberta, setJanelaAberta] = useState(false);
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

  function irPara(novoFiltro: FiltroCatalogo) {
    setParametros(escreverFiltroNoEndereco(novoFiltro));
  }

  function mudarCampo(campo: ChaveFiltroCatalogo, valor: string) {
    setCampos((anteriores) => ({ ...anteriores, [campo]: valor }));
  }

  function aplicar() {
    if (!filtrosValidos(erros)) return;
    irPara(camposParaFiltro(campos));
    setJanelaAberta(false);
  }

  function limpar() {
    setCampos(CAMPOS_FILTRO_VAZIOS);
    setParametros(new URLSearchParams());
    setJanelaAberta(false);
  }

  function abrirJanela() {
    setCampos(filtroParaCampos(filtro));
    setJanelaAberta(true);
  }

  function mudarPagina(pagina: number) {
    irPara({ ...filtro, pagina });
    topoDaLista.current?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  const dados = consulta.data;
  let lista;
  if (consulta.isError) {
    lista = (
      <EstadoErro
        mensagem={obterMensagemErro(consulta.error)}
        aoTentarNovamente={() => void consulta.refetch()}
        tentando={consulta.isFetching}
      />
    );
  } else if (dados === undefined) {
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
          aria-label={CATALOGO_TITULO_PAGINA}
        >
          {dados.itens.map((veiculo) => (
            <li key={veiculo.idVeiculo}>
              <CartaoSeminovo veiculo={veiculo} aoFalharFoto={aoFalharFoto} />
            </li>
          ))}
        </ul>
        <Paginacao
          pagina={dados.pagina}
          tamanhoPagina={dados.tamanhoPagina}
          total={dados.total}
          aoMudarPagina={mudarPagina}
          desabilitada={consulta.isPlaceholderData}
          rotuloItens={CATALOGO_ROTULO_ITENS}
        />
      </div>
    );
  }

  return (
    <div className="mx-auto flex w-full max-w-7xl flex-col gap-8 px-4 py-8 sm:px-6 lg:px-8">
      <header className="flex flex-col gap-2 sm:flex-row sm:items-end sm:justify-between sm:gap-6">
        <div className="flex flex-col gap-2">
          <h1 className="text-titulo text-navy">{CATALOGO_TITULO_PAGINA}</h1>
          <p className="max-w-2xl text-corpo text-texto-suave">{CATALOGO_SUBTITULO}</p>
        </div>
        {dados !== undefined && (
          <p className="shrink-0 text-corpo font-semibold text-navy" aria-live="polite">
            {textoTotalCatalogo(dados.total)}
          </p>
        )}
      </header>

      <div className="grid grid-cols-1 gap-8 lg:grid-cols-12">
        <aside className="hidden lg:col-span-3 lg:block">
          <div className="lg:sticky lg:top-24">
            <Cartao titulo={CATALOGO_TITULO_FILTROS}>
              <FormularioFiltros idBase="filtro" campos={campos} erros={erros} aoMudar={mudarCampo} aoAplicar={aplicar} aoLimpar={limpar} />
            </Cartao>
          </div>
        </aside>

        <section ref={topoDaLista} className="flex min-w-0 scroll-mt-24 flex-col gap-4 lg:col-span-9" aria-label={CATALOGO_TITULO_PAGINA}>
          <div className="lg:hidden">
            <Botao variante="contorno" icone="tune" larguraTotal onClick={abrirJanela}>
              {filtrosAtivos > 0 ? `${CATALOGO_BOTAO_FILTROS} (${filtrosAtivos})` : CATALOGO_BOTAO_FILTROS}
            </Botao>
          </div>
          <EtiquetasFiltros etiquetas={etiquetas} aoRemover={(chave) => irPara(removerFiltro(filtro, chave))} />
          {lista}
        </section>
      </div>

      <Modal aberto={janelaAberta} aoFechar={() => setJanelaAberta(false)} titulo={CATALOGO_TITULO_FILTROS}>
        <FormularioFiltros idBase="filtro-janela" campos={campos} erros={erros} aoMudar={mudarCampo} aoAplicar={aplicar} aoLimpar={limpar} />
      </Modal>
    </div>
  );
}
