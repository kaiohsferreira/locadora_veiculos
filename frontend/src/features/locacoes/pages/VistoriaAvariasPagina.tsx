import { useState } from 'react';
import { generatePath, useParams } from 'react-router-dom';
import { Botao, BotaoLink, EstadoErro, Icone, useAviso } from '@/components/ui';
import { CarregandoPagina } from '@/components/CarregandoPagina';
import { useTituloDocumento } from '@/hooks/useTituloDocumento';
import { useAutenticacao } from '@/routes/autenticacao/useAutenticacao';
import { obterMensagemErro } from '@/lib/erros';
import { formatarMoeda, formatarPlaca } from '@/lib/formatters';
import { MENSAGENS_VISTORIA as M } from '@/lib/mensagensVistoria';
import { MENSAGENS_RETIRADA } from '@/lib/mensagensRetirada';
import { MENSAGEM_FOTOS_ENVIADA } from '@/lib/mensagensFotosVeiculo';
import { ErroApi } from '@/types/api';
import {
  MENSAGEM_AVARIA_ADICIONAR,
  MENSAGEM_AVARIA_CARREGANDO,
  MENSAGEM_AVARIA_ENVIO_PARCIAL,
  MENSAGEM_AVARIA_ERRO_LISTA,
  MENSAGEM_AVARIA_FOTO_PENDENTE,
  MENSAGEM_AVARIA_TOTAL,
  MENSAGEM_VISTORIA_AVANCAR,
  MENSAGEM_VISTORIA_LOCACAO_NUMERO,
  MENSAGEM_VISTORIA_LOCACAO_ROTULO,
  MENSAGEM_VISTORIA_MIGALHA,
  MENSAGEM_VISTORIA_ODOMETRO_TITULO,
  MENSAGEM_VISTORIA_PLACA,
  MENSAGEM_VISTORIA_REGISTRAR,
  MENSAGEM_VISTORIA_STATUS_ATIVA,
  MENSAGEM_VISTORIA_TITULO,
  MENSAGEM_VISTORIA_VISTORIADOR,
  MENSAGEM_VISTORIA_VOLTAR,
  MENSAGEM_VISTORIA_VOLTAR_LOCACOES,
  mensagemAvariasMapeadas,
  mensagemProgressoAvarias,
} from '@/lib/mensagensVistoriaAvarias';
import { CAMINHOS } from '@/routes/caminhos';
import { useVistoriaEntrada } from '../hooks/useVistoriaEntrada';
import { lerIdLocacao } from '../vistoria/validacaoVistoria';
import type { CamposAvariaNormalizados } from '../vistoria/validacaoAvaria';
import { numerarAvarias, temMarcadorParaRedefinir, totalEstimado, type LinhaAvaria } from '../vistoria/avariasVistoria';
import { DiagramaAvarias, type MarcacaoDiagrama, type MarcadorDiagrama } from '../vistoria/DiagramaAvarias';
import { FormularioAvaria } from '../vistoria/FormularioAvaria';
import { GradeFotosAvarias, ListaAvarias } from '../vistoria/ListaAvarias';
import { modeloLinhaAvaria } from '../vistoria/modeloLinhaAvaria';
import { TrilhaVistoria } from '../vistoria/TrilhaVistoria';
import { FormularioOdometro, LeituraOdometro } from '../vistoria/CartaoOdometroCombustivel';
import { useAvariasVistoria, useEnvioAvarias, useVeiculoDaVistoria } from '../vistoria/useAvariasVistoria';

/** Id do formulário de odômetro: o botão de registrar fica no rodapé fixo, fora do cartão. */
const ID_FORMULARIO_ODOMETRO = 'formulario-entrada-vistoria';

/** Número, nome e valor de cada linha no formato do diagrama (posição vem do servidor ou do clique, por linha). */
function marcadorDaLinha(linha: LinhaAvaria): MarcadorDiagrama {
  const modelo = modeloLinhaAvaria(linha);
  const posX = linha.local !== null ? (linha.local.dados.posX === null ? null : Number(linha.local.dados.posX)) : (linha.avaria?.posX ?? null);
  const posY = linha.local !== null ? (linha.local.dados.posY === null ? null : Number(linha.local.dados.posY)) : (linha.avaria?.posY ?? null);
  return { numero: linha.numero, angulo: modelo.angulo, posX, posY, descricao: modelo.descricao, gravidade: modelo.gravidade };
}

/**
 * Vistoria de entrada com avarias (tela 19, P4-FE-02). Sem vistoria, a tela monta o mapa, a lista e o formulário de
 * odômetro; ao registrar, a vistoria é gravada e depois cada avaria com a sua foto. Com a vistoria registrada, a tela
 * vira leitura e mostra as avarias pela rota de listagem.
 */
export default function VistoriaAvariasPagina() {
  const { locacaoId } = useParams<'locacaoId'>();
  const id = lerIdLocacao(locacaoId);
  const consulta = useVistoriaEntrada(id);
  const usuario = useAutenticacao().usuario;
  const aviso = useAviso();
  useTituloDocumento(M.titulo);

  const contexto = consulta.data;
  const vistoria = contexto?.vistoria ?? null;
  const idVistoria = vistoria?.idVistoria ?? null;
  // Sem entrada registrada, o vistoriador é quem está logado; com a entrada, é quem a registrou (operadorStr). Sem nome, a caixa não aparece.
  const nomeVistoriador: string | null = vistoria !== null ? (vistoria.operadorStr ?? null) : (usuario?.nome ?? null);
  const avarias = useAvariasVistoria(idVistoria);
  const veiculo = useVeiculoDaVistoria(contexto?.locacao.idVeiculo ?? null);
  const envio = useEnvioAvarias();
  const [marcacao, setMarcacao] = useState<MarcacaoDiagrama | null>(null);
  const [formularioAberto, setFormularioAberto] = useState(false);
  // Enquanto a vistoria é gravada, o mapa e a adição de avaria ficam travados (nada entra no meio do registro).
  const [registrando, setRegistrando] = useState(false);

  // Erro na lista da vistoria registrada: com lista em cache, mostra a lista e o erro; sem nada carregado, só o erro.
  const erroNaLista = vistoria !== null && avarias.isError;
  const listaSemDados = erroNaLista && avarias.data === undefined;
  const linhas = numerarAvarias(avarias.data ?? [], envio.locais);
  const marcadores = linhas.map(marcadorDaLinha);
  const modelos = linhas.map(modeloLinhaAvaria);
  const total = totalEstimado(modelos.map((modelo) => modelo.valorEstimado));

  function abrirFormulario(nova: MarcacaoDiagrama | null): void {
    setMarcacao(nova === null ? null : { ...nova });
    setFormularioAberto(true);
  }

  function aoAdicionarAvaria(campos: CamposAvariaNormalizados, foto: File): void {
    envio.adicionar({ ...campos, foto });
    setFormularioAberto(false);
    setMarcacao(null);
  }

  /** Chamado pelo formulário depois de a vistoria ser gravada: envia cada avaria local com a sua foto. */
  async function aoRegistrarVistoria(idRegistrado: number): Promise<void> {
    await envio.enviarPendentes(idRegistrado);
  }

  /**
   * Registro recusado com 409: relê a entrada. Se ela já existe, as avarias locais que não foram enviadas viram erro, com
   * "Tentar de novo" e o aviso de envio parcial. Nada é enviado sem o clique do usuário.
   */
  async function aoRegistroRecusado(): Promise<void> {
    const atual = await consulta.refetch();
    if (atual.data?.vistoria) envio.marcarNaoEnviadas();
  }

  async function aoTentarDeNovo(chave: number): Promise<void> {
    if (idVistoria === null) return;
    await envio.tentarDeNovo(idVistoria, chave);
  }

  async function aoEnviarFotoGravada(idAvaria: number, arquivo: File): Promise<void> {
    if (idVistoria === null) return;
    try {
      await envio.substituirFotoGravada(idVistoria, idAvaria, arquivo);
      aviso.sucesso(MENSAGEM_FOTOS_ENVIADA);
    } catch (erro: unknown) {
      // 502 do PUT: a avaria continua gravada e falta a foto. O texto aprovado diz isso; o do servidor não aparece.
      const falhaDaFoto = erro instanceof ErroApi && erro.status === 502;
      aviso.erro(falhaDaFoto ? MENSAGEM_AVARIA_FOTO_PENDENTE : obterMensagemErro(erro));
    }
  }

  return (
    <div className="mx-auto flex w-full max-w-7xl flex-col gap-6 px-4 py-8 sm:px-6">
      <BotaoLink to={CAMINHOS.locacoes} variante="fantasma" icone="arrow_back">{MENSAGEM_VISTORIA_VOLTAR_LOCACOES}</BotaoLink>

      <header className="flex flex-col gap-4 rounded-xl bg-branco p-6 shadow-sm md:flex-row md:items-end md:justify-between">
        <div className="flex flex-col gap-2">
          <nav aria-label={MENSAGENS_RETIRADA.trilhaRotulo} className="flex items-center gap-1 text-micro uppercase text-texto-apoio">
            <span>{MENSAGENS_RETIRADA.trilhaLocacoes}</span>
            <span aria-hidden="true">/</span>
            <span>{MENSAGENS_RETIRADA.trilhaDespacho}</span>
            <span aria-hidden="true">/</span>
            <span className="font-semibold text-frota-oficina">{MENSAGEM_VISTORIA_MIGALHA}</span>
          </nav>
          <div className="flex flex-wrap items-baseline gap-x-4 gap-y-1">
            <h1 className="text-titulo-acesso font-bold text-texto-desenho">{MENSAGEM_VISTORIA_TITULO}</h1>
            <span className="inline-flex items-center gap-1 rounded bg-steel-fundo px-2 py-1">
              <span aria-hidden="true" className="h-2 w-2 rounded-full bg-orange-acao motion-safe:animate-pulse" />
              <span className="text-dado-tabular font-semibold text-texto-desenho">{MENSAGEM_VISTORIA_STATUS_ATIVA}</span>
            </span>
          </div>
          {contexto && (
            <div className="flex flex-wrap items-center gap-3 text-texto-desenho">
              {veiculo.data && (
                <>
                  <span className="flex items-center gap-1">
                    <Icone nome="directions_car" tamanho={18} />
                    <span className="text-corpo-denso font-bold">{veiculo.data.marca} {veiculo.data.modelo}</span>
                  </span>
                  <span aria-hidden="true" className="text-contorno-suave">•</span>
                  <span className="flex items-center gap-1 rounded bg-steel-suave px-2 py-0.5">
                    <span className="text-micro font-semibold uppercase text-texto-apoio">{MENSAGEM_VISTORIA_PLACA}</span>
                    <span className="text-dado-tabular font-bold tracking-wider text-navy-profundo">{formatarPlaca(veiculo.data.placa)}</span>
                  </span>
                  <span aria-hidden="true" className="text-contorno-suave">•</span>
                </>
              )}
              <span className="flex items-center gap-1 text-texto-apoio">
                <span className="text-micro uppercase">{MENSAGEM_VISTORIA_LOCACAO_ROTULO}</span>
                <span className="text-dado-tabular font-semibold text-texto-desenho">{MENSAGEM_VISTORIA_LOCACAO_NUMERO(contexto.locacao.idLocacao)}</span>
              </span>
            </div>
          )}
        </div>
        {nomeVistoriador !== null && (
          <div className="flex items-center gap-3 rounded-lg bg-steel-fundo px-4 py-3 self-start md:self-auto">
            <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-orange-acao/15">
              <Icone nome="fact_check" tamanho={20} />
            </span>
            <div className="flex flex-col">
              <span className="text-micro text-texto-apoio">{MENSAGEM_VISTORIA_VISTORIADOR}</span>
              <span className="text-corpo-denso font-semibold text-texto-desenho">{nomeVistoriador}</span>
            </div>
          </div>
        )}
      </header>

      <TrilhaVistoria vistoriaRegistrada={vistoria !== null} />

      {id === null ? <EstadoErro mensagem={M.idInvalido} />
        : consulta.isPending ? <CarregandoPagina />
        : consulta.isError ? <EstadoErro mensagem={obterMensagemErro(consulta.error)} aoTentarNovamente={() => { void consulta.refetch(); }} tentando={consulta.isFetching} />
        : contexto && (
          <>
            <div className="grid grid-cols-1 items-start gap-6 lg:grid-cols-12">
              <div className="flex flex-col gap-6 lg:col-span-7">
                <DiagramaAvarias
                  avarias={marcadores}
                  somenteLeitura={vistoria !== null || registrando}
                  aoMarcar={(nova) => abrirFormulario(nova)}
                  aoRedefinir={envio.redefinir}
                  podeRedefinir={temMarcadorParaRedefinir(envio.locais)}
                />
              </div>

              <div className="flex flex-col gap-6 lg:col-span-5">
                <section aria-labelledby="titulo-avarias-mapeadas" className="flex flex-col gap-4 rounded-xl bg-branco p-6 shadow-sm">
                  <div className="flex items-center justify-between gap-2">
                    <h2 id="titulo-avarias-mapeadas" className="flex items-center gap-2 text-valor-destaque font-bold text-texto-desenho">
                      <Icone nome="warning" tamanho={22} className="text-orange-acao" />
                      {mensagemAvariasMapeadas(linhas.length)}
                    </h2>
                    {vistoria === null && (
                      <Botao type="button" tamanho="pequeno" icone="add" disabled={registrando} onClick={() => abrirFormulario(null)}>
                        {MENSAGEM_AVARIA_ADICIONAR}
                      </Botao>
                    )}
                  </div>
                  <div className="flex flex-col gap-4">
                    {vistoria === null && formularioAberto && (
                      <FormularioAvaria
                        marcacao={marcacao}
                        aoAdicionar={aoAdicionarAvaria}
                        aoCancelar={() => {
                          setFormularioAberto(false);
                          setMarcacao(null);
                        }}
                      />
                    )}
                    {vistoria !== null && avarias.isPending && <p className="text-corpo-denso text-texto-apoio">{MENSAGEM_AVARIA_CARREGANDO}</p>}
                    {erroNaLista && (
                      <EstadoErro mensagem={MENSAGEM_AVARIA_ERRO_LISTA} aoTentarNovamente={() => { void avarias.refetch(); }} tentando={avarias.isFetching} />
                    )}
                    {!listaSemDados && (
                      <ListaAvarias linhas={linhas} somenteLeitura={vistoria !== null} enviando={envio.enviando} aoTentarDeNovo={(chave) => { void aoTentarDeNovo(chave); }} />
                    )}
                    {envio.progresso !== null && envio.progresso.total > 0 && (
                      <output className="block text-apoio text-texto-apoio">
                        {mensagemProgressoAvarias(envio.progresso.enviadas, envio.progresso.total)}
                      </output>
                    )}
                    {envio.falhou && <p role="alert" className="text-apoio text-status-perigo">{MENSAGEM_AVARIA_ENVIO_PARCIAL}</p>}
                    <div className="flex items-center justify-between rounded-lg bg-steel-suave p-3">
                      <span className="text-corpo-denso font-medium text-texto-desenho">{MENSAGEM_AVARIA_TOTAL}</span>
                      <span className="text-valor-destaque font-bold text-frota-oficina">{formatarMoeda(total)}</span>
                    </div>
                  </div>
                </section>

                <section aria-labelledby="titulo-odometro-vistoria" className="flex flex-col gap-4 rounded-xl bg-branco p-6 shadow-sm">
                  <h3 id="titulo-odometro-vistoria" className="flex items-center gap-2 text-titulo-md font-bold text-texto-desenho">
                    <Icone nome="speed" tamanho={20} className="text-frota-oficina" />
                    {MENSAGEM_VISTORIA_ODOMETRO_TITULO}
                  </h3>
                  {vistoria ? (
                    <LeituraOdometro vistoria={vistoria} kmSaida={contexto.locacao.kmSaida} />
                  ) : (
                    <FormularioOdometro
                      idLocacao={id}
                      kmSaida={contexto.locacao.kmSaida}
                      formId={ID_FORMULARIO_ODOMETRO}
                      aoRegistrada={aoRegistrarVistoria}
                      aoRegistroRecusado={aoRegistroRecusado}
                      aoRegistrando={setRegistrando}
                      atualizando={consulta.isFetching}
                    />
                  )}
                </section>

                <GradeFotosAvarias
                  linhas={linhas}
                  enviando={envio.enviando}
                  aoEnviarFotoGravada={(idAvaria, arquivo) => { void aoEnviarFotoGravada(idAvaria, arquivo); }}
                  aoRecarregarLista={() => { void avarias.refetch(); }}
                />
              </div>
            </div>

            <footer className="sticky bottom-4 z-30 flex flex-col items-center justify-between gap-4 rounded-xl bg-branco/95 p-4 shadow-lg backdrop-blur-md sm:flex-row">
              <BotaoLink to={generatePath(CAMINHOS.retiradaVeiculo, { reservaId: String(contexto.locacao.idReserva) })} variante="contorno" icone="arrow_back">
                {MENSAGEM_VISTORIA_VOLTAR}
              </BotaoLink>
              {vistoria === null ? (
                <Botao
                  type="submit"
                  form={ID_FORMULARIO_ODOMETRO}
                  carregando={consulta.isFetching || envio.enviando || registrando}
                  disabled={consulta.isFetching || envio.enviando || registrando}
                >
                  {MENSAGEM_VISTORIA_REGISTRAR}
                </Botao>
              ) : (
                // Avança também com avaria local não enviada: o aviso de envio parcial fica no cartão das avarias.
                <BotaoLink to={generatePath(CAMINHOS.contratoLocacao, { locacaoId: String(id) })} iconeDireita="arrow_forward">
                  {MENSAGEM_VISTORIA_AVANCAR}
                </BotaoLink>
              )}
            </footer>
          </>
        )}
    </div>
  );
}
