import { useEffect, useRef, useState } from 'react';
import { Botao, EstadoErro, EstadoVazio, Modal, Paginacao, useAviso } from '@/components/ui';
import { useTituloDocumento } from '@/hooks/useTituloDocumento';
import { obterMensagemErro } from '@/lib/erros';
import { interpretarErroFuncionario } from '@/features/equipe/erros/mapeamentoErroEquipe';
import { useAlterarSituacaoFuncionario } from '@/features/equipe/hooks/useAlterarSituacaoFuncionario';
import { useEquipe } from '@/features/equipe/hooks/useEquipe';
import { FiltrosEquipe } from '@/features/equipe/componentes/FiltrosEquipe';
import { IndicadoresEquipe } from '@/features/equipe/componentes/IndicadoresEquipe';
import { PainelFuncionario, type ModoPainelFuncionario } from '@/features/equipe/componentes/PainelFuncionario';
import { TabelaEquipe } from '@/features/equipe/componentes/TabelaEquipe';
import {
  ATRASO_BUSCA_EQUIPE_MS,
  ESTADO_FILTROS_EQUIPE_INICIAL,
  montarFiltroEquipe,
  TAMANHO_PAGINA_EQUIPE,
  temFiltroAplicado,
  type EstadoFiltrosEquipe,
} from '@/features/equipe/listagem/parametrosListagem';
import {
  MENSAGEM_EQUIPE_ADMINISTRACAO,
  MENSAGEM_EQUIPE_BOTAO_ADICIONAR,
  MENSAGEM_EQUIPE_CANCELAR,
  MENSAGEM_EQUIPE_CONFIRMAR_DESATIVACAO_TEXTO,
  MENSAGEM_EQUIPE_CONFIRMAR_DESATIVAR,
  MENSAGEM_EQUIPE_DESCRICAO,
  MENSAGEM_EQUIPE_ERRO_LISTA,
  MENSAGEM_EQUIPE_LIMPAR_FILTROS,
  MENSAGEM_EQUIPE_RODAPE_ITENS,
  MENSAGEM_EQUIPE_SUCESSO_DESATIVADO,
  MENSAGEM_EQUIPE_SUCESSO_REATIVADO,
  MENSAGEM_EQUIPE_TITULO_PAGINA,
  MENSAGEM_EQUIPE_VAZIA_COM_FILTRO,
  MENSAGEM_EQUIPE_VAZIA_COM_FILTRO_DESCRICAO,
  mensagemEquipeConfirmarDesativacaoTitulo,
  MENSAGEM_ERRO_API_PADRAO,
} from '@/lib/mensagens';
import { useAutenticacao } from '@/routes/autenticacao/useAutenticacao';
import type { FuncionarioEquipe } from '@/types/equipe';

/**
 * P10-FE-03 · Equipe da locadora (tela 41). Lista paginada com busca, cargo e situação; indicadores com os totais da
 * própria resposta; adicionar e editar pela gaveta; desativar com confirmação e reativar direto.
 */
export default function EquipePagina() {
  useTituloDocumento(MENSAGEM_EQUIPE_TITULO_PAGINA);
  const { usuario } = useAutenticacao();
  const aviso = useAviso();

  const [pagina, setPagina] = useState(1);
  const [filtros, setFiltros] = useState<EstadoFiltrosEquipe>(ESTADO_FILTROS_EQUIPE_INICIAL);
  /**
   * Busca que vai para a consulta. Digitar espera a pausa (uma consulta por pausa, não por tecla). Trocar cargo ou
   * situação, ou limpar os filtros, leva a busca da tela para a consulta na hora: nenhuma consulta sai com a busca antiga.
   */
  const [buscaConsulta, setBuscaConsulta] = useState(ESTADO_FILTROS_EQUIPE_INICIAL.busca);
  useEffect(() => {
    if (buscaConsulta === filtros.busca) return undefined;
    const temporizador = window.setTimeout(() => setBuscaConsulta(filtros.busca), ATRASO_BUSCA_EQUIPE_MS);
    return () => window.clearTimeout(temporizador);
  }, [filtros.busca, buscaConsulta]);
  const consulta = useEquipe(montarFiltroEquipe(pagina, { ...filtros, busca: buscaConsulta }));
  const dados = consulta.data;
  const situacao = useAlterarSituacaoFuncionario();

  // Página além da última (ex.: a última linha dela saiu da lista de ativos): volta para a última existente. Feito
  // durante o render, como o React indica para estado derivado; não há efeito nem laço, pois a página passa a caber.
  if (dados !== undefined) {
    const ultimaPagina = Math.max(1, Math.ceil(dados.total / TAMANHO_PAGINA_EQUIPE));
    if (pagina > ultimaPagina) setPagina(ultimaPagina);
  }

  const [painel, setPainel] = useState<ModoPainelFuncionario | null>(null);
  const [paraDesativar, setParaDesativar] = useState<FuncionarioEquipe | null>(null);
  /** Botão que abriu a gaveta (ou a confirmação): recebe o foco de volta quando ela fecha. */
  const refGatilho = useRef<HTMLElement | null>(null);
  /** Campo de busca: destino do foco quando o botão da desativação saiu da lista. */
  const refBusca = useRef<HTMLInputElement>(null);
  /** Desativação concluída: ao fechar a confirmação, a página decide o foco (ver o efeito abaixo). */
  const focarBuscaAoFecharDesativacao = useRef(false);

  // Ao fechar a confirmação depois de desativar, o Modal devolve o foco ao botão que o abriu. Se esse botão saiu da
  // lista (filtro de ativos), a devolução não acha nada e o foco iria para o body: então vai para a busca. O efeito
  // roda depois da limpeza do Modal, que já tentou devolver o foco.
  useEffect(() => {
    if (paraDesativar !== null || !focarBuscaAoFecharDesativacao.current) return;
    focarBuscaAoFecharDesativacao.current = false;
    const gatilho = refGatilho.current;
    if (gatilho === null || !gatilho.isConnected) refBusca.current?.focus();
  }, [paraDesativar]);

  /** Qualquer mudança de filtro volta para a primeira página. */
  function alterarFiltros(parcial: Partial<EstadoFiltrosEquipe>) {
    // Cargo e situação não esperam a pausa da busca: a consulta já sai com a busca que está na tela.
    if (parcial.busca === undefined) setBuscaConsulta(filtros.busca);
    setFiltros((anterior) => ({ ...anterior, ...parcial }));
    setPagina(1);
  }

  function limparFiltros() {
    setBuscaConsulta(ESTADO_FILTROS_EQUIPE_INICIAL.busca);
    setFiltros(ESTADO_FILTROS_EQUIPE_INICIAL);
    setPagina(1);
  }

  function abrirCadastro(gatilho: HTMLElement) {
    refGatilho.current = gatilho;
    setPainel({ tipo: 'cadastro' });
  }

  function abrirEdicao(funcionario: FuncionarioEquipe, gatilho: HTMLElement) {
    refGatilho.current = gatilho;
    setPainel({ tipo: 'edicao', idFuncionario: funcionario.idFuncionario });
  }

  function pedirDesativacao(funcionario: FuncionarioEquipe, gatilho: HTMLElement) {
    refGatilho.current = gatilho;
    setParaDesativar(funcionario);
  }

  function confirmarDesativacao() {
    if (paraDesativar === null) return;
    const funcionario = paraDesativar;
    situacao.mutate(
      { idFuncionario: funcionario.idFuncionario, ativo: false },
      {
        onSuccess: () => {
          aviso.sucesso(MENSAGEM_EQUIPE_SUCESSO_DESATIVADO);
          focarBuscaAoFecharDesativacao.current = true;
          setParaDesativar(null);
        },
        onError: (erro) => {
          setParaDesativar(null);
          aviso.erro(mensagemErroSituacao(erro, funcionario));
        },
      },
    );
  }

  function reativar(funcionario: FuncionarioEquipe, gatilho: HTMLElement) {
    refGatilho.current = gatilho;
    situacao.mutate(
      { idFuncionario: funcionario.idFuncionario, ativo: true },
      {
        onSuccess: () => aviso.sucesso(MENSAGEM_EQUIPE_SUCESSO_REATIVADO),
        onError: (erro) => aviso.erro(mensagemErroSituacao(erro, funcionario)),
      },
    );
  }

  const idEmAndamento = situacao.isPending ? (situacao.variables?.idFuncionario ?? null) : null;
  const filtrado = temFiltroAplicado(filtros);
  const nomeLocadora = usuario?.empresaNomeFantasia;
  const modalAberto = paraDesativar !== null;

  return (
    <div className="flex flex-col gap-6">
      <header className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
        <div className="flex flex-col">
          <div className="mb-1 flex flex-wrap items-center gap-2 text-legenda font-semibold uppercase tracking-wide text-texto-suave">
            {nomeLocadora && (
              <>
                <span>{nomeLocadora}</span>
                <span aria-hidden="true">•</span>
              </>
            )}
            <span className="text-texto">{MENSAGEM_EQUIPE_ADMINISTRACAO}</span>
          </div>
          <h1 className="text-titulo text-navy">{MENSAGEM_EQUIPE_TITULO_PAGINA}</h1>
          <p className="mt-1 text-corpo text-texto-suave">{MENSAGEM_EQUIPE_DESCRICAO}</p>
        </div>
        <div className="flex shrink-0 items-center gap-2">
          <Botao icone="person_add" onClick={(evento) => abrirCadastro(evento.currentTarget)}>
            {MENSAGEM_EQUIPE_BOTAO_ADICIONAR}
          </Botao>
        </div>
      </header>

      <IndicadoresEquipe totais={dados} carregando={consulta.isPending} />

      <FiltrosEquipe
        filtros={filtros}
        aoMudarBusca={(busca) => alterarFiltros({ busca })}
        aoMudarCargo={(cargo) => alterarFiltros({ cargo })}
        aoMudarSituacao={(situacaoEscolhida) => alterarFiltros({ situacao: situacaoEscolhida })}
        refBusca={refBusca}
      />

      <section className="overflow-hidden rounded-cartao bg-branco shadow-cartao">
        {consulta.isError && !dados ? (
          <EstadoErro
            titulo={MENSAGEM_EQUIPE_ERRO_LISTA}
            mensagem={interpretarErroFuncionario(consulta.error, { ehPropriaConta: false }).aviso ?? MENSAGEM_ERRO_API_PADRAO}
            aoTentarNovamente={() => void consulta.refetch()}
            tentando={consulta.isFetching}
          />
        ) : (
          <>
            <TabelaEquipe
              itens={dados?.itens ?? []}
              carregando={consulta.isPending}
              idEmAndamento={idEmAndamento}
              aoEditar={abrirEdicao}
              aoDesativar={pedirDesativacao}
              aoReativar={reativar}
              vazio={
                filtrado ? (
                  <EstadoVazio
                    titulo={MENSAGEM_EQUIPE_VAZIA_COM_FILTRO}
                    descricao={MENSAGEM_EQUIPE_VAZIA_COM_FILTRO_DESCRICAO}
                    acao={
                      <Botao variante="contorno" onClick={limparFiltros}>
                        {MENSAGEM_EQUIPE_LIMPAR_FILTROS}
                      </Botao>
                    }
                  />
                ) : undefined
              }
            />
            {dados && (
              <div className="border-t border-borda">
                <Paginacao
                  pagina={dados.pagina}
                  tamanhoPagina={dados.tamanhoPagina}
                  total={dados.total}
                  aoMudarPagina={setPagina}
                  desabilitada={consulta.isFetching}
                  rotuloItens={MENSAGEM_EQUIPE_RODAPE_ITENS}
                />
              </div>
            )}
          </>
        )}
      </section>

      <Modal
        aberto={modalAberto}
        aoFechar={() => {
          if (!situacao.isPending) setParaDesativar(null);
        }}
        titulo={paraDesativar ? mensagemEquipeConfirmarDesativacaoTitulo(paraDesativar.nome) : ''}
        descricao={MENSAGEM_EQUIPE_CONFIRMAR_DESATIVACAO_TEXTO}
        tamanho="pequeno"
        fecharAoClicarFora={!situacao.isPending}
        rodape={
          <>
            <Botao variante="contorno" onClick={() => setParaDesativar(null)} disabled={situacao.isPending}>
              {MENSAGEM_EQUIPE_CANCELAR}
            </Botao>
            <Botao variante="perigo" onClick={confirmarDesativacao} carregando={situacao.isPending}>
              {MENSAGEM_EQUIPE_CONFIRMAR_DESATIVAR}
            </Botao>
          </>
        }
      >
        {null}
      </Modal>

      {painel !== null && (
        <PainelFuncionario modo={painel} refGatilho={refGatilho} aoFechar={() => setPainel(null)} />
      )}
    </div>
  );
}

/** Mensagem da falha ao desativar ou reativar: conflito de último administrador vira aviso; o resto, a mensagem pronta. */
function mensagemErroSituacao(erro: unknown, funcionario: FuncionarioEquipe): string {
  return interpretarErroFuncionario(erro, { ehPropriaConta: funcionario.ehVoce }).aviso ?? obterMensagemErro(erro);
}
