import { useMemo, useRef, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { Botao, EstadoErro, EstadoVazio, Icone, Paginacao } from '@/components/ui';
import { FiltroPlaca, PilulasSituacao } from '@/features/seguros/componentes/FiltrosSeguros';
import { IndicadoresSeguros } from '@/features/seguros/componentes/IndicadoresSeguros';
import { PainelApolice, type ModoPainelApolice } from '@/features/seguros/componentes/PainelApolice';
import { TabelaApolices } from '@/features/seguros/componentes/TabelaApolices';
import { useApolices, useTotalApolices } from '@/features/seguros/hooks/useApolices';
import {
  escreverFiltrosSeguros,
  lerFiltrosSeguros,
  montarFiltroApolices,
  OPCOES_ITENS_POR_PAGINA,
  type EstadoFiltrosSeguros,
} from '@/features/seguros/regras/enderecoSeguros';
import { useTituloDocumento } from '@/hooks/useTituloDocumento';
import { obterMensagemErro } from '@/lib/erros';
import {
  SEGUROS_BOTAO_NOVA,
  SEGUROS_ERRO_LISTA,
  SEGUROS_ITENS_POR_PAGINA,
  SEGUROS_RODAPE_ITENS,
  SEGUROS_SUBTITULO,
  SEGUROS_TITULO_DOCUMENTO,
  SEGUROS_TITULO_LISTA,
  SEGUROS_TITULO_PAGINA,
  SEGUROS_TRILHA,
  SEGUROS_TRILHA_ROTULO,
  SEGUROS_VAZIO_A_VENCER_TITULO,
  SEGUROS_VAZIO_FILTRO_TEXTO,
  SEGUROS_VAZIO_TEXTO,
  SEGUROS_VAZIO_TITULO,
  SEGUROS_VAZIO_VEICULO_TITULO,
  SEGUROS_VER_TODAS,
  textoQuantidadeApolices,
} from '@/lib/mensagensSeguros';
import type { Apolice, VeiculoPlaca } from '@/types/seguros';

/** Trilha do desenho 13, em caixa alta, com o último item em laranja. */
function Trilha() {
  return (
    <nav aria-label={SEGUROS_TRILHA_ROTULO}>
      <ol className="flex flex-wrap items-center gap-1 text-legenda uppercase tracking-wide text-texto-suave">
        {SEGUROS_TRILHA.map((item, indice) => {
          const ultimo = indice === SEGUROS_TRILHA.length - 1;
          return (
            <li key={item} className="flex items-center gap-1">
              {ultimo ? (
                <span aria-current="page" className="font-semibold text-orange-escuro">
                  {item}
                </span>
              ) : (
                <>
                  <span>{item}</span>
                  <Icone nome="chevron_right" tamanho={14} />
                </>
              )}
            </li>
          );
        })}
      </ol>
    </nav>
  );
}

/** Seletor "Itens por página" do rodapé da tabela. */
function ItensPorPagina({ valor, aoMudar }: { valor: number; aoMudar: (tamanho: number) => void }) {
  return (
    <label className="flex items-center gap-2 text-corpo-denso text-texto-suave">
      {SEGUROS_ITENS_POR_PAGINA}
      <select
        value={valor}
        onChange={(evento) => aoMudar(Number(evento.target.value))}
        className="h-8 rounded-md border border-borda bg-branco px-2 text-corpo-denso text-texto focus:outline-none focus:ring-2 focus:ring-orange"
      >
        {OPCOES_ITENS_POR_PAGINA.map((opcao) => (
          <option key={opcao} value={opcao}>
            {opcao}
          </option>
        ))}
      </select>
    </label>
  );
}

/**
 * P9-FE-01 · Gestão de seguros (RF-41), no back office, desenho 13_back_office_gest_o_de_seguros. Cabeçalho com a trilha
 * e "+ Cadastrar apólice", cartões em linha, "Apólices da Frota em Vigência" com o selo de quantidade e as pílulas
 * "Todas" e "Vencendo em breve", tabela paginada no servidor com "Itens por página" e gaveta lateral para cadastrar e
 * editar. Filtros, página e itens por página ficam no endereço. O status sempre aparece com texto, a partir do que o
 * servidor calculou.
 *
 * Fora do desenho por não existirem no sistema: busca rápida do topo e seletor de pátio, "Exportar Relatório Geral", a
 * busca única com os filtros de seguradora, vigência, status e categoria, "Renovar" e ações em lote, e os cartões de
 * integração EDI, renovações em lote e abertura de sinistro.
 */
export default function GestaoSegurosPagina() {
  useTituloDocumento(SEGUROS_TITULO_DOCUMENTO);
  const [parametros, setParametros] = useSearchParams();
  const chaveEndereco = parametros.toString();
  const estado = useMemo(() => lerFiltrosSeguros(new URLSearchParams(chaveEndereco)), [chaveEndereco]);

  const consulta = useApolices(montarFiltroApolices(estado));
  const totalCadastradas = useTotalApolices(false);
  const totalAVencer = useTotalApolices(true);

  const [painel, setPainel] = useState<ModoPainelApolice | null>(null);
  const refGatilho = useRef<HTMLElement | null>(null);

  function irPara(novo: EstadoFiltrosSeguros) {
    setParametros(escreverFiltrosSeguros(novo));
  }

  function escolherVeiculo(veiculo: VeiculoPlaca | null) {
    irPara({ ...estado, pagina: 1, veiculo: veiculo ? { idVeiculo: veiculo.idVeiculo, placa: veiculo.placa } : null });
  }

  function filtrarVeiculoDaApolice(apolice: Apolice) {
    irPara({ ...estado, pagina: 1, veiculo: { idVeiculo: apolice.idVeiculo, placa: apolice.placaVeiculo } });
  }

  function mudarSituacao(apenasAVencer: boolean) {
    irPara({ ...estado, pagina: 1, apenasAVencer });
  }

  function mudarItensPorPagina(tamanhoPagina: number) {
    irPara({ ...estado, pagina: 1, tamanhoPagina });
  }

  function verTodas() {
    irPara({ ...estado, pagina: 1, apenasAVencer: false, veiculo: null });
  }

  function abrirCadastro(gatilho: HTMLElement) {
    refGatilho.current = gatilho;
    setPainel({ tipo: 'cadastro' });
  }

  function abrirEdicao(apolice: Apolice, gatilho: HTMLElement) {
    refGatilho.current = gatilho;
    setPainel({ tipo: 'edicao', idSeguro: apolice.idSeguro });
  }

  const dados = consulta.data;
  const filtrado = estado.apenasAVencer || estado.veiculo !== null;
  let vazio;
  if (!filtrado) {
    vazio = (
      <EstadoVazio
        icone="shield"
        titulo={SEGUROS_VAZIO_TITULO}
        descricao={SEGUROS_VAZIO_TEXTO}
        acao={
          <Botao icone="add" onClick={(evento) => abrirCadastro(evento.currentTarget)}>
            {SEGUROS_BOTAO_NOVA}
          </Botao>
        }
      />
    );
  } else {
    vazio = (
      <EstadoVazio
        icone="search_off"
        titulo={estado.veiculo !== null ? SEGUROS_VAZIO_VEICULO_TITULO : SEGUROS_VAZIO_A_VENCER_TITULO}
        descricao={SEGUROS_VAZIO_FILTRO_TEXTO}
        acao={
          <Botao variante="contorno" onClick={verTodas}>
            {SEGUROS_VER_TODAS}
          </Botao>
        }
      />
    );
  }

  return (
    <div className="flex flex-col gap-6">
      <header className="flex flex-col gap-2">
        <Trilha />
        <div className="flex flex-col gap-4 md:flex-row md:items-end md:justify-between">
          <div className="flex flex-col">
            <h1 className="text-titulo font-bold text-navy">{SEGUROS_TITULO_PAGINA}</h1>
            <p className="mt-1 text-corpo text-texto-suave">{SEGUROS_SUBTITULO}</p>
          </div>
          <div className="flex shrink-0">
            <Botao icone="add" larguraTotal className="md:w-auto" onClick={(evento) => abrirCadastro(evento.currentTarget)}>
              {SEGUROS_BOTAO_NOVA}
            </Botao>
          </div>
        </div>
      </header>

      <IndicadoresSeguros
        totalCadastradas={totalCadastradas.data?.total}
        totalAVencer={totalAVencer.data?.total}
        carregando={totalCadastradas.isPending || totalAVencer.isPending}
        aoVerAVencer={() => mudarSituacao(true)}
      />

      <section className="overflow-hidden rounded-cartao bg-branco shadow-cartao" aria-labelledby="titulo-lista-apolices">
        <div className="flex flex-col gap-4 p-4 sm:p-6">
          <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
            <div className="flex items-center gap-2">
              <span aria-hidden="true" className="h-6 w-1.5 rounded-full bg-orange" />
              <h2 id="titulo-lista-apolices" className="text-secao font-bold text-navy">
                {SEGUROS_TITULO_LISTA}
              </h2>
              {dados && (
                <span className="rounded-full bg-steel-suave px-2 py-0.5 text-legenda font-semibold text-navy" data-testid="selo-quantidade">
                  {textoQuantidadeApolices(dados.total)}
                </span>
              )}
            </div>
            <PilulasSituacao
              apenasAVencer={estado.apenasAVencer}
              totalTodas={totalCadastradas.data?.total}
              totalVencendo={totalAVencer.data?.total}
              aoMudarSituacao={mudarSituacao}
            />
          </div>
          <FiltroPlaca veiculo={estado.veiculo} aoEscolherVeiculo={escolherVeiculo} />
        </div>

        {consulta.isError && !dados ? (
          <EstadoErro
            titulo={SEGUROS_ERRO_LISTA}
            mensagem={obterMensagemErro(consulta.error)}
            aoTentarNovamente={() => void consulta.refetch()}
            tentando={consulta.isFetching}
          />
        ) : (
          <>
            <div className={consulta.isPlaceholderData ? 'opacity-60 transition-opacity' : 'transition-opacity'}>
              <TabelaApolices
                itens={dados?.itens ?? []}
                carregando={consulta.isPending}
                vazio={vazio}
                aoEditar={abrirEdicao}
                aoFiltrarVeiculo={filtrarVeiculoDaApolice}
              />
            </div>
            {dados && dados.total > 0 && (
              <div className="border-t border-borda">
                <Paginacao
                  pagina={dados.pagina}
                  tamanhoPagina={dados.tamanhoPagina}
                  total={dados.total}
                  aoMudarPagina={(pagina) => irPara({ ...estado, pagina })}
                  desabilitada={consulta.isFetching}
                  rotuloItens={SEGUROS_RODAPE_ITENS}
                  complemento={<ItensPorPagina valor={estado.tamanhoPagina} aoMudar={mudarItensPorPagina} />}
                />
              </div>
            )}
          </>
        )}
      </section>

      {painel !== null && <PainelApolice modo={painel} refGatilho={refGatilho} aoFechar={() => setPainel(null)} />}
    </div>
  );
}
