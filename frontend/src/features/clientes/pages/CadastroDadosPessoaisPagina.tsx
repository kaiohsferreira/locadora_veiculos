import { forwardRef, useEffect, useRef, useState, type MouseEvent } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { flushSync } from 'react-dom';
import { Link, Navigate, useNavigate } from 'react-router-dom';
import { Botao, BotaoLink, Icone, useAviso } from '@/components/ui';
import { useTituloDocumento } from '@/hooks/useTituloDocumento';
import { useCadastrarCliente } from '@/features/clientes/hooks/useCadastrarCliente';
import { useEntrar } from '@/features/clientes/hooks/useEntrar';
import {
  CamposDadosPessoais,
  CamposEndereco,
  CamposHabilitacao,
  CamposSenha,
} from '@/features/clientes/cadastro/CamposCadastro';
import { IndicadorEtapas } from '@/features/clientes/cadastro/IndicadorEtapas';
import {
  escolherCampoDoConflito,
  executarCadastro,
  primeiroCampoNaTela,
} from '@/features/clientes/cadastro/fluxoCadastro';
import {
  lerPreenchimentoCadastro,
  type PreenchimentoCadastro,
} from '@/features/clientes/cadastro/preenchimentoCadastro';
import {
  ESQUEMA_CADASTRO,
  FORMULARIO_INICIAL,
  type FormularioCadastro,
} from '@/features/clientes/cadastro/esquemaCadastro';
import { aplicarErrosDeCampo, obterMensagemErro } from '@/lib/erros';
import { limparPreenchimentoGoogle, obterPreenchimentoGoogle } from '@/lib/preenchimentoGoogle';
import {
  GOOGLE_CADASTRO_NECESSARIO,
  MENSAGEM_CADASTRO_ABERTURA,
  MENSAGEM_CADASTRO_AVISO_ANALISE,
  MENSAGEM_CADASTRO_BOTAO_CONTINUAR,
  MENSAGEM_CADASTRO_DECLARACAO_TITULAR,
  MENSAGEM_CADASTRO_JA_POSSUI_CADASTRO,
  MENSAGEM_CADASTRO_LEGENDA_DADOS,
  MENSAGEM_CADASTRO_LINK_LOGIN,
  MENSAGEM_CADASTRO_SUBTITULO,
  MENSAGEM_CADASTRO_SUCESSO_COM_LOGIN,
  MENSAGEM_CADASTRO_SUCESSO_SEM_LOGIN,
  MENSAGEM_CADASTRO_TITULO_CARTAO,
  MENSAGEM_CADASTRO_TITULO_PAGINA,
  MENSAGEM_CADASTRO_TRILHA_ROTULO,
  MENSAGEM_CADASTRO_VOLTAR_INICIO,
} from '@/lib/mensagens';
import { useAutenticacao } from '@/routes/autenticacao/useAutenticacao';
import { CAMINHOS } from '@/routes/caminhos';
import { ErroApi } from '@/types/api';

/** Campos que o backend pode apontar em um 400 (os nomes do ClienteVO). */
const CAMPOS_PAYLOAD = [
  'nome',
  'cpf',
  'dataNascimento',
  'email',
  'telefone',
  'cnhNumero',
  'cnhCategoria',
  'cnhValidade',
  'cep',
  'logradouro',
  'numero',
  'complemento',
  'bairro',
  'cidade',
  'uf',
  'senha',
] as const;

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
 * P2-FE-02 · Cadastro do cliente, etapa de dados pessoais (RF-07).
 * Um único envio: dados pessoais, CNH, endereço (opcional) e senha. Depois do 201, o
 * POST não devolve token, então a tela entra com o login do cliente.
 */
export default function CadastroDadosPessoaisPagina() {
  useTituloDocumento(MENSAGEM_CADASTRO_TITULO_PAGINA);
  const navigate = useNavigate();
  const aviso = useAviso();
  const { autenticado } = useAutenticacao();
  const cadastro = useCadastrarCliente();
  const entrar = useEntrar();
  const { reset: resetarCadastro } = cadastro;
  const { reset: resetarLogin } = entrar;

  // Lê o pré-preenchimento do Google na montagem, só uma vez. O inicializador do estado roda duas
  // vezes no StrictMode, mas as duas leituras veem o mesmo valor: a limpeza fica no efeito abaixo,
  // depois do mount, e o estado (já capturado) não se perde na remontagem simulada.
  const [preenchimentoGoogle] = useState<PreenchimentoCadastro>(() =>
    lerPreenchimentoCadastro(obterPreenchimentoGoogle()),
  );
  const veioDoGoogle = preenchimentoGoogle.email !== '';

  // `enviando` (estado) controla a tela. `emEnvio` (ref síncrona) impede um segundo envio
  // antes do próximo render: Enter repetido ou duplo clique.
  const [enviando, setEnviando] = useState(false);
  const [alertaGeral, setAlertaGeral] = useState<string | null>(null);
  const emEnvio = useRef(false);
  const montada = useRef(false);
  const alertaRef = useRef<HTMLDivElement>(null);

  // Consome o pré-preenchimento: a memória é limpa depois que a tela já capturou os valores.
  useEffect(() => {
    limparPreenchimentoGoogle();
  }, []);

  // Ao sair da tela, esquece as mutações (senha e dados pessoais não ficam em cache).
  // A ref `montada` impede navegar ou avisar depois que a tela desmontou.
  useEffect(() => {
    montada.current = true;
    return () => {
      montada.current = false;
      resetarCadastro();
      resetarLogin();
    };
  }, [resetarCadastro, resetarLogin]);

  const {
    register,
    handleSubmit,
    setError,
    setFocus,
    formState: { errors },
  } = useForm<FormularioCadastro>({
    // Nome e e-mail vindos do login com Google são só valores iniciais: a pessoa pode editar.
    defaultValues: { ...FORMULARIO_INICIAL, ...preenchimentoGoogle },
    resolver: zodResolver(ESQUEMA_CADASTRO),
  });

  /**
   * Erro do POST: 409 vai para o campo citado (CNH ou CPF); 400 vai para os campos que o
   * backend apontou; o restante vira alerta geral. O foco vai ao primeiro campo com erro
   * na ordem da tela, ou ao alerta geral, depois de o formulário ser reabilitado.
   */
  function tratarErroCadastro(erro: unknown) {
    const mensagem = obterMensagemErro(erro);
    const status = erro instanceof ErroApi ? erro.status : 0;
    const alvos: string[] = [];

    if (status === 409) {
      const campo = escolherCampoDoConflito(mensagem);
      if (campo !== null) {
        setError(campo, { type: 'server', message: mensagem });
        alvos.push(campo);
      }
    } else if (status === 400) {
      aplicarErrosDeCampo(erro, CAMPOS_PAYLOAD, (campo, mensagemCampo) => {
        setError(campo, { type: 'server', message: mensagemCampo });
        alvos.push(campo);
      });
    }

    const primeiroCampo = primeiroCampoNaTela(alvos);
    flushSync(() => {
      setEnviando(false);
      if (primeiroCampo === undefined) setAlertaGeral(mensagem);
    });

    if (primeiroCampo !== undefined) {
      setFocus(primeiroCampo);
      return;
    }
    alertaRef.current?.focus();
  }

  /** Envio já validado pelo React Hook Form. Só roda dentro do evento de submit. */
  async function enviar(dados: FormularioCadastro) {
    if (emEnvio.current) return;
    emEnvio.current = true;
    setAlertaGeral(null);
    setEnviando(true);

    const resultado = await executarCadastro(
      {
        cadastrar: (requisicao) => cadastro.mutateAsync(requisicao),
        entrar: (requisicao) => entrar.mutateAsync(requisicao),
      },
      dados,
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
    navigate(CAMINHOS.cadastroDocumentos, { replace: true });
  }

  // Quem já tem sessão não precisa de cadastro. Durante o fluxo (`enviando`) não redireciona,
  // senão o login automático mandaria o usuário para o início em vez dos documentos.
  if (autenticado && !enviando) {
    return <Navigate to={CAMINHOS.inicio} replace />;
  }

  return (
    <div className="mx-auto w-full max-w-3xl px-4 py-8 sm:px-6 lg:px-8">
      <nav aria-label={MENSAGEM_CADASTRO_TRILHA_ROTULO}>
        <ol className="flex flex-wrap items-center gap-2 text-corpo-denso text-texto-suave">
          <li>
            <BotaoLink
              to={CAMINHOS.inicio}
              variante="fantasma"
              tamanho="pequeno"
              icone="arrow_back"
              aria-disabled={enviando || undefined}
              onClick={impedirNavegacaoEmEnvio(enviando)}
            >
              {MENSAGEM_CADASTRO_VOLTAR_INICIO}
            </BotaoLink>
          </li>
          <li aria-hidden="true">/</li>
          <li aria-current="page">{MENSAGEM_CADASTRO_ABERTURA}</li>
        </ol>
      </nav>

      <section className="mt-4 rounded-cartao bg-branco shadow-cartao">
        {veioDoGoogle && (
          <output className="flex items-start gap-3 rounded-t-cartao border-b border-steel bg-steel-claro px-6 py-4 text-corpo-denso text-navy">
            <Icone nome="info" tamanho={20} className="mt-0.5 text-steel" />
            <span>{GOOGLE_CADASTRO_NECESSARIO}</span>
          </output>
        )}
        <header className="border-b border-borda px-6 py-4">
          <h1 className="text-secao text-navy">{MENSAGEM_CADASTRO_TITULO_CARTAO}</h1>
          <p className="mt-1 text-corpo-denso text-texto-suave">{MENSAGEM_CADASTRO_SUBTITULO}</p>
        </header>

        <div className="border-b border-borda px-6 py-4">
          <IndicadorEtapas />
        </div>

        <form
          noValidate
          onSubmit={(evento) => {
            void handleSubmit(enviar)(evento);
          }}
          className="flex flex-col"
        >
          <fieldset disabled={enviando} className="flex min-w-0 flex-col gap-8 border-0 p-6">
            <legend className="sr-only">{MENSAGEM_CADASTRO_LEGENDA_DADOS}</legend>

            <div className="flex items-start gap-3 rounded-md border border-steel bg-steel-claro p-4 text-corpo-denso text-navy">
              <Icone nome="info" tamanho={20} className="mt-0.5 text-steel" />
              <p>{MENSAGEM_CADASTRO_AVISO_ANALISE}</p>
            </div>

            <CamposDadosPessoais registrar={register} erros={errors} />
            <CamposHabilitacao registrar={register} erros={errors} />
            <CamposEndereco registrar={register} erros={errors} />
            <CamposSenha registrar={register} erros={errors} />
          </fieldset>

          <div className="flex flex-col gap-4 border-t border-borda px-6 py-6">
            <AlertaGeral ref={alertaRef} mensagem={alertaGeral} />

            <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
              <p className="text-corpo-denso text-texto-suave">
                {MENSAGEM_CADASTRO_JA_POSSUI_CADASTRO}{' '}
                <Link
                  to={CAMINHOS.entrar}
                  aria-disabled={enviando || undefined}
                  onClick={impedirNavegacaoEmEnvio(enviando)}
                  className="font-semibold text-navy underline underline-offset-2 hover:text-navy-claro"
                >
                  {MENSAGEM_CADASTRO_LINK_LOGIN}
                </Link>
              </p>

              <div className="flex flex-col gap-2 sm:items-end">
                <Botao type="submit" iconeDireita="arrow_forward" carregando={enviando}>
                  {MENSAGEM_CADASTRO_BOTAO_CONTINUAR}
                </Botao>
                <p className="max-w-sm text-legenda text-texto-suave sm:text-right">
                  {MENSAGEM_CADASTRO_DECLARACAO_TITULAR}
                </p>
              </div>
            </div>
          </div>
        </form>
      </section>
    </div>
  );
}
