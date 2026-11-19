import { forwardRef, useCallback, useEffect, useRef, useState, type MouseEvent } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { flushSync } from 'react-dom';
import { Link, Navigate, useNavigate } from 'react-router-dom';
import { Botao, EstadoErro, Icone, useAviso } from '@/components/ui';
import { useTituloDocumento } from '@/hooks/useTituloDocumento';
import { useCadastrarCliente } from '@/features/clientes/hooks/useCadastrarCliente';
import { useDocumentosCliente, useEnviarDocumentoCliente } from '@/features/clientes/hooks/useDocumentosCliente';
import { useEntrar } from '@/features/clientes/hooks/useEntrar';
import { useVerificarCadastro } from '@/features/clientes/hooks/useVerificarCadastro';
import { BlocoEndereco, BlocoHabilitacao, BlocoSenha } from '@/features/clientes/cadastro/CamposCadastro';
import { CartaoDocumento } from '@/features/clientes/cadastro/CartaoDocumento';
import { IndicadorEtapasProgresso } from '@/features/clientes/cadastro/IndicadorEtapas';
import {
  DOCUMENTOS_INICIAL,
  ESQUEMA_DOCUMENTOS,
  type FormularioCadastro,
  type FormularioDocumentos,
} from '@/features/clientes/cadastro/esquemaCadastro';
import {
  CAMPOS_PAYLOAD_API,
  ehCampoDaEtapa2,
  etapaDeDestino,
  errosParaAplicarNaEtapa,
  extrairDocumentos,
  juntarFormulario,
  separarErrosPorEtapa,
  validarCadastroCompleto,
  type ErroCampoCadastro,
} from '@/features/clientes/cadastro/etapasCadastro';
import {
  acaoAposEnvio,
  escolherCampoDoConflito,
  executarCadastro,
  primeiroCampoNaTela,
  type DependenciasCadastro,
  type ResultadoCadastro,
} from '@/features/clientes/cadastro/fluxoCadastro';
import { useCadastroEtapas } from '@/features/clientes/cadastro/useCadastroEtapas';
import {
  CAMPO_API_POR_CAMPO_FORMULARIO,
  TIPO_ERRO_DUPLICADO,
  deveBloquearContinuar,
  mensagemDeDuplicidade,
  normalizarValorVerificacao,
  resultadoVigente,
  valorVerificavel,
  type RegistroVerificacao,
} from '@/features/clientes/cadastro/verificacaoCadastro';
import {
  conferirArquivoDocumento,
  documentosFaltando,
  documentosPendentes,
  mascararCpf,
  semTipo,
  situacaoModoSoEnviar,
  tiposNoServidor,
  type ArquivosDocumento,
  type MotivoRecusaDocumento,
} from '@/lib/documentosCliente';
import { aplicarErrosDeCampo, obterMensagemErro } from '@/lib/erros';
import {
  MENSAGEM_CARREGANDO,
  MENSAGEM_CADASTRO_BOTAO_FINALIZAR,
  MENSAGEM_CADASTRO_FAIXA_CONFERENCIA,
  MENSAGEM_CADASTRO_SELO_DOCUMENTOS,
  MENSAGEM_CADASTRO_SUBTITULO_DOCUMENTOS,
  MENSAGEM_CADASTRO_SUCESSO_COM_LOGIN,
  MENSAGEM_CADASTRO_SUCESSO_SEM_LOGIN,
  MENSAGEM_CADASTRO_TITULO_DOCUMENTOS,
  MENSAGEM_CADASTRO_TITULO_PAGINA_DOCUMENTOS,
  MENSAGEM_CADASTRO_TRILHA_CADASTRO,
  MENSAGEM_CADASTRO_TRILHA_DOCUMENTOS,
  MENSAGEM_CADASTRO_TRILHA_ROTULO,
  MENSAGEM_CADASTRO_TRILHA_VALIDACAO,
  MENSAGEM_CADASTRO_VOLTAR_DADOS_PESSOAIS,
  MENSAGEM_DOCUMENTO_BOTAO_ENVIANDO,
  MENSAGEM_DOCUMENTO_BOTAO_ENVIAR,
  MENSAGEM_DOCUMENTO_CPF_VINCULADO,
  MENSAGEM_DOCUMENTO_FALTA_ARQUIVO,
  MENSAGEM_DOCUMENTO_FORMATOS_CNH,
  MENSAGEM_DOCUMENTO_FORMATOS_COMPROVANTE,
  MENSAGEM_DOCUMENTO_INFORMACOES_COMPLEMENTARES,
  MENSAGEM_DOCUMENTO_SUCESSO_SO_ENVIAR,
  MENSAGEM_DOCUMENTO_TITULO_CNH,
  MENSAGEM_DOCUMENTO_TITULO_COMPROVANTE,
  MENSAGEM_ERRO_API_PADRAO,
} from '@/lib/mensagens';
import { useAutenticacao } from '@/routes/autenticacao/useAutenticacao';
import { CAMINHOS } from '@/routes/caminhos';
import { ErroApi } from '@/types/api';
import type { DocumentoClienteGravado, TipoDocumentoCliente } from '@/types/documentosCliente';

const FOCO_LINK =
  'focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-steel';

interface AlertaGeralProps {
  mensagem: string | null;
}

/** Alerta geral do formulário (erros que não são de um campo). Ícone e texto, sem depender da cor. */
const AlertaGeral = forwardRef<HTMLDivElement, AlertaGeralProps>(function AlertaGeral({ mensagem }, ref) {
  if (mensagem === null) return null;
  return (
    <div
      ref={ref}
      role="alert"
      tabIndex={-1}
      className="flex items-start gap-2 rounded-md border border-status-perigo bg-status-perigo-fundo p-3 text-corpo-denso font-medium text-status-perigo focus:outline-none focus-visible:ring-2 focus-visible:ring-status-perigo/30"
    >
      <Icone nome="error" tamanho={20} className="mt-0.5" />
      <p>{mensagem}</p>
    </div>
  );
});

/** Com o envio em andamento, o link da página não navega (o fluxo ainda está em curso). */
function impedirNavegacaoEmEnvio(enviando: boolean) {
  return (evento: MouseEvent<HTMLAnchorElement>) => {
    if (enviando) evento.preventDefault();
  };
}

/** Mapa tipo → documento gravado no servidor (sem o link de leitura). */
type DocumentosPorTipo = Partial<Record<TipoDocumentoCliente, DocumentoClienteGravado>>;

/** Mapa tipo → motivo da recusa do último arquivo escolhido. */
type RecusasPorTipo = Partial<Record<TipoDocumentoCliente, MotivoRecusaDocumento>>;

/** Mapa tipo → marca de falha no último envio. */
type FalhasPorTipo = Partial<Record<TipoDocumentoCliente, true>>;

/** Mapa tipo → percentual do envio em andamento. */
type ProgressosPorTipo = Partial<Record<TipoDocumentoCliente, number>>;

/** Lista do servidor em mapa por tipo (no máximo um por tipo; o backend já devolve o ativo). */
function documentosPorTipo(lista: readonly DocumentoClienteGravado[] | undefined): DocumentosPorTipo {
  const mapa: DocumentosPorTipo = {};
  for (const documento of lista ?? []) mapa[documento.tipo] = documento;
  return mapa;
}

/**
 * P2-FE-02 e P2-FE-03 · Documentos do cadastro (tela 21). Três modos:
 * - cadastro (F1): visitante com os dados da etapa 1 em memória; a CNH, o endereço e a senha estão aqui e o
 *   botão final cria a conta, entra e envia os dois arquivos;
 * - depois de criar a conta (F2): a página fica nela; só os cartões e o botão de envio (sem refazer o cadastro);
 * - só enviar (F4): cliente logado que chegou direto aqui; a lista do servidor mostra o que já existe.
 */
export default function CadastroDocumentosPagina() {
  useTituloDocumento(MENSAGEM_CADASTRO_TITULO_PAGINA_DOCUMENTOS);
  const navigate = useNavigate();
  const aviso = useAviso();
  const { autenticado, usuario } = useAutenticacao();
  const {
    dadosPessoais,
    documentos,
    errosServidor,
    arquivosDocumentos,
    salvarDocumentos,
    salvarArquivosDocumentos,
    definirErrosServidor,
    removerErrosServidor,
  } = useCadastroEtapas();
  const cadastro = useCadastrarCliente();
  const entrar = useEntrar();
  const enviarDocumento = useEnviarDocumentoCliente();
  const { verificar } = useVerificarCadastro();
  const { reset: resetarCadastro } = cadastro;
  const { reset: resetarLogin } = entrar;

  // Cliente logado (perfil cliente) é quem envia os documentos. Funcionário continua sendo mandado embora.
  const clienteLogado = autenticado && usuario?.perfil === 'cliente';
  // Quem abriu a página já logado (F4) fica no modo só enviar. Quem entrou sem sessão (F1) só muda de modo
  // depois que a conta foi criada nesta visita (`idCriado`): o login automático não troca a tela no meio do envio.
  const [abriuSoEnviar] = useState(() => clienteLogado);

  // Id do cliente criado nesta visita (F1 → F2). Sem ele, o id vem da sessão (F4).
  const [idCriado, setIdCriado] = useState<number | null>(null);
  const idCliente = idCriado ?? (clienteLogado ? (usuario?.idUsuario ?? null) : null);
  const soEnviar = clienteLogado && (abriuSoEnviar || idCriado !== null) && idCliente !== null;

  // `enviando` (estado) controla a tela. `emEnvio` (ref síncrona) impede um segundo envio antes do próximo
  // render: Enter repetido ou duplo clique.
  const [enviando, setEnviando] = useState(false);
  const [alertaGeral, setAlertaGeral] = useState<string | null>(null);
  const emEnvio = useRef(false);
  const montada = useRef(false);
  const alertaRef = useRef<HTMLDivElement>(null);
  const controleEnvio = useRef<AbortController | null>(null);

  // Estado dos cartões nesta visita: o que já subiu, o que está subindo, falhas, recusas e documentos gravados.
  const [enviados, setEnviados] = useState<ArquivosDocumento>({});
  const [recebidos, setRecebidos] = useState<DocumentosPorTipo>({});
  const [documentoEnviando, setDocumentoEnviando] = useState<TipoDocumentoCliente | null>(null);
  const [progressos, setProgressos] = useState<ProgressosPorTipo>({});
  const [falhas, setFalhas] = useState<FalhasPorTipo>({});
  const [recusas, setRecusas] = useState<RecusasPorTipo>({});

  // Lista do servidor (cliente logado). Só consulta quando há um cliente da sessão ou criado.
  const consultaDocumentos = useDocumentosCliente(soEnviar ? idCliente : null);
  const porTipoServidor: DocumentosPorTipo = { ...documentosPorTipo(consultaDocumentos.data), ...recebidos };
  const noServidor = tiposNoServidor(porTipoServidor);

  const {
    register,
    handleSubmit,
    setError,
    setFocus,
    getValues,
    subscribe,
    getFieldState,
    clearErrors,
    formState: { errors },
  } = useForm<FormularioDocumentos>({
    // CNH e endereço já digitados voltam ao reabrir a tela; a senha nunca é guardada.
    defaultValues: { ...DOCUMENTOS_INICIAL, ...documentos, senha: '', confirmarSenha: '' },
    resolver: zodResolver(ESQUEMA_DOCUMENTOS),
  });

  // Ao sair da tela, esquece as mutações (senha e dados pessoais não ficam em cache) e guarda em
  // memória só a CNH e o endereço (nunca a senha). A ref `montada` impede navegar ou avisar depois
  // que a tela desmontou. Cancela o envio que ainda estiver em curso.
  useEffect(() => {
    montada.current = true;
    controleEnvio.current = new AbortController();
    return () => {
      montada.current = false;
      controleEnvio.current?.abort();
      salvarDocumentos(extrairDocumentos(getValues()));
      resetarCadastro();
      resetarLogin();
    };
  }, [getValues, salvarDocumentos, resetarCadastro, resetarLogin]);

  // Editar um campo com erro vindo da API (ou de duplicidade) limpa o erro daquele campo (a validação normal segue valendo).
  useEffect(() => {
    return subscribe({
      formState: { values: true },
      callback: ({ name, type }) => {
        if (type !== 'change' || name === undefined || !ehCampoDaEtapa2(name)) return;
        const tipoErro = getFieldState(name).error?.type;
        if (tipoErro === 'server' || tipoErro === TIPO_ERRO_DUPLICADO) clearErrors(name);
      },
    });
  }, [subscribe, getFieldState, clearErrors]);

  // Verificação da CNH ao sair do campo. O registro diz para qual número (só dígitos) o resultado vale:
  // resultado de número antigo é descartado. O envio só consulta o que já se sabe, nunca espera a verificação.
  const registroCnh = useRef<RegistroVerificacao | undefined>(undefined);

  const verificarCnh = useCallback(async () => {
    const valor = valorVerificavel('cnhNumero', getValues('cnhNumero'));
    if (valor === null) return;

    const marcarDuplicado = () => {
      if (normalizarValorVerificacao('cnhNumero', getValues('cnhNumero')) !== valor) return;
      setError('cnhNumero', { type: TIPO_ERRO_DUPLICADO, message: mensagemDeDuplicidade('cnhNumero') });
    };

    const registro = registroCnh.current;
    if (registro?.valor === valor) {
      if (registro.estado === 'existe') marcarDuplicado();
      return;
    }

    registroCnh.current = { valor, estado: 'verificando' };
    const resultado = await verificar(CAMPO_API_POR_CAMPO_FORMULARIO.cnhNumero, valor);
    if (registroCnh.current?.valor !== valor) return;
    registroCnh.current = { valor, estado: resultado };
    if (resultado === 'existe') marcarDuplicado();
  }, [verificar, getValues, setError]);

  const aoSairCnh = useCallback(() => {
    void verificarCnh();
  }, [verificarCnh]);

  /** Mostra os erros de campo desta tela e leva o foco ao primeiro, na ordem da tela. */
  function mostrarErrosDaEtapa2(erros: readonly ErroCampoCadastro[]) {
    for (const erro of erros) {
      if (ehCampoDaEtapa2(erro.campo)) setError(erro.campo, { type: 'server', message: erro.mensagem });
    }
    const primeiro = primeiroCampoNaTela(erros.map((erro) => erro.campo));
    if (primeiro !== undefined && ehCampoDaEtapa2(primeiro)) setFocus(primeiro);
  }

  // Erros que ficaram esperando esta tela (a API apontou campos da etapa 1 e da etapa 2 de uma vez,
  // e a pessoa foi primeiro para a etapa 1).
  useEffect(() => {
    const erros = errosParaAplicarNaEtapa(2, errosServidor);
    if (erros.length === 0) return;
    for (const erro of erros) {
      if (ehCampoDaEtapa2(erro.campo)) setError(erro.campo, { type: 'server', message: erro.mensagem });
    }
    const primeiro = primeiroCampoNaTela(erros.map((erro) => erro.campo));
    if (primeiro !== undefined && ehCampoDaEtapa2(primeiro)) setFocus(primeiro);
    removerErrosServidor(2);
  }, [errosServidor, removerErrosServidor, setError, setFocus]);

  /**
   * Encaminha os erros de campo: se algum é da etapa 1, volta para a etapa 1 (os erros da etapa 2
   * esperam no provedor); senão, mostra os da etapa 2 aqui. Sem campo, vira o alerta geral.
   * O foco vai ao primeiro campo com erro, ou ao alerta geral, depois de o formulário ser reabilitado.
   */
  function encaminharErros(erros: readonly ErroCampoCadastro[], mensagemGeral: string) {
    const porEtapa = separarErrosPorEtapa(erros);
    const destino = etapaDeDestino(porEtapa);

    if (destino === 1) {
      definirErrosServidor([...porEtapa.etapa1, ...porEtapa.etapa2]);
      navigate(CAMINHOS.cadastroDadosPessoais);
      return;
    }

    flushSync(() => {
      setEnviando(false);
      if (destino === null) setAlertaGeral(mensagemGeral);
    });

    if (destino === 2) {
      mostrarErrosDaEtapa2(porEtapa.etapa2);
      return;
    }
    alertaRef.current?.focus();
  }

  /**
   * Erro do POST: 409 vai para o campo citado (CNH na etapa 2, CPF na etapa 1); 400 vai para os campos
   * que o backend apontou, cada um na sua etapa; o restante vira alerta geral aqui.
   */
  function tratarErroCadastro(erro: unknown) {
    const mensagem = obterMensagemErro(erro);
    const status = erro instanceof ErroApi ? erro.status : 0;
    const erros: ErroCampoCadastro[] = [];

    if (status === 409) {
      const campo = escolherCampoDoConflito(mensagem);
      if (campo !== null) erros.push({ campo, mensagem });
    } else if (status === 400) {
      aplicarErrosDeCampo(erro, CAMPOS_PAYLOAD_API, (campo, mensagemCampo) => {
        erros.push({ campo, mensagem: mensagemCampo });
      });
    }

    encaminharErros(erros, mensagem);
  }

  /**
   * Envia um documento pelo cliente HTTP (com progresso e cancelamento da página). Cada sucesso guarda o
   * documento gravado; a falha segue para quem chamou, que para a sequência.
   */
  const enviarUmDocumento = useCallback(
    (idDoCliente: number, tipo: TipoDocumentoCliente, arquivo: File): Promise<DocumentoClienteGravado> => {
      setDocumentoEnviando(tipo);
      setProgressos((atual) => ({ ...atual, [tipo]: 0 }));
      return enviarDocumento
        .mutateAsync({
          idCliente: idDoCliente,
          tipo,
          arquivo,
          opcoes: {
            aoProgredir: (percentual) => {
              if (montada.current) setProgressos((atual) => ({ ...atual, [tipo]: percentual }));
            },
            sinal: controleEnvio.current?.signal,
          },
        })
        .then((resposta) => {
          if (montada.current) setRecebidos((atual) => ({ ...atual, [tipo]: resposta }));
          // Gravado no servidor: sai da memória da etapa, para não voltar como "Selecionado" ao remontar a tela.
          salvarArquivosDocumentos((atual) => (atual[tipo] === arquivo ? semTipo(atual, tipo) : atual));
          return resposta;
        })
        .finally(() => {
          if (montada.current) setDocumentoEnviando(null);
        });
    },
    [enviarDocumento, salvarArquivosDocumentos],
  );

  /**
   * Sequência do envio: com `valores` (F1) cria a conta, entra e envia os documentos; sem `valores` (F2 e F4)
   * só envia o que falta, sem refazer o cadastro. Nunca manda o que já subiu.
   */
  async function executarEnvio(valores: FormularioCadastro | null) {
    if (emEnvio.current) return;
    emEnvio.current = true;
    setAlertaGeral(null);
    setFalhas({});
    setEnviando(true);

    const dependencias: DependenciasCadastro = {
      cadastrar: (requisicao) => cadastro.mutateAsync(requisicao),
      entrar: (requisicao) => entrar.mutateAsync(requisicao),
      enviarDocumento: enviarUmDocumento,
    };
    const resultado: ResultadoCadastro = await executarCadastro(dependencias, {
      valores,
      idCliente,
      arquivos: arquivosDocumentos,
      enviados,
    });

    if (!montada.current) return;

    const acao = acaoAposEnvio(resultado, valores !== null);

    if (resultado.tipo === 'erro-cadastro') {
      emEnvio.current = false;
      tratarErroCadastro(resultado.erro);
      return;
    }

    if (acao === 'ir-entrar') {
      // O cadastro já existe; não se refaz o POST. Nenhum documento foi enviado: o usuário entra e volta aqui (F3 → F4).
      aviso.sucesso(MENSAGEM_CADASTRO_SUCESSO_SEM_LOGIN);
      navigate(CAMINHOS.entrar, { replace: true, state: { de: CAMINHOS.cadastroDocumentos } });
      return;
    }

    if (resultado.tipo === 'concluido' || resultado.tipo === 'erro-documento') {
      setIdCriado(resultado.idCliente);
      setEnviados(resultado.enviados);
    }
    setEnviando(false);
    emEnvio.current = false;

    if (resultado.tipo === 'erro-documento') {
      setFalhas({ [resultado.documento]: true });
      // Limite de envios do cliente (429): a mensagem do próprio backend, já aprovada, vai ao alerta geral.
      if (resultado.erro instanceof ErroApi && resultado.erro.status === 429) {
        setAlertaGeral(obterMensagemErro(resultado.erro));
      }
      return;
    }

    if (acao === 'ir-inicio') {
      aviso.sucesso(MENSAGEM_CADASTRO_SUCESSO_COM_LOGIN);
      navigate(CAMINHOS.inicio, { replace: true });
      return;
    }
    aviso.sucesso(MENSAGEM_DOCUMENTO_SUCESSO_SO_ENVIAR);
  }

  /** Envio já validado pelo React Hook Form (etapa 1 e 2 juntas, cadastro completo). */
  async function enviarCadastro(dadosEtapa2: FormularioDocumentos) {
    if (emEnvio.current || dadosPessoais === null) return;

    const validacao = validarCadastroCompleto(juntarFormulario(dadosPessoais, dadosEtapa2));
    if (!validacao.ok) {
      encaminharErros(validacao.erros, MENSAGEM_ERRO_API_PADRAO);
      return;
    }

    // CNH já conhecida como cadastrada: mostra o erro no campo e não faz o POST. Não espera a
    // verificação pendente: o POST é a autoridade (o 409 continua tratado abaixo).
    const resultadoCnh = resultadoVigente(registroCnh.current, 'cnhNumero', dadosEtapa2.cnhNumero);
    if (deveBloquearContinuar(resultadoCnh)) {
      setError('cnhNumero', { type: TIPO_ERRO_DUPLICADO, message: mensagemDeDuplicidade('cnhNumero') });
      setFocus('cnhNumero');
      return;
    }

    // Os dois arquivos são obrigatórios para criar a conta.
    if (documentosFaltando(arquivosDocumentos, []).length > 0) {
      setAlertaGeral(MENSAGEM_DOCUMENTO_FALTA_ARQUIVO);
      alertaRef.current?.focus();
      return;
    }

    await executarEnvio(validacao.formulario);
  }

  /** Envio em modo só enviar (F2 e F4): só o que foi escolhido e ainda não subiu. */
  async function enviarPendentes() {
    if (emEnvio.current) return;
    if (documentosFaltando(arquivosDocumentos, noServidor).length > 0) {
      setAlertaGeral(MENSAGEM_DOCUMENTO_FALTA_ARQUIVO);
      alertaRef.current?.focus();
      return;
    }
    // Nada a enviar: os dois documentos já estão gravados (o aviso é verdadeiro, não há envio a fazer).
    if (documentosPendentes(arquivosDocumentos, enviados).length === 0) {
      aviso.sucesso(MENSAGEM_DOCUMENTO_SUCESSO_SO_ENVIAR);
      return;
    }
    await executarEnvio(null);
  }

  /** Escolha de arquivo: confere tipo e tamanho no navegador; o que passa fica na memória da etapa. */
  async function escolherArquivo(tipo: TipoDocumentoCliente, arquivo: File) {
    let motivo: MotivoRecusaDocumento | null;
    try {
      motivo = await conferirArquivoDocumento(arquivo);
    } catch {
      motivo = 'formato';
    }
    if (!montada.current) return;

    setRecusas((atual) => (motivo === null ? semTipo(atual, tipo) : { ...atual, [tipo]: motivo }));
    if (motivo !== null) return;
    setFalhas((atual) => semTipo(atual, tipo));
    salvarArquivosDocumentos((atual) => ({ ...atual, [tipo]: arquivo }));
  }

  /** Arquivo escolhido que ainda não subiu (o que aparece como "Selecionado"). */
  function arquivoPendente(tipo: TipoDocumentoCliente): File | null {
    const arquivo = arquivosDocumentos[tipo];
    return arquivo !== undefined && arquivo !== enviados[tipo] ? arquivo : null;
  }

  /** Documento gravado que vale para o cartão (quando não há troca pendente). */
  function documentoGravado(tipo: TipoDocumentoCliente): DocumentoClienteGravado | null {
    return arquivoPendente(tipo) === null ? (porTipoServidor[tipo] ?? null) : null;
  }

  // Sem a sessão de cliente e sem os dados da etapa 1 não há o que fazer aqui.
  if (autenticado && !clienteLogado && !enviando) {
    return <Navigate to={CAMINHOS.inicio} replace />;
  }
  if (!autenticado) {
    // Quem estava logado e perdeu a sessão (401 no envio) entra de novo.
    if (abriuSoEnviar || idCriado !== null) return <Navigate to={CAMINHOS.entrar} replace />;
    if (dadosPessoais === null) return <Navigate to={CAMINHOS.cadastroDadosPessoais} replace />;
  }
  if (clienteLogado && idCliente === null) {
    return <Navigate to={CAMINHOS.inicio} replace />;
  }

  const cpfMascarado = dadosPessoais !== null ? mascararCpf(dadosPessoais.cpf) : '';
  const rotuloBotao = enviando ? MENSAGEM_DOCUMENTO_BOTAO_ENVIANDO : soEnviar ? MENSAGEM_DOCUMENTO_BOTAO_ENVIAR : MENSAGEM_CADASTRO_BOTAO_FINALIZAR;
  // Só enviar: o aviso e o botão dependem do que já está gravado e do arquivo novo escolhido.
  const situacaoSoEnviar = soEnviar ? situacaoModoSoEnviar(noServidor, documentosPendentes(arquivosDocumentos, enviados)) : null;
  const mostrarBotaoEnvio = situacaoSoEnviar === null || situacaoSoEnviar.mostrarBotao;

  return (
    <div className="mx-auto w-full max-w-[1360px] px-4 py-8 md:px-6">
      <nav aria-label={MENSAGEM_CADASTRO_TRILHA_ROTULO} className="mb-6">
        <ol className="flex flex-wrap items-center gap-1 text-corpo-denso text-status-neutro">
          <li>{MENSAGEM_CADASTRO_TRILHA_CADASTRO}</li>
          <li aria-hidden="true" className="flex items-center">
            <Icone nome="chevron_right" tamanho={14} />
          </li>
          <li>{MENSAGEM_CADASTRO_TRILHA_VALIDACAO}</li>
          <li aria-hidden="true" className="flex items-center">
            <Icone nome="chevron_right" tamanho={14} />
          </li>
          <li aria-current="page" className="text-navy">
            {MENSAGEM_CADASTRO_TRILHA_DOCUMENTOS}
          </li>
        </ol>
      </nav>

      <IndicadorEtapasProgresso />

      <section className="w-full rounded-lg bg-branco p-6 shadow-md md:p-8">
        <header className="pb-6">
          <div className="max-w-3xl">
            <div className="mb-1 inline-flex items-center gap-1 rounded-xl bg-steel-alto px-2 py-0.5 text-legenda font-semibold text-navy">
              <Icone nome="document_scanner" tamanho={16} className="text-status-pendente" />
              {MENSAGEM_CADASTRO_SELO_DOCUMENTOS}
            </div>
            <h1 className="text-titulo-documentos tracking-tight text-navy">{MENSAGEM_CADASTRO_TITULO_DOCUMENTOS}</h1>
            <p className="mt-1 text-corpo-denso text-status-neutro">{MENSAGEM_CADASTRO_SUBTITULO_DOCUMENTOS}</p>
          </div>
        </header>

        <form
          noValidate
          onSubmit={(evento) => {
            if (soEnviar) {
              evento.preventDefault();
              void enviarPendentes();
              return;
            }
            void handleSubmit(enviarCadastro)(evento);
          }}
        >
          {situacaoSoEnviar !== null && (
            <p className="mb-6 flex items-start gap-2 rounded-sm bg-steel-suave p-3 text-corpo-denso text-navy">
              <Icone nome="info" tamanho={18} className="mt-0.5 shrink-0 text-steel" />
              <span>{situacaoSoEnviar.aviso}</span>
            </p>
          )}

          {soEnviar && consultaDocumentos.isPending && <p className="mb-4 text-corpo-denso text-status-neutro">{MENSAGEM_CARREGANDO}</p>}
          {soEnviar && consultaDocumentos.isError && (
            <div className="mb-6">
              <EstadoErro
                mensagem={obterMensagemErro(consultaDocumentos.error)}
                aoTentarNovamente={() => void consultaDocumentos.refetch()}
              />
            </div>
          )}

          <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
            <CartaoDocumento
              tipo="CNH"
              titulo={MENSAGEM_DOCUMENTO_TITULO_CNH}
              formatos={MENSAGEM_DOCUMENTO_FORMATOS_CNH}
              arquivo={arquivoPendente('CNH')}
              enviado={documentoGravado('CNH')}
              enviando={documentoEnviando === 'CNH'}
              progresso={progressos.CNH ?? 0}
              falhou={falhas.CNH === true}
              recusa={recusas.CNH ?? null}
              desabilitado={enviando}
              aoEscolher={(arquivo) => void escolherArquivo('CNH', arquivo)}
              aoTentarNovamente={() => void enviarPendentes()}
            />
            <CartaoDocumento
              tipo="COMPROVANTE_RESIDENCIA"
              titulo={MENSAGEM_DOCUMENTO_TITULO_COMPROVANTE}
              formatos={MENSAGEM_DOCUMENTO_FORMATOS_COMPROVANTE}
              arquivo={arquivoPendente('COMPROVANTE_RESIDENCIA')}
              enviado={documentoGravado('COMPROVANTE_RESIDENCIA')}
              enviando={documentoEnviando === 'COMPROVANTE_RESIDENCIA'}
              progresso={progressos.COMPROVANTE_RESIDENCIA ?? 0}
              falhou={falhas.COMPROVANTE_RESIDENCIA === true}
              recusa={recusas.COMPROVANTE_RESIDENCIA ?? null}
              desabilitado={enviando}
              aoEscolher={(arquivo) => void escolherArquivo('COMPROVANTE_RESIDENCIA', arquivo)}
              aoTentarNovamente={() => void enviarPendentes()}
            />
          </div>

          {!soEnviar && (
            <fieldset disabled={enviando} className="mt-8 min-w-0 border-0 p-0">
              <BlocoHabilitacao registrar={register} erros={errors} aoSairCnh={aoSairCnh} />
              {cpfMascarado !== '' && (
                <div className="mb-6 flex flex-wrap items-center gap-2 rounded-lg bg-steel-fundo/70 p-4">
                  <span className="text-corpo-denso text-status-neutro">{MENSAGEM_DOCUMENTO_INFORMACOES_COMPLEMENTARES}</span>
                  <span className="inline-flex items-center gap-1 rounded-sm bg-steel-suave px-2 py-1 text-legenda text-navy">
                    <Icone nome="assignment_ind" tamanho={16} className="text-status-pendente" />
                    {MENSAGEM_DOCUMENTO_CPF_VINCULADO} {cpfMascarado}
                  </span>
                </div>
              )}
              <BlocoEndereco registrar={register} erros={errors} />
              <BlocoSenha registrar={register} erros={errors} />
            </fieldset>
          )}

          <div className="flex flex-col gap-6 pt-4">
            <AlertaGeral ref={alertaRef} mensagem={alertaGeral} />

            <div className="flex flex-col items-center justify-between gap-4 sm:flex-row">
              {soEnviar ? (
                <span aria-hidden="true" />
              ) : (
                <Link
                  to={CAMINHOS.cadastroDadosPessoais}
                  aria-disabled={enviando || undefined}
                  onClick={impedirNavegacaoEmEnvio(enviando)}
                  className={`flex h-11 w-full items-center justify-center gap-1 rounded-sm bg-steel-suave px-6 text-corpo-denso font-semibold text-navy transition-colors hover:bg-steel-alto sm:w-auto ${FOCO_LINK}`}
                >
                  <Icone nome="arrow_back" tamanho={18} />
                  {MENSAGEM_CADASTRO_VOLTAR_DADOS_PESSOAIS}
                </Link>
              )}

              {mostrarBotaoEnvio && (
                <Botao
                  type="submit"
                  tamanho="grande"
                  iconeDireita="arrow_forward"
                  carregando={enviando}
                  className="w-full !h-11 !rounded-sm !px-8 shadow-md sm:w-auto"
                >
                  {rotuloBotao}
                </Botao>
              )}
            </div>

            <div className="flex items-center justify-center gap-2 rounded bg-steel-fundo/70 px-4 py-2 text-center text-micro text-status-neutro">
              <Icone nome="lock" tamanho={18} className="text-status-pendente" />
              <span>{MENSAGEM_CADASTRO_FAIXA_CONFERENCIA}</span>
            </div>
          </div>
        </form>
      </section>
    </div>
  );
}
