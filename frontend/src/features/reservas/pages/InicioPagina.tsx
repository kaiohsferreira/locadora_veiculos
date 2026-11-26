import { useState, type FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { BotaoLink, EstadoErro, Icone } from '@/components/ui';
import { useTituloDocumento } from '@/hooks/useTituloDocumento';
import { MENSAGENS_BUSCA as texto } from '@/lib/mensagensBusca';
import { CAMINHOS } from '@/routes/caminhos';
import { CamposPeriodoBusca } from '../busca/CamposPeriodoBusca';
import { BUSCA_VAZIA, hojeLocal, montarParametrosBusca, validarBusca, type ErrosBusca, type RascunhoBusca } from '../busca/parametrosBusca';
import { useCategoriasVeiculos } from '../hooks/useVeiculosDisponiveis';

/** P3-FE-01 · Pesquisa de aluguel e acesso ao catálogo de seminovos. */
export default function InicioPagina() {
  useTituloDocumento(texto.tituloInicio);
  const navegar = useNavigate();
  const hoje = hojeLocal();
  const categorias = useCategoriasVeiculos();
  const [rascunho, definirRascunho] = useState<RascunhoBusca>(BUSCA_VAZIA);
  const [tentouBuscar, definirTentouBuscar] = useState(false);
  const erros: ErrosBusca = tentouBuscar ? validarBusca(rascunho, hoje) : {};

  function alterar(campo: keyof RascunhoBusca, valor: string) {
    definirRascunho((atual) => ({ ...atual, [campo]: valor }));
  }

  function enviar(evento: FormEvent<HTMLFormElement>) {
    evento.preventDefault();
    definirTentouBuscar(true);
    if (Object.keys(validarBusca(rascunho, hoje)).length > 0) return;
    navegar({ pathname: CAMINHOS.resultadosBusca, search: montarParametrosBusca(rascunho).toString() });
  }

  return (
    <div className="bg-offwhite">
      {/* O protótipo também traz horários, mapa, selos, fotos de categoria, vantagens e indicadores comerciais
          sem suporte nos dados da página. O preço inicial por categoria segue em decisão. */}
      <section aria-labelledby="titulo-inicio" className="bg-gradient-to-br from-navy-escuro via-navy to-navy-claro pb-24 pt-12 sm:pt-16 lg:pb-32">
        <div className="mx-auto w-full max-w-7xl px-4 sm:px-6 lg:px-8">
          <h1 id="titulo-inicio" className="max-w-xl text-titulo-hero text-branco">{texto.tituloInicio}</h1>
        </div>
      </section>

      <div className="mx-auto -mt-12 w-full max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="overflow-hidden rounded-cartao bg-branco shadow-elevado">
          <nav aria-label={texto.modalidade} className="flex flex-wrap gap-2 bg-steel-fundo px-2 pt-2 sm:px-4">
            <span aria-current="page" className="inline-flex items-center gap-2 rounded-t-cartao bg-branco px-4 py-3 text-corpo-denso font-semibold text-navy">
              <Icone nome="car_rental" tamanho={18} /> {texto.alugar}
            </span>
            <Link to={CAMINHOS.seminovos} className="inline-flex items-center gap-2 rounded-t-cartao px-4 py-3 text-corpo-denso font-semibold text-navy hover:bg-steel-suave">
              <Icone nome="sell" tamanho={18} /> {texto.comprar}
            </Link>
          </nav>
          <form noValidate onSubmit={enviar} className="p-4 sm:p-6">
            <CamposPeriodoBusca rascunho={rascunho} hoje={hoje} erros={erros} aoAlterar={alterar} textoBotao={texto.buscarVeiculos} />
          </form>
        </div>
      </div>

      <section aria-labelledby="titulo-categorias" className="mx-auto w-full max-w-7xl px-4 py-12 sm:px-6 lg:px-8">
        <h2 id="titulo-categorias" className="mb-6 text-titulo text-navy">{texto.categorias}</h2>
        {categorias.isPending ? (
          <output className="block text-corpo-denso text-texto-apoio">{texto.carregandoCategorias}</output>
        ) : categorias.isError ? (
          <EstadoErro titulo={texto.erroCategorias} aoTentarNovamente={() => void categorias.refetch()} tentando={categorias.isFetching} />
        ) : categorias.data.itens.length === 0 ? (
          <p className="text-corpo-denso text-texto-apoio">{texto.semCategorias}</p>
        ) : (
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            {categorias.data.itens.map((categoria) => {
              const parametros = montarParametrosBusca(rascunho);
              parametros.set('idCategoria', String(categoria.idCategoria));
              return (
                <article key={categoria.idCategoria} className="flex min-w-0 flex-col overflow-hidden rounded-cartao border border-borda bg-branco shadow-cartao">
                  <div className="flex h-24 items-center justify-center bg-steel-fundo text-navy">
                    <Icone nome="directions_car" tamanho={48} />
                  </div>
                  <div className="flex flex-1 flex-col gap-3 p-4">
                    <h3 className="break-words text-secao text-navy">{categoria.nome}</h3>
                    {categoria.descricao && <p className="break-words text-corpo-denso text-texto-apoio">{categoria.descricao}</p>}
                    <BotaoLink
                      to={{ pathname: CAMINHOS.resultadosBusca, search: parametros.toString() }}
                      variante="secundario"
                      iconeDireita="chevron_right"
                      className="mt-auto"
                      aria-label={texto.verCategoriaNome(categoria.nome)}
                    >
                      {texto.verCategoria}
                    </BotaoLink>
                  </div>
                </article>
              );
            })}
          </div>
        )}
      </section>
    </div>
  );
}
