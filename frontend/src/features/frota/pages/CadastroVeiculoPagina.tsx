import { useEffect, useId, useMemo, useRef, useState, type ReactNode } from 'react';
import { useForm, useWatch, type FieldError, type FieldErrors } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useQueryClient } from '@tanstack/react-query';
import { useLocation, useNavigate, useParams } from 'react-router-dom';
import { Botao, EstadoErro, useAviso, useAvisosAcimaDaBarra } from '@/components/ui';
import { useTituloDocumento } from '@/hooks/useTituloDocumento';
import { BarraAcoesVeiculo } from '@/features/frota/componentes/BarraAcoesVeiculo';
import { BlocoFotosVeiculo } from '@/features/frota/componentes/BlocoFotosVeiculo';
import { CabecalhoCadastroVeiculo } from '@/features/frota/componentes/CabecalhoCadastroVeiculo';
import { CamposAquisicaoVeiculo } from '@/features/frota/componentes/CamposAquisicaoVeiculo';
import { CamposComerciaisVeiculo } from '@/features/frota/componentes/CamposComerciaisVeiculo';
import { CamposIdentificacaoVeiculo } from '@/features/frota/componentes/CamposIdentificacaoVeiculo';
import { chaveVeiculo } from '@/features/frota/hooks/chavesVeiculos';
import { useCadastrarVeiculo } from '@/features/frota/hooks/useCadastrarVeiculo';
import { useCategorias } from '@/features/frota/hooks/useCategorias';
import { useEditarVeiculo } from '@/features/frota/hooks/useEditarVeiculo';
import { useRascunhoVeiculo } from '@/features/frota/hooks/useRascunhoVeiculo';
import { useVeiculo } from '@/features/frota/hooks/useVeiculo';
import {
  avisoRascunhoVisivel,
  caminhoEdicaoAposLote,
  destinoCancelamentoVeiculo,
  identificarRotaVeiculo,
  origemDaTelaVeiculo,
  podeEnviarCadastro,
} from '@/features/frota/formulario/fluxoCadastroVeiculo';
import { montarRequisicaoVeiculo, valoresIniciaisCadastro, valoresIniciaisEdicao } from '@/lib/conversaoVeiculo';
import {
  CAMPOS_FORMULARIO_VEICULO,
  criarEsquemaVeiculo,
  type CampoFormularioVeiculo,
  type ValoresFormularioVeiculo,
} from '@/lib/esquemaVeiculo';
import { obterMensagemErro } from '@/lib/erros';
import { MENSAGEM_CARREGANDO } from '@/lib/mensagens';
import {
  interpretarErroVeiculo,
  primeiroCampoVeiculoComErro,
  type ErrosCamposVeiculo,
} from '@/lib/mapeamentoErroVeiculo';
import { opcoesCategoriaVeiculo, situacaoCategoriasVeiculo } from '@/lib/listasVeiculo';
import {
  MENSAGEM_VEICULO_CATEGORIAS_CARREGANDO,
  MENSAGEM_VEICULO_CATEGORIAS_ERRO,
  MENSAGEM_VEICULO_CATEGORIAS_VAZIO,
  MENSAGEM_VEICULO_EDICAO_CONCLUIDA,
  MENSAGEM_VEICULO_FOTOS_COM_FALHA,
  MENSAGEM_VEICULO_NAO_ENCONTRADO,
  MENSAGEM_VEICULO_NAO_ENCONTRADO_DESCRICAO,
  MENSAGEM_VEICULO_RASCUNHO_DESCARTAR,
  MENSAGEM_VEICULO_RASCUNHO_FALHOU,
  MENSAGEM_VEICULO_RASCUNHO_RECUPERADO,
  MENSAGEM_VEICULO_RASCUNHO_SALVO,
  MENSAGEM_VEICULO_TENTAR_DE_NOVO,
  MENSAGEM_VEICULO_TITULO_CADASTRAR,
  MENSAGEM_VEICULO_TITULO_EDITAR,
} from '@/lib/mensagensVeiculos';
import { CAMINHOS } from '@/routes/caminhos';
import type { VeiculoRetorno } from '@/types/veiculos';

/** Erros de validação do formulário (Zod), no formato que a tela aplica nos campos. */
function errosDoFormulario(errors: FieldErrors<ValoresFormularioVeiculo>): ErrosCamposVeiculo {
  const mapa: ErrosCamposVeiculo = {};
  for (const campo of CAMPOS_FORMULARIO_VEICULO) {
    const erro: FieldError | undefined = errors[campo];
    if (typeof erro?.message === 'string') mapa[campo] = erro.message;
  }
  return mapa;
}

/** Leva o foco ao campo. Cor é um grupo de rádio: foca a primeira amostra. */
function focarCampo(formulario: HTMLFormElement | null, campo: CampoFormularioVeiculo | null): void {
  if (formulario === null || campo === null) return;
  const seletor = campo === 'cor' ? 'input[type="radio"]' : `[name="${campo}"]`;
  formulario.querySelector<HTMLElement>(seletor)?.focus();
}

/** Veículo com id que não é número válido, ou que não existe para esta locadora: sem formulário e sem texto de apoio. */
function VeiculoNaoEncontrado() {
  return (
    <>
      <CabecalhoCadastroVeiculo modo="edicao" caminhoFrota={CAMINHOS.frota} mostrarApoio={false} />
      <EstadoErro titulo={MENSAGEM_VEICULO_NAO_ENCONTRADO} mensagem={MENSAGEM_VEICULO_NAO_ENCONTRADO_DESCRICAO} />
    </>
  );
}

interface TelaVeiculoProps {
  /** Id do veículo na edição; nulo no cadastro. */
  idVeiculo: number | null;
  /** Veículo carregado (só na edição). Os valores do formulário saem dele na montagem; depois a consulta não mexe no digitado. */
  veiculo: VeiculoRetorno | null;
  /** Se a pessoa chegou a esta tela navegando dentro do sistema (decide o "Cancelar / Voltar"). */
  veioDoSistema: boolean;
}

/**
 * Tela 05 (P1-FE-03): cadastro (sem id) e edição (com id) do veículo. O formulário parte vazio ou do rascunho deste
 * navegador (cadastro) ou dos valores do veículo carregado (edição). Depois do cadastro, a pessoa fica na tela enviando
 * as fotos; a URL só passa para a edição quando nenhuma foto está pendente (lote sem falha, ou as com erro reenviadas
 * ou removidas).
 */
function TelaVeiculo({ idVeiculo, veiculo, veioDoSistema }: TelaVeiculoProps) {
  const edicao = idVeiculo !== null;
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const aviso = useAviso();
  // Esta tela tem a barra de ações fixa embaixo: os avisos sobem para ficar acima dela (só nesta tela).
  useAvisosAcimaDaBarra();
  const formId = useId();
  const refFormulario = useRef<HTMLFormElement>(null);
  const esquema = useMemo(() => criarEsquemaVeiculo(), []);
  const rascunho = useRascunhoVeiculo(!edicao);
  const [rascunhoAberto, setRascunhoAberto] = useState(rascunho.rascunhoInicial !== null);

  const form = useForm<ValoresFormularioVeiculo>({
    defaultValues:
      veiculo !== null ? valoresIniciaisEdicao(veiculo) : (rascunho.rascunhoInicial ?? valoresIniciaisCadastro()),
    resolver: zodResolver(esquema),
  });
  const { control, handleSubmit, setValue, getValues, reset, setError, formState } = form;
  const valores = useWatch({ control }) as ValoresFormularioVeiculo;
  const errosValidacao = errosDoFormulario(formState.errors);

  const [idCriado, setIdCriado] = useState<number | null>(null);
  const [versaoErroServidor, setVersaoErroServidor] = useState(0);
  const aguardandoLote = useRef(false);
  const enviandoRef = useRef(false);
  const focoPendente = useRef<CampoFormularioVeiculo | null>(null);

  const categorias = useCategorias();
  const cadastro = useCadastrarVeiculo();
  const edicaoVeiculo = useEditarVeiculo();
  const enviando = cadastro.isPending || edicaoVeiculo.isPending;
  const bloqueado = enviando || (!edicao && idCriado !== null);

  // Erro do servidor: o foco vai ao primeiro campo com erro, depois que o formulário volta a ficar habilitado.
  useEffect(() => {
    if (bloqueado) return;
    const campo = focoPendente.current;
    if (campo === null) return;
    focoPendente.current = null;
    focarCampo(refFormulario.current, campo);
  }, [bloqueado, versaoErroServidor]);

  function aplicarErroServidor(erro: unknown) {
    const resultado = interpretarErroVeiculo(erro);
    for (const campo of CAMPOS_FORMULARIO_VEICULO) {
      const mensagem = resultado.campos[campo];
      if (mensagem !== undefined) setError(campo, { type: 'server', message: mensagem });
    }
    if (resultado.aviso !== null) aviso.erro(resultado.aviso);
    const primeiro = primeiroCampoVeiculoComErro(resultado.campos);
    if (primeiro !== null) {
      focoPendente.current = primeiro;
      setVersaoErroServidor((versao) => versao + 1);
    }
  }

  function aoEnviar(dados: ValoresFormularioVeiculo) {
    if (!podeEnviarCadastro({ enviando: enviandoRef.current, criado: !edicao && idCriado !== null })) return;
    enviandoRef.current = true;
    if (idVeiculo !== null) {
      edicaoVeiculo.mutate(
        { idVeiculo, requisicao: montarRequisicaoVeiculo(dados) },
        {
          onSuccess: () => aviso.sucesso(MENSAGEM_VEICULO_EDICAO_CONCLUIDA),
          onError: aplicarErroServidor,
          onSettled: () => {
            enviandoRef.current = false;
          },
        },
      );
      return;
    }
    cadastro.mutate(montarRequisicaoVeiculo(dados), {
      onSuccess: (criado) => {
        aguardandoLote.current = true;
        queryClient.setQueryData(chaveVeiculo(criado.idVeiculo), criado);
        rascunho.apagar();
        setIdCriado(criado.idVeiculo);
      },
      onError: aplicarErroServidor,
      onSettled: () => {
        enviandoRef.current = false;
      },
    });
  }

  function aoInvalido(errosDoEnvio: FieldErrors<ValoresFormularioVeiculo>) {
    focarCampo(refFormulario.current, primeiroCampoVeiculoComErro(errosDoFormulario(errosDoEnvio)));
  }

  /**
   * Fim de um lote de fotos do cadastro. Com falha, fica na tela (o aviso pede "Tentar de novo"). Sem falha, segue
   * para a edição do veículo criado. A fila chama de novo quando as fotos com erro são removidas.
   */
  function tratarFimDoLote(temFalha: boolean) {
    if (!aguardandoLote.current) return;
    if (temFalha) {
      aviso.erro(MENSAGEM_VEICULO_FOTOS_COM_FALHA);
      return;
    }
    aguardandoLote.current = false;
    const caminho = caminhoEdicaoAposLote(idCriado, temFalha);
    if (caminho !== null) navigate(caminho, { replace: true, state: { veioDoSistema } });
  }

  function aoMudar(campo: CampoFormularioVeiculo, valor: string) {
    setValue(campo, valor, { shouldValidate: formState.isSubmitted });
  }

  function salvarRascunho() {
    if (rascunho.salvar(getValues())) aviso.sucesso(MENSAGEM_VEICULO_RASCUNHO_SALVO);
    else aviso.erro(MENSAGEM_VEICULO_RASCUNHO_FALHOU);
  }

  function descartarRascunho() {
    // Depois da criação o formulário é do veículo criado: o rascunho não pode limpá-lo.
    if (idCriado !== null) return;
    rascunho.apagar();
    reset(valoresIniciaisCadastro());
    setRascunhoAberto(false);
  }

  function cancelar() {
    if (destinoCancelamentoVeiculo(veioDoSistema) === 'voltar') navigate(-1);
    else navigate(CAMINHOS.frota);
  }

  const categoriaAtual = veiculo !== null ? { idCategoria: veiculo.idCategoria, nome: veiculo.categoriaNome } : null;
  const opcoesCategoria = opcoesCategoriaVeiculo(categorias.data?.itens ?? [], categoriaAtual);
  const situacaoCategorias = situacaoCategoriasVeiculo({
    carregando: categorias.isPending,
    erro: categorias.isError,
    quantidadeOpcoes: opcoesCategoria.length,
  });

  let apoioCategoria: ReactNode = null;
  if (situacaoCategorias === 'carregando') {
    apoioCategoria = (
      <output className="block text-micro text-texto-apoio">{MENSAGEM_VEICULO_CATEGORIAS_CARREGANDO}</output>
    );
  } else if (situacaoCategorias === 'erro') {
    apoioCategoria = (
      <div role="alert" className="flex flex-wrap items-center gap-2 text-micro text-status-perigo">
        <span>{MENSAGEM_VEICULO_CATEGORIAS_ERRO}</span>
        <Botao variante="contorno" tamanho="pequeno" onClick={() => void categorias.refetch()}>
          {MENSAGEM_VEICULO_TENTAR_DE_NOVO}
        </Botao>
      </div>
    );
  } else if (situacaoCategorias === 'vazio') {
    apoioCategoria = <p className="text-micro text-texto-apoio">{MENSAGEM_VEICULO_CATEGORIAS_VAZIO}</p>;
  }

  const avisoRascunho = avisoRascunhoVisivel({ edicao, rascunhoAberto, criado: idCriado !== null });
  const descricaoVeiculo = [valores.marca, valores.modelo].map((texto) => texto.trim()).filter(Boolean).join(' ');
  const idBloco = edicao ? idVeiculo : idCriado;
  const jaCriado = !edicao && idCriado !== null;

  return (
    <>
      <CabecalhoCadastroVeiculo modo={edicao ? 'edicao' : 'cadastro'} caminhoFrota={CAMINHOS.frota} />

      {avisoRascunho && (
        <output className="mb-6 flex flex-wrap items-center justify-between gap-4 rounded-cartao bg-steel-fundo p-4">
          <span className="text-corpo-denso text-texto-apoio">{MENSAGEM_VEICULO_RASCUNHO_RECUPERADO}</span>
          <Botao variante="contorno" tamanho="pequeno" onClick={descartarRascunho}>
            {MENSAGEM_VEICULO_RASCUNHO_DESCARTAR}
          </Botao>
        </output>
      )}

      <div className="grid grid-cols-1 gap-8 pb-8 lg:grid-cols-12">
        <form
          id={formId}
          ref={refFormulario}
          noValidate
          onSubmit={(evento) => void handleSubmit(aoEnviar, aoInvalido)(evento)}
          className="flex min-w-0 flex-col gap-6 lg:col-span-8"
        >
          <fieldset disabled={bloqueado} className="contents">
            <CamposIdentificacaoVeiculo valores={valores} erros={errosValidacao} aoMudar={aoMudar} />
            <CamposComerciaisVeiculo
              valores={valores}
              erros={errosValidacao}
              aoMudar={aoMudar}
              opcoesCategoria={opcoesCategoria}
              apoioCategoria={apoioCategoria}
              categoriaIndisponivel={situacaoCategorias !== 'pronto'}
            />
            <CamposAquisicaoVeiculo valores={valores} erros={errosValidacao} aoMudar={aoMudar} />
          </fieldset>
        </form>

        <div className="flex min-w-0 flex-col gap-6 lg:col-span-4">
          <BlocoFotosVeiculo
            idVeiculo={idBloco}
            descricaoVeiculo={descricaoVeiculo || undefined}
            aoFimDoLote={edicao ? undefined : tratarFimDoLote}
          />
        </div>
      </div>

      <BarraAcoesVeiculo
        modo={edicao || jaCriado ? 'edicao' : 'cadastro'}
        formId={formId}
        enviando={enviando}
        concluido={jaCriado}
        aoCancelar={cancelar}
        aoSalvarRascunho={edicao ? undefined : salvarRascunho}
      />
    </>
  );
}

interface EdicaoVeiculoPaginaProps {
  idVeiculo: number;
  veioDoSistema: boolean;
}

/**
 * Edição: carrega o veículo e só então monta o formulário com ele. Enquanto carrega, ou quando a consulta falha antes
 * de qualquer dado, mostra o estado da tela no lugar do formulário.
 */
function EdicaoVeiculoPagina({ idVeiculo, veioDoSistema }: EdicaoVeiculoPaginaProps) {
  const consulta = useVeiculo(idVeiculo);
  const veiculo = consulta.data;

  if (veiculo !== undefined) {
    return <TelaVeiculo idVeiculo={idVeiculo} veiculo={veiculo} veioDoSistema={veioDoSistema} />;
  }

  if (consulta.isError) {
    if (interpretarErroVeiculo(consulta.error).veiculoNaoEncontrado) return <VeiculoNaoEncontrado />;
    return (
      <>
        <CabecalhoCadastroVeiculo modo="edicao" caminhoFrota={CAMINHOS.frota} />
        <EstadoErro mensagem={obterMensagemErro(consulta.error)} aoTentarNovamente={() => void consulta.refetch()} />
      </>
    );
  }

  return (
    <>
      <CabecalhoCadastroVeiculo modo="edicao" caminhoFrota={CAMINHOS.frota} />
      <output className="block text-corpo-denso text-texto-apoio">{MENSAGEM_CARREGANDO}</output>
    </>
  );
}

/**
 * P1-FE-03 · Cadastro e edição de veículo (tela 05). Sem id na rota é cadastro; com id é a edição do veículo. Um id que
 * não é número válido não existe. Cada modo é uma tela própria (chave na troca), então o cadastro não reaproveita o
 * estado da edição. O título da aba segue a rota.
 */
export default function CadastroVeiculoPagina() {
  const { idVeiculo } = useParams();
  const localizacao = useLocation();
  const rota = identificarRotaVeiculo(idVeiculo);
  const veioDoSistema = origemDaTelaVeiculo(localizacao);
  let titulo = MENSAGEM_VEICULO_NAO_ENCONTRADO;
  if (rota.tipo === 'cadastro') titulo = MENSAGEM_VEICULO_TITULO_CADASTRAR;
  if (rota.tipo === 'edicao') titulo = MENSAGEM_VEICULO_TITULO_EDITAR;
  useTituloDocumento(titulo);

  if (rota.tipo === 'invalido') return <VeiculoNaoEncontrado />;
  if (rota.tipo === 'cadastro') {
    return <TelaVeiculo key="cadastro" idVeiculo={null} veiculo={null} veioDoSistema={veioDoSistema} />;
  }
  return <EdicaoVeiculoPagina key={`edicao-${rota.idVeiculo}`} idVeiculo={rota.idVeiculo} veioDoSistema={veioDoSistema} />;
}
