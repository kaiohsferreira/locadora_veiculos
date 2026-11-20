import { useMemo, useState, type FormEvent } from 'react';
import { generatePath, useSearchParams } from 'react-router-dom';
import { useTituloDocumento } from '@/hooks/useTituloDocumento';
import { CAMINHOS } from '@/routes/caminhos';
import { obterMensagemErro } from '@/lib/erros';
import { formatarData } from '@/lib/formatters';
import { Botao, BotaoLink, Campo, Cartao, EstadoErro, EstadoVazio, Esqueleto, Paginacao, Select } from '@/components/ui';
import { useAoFalharFotoCapa } from '@/features/area-cliente/hooks/useAoFalharFotoCapa';
import { FotoVeiculo } from '@/features/area-cliente/reservas/componentes/FotoVeiculo';
import { useCategoriasVeiculos, useVeiculosDisponiveis } from '../hooks/useVeiculosDisponiveis';
import type { FiltroVeiculosDisponiveis, VeiculoDisponivel } from '@/types/veiculos';

const TAMANHO_PAGINA = 12;

interface FiltrosBusca {
  localRetirada: string;
  dataRetirada: string;
  dataDevolucao: string;
  idCategoria: string;
  valorDiariaMin: string;
  valorDiariaMax: string;
}

function filtrosDaUrl(parametros: URLSearchParams): FiltrosBusca {
  return {
    localRetirada: parametros.get('localRetirada') ?? '',
    dataRetirada: parametros.get('dataRetirada') ?? '',
    dataDevolucao: parametros.get('dataDevolucao') ?? '',
    idCategoria: parametros.get('idCategoria') ?? '',
    valorDiariaMin: parametros.get('valorDiariaMin') ?? '',
    valorDiariaMax: parametros.get('valorDiariaMax') ?? '',
  };
}

function valorNumericoOpcional(valor: string): number | undefined {
  if (valor.trim() === '') return undefined;
  const numero = Number(valor);
  return Number.isFinite(numero) ? numero : undefined;
}

function CartaoVeiculo({ veiculo, buscaAtual, aoFalharFoto }: { veiculo: VeiculoDisponivel; buscaAtual: string; aoFalharFoto: () => void }) {
  const titulo = `${veiculo.marca} ${veiculo.modelo}`;
  const localLocadora = [veiculo.locadoraCidade, veiculo.locadoraUf].filter(Boolean).join(' · ');
  return (
    <article className="overflow-hidden rounded-lg border border-borda bg-branco shadow-cartao">
      <FotoVeiculo fotoCapa={veiculo.fotoCapa} descricao={titulo} aoFalhar={aoFalharFoto} />
      <div className="space-y-4 p-5">
        <div>
          <p className="text-legenda font-medium uppercase tracking-wide text-orange-escuro">{veiculo.categoriaNome}</p>
          <h2 className="mt-1 text-xl font-bold text-navy">{titulo}</h2>
          <p className="mt-1 text-corpo-denso text-texto-suave">
            {veiculo.anoModelo}{veiculo.cor ? ` · ${veiculo.cor}` : ''}
          </p>
          <p className="mt-1 text-legenda text-texto-suave">
            {veiculo.locadoraNomeFantasia}{localLocadora ? ` · ${localLocadora}` : ''}
          </p>
        </div>
        <p className="text-corpo-denso text-texto-suave">
          <span className="text-xl font-bold text-navy">{veiculo.valorDiariaStr}</span> / diária
        </p>
        <BotaoLink
          to={{
            pathname: generatePath(CAMINHOS.detalheVeiculo, { veiculoId: String(veiculo.idVeiculo) }),
            search: buscaAtual ? `?${buscaAtual}` : '',
          }}
          variante="contorno"
          larguraTotal
          iconeDireita="arrow_forward"
          aria-label={`Ver detalhes de ${titulo}`}
        >
          Ver veículo
        </BotaoLink>
      </div>
    </article>
  );
}

function CartoesCarregando() {
  return (
    <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3" aria-label="Carregando veículos" aria-live="polite">
      {Array.from({ length: 6 }, (_, indice) => (
        <div key={indice} className="space-y-4 rounded-lg border border-borda bg-branco p-5">
          <Esqueleto className="h-40 w-full" />
          <Esqueleto className="h-4 w-2/5" />
          <Esqueleto className="h-6 w-3/4" />
          <Esqueleto className="h-10 w-full" />
        </div>
      ))}
    </div>
  );
}

/** Resultados públicos usados no fluxo de busca P3-FE-01; a tela também corresponde à P3-FE-02. */
export default function ResultadosBuscaPagina() {
  const [parametros] = useSearchParams();
  return <ConteudoResultadosBusca key={parametros.toString()} />;
}

function ConteudoResultadosBusca() {
  useTituloDocumento('Veículos disponíveis');
  const [parametros, definirParametros] = useSearchParams();
  const [filtros, definirFiltros] = useState<FiltrosBusca>(() => filtrosDaUrl(parametros));
  const [erroFormulario, definirErroFormulario] = useState('');
  const [pagina, definirPagina] = useState(Number(parametros.get('pagina')) || 1);
  const filtrosAplicados = filtrosDaUrl(parametros);
  const categorias = useCategoriasVeiculos();

  const filtroApi = useMemo<FiltroVeiculosDisponiveis>(() => ({
    dataRetirada: filtrosAplicados.dataRetirada,
    dataDevolucao: filtrosAplicados.dataDevolucao,
    ...(filtrosAplicados.idCategoria ? { idCategoria: Number(filtrosAplicados.idCategoria) } : {}),
    ...(valorNumericoOpcional(filtrosAplicados.valorDiariaMin) !== undefined
      ? { valorDiariaMin: valorNumericoOpcional(filtrosAplicados.valorDiariaMin) }
      : {}),
    ...(valorNumericoOpcional(filtrosAplicados.valorDiariaMax) !== undefined
      ? { valorDiariaMax: valorNumericoOpcional(filtrosAplicados.valorDiariaMax) }
      : {}),
    pagina,
    tamanhoPagina: TAMANHO_PAGINA,
  }), [filtrosAplicados.dataDevolucao, filtrosAplicados.dataRetirada, filtrosAplicados.idCategoria, filtrosAplicados.valorDiariaMax, filtrosAplicados.valorDiariaMin, pagina]);

  const datasValidas = Boolean(
    filtrosAplicados.dataRetirada &&
    filtrosAplicados.dataDevolucao &&
    filtrosAplicados.dataRetirada < filtrosAplicados.dataDevolucao,
  );
  const localRetiradaValido = filtrosAplicados.localRetirada.trim().length > 0;
  const contextoValido = datasValidas && localRetiradaValido;
  const disponibilidade = useVeiculosDisponiveis(filtroApi, contextoValido);
  const aoFalharFoto = useAoFalharFotoCapa(() => void disponibilidade.refetch());

  function alterarFiltro<K extends keyof FiltrosBusca>(campo: K, valor: FiltrosBusca[K]) {
    definirFiltros((atuais) => ({ ...atuais, [campo]: valor }));
    definirErroFormulario('');
  }

  function buscar(evento: FormEvent<HTMLFormElement>) {
    evento.preventDefault();
    if (filtros.localRetirada.trim() === '') {
      definirErroFormulario('Informe a unidade de retirada.');
      return;
    }
    if (!filtros.dataRetirada || !filtros.dataDevolucao) {
      definirErroFormulario('Informe as datas de retirada e devolução.');
      return;
    }
    if (filtros.dataRetirada >= filtros.dataDevolucao) {
      definirErroFormulario('A devolução deve acontecer depois da retirada.');
      return;
    }
    const minimo = valorNumericoOpcional(filtros.valorDiariaMin);
    const maximo = valorNumericoOpcional(filtros.valorDiariaMax);
    if ((filtros.valorDiariaMin && (minimo === undefined || minimo < 0)) || (filtros.valorDiariaMax && (maximo === undefined || maximo < 0))) {
      definirErroFormulario('Informe valores de diária válidos e maiores ou iguais a zero.');
      return;
    }
    if (minimo !== undefined && maximo !== undefined && minimo > maximo) {
      definirErroFormulario('O valor máximo da diária deve ser maior ou igual ao mínimo.');
      return;
    }

    definirErroFormulario('');
    definirPagina(1);
    const novosParametros = new URLSearchParams();
    novosParametros.set('localRetirada', filtros.localRetirada.trim());
    novosParametros.set('dataRetirada', filtros.dataRetirada);
    novosParametros.set('dataDevolucao', filtros.dataDevolucao);
    if (filtros.idCategoria) novosParametros.set('idCategoria', filtros.idCategoria);
    if (filtros.valorDiariaMin) novosParametros.set('valorDiariaMin', filtros.valorDiariaMin);
    if (filtros.valorDiariaMax) novosParametros.set('valorDiariaMax', filtros.valorDiariaMax);
    definirParametros(novosParametros);
  }

  function mudarPagina(novaPagina: number) {
    definirPagina(novaPagina);
    const novosParametros = new URLSearchParams(parametros);
    novosParametros.set('pagina', String(novaPagina));
    definirParametros(novosParametros);
  }

  const total = disponibilidade.data?.total ?? 0;
  const descricaoPeriodo = datasValidas
    ? `Unidade de retirada: ${filtrosAplicados.localRetirada || 'não informada'} · retirada ${formatarData(filtrosAplicados.dataRetirada)} · devolução ${formatarData(filtrosAplicados.dataDevolucao)}`
    : 'Escolha o período para consultar os veículos disponíveis.';

  return (
    <div className="mx-auto w-full max-w-7xl px-4 py-8 sm:px-6 lg:px-8 lg:py-12">
      <header className="mb-8">
        <p className="text-legenda font-semibold uppercase tracking-wider text-orange-escuro">Busca de veículos</p>
        <h1 className="mt-2 text-3xl font-bold text-navy sm:text-4xl">Encontre o carro para sua viagem</h1>
        <p className="mt-2 text-corpo-denso text-texto-suave">{descricaoPeriodo}</p>
      </header>

      <div className="grid gap-8 lg:grid-cols-[20rem_minmax(0,1fr)] lg:items-start">
        <aside aria-label="Filtros da busca" className="min-w-0">

          <Cartao titulo="Ajuste sua busca" descricao="Filtre por período, categoria e valor da diária.">
            <form className="space-y-5" onSubmit={buscar} noValidate>
              <div className="grid gap-4">
                <Campo
                  rotulo="Unidade de retirada"
                  name="localRetirada"
                  type="text"
                  autoComplete="off"
                  maxLength={100}
                  required
                  dica="Contexto livre de retirada. A consulta considera a frota global, sem filtro por unidade."
                  value={filtros.localRetirada}
                  erro={erroFormulario && filtros.localRetirada.trim() === '' ? 'Informe a unidade de retirada.' : undefined}
                  onChange={(evento) => alterarFiltro('localRetirada', evento.target.value)}
                />
                <Campo
                  rotulo="Data de retirada"
                  type="date"
                  name="dataRetirada"
                  required
                  value={filtros.dataRetirada}
                  erro={erroFormulario && !filtros.dataRetirada ? 'Informe a data de retirada.' : undefined}
                  onChange={(evento) => alterarFiltro('dataRetirada', evento.target.value)}
                />
                <Campo
                  rotulo="Data de devolução"
                  type="date"
                  name="dataDevolucao"
                  required
                  min={filtros.dataRetirada || undefined}
                  value={filtros.dataDevolucao}
                  erro={erroFormulario && (!filtros.dataDevolucao || filtros.dataRetirada >= filtros.dataDevolucao) ? erroFormulario : undefined}
                  onChange={(evento) => alterarFiltro('dataDevolucao', evento.target.value)}
                />
                <Select
                  rotulo="Categoria"
                  name="idCategoria"
                  value={filtros.idCategoria}
                  disabled={categorias.isPending || categorias.isError}
                  dica={categorias.isPending ? 'Carregando categorias…' : undefined}
                  opcoes={(categorias.data?.itens ?? []).map((categoria) => ({ valor: String(categoria.idCategoria), rotulo: categoria.nome }))}
                  onChange={(evento) => alterarFiltro('idCategoria', evento.target.value)}
                />
                <Campo
                  rotulo="Diária a partir de"
                  type="number"
                  name="valorDiariaMin"
                  min="0"
                  step="0.01"
                  inputMode="decimal"
                  placeholder="R$ 0,00"
                  value={filtros.valorDiariaMin}
                  erro={erroFormulario && filtros.valorDiariaMin && (valorNumericoOpcional(filtros.valorDiariaMin) === undefined || Number(filtros.valorDiariaMin) < 0) ? erroFormulario : undefined}
                  onChange={(evento) => alterarFiltro('valorDiariaMin', evento.target.value)}
                />
                <Campo
                  rotulo="Diária até"
                  type="number"
                  name="valorDiariaMax"
                  min="0"
                  step="0.01"
                  inputMode="decimal"
                  placeholder="Sem limite"
                  value={filtros.valorDiariaMax}
                  erro={erroFormulario && filtros.valorDiariaMax && ((valorNumericoOpcional(filtros.valorDiariaMax) === undefined || Number(filtros.valorDiariaMax) < 0) || (valorNumericoOpcional(filtros.valorDiariaMin) !== undefined && Number(filtros.valorDiariaMin) > Number(filtros.valorDiariaMax))) ? erroFormulario : undefined}
                  onChange={(evento) => alterarFiltro('valorDiariaMax', evento.target.value)}
                />
              </div>
              {categorias.isError && (
                <EstadoErro
                  titulo="Não foi possível carregar as categorias"
                  mensagem={obterMensagemErro(categorias.error)}
                  aoTentarNovamente={() => void categorias.refetch()}
                  tentando={categorias.isFetching}
                />
              )}
              {erroFormulario && <p role="alert" className="text-corpo-denso font-medium text-status-perigo">{erroFormulario}</p>}
              <div className="flex flex-col gap-3">
                <p className="text-legenda text-texto-suave">A disponibilidade é global e não é filtrada por unidade. Reservas sobrepostas ao período são removidas.</p>
                <Botao type="submit" tamanho="grande" larguraTotal carregando={disponibilidade.isFetching} icone="search">
                  Buscar veículos
                </Botao>
              </div>
            </form>
          </Cartao>
        </aside>

        <section aria-labelledby="titulo-resultados" className="min-w-0">
          <div className="mb-5 flex flex-wrap items-end justify-between gap-3">
            <div>
              <h2 id="titulo-resultados" className="text-2xl font-bold text-navy">Veículos disponíveis</h2>
              {contextoValido && !disponibilidade.isPending && !disponibilidade.isError && (
                <p className="mt-1 text-corpo-denso text-texto-suave" aria-live="polite">
                  {total === 1 ? '1 veículo encontrado' : `${total} veículos encontrados`}
                </p>
              )}
          </div>
        </div>

          {!contextoValido ? (
            <EstadoVazio
              titulo={localRetiradaValido ? 'Informe um período para começar' : 'Informe a unidade de retirada'}
              descricao={localRetiradaValido
                ? 'Escolha as datas de retirada e devolução e selecione Buscar veículos.'
                : 'Digite o nome da unidade para manter o contexto da sua retirada.'}
              icone="place"
            />
          ) : disponibilidade.isPending ? (
            <CartoesCarregando />
          ) : disponibilidade.isError ? (
            <EstadoErro
              titulo="Não foi possível buscar os veículos"
              mensagem={obterMensagemErro(disponibilidade.error)}
              aoTentarNovamente={() => void disponibilidade.refetch()}
              tentando={disponibilidade.isFetching}
            />
          ) : disponibilidade.data.itens.length === 0 ? (
            <EstadoVazio titulo="Nenhum veículo disponível para este período" descricao="Altere as datas ou os filtros de categoria e diária para tentar novamente." icone="directions_car" />
          ) : (
            <>
              <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
                {disponibilidade.data.itens.map((veiculo) => (
                  <CartaoVeiculo key={veiculo.idVeiculo} veiculo={veiculo} buscaAtual={parametros.toString()} aoFalharFoto={aoFalharFoto} />
                ))}
              </div>
              <div className="mt-5 rounded-lg border border-borda bg-branco">
                <Paginacao
                  pagina={disponibilidade.data.pagina}
                  tamanhoPagina={disponibilidade.data.tamanhoPagina}
                  total={disponibilidade.data.total}
                  aoMudarPagina={mudarPagina}
                  desabilitada={disponibilidade.isFetching}
                  rotuloItens="veículos"
                />
              </div>
            </>
          )}
        </section>
      </div>
    </div>
  );
}
