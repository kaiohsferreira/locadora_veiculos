import { forwardRef, useCallback, useEffect, useRef, useState, type MouseEvent } from 'react';
import { useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { flushSync } from 'react-dom';
import { Link, Navigate, useNavigate } from 'react-router-dom';
import { Botao, Icone } from '@/components/ui';
import { useTituloDocumento } from '@/hooks/useTituloDocumento';
import { useEntrar } from '@/features/clientes/hooks/useEntrar';
import {
  MENSAGEM_CADASTRO_ABERTURA,
  MENSAGEM_CADASTRO_TRILHA_ROTULO,
  MENSAGEM_CADASTRO_VOLTAR_INICIO,
  MENSAGEM_LOCADORA_BOTAO_CRIAR_CONTA,
  MENSAGEM_LOCADORA_BOTAO_VOLTAR_EMPRESA,
  MENSAGEM_LOCADORA_ETIQUETA_PESSOA_JURIDICA,
  MENSAGEM_LOCADORA_FAIXA_ETAPA_2,
  MENSAGEM_LOCADORA_FAIXA_SEGURANCA_RESPONSAVEL,
  MENSAGEM_LOCADORA_SUBTITULO_RESPONSAVEL,
  MENSAGEM_LOCADORA_TITULO_PAGINA_RESPONSAVEL,
  MENSAGEM_LOCADORA_TITULO_RESPONSAVEL,
} from '@/lib/mensagens';
import { useAutenticacao } from '@/routes/autenticacao/useAutenticacao';
import { CAMINHOS } from '@/routes/caminhos';
import { AceiteTermosResponsavel, BlocoSenhaResponsavel, CamposIdentidadeResponsavel } from '@/features/locadoras/cadastro/CamposResponsavel';
import { useCadastroLocadora } from '@/features/locadoras/cadastro/ContextoCadastroLocadora';
import { IndicadorEtapasDesenhoLocadora } from '@/features/locadoras/cadastro/IndicadorEtapasDesenhoLocadora';
import { executarCadastroLocadora } from '@/features/locadoras/cadastro/fluxoCadastroLocadora';
import { montarRequisicaoCadastroLocadora } from '@/features/locadoras/cadastro/conversaoLocadora';
import {
  conflitosDoEnvio,
  errosDaEtapaLocadora,
  interpretarErroCadastroLocadora,
  type ErroCampoLocadora,
} from '@/features/locadoras/cadastro/mapeamentoErroLocadora';
import {
  CAMPOS_RESPONSAVEL,
  DADOS_RESPONSAVEL_INICIAL,
  ehCampoResponsavel,
  ESQUEMA_RESPONSAVEL,
  extrairDadosResponsavel,
  type FormularioResponsavel,
} from '@/features/locadoras/cadastro/esquemaResponsavel';
import { TIPO_ERRO_DUPLICADO_LOCADORA } from '@/features/locadoras/cadastro/verificacaoLocadora';
import { useCadastrarLocadora } from '@/features/locadoras/hooks/useCadastrarLocadora';
import type { RequisicaoCadastroLocadora } from '@/types/empresas';

const FOCO_LINK =
  'focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-steel';

interface AlertaGeralProps {
  mensagem: string | null;
}

/** Alerta geral da etapa (erros que não são de um campo: rede, limite, indisponível). Ícone e texto. */
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

/** Com o envio em andamento, os links da página não navegam (o fluxo ainda está em curso). */
function impedirNavegacaoEmEnvio(enviando: boolean) {
  return (evento: MouseEvent<HTMLAnchorElement>) => {
    if (enviando) evento.preventDefault();
  };
}

/**
 * P10-FE-02 · Cadastro da locadora, etapa 2: responsável e acesso (RF-53, RF-55, desenho 37). Valida pelo esquema
 * e envia tudo de uma vez (POST /api/empresas + login automático do responsável). A senha só existe aqui e na
 * chamada: não vai para o provedor nem para o armazenamento do navegador. Ao sair, guarda só os dados sem senha.
 */
export default function CadastroLocadoraResponsavelPagina() {
  useTituloDocumento(MENSAGEM_LOCADORA_TITULO_PAGINA_RESPONSAVEL);
  const navigate = useNavigate();
  const { autenticado } = useAutenticacao();
  const {
    dadosEmpresa,
    dadosResponsavel,
    salvarDadosResponsavel,
    resultado,
    salvarResultado,
    errosServidor,
    definirErrosServidor,
    removerErrosServidor,
    registrarConflitos,
  } = useCadastroLocadora();
  const cadastro = useCadastrarLocadora();
  const entrar = useEntrar();
  const { reset: resetarCadastro } = cadastro;
  const { reset: resetarLogin } = entrar;

  // `enviando` (estado) controla a tela. `emEnvio` (ref síncrona) impede um segundo envio antes do próximo
  // render: Enter repetido ou duplo clique.
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
    control,
    subscribe,
    getFieldState,
    clearErrors,
    formState: { errors },
  } = useForm<FormularioResponsavel>({
    // Dados já digitados (ao voltar da etapa 1) vencem; a senha nunca é guardada, então volta vazia.
    defaultValues: { ...DADOS_RESPONSAVEL_INICIAL, ...dadosResponsavel, senha: '', confirmarSenha: '' },
    resolver: zodResolver(ESQUEMA_RESPONSAVEL),
  });

  const senhaDigitada = useWatch({ control, name: 'senha' });
  const confirmacaoDigitada = useWatch({ control, name: 'confirmarSenha' });

  // Ao sair da tela, esquece as mutações (senha e dados não ficam em cache) e guarda em memória só os dados do
  // responsável, sem a senha. A ref `montada` impede navegar ou avisar depois que a tela desmontou.
  useEffect(() => {
    montada.current = true;
    return () => {
      montada.current = false;
      salvarDadosResponsavel(extrairDadosResponsavel(getValues()));
      resetarCadastro();
      resetarLogin();
    };
  }, [getValues, salvarDadosResponsavel, resetarCadastro, resetarLogin]);

  // Editar um campo com erro vindo do servidor limpa o erro daquele campo (a validação normal segue valendo).
  useEffect(() => {
    return subscribe({
      formState: { values: true },
      callback: ({ name, type }) => {
        if (type !== 'change' || name === undefined || !ehCampoResponsavel(name)) return;
        const tipoErro = getFieldState(name).error?.type;
        if (tipoErro === 'server' || tipoErro === TIPO_ERRO_DUPLICADO_LOCADORA) clearErrors(name);
      },
    });
  }, [subscribe, getFieldState, clearErrors]);

  /** Mostra os erros desta etapa no formulário e leva o foco ao primeiro, na ordem da tela. */
  const aplicarErrosNoFormulario = useCallback(
    (erros: readonly ErroCampoLocadora[]) => {
      const desta = erros.filter((erro) => erro.etapa === 2 && ehCampoResponsavel(erro.campo));
      for (const erro of desta) {
        if (ehCampoResponsavel(erro.campo)) setError(erro.campo, { type: 'server', message: erro.mensagem });
      }
      const primeiro = CAMPOS_RESPONSAVEL.find((campo) => desta.some((erro) => erro.campo === campo));
      if (primeiro !== undefined) setFocus(primeiro);
    },
    [setError, setFocus],
  );

  // Erros da etapa 2 que esperavam esta tela (o envio apontou campos da etapa 1 e da 2 de uma vez, e a pessoa
  // foi primeiro para a etapa 1). Só aplica quando não resta erro da etapa 1.
  useEffect(() => {
    const erros = errosDaEtapaLocadora(2, errosServidor);
    if (erros.length === 0) return;
    aplicarErrosNoFormulario(erros);
    removerErrosServidor(2);
  }, [errosServidor, removerErrosServidor, aplicarErrosNoFormulario]);

  /**
   * Erro do envio. Campo da etapa 1 (CNPJ, e-mail da empresa, dados da sede): volta para a etapa 1 com o erro
   * no campo. CPF do responsável: mostra aqui. Sem campo: alerta geral (404, 429, rede, 500).
   */
  function tratarErroCadastro(erro: unknown, requisicao: RequisicaoCadastroLocadora) {
    const plano = interpretarErroCadastroLocadora(erro);
    registrarConflitos(conflitosDoEnvio(plano, requisicao));

    if (plano.erros.some((item) => item.etapa === 1)) {
      definirErrosServidor(plano.erros);
      navigate(CAMINHOS.cadastroLocadoraEmpresa);
      return;
    }

    flushSync(() => {
      setEnviando(false);
      setAlertaGeral(plano.mensagemGeral);
    });

    if (plano.erros.length > 0) {
      aplicarErrosNoFormulario(plano.erros);
      return;
    }
    alertaRef.current?.focus();
  }

  /**
   * Envio já validado. Um POST só; se a conta foi criada, o resultado vai para o provedor (a etapa 3 mostra) e
   * a tela segue para a confirmação. Se o login automático falhou, a etapa 3 oferece "Fazer login".
   */
  async function enviar(dados: FormularioResponsavel) {
    if (emEnvio.current || dadosEmpresa === null) return;
    emEnvio.current = true;
    setAlertaGeral(null);
    setEnviando(true);

    const requisicao = montarRequisicaoCadastroLocadora(dadosEmpresa, dados);
    const resultadoEnvio = await executarCadastroLocadora(
      {
        cadastrar: (corpo) => cadastro.mutateAsync(corpo),
        entrar: (credenciais) => entrar.mutateAsync(credenciais),
      },
      requisicao,
    );

    if (resultadoEnvio.tipo === 'criado') {
      salvarResultado({
        empresa: resultadoEnvio.empresa,
        emailAcesso: requisicao.responsavelEmail,
        loginAutomatico: resultadoEnvio.loginAutomatico,
      });
      if (montada.current) navigate(CAMINHOS.cadastroLocadoraConfirmacao, { replace: true });
      return;
    }

    emEnvio.current = false;
    if (!montada.current) return;
    tratarErroCadastro(resultadoEnvio.erro, requisicao);
  }

  // Sem os dados da etapa 1 (ex.: a página foi recarregada), não há o que enviar: volta para a etapa 1.
  if (dadosEmpresa === null) {
    return <Navigate to={CAMINHOS.cadastroLocadoraEmpresa} replace />;
  }

  // Conta já criada: não se refaz o POST. Vai para a confirmação. Durante o envio não redireciona, senão o
  // login automático mandaria a pessoa embora antes da confirmação.
  if (resultado !== null && !enviando) {
    return <Navigate to={CAMINHOS.cadastroLocadoraConfirmacao} replace />;
  }

  // Quem já tem sessão não precisa de cadastro. Durante o envio, não redireciona (o login automático abre a sessão).
  if (autenticado && !enviando) {
    return <Navigate to={CAMINHOS.inicio} replace />;
  }

  return (
    <div className="mx-auto w-full max-w-4xl px-6 py-8">
      <div className="mb-6 flex flex-wrap items-center justify-between gap-2">
        <nav aria-label={MENSAGEM_CADASTRO_TRILHA_ROTULO}>
          <ol className="flex flex-wrap items-center gap-1 text-corpo-denso text-status-neutro">
            <li>
              <Link
                to={CAMINHOS.inicio}
                aria-disabled={enviando || undefined}
                onClick={impedirNavegacaoEmEnvio(enviando)}
                className={`flex items-center gap-1 rounded transition-colors hover:text-navy ${FOCO_LINK}`}
              >
                <Icone nome="arrow_back" tamanho={16} />
                {MENSAGEM_CADASTRO_VOLTAR_INICIO}
              </Link>
            </li>
            <li aria-hidden="true" className="text-borda-forte">
              /
            </li>
            <li aria-current="page" className="text-navy">
              {MENSAGEM_CADASTRO_ABERTURA}
            </li>
          </ol>
        </nav>
        <span className="inline-flex items-center gap-1.5 rounded-sm bg-steel-suave px-2 py-1 text-legenda uppercase tracking-wider text-navy">
          <span aria-hidden="true" className="h-1.5 w-1.5 rounded-full bg-orange" />
          <span className="font-semibold">{MENSAGEM_LOCADORA_ETIQUETA_PESSOA_JURIDICA}</span>
        </span>
      </div>

      <section className="overflow-hidden rounded-lg bg-branco shadow-destaque">
        <IndicadorEtapasDesenhoLocadora etapaAtual={2} />

        <div className="px-6 pb-6 pt-8 md:px-8 md:pb-8">
          <div className="mb-6">
            <h1 className="text-titulo tracking-tight text-navy">{MENSAGEM_LOCADORA_TITULO_RESPONSAVEL}</h1>
            <p className="mt-1 text-corpo text-status-neutro">{MENSAGEM_LOCADORA_SUBTITULO_RESPONSAVEL}</p>
          </div>

          <form
            noValidate
            onSubmit={(evento) => {
              void handleSubmit(enviar)(evento);
            }}
            className="space-y-6"
          >
            <fieldset disabled={enviando} className="min-w-0 space-y-6 border-0 p-0">
              <CamposIdentidadeResponsavel registrar={register} erros={errors} />
              <BlocoSenhaResponsavel
                registrar={register}
                erros={errors}
                senha={senhaDigitada}
                confirmacao={confirmacaoDigitada}
              />
              <AceiteTermosResponsavel registrar={register} erro={errors.aceiteTermos?.message} />
            </fieldset>

            <div className="flex flex-col gap-6 pt-2">
              <AlertaGeral ref={alertaRef} mensagem={alertaGeral} />

              <div className="flex flex-col items-center justify-between gap-4 sm:flex-row">
                <Link
                  to={CAMINHOS.cadastroLocadoraEmpresa}
                  aria-disabled={enviando || undefined}
                  onClick={impedirNavegacaoEmEnvio(enviando)}
                  className={`flex h-10 w-full items-center justify-center gap-1 rounded-sm bg-steel-suave px-6 text-corpo text-navy transition-colors hover:bg-steel-alto sm:w-auto ${FOCO_LINK}`}
                >
                  <Icone nome="arrow_back" tamanho={18} />
                  {MENSAGEM_LOCADORA_BOTAO_VOLTAR_EMPRESA}
                </Link>

                <Botao
                  type="submit"
                  tamanho="grande"
                  iconeDireita="arrow_forward"
                  carregando={enviando}
                  className="w-full !h-11 !rounded-sm !px-8 shadow-md sm:w-auto"
                >
                  {MENSAGEM_LOCADORA_BOTAO_CRIAR_CONTA}
                </Botao>
              </div>
            </div>
          </form>
        </div>

        <div className="flex flex-col items-center justify-between gap-1 bg-steel-fundo px-6 py-2 text-status-neutro sm:flex-row md:px-8">
          <span className="flex items-center gap-1.5 text-corpo">
            <Icone nome="lock" tamanho={16} className="text-status-pendente" />
            {MENSAGEM_LOCADORA_FAIXA_SEGURANCA_RESPONSAVEL}
          </span>
          <span className="font-mono text-micro font-normal tracking-normal">{MENSAGEM_LOCADORA_FAIXA_ETAPA_2}</span>
        </div>
      </section>
    </div>
  );
}
