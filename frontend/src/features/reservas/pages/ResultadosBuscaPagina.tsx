import { useState, type FormEvent } from 'react';
import { useSearchParams } from 'react-router-dom';
import { Esqueleto, EstadoErro, EstadoVazio, Paginacao } from '@/components/ui';
import { useTituloDocumento } from '@/hooks/useTituloDocumento';
import { obterMensagemErro } from '@/lib/erros';
import { formatarData } from '@/lib/formatters';
import { MENSAGENS_BUSCA as texto } from '@/lib/mensagensBusca';
import { useAoFalharFotoCapa } from '@/features/area-cliente/hooks/useAoFalharFotoCapa';
import { AvisoFotosReservas } from '@/features/area-cliente/reservas/componentes/AvisoFotosReservas';
import { CamposPeriodoBusca } from '../busca/CamposPeriodoBusca';
import { FiltrosDisponibilidade } from '../busca/FiltrosDisponibilidade';
import { LinhaVeiculo } from '../busca/LinhaVeiculo';
import {
  avaliarPeriodo, BUSCA_VAZIA, errosBuscaDaApi, hojeLocal, lerBuscaDaUrl, lerLocal,
  montarFiltroBusca, montarParametrosBusca, validarBusca, type RascunhoBusca,
} from '../busca/parametrosBusca';
import { useVeiculosDisponiveis } from '../hooks/useVeiculosDisponiveis';

interface ResultadosBuscaProps {
  aplicados: RascunhoBusca;
  pagina: number;
  definirParametros: (parametros: URLSearchParams) => void;
}

function ResultadosCarregando() {
  return (
    <div aria-busy="true" className="space-y-4">
      <output className="block text-corpo-denso text-texto-apoio">{texto.carregandoVeiculos}</output>
      {Array.from({ length: 3 }, (_, indice) => (
        <div key={indice} className="grid gap-4 rounded-cartao border border-borda bg-branco p-4 sm:grid-cols-3" aria-hidden="true">
          <Esqueleto className="h-24 w-full" />
          <Esqueleto className="h-6 w-full" />
          <Esqueleto className="h-10 w-full" />
        </div>
      ))}
    </div>
  );
}

/** P3-FE-02 · Resultados públicos da pesquisa de veículos. */
export default function ResultadosBuscaPagina() {
  const [parametros, definirParametros] = useSearchParams();
  const { rascunho, pagina } = lerBuscaDaUrl(parametros);
  // A troca dos filtros aplicados sincroniza com a navegação. A página fica fora da chave para manter a edição.
  return <ResultadosBusca key={montarParametrosBusca(rascunho).toString()} aplicados={rascunho} pagina={pagina} definirParametros={definirParametros} />;
}

function ResultadosBusca({ aplicados, pagina, definirParametros }: ResultadosBuscaProps) {
  useTituloDocumento(texto.tituloResultados);
  const hoje = hojeLocal();
  const [rascunho, definirRascunho] = useState(aplicados);
  const [tentouBuscar, definirTentouBuscar] = useState(false);
  const periodo = avaliarPeriodo(aplicados, hoje);
  const disponibilidade = useVeiculosDisponiveis(montarFiltroBusca(aplicados, pagina), periodo.diarias > 0);
  const aoFalharFoto = useAoFalharFotoCapa(() => void disponibilidade.refetch());
  const errosApi = errosBuscaDaApi(disponibilidade.error);
  const temErroDeCampoApi = Object.values(errosApi).some(Boolean);
  const erros = tentouBuscar ? validarBusca(rascunho, hoje) : {};
  for (const campo of Object.keys(errosApi) as (keyof RascunhoBusca)[]) {
    if (rascunho[campo] === aplicados[campo]) erros[campo] ??= errosApi[campo];
  }

  function alterar(campo: keyof RascunhoBusca, valor: string) {
    definirRascunho((atual) => ({ ...atual, [campo]: valor }));
  }

  function enviar(evento: FormEvent<HTMLFormElement>) {
    evento.preventDefault();
    definirTentouBuscar(true);
    if (Object.keys(validarBusca(rascunho, hoje)).length > 0) return;
    definirTentouBuscar(false);
    const novaBusca = montarParametrosBusca(rascunho);
    if (novaBusca.toString() === montarParametrosBusca(aplicados, pagina).toString()) {
      void disponibilidade.refetch();
      return;
    }
    definirParametros(novaBusca);
  }

  function limpar() {
    const semFiltros = {
      idCategoria: BUSCA_VAZIA.idCategoria,
      valorDiariaMin: BUSCA_VAZIA.valorDiariaMin,
      valorDiariaMax: BUSCA_VAZIA.valorDiariaMax,
    };
    definirRascunho((atual) => ({ ...atual, ...semFiltros }));
    definirParametros(montarParametrosBusca({ ...aplicados, ...semFiltros }));
  }

  const local = lerLocal(aplicados.local);
  const itens = disponibilidade.data?.itens ?? [];
  const total = disponibilidade.data?.total;
  const avisoFoto = itens.find((veiculo) => veiculo.erroFotoCapa)?.erroFotoCapa;
  const consulta = montarParametrosBusca(aplicados, pagina).toString();

  return (
    <div className="bg-offwhite">
      {/* Hora, câmbio, combustível, opcionais, selos, ordenação, contagem por categoria, notas e
          quantidade de agências dependem de dados ausentes no contrato. Total e diárias por veículo seguem em decisão. */}
      <form noValidate onSubmit={enviar}>
        <div className="bg-navy py-5">
          <div className="mx-auto w-full max-w-7xl px-4 sm:px-6 lg:px-8">
            <div className="rounded-cartao bg-branco p-4 shadow-cartao">
              <CamposPeriodoBusca
                rascunho={rascunho}
                hoje={hoje}
                erros={erros}
                aoAlterar={alterar}
                textoBotao={texto.buscar}
                buscando={disponibilidade.isFetching}
              />
            </div>
          </div>
        </div>

        <div className="mx-auto grid w-full max-w-7xl gap-6 px-4 py-8 sm:px-6 lg:grid-cols-[17rem_minmax(0,1fr)] lg:items-start lg:px-8">
          <FiltrosDisponibilidade rascunho={rascunho} erros={erros} aoAlterar={alterar} aoLimpar={limpar} />
          <section aria-labelledby="titulo-resultados" className="min-w-0 space-y-4">
            <header className="rounded-cartao border border-borda bg-branco p-4 pl-5 shadow-cartao">
              <div className="border-l-4 border-orange pl-3">
                <h1 id="titulo-resultados" className="text-titulo text-navy">
                  {total === undefined ? texto.tituloResultados : texto.quantidadeVeiculos(total)}
                </h1>
                {periodo.diarias > 0 && (
                  <p className="mt-1 text-corpo-denso text-texto-apoio">
                    {texto.resumo(
                      local ? texto.cidadeUf(local.cidade, local.uf) : texto.qualquerCidade,
                      formatarData(aplicados.dataRetirada),
                      formatarData(aplicados.dataDevolucao),
                      texto.numeroDiarias(periodo.diarias),
                    )}
                  </p>
                )}
              </div>
            </header>
            {periodo.diarias === 0 ? (
              <EstadoVazio titulo={texto.semPeriodo} descricao={texto.dicaPeriodo} icone="event" />
            ) : disponibilidade.isPending ? (
              <ResultadosCarregando />
            ) : disponibilidade.isError ? (
              !temErroDeCampoApi && (
                <EstadoErro
                  titulo={texto.erroVeiculos}
                  mensagem={obterMensagemErro(disponibilidade.error)}
                  aoTentarNovamente={() => void disponibilidade.refetch()}
                  tentando={disponibilidade.isFetching}
                />
              )
            ) : (
              <>
                {avisoFoto && <AvisoFotosReservas mensagem={avisoFoto} />}
                {itens.length === 0 ? (
                  <EstadoVazio titulo={texto.semVeiculos} descricao={texto.dicaSemVeiculos} icone="directions_car" />
                ) : (
                  <div className="space-y-4">
                    {itens.map((veiculo) => (
                      <LinhaVeiculo key={veiculo.idVeiculo} veiculo={veiculo} consulta={consulta} aoFalharFoto={aoFalharFoto} />
                    ))}
                  </div>
                )}
                {total !== undefined && total > disponibilidade.data.tamanhoPagina && (
                  <Paginacao
                    pagina={disponibilidade.data.pagina}
                    tamanhoPagina={disponibilidade.data.tamanhoPagina}
                    total={total}
                    rotuloItens={texto.veiculos}
                    aoMudarPagina={(novaPagina) => definirParametros(montarParametrosBusca(aplicados, novaPagina))}
                  />
                )}
              </>
            )}
          </section>
        </div>
      </form>
    </div>
  );
}
