import { forwardRef, useCallback, useEffect, useRef, useState, type MouseEvent } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { flushSync } from 'react-dom';
import { Link, Navigate, useNavigate } from 'react-router-dom';
import { Botao, Icone, useAviso } from '@/components/ui';
import { useTituloDocumento } from '@/hooks/useTituloDocumento';
import { useCadastrarCliente } from '@/features/clientes/hooks/useCadastrarCliente';
import { useEntrar } from '@/features/clientes/hooks/useEntrar';
import { useVerificarCadastro } from '@/features/clientes/hooks/useVerificarCadastro';
import { BlocoEndereco, BlocoHabilitacao, BlocoSenha } from '@/features/clientes/cadastro/CamposCadastro';
import { IndicadorEtapasProgresso } from '@/features/clientes/cadastro/IndicadorEtapas';
import {
  DOCUMENTOS_INICIAL,
  ESQUEMA_DOCUMENTOS,
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
  escolherCampoDoConflito,
  executarCadastro,
  primeiroCampoNaTela,
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
import { aplicarErrosDeCampo, obterMensagemErro } from '@/lib/erros';
import {
  MENSAGEM_CADASTRO_BOTAO_FINALIZAR,
  MENSAGEM_CADASTRO_FAIXA_CONFERENCIA,
  MENSAGEM_CADASTRO_SELO_DOCUMENTOS,
  MENSAGEM_CADASTRO_SUBTITULO_DOCUMENTOS,
  MENSAGEM_CADASTRO_SUCESSO_COM_LOGIN,
  MENSAGEM_CADASTRO_SUCESSO_SEM_LOGIN,
  MENSAGEM_CADASTRO_TITULO_DOCUMENTOS,
  MENSAGEM_CADASTRO_TITULO_PAGINA_DOCUMENTOS,
  MENSAGEM_CADASTRO_TRILHA_CADASTRO,
  MENSAGEM_CADASTRO_TRILHA_DADOS_PESSOAIS,
  MENSAGEM_CADASTRO_TRILHA_DOCUMENTOS,
  MENSAGEM_CADASTRO_TRILHA_ROTULO,
  MENSAGEM_CADASTRO_VOLTAR_DADOS_PESSOAIS,
  MENSAGEM_ERRO_API_PADRAO,
} from '@/lib/mensagens';
import { useAutenticacao } from '@/routes/autenticacao/useAutenticacao';
import { CAMINHOS } from '@/routes/caminhos';
import { ErroApi } from '@/types/api';

const FOCO_LINK =
  'focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-steel';

interface AlertaGeralProps {
  mensagem: string | null;
}

/** Alerta geral do formulário (erros que não são de um campo). Ícone e texto, sem depender da cor. */
const AlertaGeral = forwardRef<HTMLDivElement, AlertaGeralProps>(function AlertaGeral(
  { mensagem },
  ref,
) {
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

/** Com o envio em andamento, os links da página não navegam (o fluxo ainda está em curso). */
function impedirNavegacaoEmEnvio(enviando: boolean) {
  return (evento: MouseEvent<HTMLAnchorElement>) => {
    if (enviando) evento.preventDefault();
  };
}

/**
 * P2-FE-02 · Cadastro do cliente, etapa 2: documentos e acesso (RF-07, visual do design 21).
 * Tela provisória: a CNH é digitada (o envio de arquivos e a leitura automática ficam para a P2-FE-03).
 * Aqui estão a CNH, o endereço opcional e a senha. O botão final junta as duas etapas, valida tudo
 * pelo esquema completo e envia de uma vez (POST /api/clientes + login automático).
 */
export default function CadastroDocumentosPagina() {
  useTituloDocumento(MENSAGEM_CADASTRO_TITULO_PAGINA_DOCUMENTOS);
  const navigate = useNavigate();
  const aviso = useAviso();
  const { autenticado } = useAutenticacao();
  const {
    dadosPessoais,
    documentos,
    errosServidor,
    salvarDocumentos,
    definirErrosServidor,
    removerErrosServidor,
  } = useCadastroEtapas();
  const cadastro = useCadastrarCliente();
  const entrar = useEntrar();
  const { verificar } = useVerificarCadastro();
  const { reset: resetarCadastro } = cadastro;
  const { reset: resetarLogin } = entrar;

  // `enviando` (estado) controla a tela. `emEnvio` (ref síncrona) impede um segundo envio
  // antes do próximo render: Enter repetido ou duplo clique.
  const [enviando, setEnviando] = useState(false);
  const [alertaGeral, setAlertaGeral] = useState<string | null>(null);
  const emEnvio = useRef(false);
  const montada = useRef(false);
  const alertaRef = useRef<HTMLDivElement>(null);

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
  // que a tela desmontou.
  useEffect(() => {
    montada.current = true;
    return () => {
      montada.current = false;
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

  /** Envio já validado pelo React Hook Form (etapa 2). Junta as duas etapas e valida tudo de novo. */
  async function enviar(dadosEtapa2: FormularioDocumentos) {
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

    emEnvio.current = true;
    setAlertaGeral(null);
    setEnviando(true);

    const resultado = await executarCadastro(
      {
        cadastrar: (requisicao) => cadastro.mutateAsync(requisicao),
        entrar: (requisicao) => entrar.mutateAsync(requisicao),
      },
      validacao.formulario,
    );

    if (!montada.current) return;

    if (resultado.tipo === 'erro-cadastro') {
      emEnvio.current = false;
      tratarErroCadastro(resultado.erro);
      return;
    }

    if (resultado.tipo === 'sem-login') {
      // O cadastro já existe; não se refaz o POST. O usuário entra manualmente.
      aviso.sucesso(MENSAGEM_CADASTRO_SUCESSO_SEM_LOGIN);
      navigate(CAMINHOS.entrar, { replace: true });
      return;
    }

    aviso.sucesso(MENSAGEM_CADASTRO_SUCESSO_COM_LOGIN);
    navigate(CAMINHOS.inicio, { replace: true });
  }

  // Sem os dados da etapa 1 (ex.: a página foi recarregada) não há o que enviar: volta para a etapa 1.
  if (dadosPessoais === null) {
    return <Navigate to={CAMINHOS.cadastroDadosPessoais} replace />;
  }

  // Quem já tem sessão não precisa de cadastro. Durante o fluxo (`enviando`) não redireciona,
  // senão o login automático mandaria o usuário para o início antes do aviso de sucesso.
  if (autenticado && !enviando) {
    return <Navigate to={CAMINHOS.inicio} replace />;
  }

  return (
    <div className="mx-auto w-full max-w-[1360px] px-6 py-8">
      <nav aria-label={MENSAGEM_CADASTRO_TRILHA_ROTULO} className="mb-6">
        <ol className="flex flex-wrap items-center gap-1 text-corpo text-status-neutro">
          <li>{MENSAGEM_CADASTRO_TRILHA_CADASTRO}</li>
          <li aria-hidden="true" className="flex items-center">
            <Icone nome="chevron_right" tamanho={14} />
          </li>
          <li>
            <Link
              to={CAMINHOS.cadastroDadosPessoais}
              aria-disabled={enviando || undefined}
              onClick={impedirNavegacaoEmEnvio(enviando)}
              className={`rounded transition-colors hover:text-navy ${FOCO_LINK}`}
            >
              {MENSAGEM_CADASTRO_TRILHA_DADOS_PESSOAIS}
            </Link>
          </li>
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
            <div className="mb-1 inline-flex items-center gap-1 rounded-xl bg-steel-alto px-2 py-0.5 text-corpo text-navy">
              <Icone nome="id_card" tamanho={16} className="text-status-pendente" />
              {MENSAGEM_CADASTRO_SELO_DOCUMENTOS}
            </div>
            <h1 className="text-corpo tracking-tight text-navy">{MENSAGEM_CADASTRO_TITULO_DOCUMENTOS}</h1>
            <p className="mt-1 text-corpo text-status-neutro">{MENSAGEM_CADASTRO_SUBTITULO_DOCUMENTOS}</p>
          </div>
        </header>

        <form
          noValidate
          onSubmit={(evento) => {
            void handleSubmit(enviar)(evento);
          }}
        >
          <fieldset disabled={enviando} className="min-w-0 border-0 p-0">
            <BlocoHabilitacao registrar={register} erros={errors} aoSairCnh={aoSairCnh} />
            <BlocoEndereco registrar={register} erros={errors} />
            <BlocoSenha registrar={register} erros={errors} />
          </fieldset>

          <div className="flex flex-col gap-6 pt-4">
            <AlertaGeral ref={alertaRef} mensagem={alertaGeral} />

            <div className="flex flex-col items-center justify-between gap-4 sm:flex-row">
              <Link
                to={CAMINHOS.cadastroDadosPessoais}
                aria-disabled={enviando || undefined}
                onClick={impedirNavegacaoEmEnvio(enviando)}
                className={`flex h-10 w-full items-center justify-center gap-1 rounded-sm bg-steel-suave px-6 text-corpo text-navy transition-colors hover:bg-steel-alto sm:w-auto ${FOCO_LINK}`}
              >
                <Icone nome="arrow_back" tamanho={18} />
                {MENSAGEM_CADASTRO_VOLTAR_DADOS_PESSOAIS}
              </Link>

              <Botao
                type="submit"
                tamanho="grande"
                iconeDireita="arrow_forward"
                carregando={enviando}
                className="w-full !h-11 !rounded-sm !px-8 shadow-md sm:w-auto"
              >
                {MENSAGEM_CADASTRO_BOTAO_FINALIZAR}
              </Botao>
            </div>

            <div className="flex items-center justify-center gap-2 rounded bg-steel-fundo/70 px-4 py-2 text-center text-corpo text-status-neutro">
              <Icone nome="lock" tamanho={18} className="text-status-pendente" />
              <span>{MENSAGEM_CADASTRO_FAIXA_CONFERENCIA}</span>
            </div>
          </div>
        </form>
      </section>
    </div>
  );
}
