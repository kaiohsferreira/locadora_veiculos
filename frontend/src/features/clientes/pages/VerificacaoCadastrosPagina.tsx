import { useEffect, useMemo, useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { Botao, EstadoErro, EstadoVazio, Esqueleto, Icone, Paginacao, useAviso } from '@/components/ui';
import { useTituloDocumento } from '@/hooks/useTituloDocumento';
import {
  MENSAGEM_VERIFICACAO_BOTAO_SINCRONIZAR,
  MENSAGEM_VERIFICACAO_DESCRICAO,
  MENSAGEM_VERIFICACAO_LIMPAR_FILTROS,
  MENSAGEM_VERIFICACAO_MIGALHA_LEGENDA,
  MENSAGEM_VERIFICACAO_MIGALHA_MODULO,
  MENSAGEM_VERIFICACAO_MIGALHA_RAIZ,
  MENSAGEM_VERIFICACAO_PAINEL_CARREGANDO,
  MENSAGEM_VERIFICACAO_PAINEL_ERRO_TITULO,
  MENSAGEM_VERIFICACAO_RODAPE_ITENS,
  MENSAGEM_VERIFICACAO_TITULO_PAGINA,
  MENSAGEM_VERIFICACAO_VAZIA_DESCRICAO,
  MENSAGEM_VERIFICACAO_VAZIA_FILTRO_DESCRICAO,
  MENSAGEM_VERIFICACAO_VAZIA_FILTRO_TITULO,
  MENSAGEM_VERIFICACAO_VAZIA_TITULO,
} from '@/lib/mensagens';
import { ATRASO_BUSCA_FILA_MS } from '@/features/clientes/verificacao/regras';
import { criarAgendadorBusca } from '@/features/frota/listagem/agendarBusca';
import {
  alterarListagemFila,
  ESTADO_LISTAGEM_FILA_INICIAL,
  mensagemErroVerificacao,
  mensagemSucessoDecisao,
  montarFiltroFila,
  paginaAposRespostaFila,
  requisicaoDecisao,
  situacaoDoSeletor,
  TAMANHO_PAGINA_FILA,
  temFiltroFila,
  type EstadoListagemFila,
} from '@/features/clientes/verificacao/regras';
import { CartaoPendentesVerificacao } from '@/features/clientes/verificacao/CartaoPendentesVerificacao';
import { DadosVerificacao } from '@/features/clientes/verificacao/DadosVerificacao';
import { DecisaoVerificacao } from '@/features/clientes/verificacao/DecisaoVerificacao';
import { DocumentosVerificacao, type EstadoConsultaDocumentos } from '@/features/clientes/verificacao/DocumentosVerificacao';
import { FiltrosFilaVerificacao } from '@/features/clientes/verificacao/FiltrosFilaVerificacao';
import { PainelVerificacao } from '@/features/clientes/verificacao/PainelVerificacao';
import { TabelaFilaVerificacao } from '@/features/clientes/verificacao/TabelaFilaVerificacao';
import {
  CHAVE_VERIFICACAO,
  useDecidirVerificacao,
  useDocumentosVerificacao,
  useFilaVerificacao,
  useTotalPendentesVerificacao,
  useVerificacaoCliente,
} from '@/features/clientes/hooks/useVerificacaoCadastros';
import type { DecisaoVerificacaoCliente } from '@/types/verificacaoCadastros';

/**
 * P2-FE-04 · Verificação de cadastros (tela 18). Fila dos cadastros pedidos na locadora do funcionário, com o total
 * pendente, busca por nome, CPF ou e-mail e filtro de status. Ao abrir um cadastro, o painel mostra os dados, os
 * documentos (pelo link temporário) e a decisão. Só ATENDENTE e ADMINISTRADOR chegam aqui.
 */
export default function VerificacaoCadastrosPagina() {
  useTituloDocumento(MENSAGEM_VERIFICACAO_TITULO_PAGINA);
  const aviso = useAviso();
  const queryClient = useQueryClient();

  const [estado, setEstado] = useState<EstadoListagemFila>(ESTADO_LISTAGEM_FILA_INICIAL);
  /** Texto digitado na busca: vai para a consulta depois da pausa (ver o agendador abaixo). */
  const [buscaDigitada, setBuscaDigitada] = useState('');
  const [idSelecionado, setIdSelecionado] = useState<number | null>(null);
  const [justificativa, setJustificativa] = useState('');
  const [decisaoEnviada, setDecisaoEnviada] = useState<DecisaoVerificacaoCliente | null>(null);

  const agendador = useMemo(
    () =>
      criarAgendadorBusca(
        (busca) => setEstado((atual) => alterarListagemFila(atual, { tipo: 'busca', busca })),
        ATRASO_BUSCA_FILA_MS,
      ),
    [],
  );
  useEffect(() => () => agendador.cancelar(), [agendador]);

  const fila = useFilaVerificacao(montarFiltroFila(estado));
  const pendentes = useTotalPendentesVerificacao();
  const cliente = useVerificacaoCliente(idSelecionado);
  const documentos = useDocumentosVerificacao(idSelecionado);
  const decisao = useDecidirVerificacao();

  const dados = fila.data;
  // Página além da última (ex.: a última linha saiu do filtro): volta para a última existente (regra em paginaAposRespostaFila).
  const paginaCorrigida = paginaAposRespostaFila(estado.pagina, dados?.total, !fila.isPlaceholderData);
  if (paginaCorrigida !== estado.pagina) {
    setEstado((atual) => alterarListagemFila(atual, { tipo: 'pagina', pagina: paginaCorrigida }));
  }

  function aoMudarBusca(texto: string) {
    setBuscaDigitada(texto);
    agendador.agendar(texto);
  }

  function aoMudarStatus(valor: string) {
    setEstado((atual) => alterarListagemFila(atual, { tipo: 'status', status: situacaoDoSeletor(valor) }));
  }

  function aoMudarPagina(pagina: number) {
    setEstado((atual) => alterarListagemFila(atual, { tipo: 'pagina', pagina }));
  }

  function limparFiltros() {
    agendador.cancelar();
    setBuscaDigitada('');
    setEstado(ESTADO_LISTAGEM_FILA_INICIAL);
  }

  function abrirCadastro(idCliente: number) {
    if (idCliente !== idSelecionado) setJustificativa('');
    setIdSelecionado(idCliente);
  }

  function fecharPainel() {
    setIdSelecionado(null);
    setJustificativa('');
  }

  function sincronizarFila() {
    void queryClient.invalidateQueries({ queryKey: [CHAVE_VERIFICACAO, 'fila'] });
    void queryClient.invalidateQueries({ queryKey: [CHAVE_VERIFICACAO, 'pendentes'] });
    if (idSelecionado !== null) {
      void queryClient.invalidateQueries({ queryKey: [CHAVE_VERIFICACAO, 'detalhe', idSelecionado] });
    }
  }

  function decidir(tipo: DecisaoVerificacaoCliente) {
    if (idSelecionado === null) return;
    setDecisaoEnviada(tipo);
    decisao.mutate(
      { idCliente: idSelecionado, requisicao: requisicaoDecisao(tipo, justificativa) },
      {
        onSuccess: () => {
          aviso.sucesso(mensagemSucessoDecisao(tipo));
          setJustificativa('');
        },
        onError: (erro) => aviso.erro(mensagemErroVerificacao(erro)),
        onSettled: () => setDecisaoEnviada(null),
      },
    );
  }

  const comPainel = idSelecionado !== null;
  const filtrado = temFiltroFila(estado);
  const vazio = filtrado ? (
    <EstadoVazio
      titulo={MENSAGEM_VERIFICACAO_VAZIA_FILTRO_TITULO}
      descricao={MENSAGEM_VERIFICACAO_VAZIA_FILTRO_DESCRICAO}
      acao={
        <Botao variante="contorno" onClick={limparFiltros}>
          {MENSAGEM_VERIFICACAO_LIMPAR_FILTROS}
        </Botao>
      }
    />
  ) : (
    <EstadoVazio titulo={MENSAGEM_VERIFICACAO_VAZIA_TITULO} descricao={MENSAGEM_VERIFICACAO_VAZIA_DESCRICAO} />
  );

  const estadoDocumentos: EstadoConsultaDocumentos =
    documentos.isError
      ? { situacao: 'erro' }
      : documentos.isPending
        ? { situacao: 'carregando' }
        : { situacao: 'ok', documentos: documentos.data ?? [] };

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-2">
        <nav aria-label={MENSAGEM_VERIFICACAO_MIGALHA_LEGENDA} className="flex flex-wrap items-center gap-1 text-corpo-denso text-texto-apoio">
          <span className="text-rotulo">{MENSAGEM_VERIFICACAO_MIGALHA_RAIZ}</span>
          <Icone nome="chevron_right" tamanho={14} />
          <span className="text-rotulo">{MENSAGEM_VERIFICACAO_MIGALHA_MODULO}</span>
          <Icone nome="chevron_right" tamanho={14} />
          <span aria-current="page" className="font-semibold text-frota-oficina">
            {MENSAGEM_VERIFICACAO_TITULO_PAGINA}
          </span>
        </nav>
        <div className="flex flex-col gap-4 md:flex-row md:items-end md:justify-between">
          <div className="flex flex-col">
            <h1 className="text-titulo-documentos text-navy-profundo">{MENSAGEM_VERIFICACAO_TITULO_PAGINA}</h1>
            <p className="mt-1 text-corpo-denso text-texto-apoio">{MENSAGEM_VERIFICACAO_DESCRICAO}</p>
          </div>
          <Botao
            variante="secundario"
            icone="refresh"
            carregando={fila.isFetching}
            onClick={sincronizarFila}
            className="self-start md:self-auto"
          >
            {MENSAGEM_VERIFICACAO_BOTAO_SINCRONIZAR}
          </Botao>
        </div>
      </div>

      <div className="grid grid-cols-1 gap-4 md:grid-cols-4">
        <CartaoPendentesVerificacao total={pendentes.data?.total ?? null} carregando={pendentes.isPending} />
      </div>

      <div className="grid grid-cols-1 items-start gap-6 xl:grid-cols-12">
        <div className={`flex min-w-0 flex-col gap-4 ${comPainel ? 'xl:col-span-7' : 'xl:col-span-12'}`}>
          <FiltrosFilaVerificacao
            busca={buscaDigitada}
            aoMudarBusca={aoMudarBusca}
            status={estado.status}
            aoMudarStatus={aoMudarStatus}
          />

          <section className="flex flex-col overflow-hidden rounded-cartao bg-branco shadow-cartao">
            {fila.isError ? (
              <EstadoErro mensagem={mensagemErroVerificacao(fila.error)} aoTentarNovamente={() => void fila.refetch()} />
            ) : (
              <>
                <TabelaFilaVerificacao
                  itens={dados?.itens ?? []}
                  carregando={fila.isPending}
                  vazio={vazio}
                  idSelecionado={idSelecionado}
                  aoAbrir={abrirCadastro}
                  comPainel={comPainel}
                />
                {dados !== undefined && (
                  <Paginacao
                    variante="frota"
                    pagina={estado.pagina}
                    tamanhoPagina={TAMANHO_PAGINA_FILA}
                    total={dados.total}
                    aoMudarPagina={aoMudarPagina}
                    desabilitada={fila.isFetching}
                    rotuloItens={MENSAGEM_VERIFICACAO_RODAPE_ITENS}
                  />
                )}
              </>
            )}
          </section>
        </div>

        {comPainel && (
          <div className="min-w-0 xl:col-span-5">
            <PainelVerificacao nomeCliente={cliente.data?.nome ?? null} aoFechar={fecharPainel}>
              {cliente.isPending && (
                <output aria-label={MENSAGEM_VERIFICACAO_PAINEL_CARREGANDO} className="flex flex-col gap-3">
                  <Esqueleto className="h-40 w-full" />
                  <Esqueleto className="h-24 w-full" />
                </output>
              )}

              {cliente.isError && (
                <EstadoErro
                  titulo={MENSAGEM_VERIFICACAO_PAINEL_ERRO_TITULO}
                  mensagem={mensagemErroVerificacao(cliente.error)}
                  aoTentarNovamente={() => void cliente.refetch()}
                />
              )}

              {cliente.data !== undefined && (
                <div className="flex flex-col gap-6">
                  <DadosVerificacao cliente={cliente.data} />
                  <DocumentosVerificacao
                    estado={estadoDocumentos}
                    aoTentarNovamente={() => void documentos.refetch()}
                  />
                  <DecisaoVerificacao
                    status={cliente.data.status}
                    justificativa={justificativa}
                    aoMudarJustificativa={setJustificativa}
                    enviando={decisao.isPending}
                    decisaoEnviada={decisaoEnviada}
                    aoAprovar={() => decidir('APROVADO')}
                    aoRecusar={() => decidir('RECUSADO')}
                  />
                </div>
              )}
            </PainelVerificacao>
          </div>
        )}
      </div>
    </div>
  );
}
