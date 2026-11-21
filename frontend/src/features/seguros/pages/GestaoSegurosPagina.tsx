import { useMemo, useRef, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { Botao, EstadoErro, EstadoVazio, Paginacao } from '@/components/ui';
import { FiltrosSeguros } from '@/features/seguros/componentes/FiltrosSeguros';
import { IndicadoresSeguros } from '@/features/seguros/componentes/IndicadoresSeguros';
import { PainelApolice, type ModoPainelApolice } from '@/features/seguros/componentes/PainelApolice';
import { TabelaApolices } from '@/features/seguros/componentes/TabelaApolices';
import { useApolices, useTotalApolices } from '@/features/seguros/hooks/useApolices';
import {
  escreverFiltrosSeguros,
  lerFiltrosSeguros,
  montarFiltroApolices,
  type EstadoFiltrosSeguros,
} from '@/features/seguros/regras/enderecoSeguros';
import { useTituloDocumento } from '@/hooks/useTituloDocumento';
import { obterMensagemErro } from '@/lib/erros';
import {
  SEGUROS_BOTAO_NOVA,
  SEGUROS_ERRO_LISTA,
  SEGUROS_RODAPE_ITENS,
  SEGUROS_SUBTITULO,
  SEGUROS_TITULO_PAGINA,
  SEGUROS_VAZIO_A_VENCER_TITULO,
  SEGUROS_VAZIO_FILTRO_TEXTO,
  SEGUROS_VAZIO_TEXTO,
  SEGUROS_VAZIO_TITULO,
  SEGUROS_VAZIO_VEICULO_TITULO,
  SEGUROS_VER_TODAS,
} from '@/lib/mensagensSeguros';
import type { Apolice, VeiculoPlaca } from '@/types/seguros';

/**
 * P9-FE-01 · Gestão de seguros (RF-41), no back office. Sem protótipo: segue o molde da tela de Equipe (cabeçalho com a
 * ação principal, indicadores, faixa de filtros, tabela paginada no servidor e gaveta lateral para cadastrar e editar).
 * Filtros e página ficam no endereço. O vencimento sempre aparece com texto, a partir do que o servidor calculou.
 */
export default function GestaoSegurosPagina() {
  useTituloDocumento(SEGUROS_TITULO_PAGINA);
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

  function verTodas() {
    irPara({ pagina: 1, apenasAVencer: false, veiculo: null });
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
      <header className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
        <div className="flex flex-col">
          <h1 className="text-titulo text-navy">{SEGUROS_TITULO_PAGINA}</h1>
          <p className="mt-1 text-corpo text-texto-suave">{SEGUROS_SUBTITULO}</p>
        </div>
        <div className="flex shrink-0">
          <Botao icone="shield" larguraTotal className="md:w-auto" onClick={(evento) => abrirCadastro(evento.currentTarget)}>
            {SEGUROS_BOTAO_NOVA}
          </Botao>
        </div>
      </header>

      <IndicadoresSeguros
        totalCadastradas={totalCadastradas.data?.total}
        totalAVencer={totalAVencer.data?.total}
        carregando={totalCadastradas.isPending || totalAVencer.isPending}
        aoVerAVencer={() => mudarSituacao(true)}
      />

      <FiltrosSeguros
        veiculo={estado.veiculo}
        apenasAVencer={estado.apenasAVencer}
        aoEscolherVeiculo={escolherVeiculo}
        aoMudarSituacao={mudarSituacao}
      />

      <section className="overflow-hidden rounded-cartao bg-branco shadow-cartao">
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
