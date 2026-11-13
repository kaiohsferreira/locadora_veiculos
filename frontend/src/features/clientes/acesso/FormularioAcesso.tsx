import { useEffect, useState, type MouseEvent } from 'react';
import { useForm, Controller, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Link, useLocation } from 'react-router-dom';
import { Botao, Campo, CampoSenha, Icone } from '@/components/ui';
import { aplicarErrosDeCampo, obterErroDeCampo, obterMensagemErro } from '@/lib/erros';
import { formatarCpf } from '@/lib/formatters';
import {
  ACESSO_BOTAO_ENTRAR,
  ACESSO_EQUIPE_SEM_CADASTRO,
  ACESSO_LINK_CRIAR_CONTA,
  ACESSO_LINK_ESQUECI_SENHA,
  ACESSO_PERGUNTA_SEM_CONTA,
  ACESSO_PLACEHOLDER_CPF,
  ACESSO_ROTULO_CPF_CADASTRADO,
  ACESSO_ROTULO_SENHA_ACESSO,
  ACESSO_SUBTITULO,
  ACESSO_TITULO,
} from '@/lib/mensagens';
import { BlocoGoogle } from '@/features/clientes/google/BlocoGoogle';
import { lerEstadoRecuperacao, montarEstadoRecuperacao } from '@/features/clientes/recuperacao/estadoRecuperacao';
import { CAMINHOS, obterCaminhoRetorno } from '@/routes/caminhos';
import { ErroApi } from '@/types/api';
import type { PerfilApi } from '@/types/autenticacao';
import { CampoLembrarDeMim } from './CampoLembrarDeMim';
import { esquemaAcesso, type EntradaAcesso, type ValoresAcesso } from './esquemaAcesso';
import { SeletorPerfil } from './SeletorPerfil';

/** Campos que o backend pode apontar num erro 400 (ProblemDetails.errors). */
const CAMPOS_ACESSO = ['perfil', 'cpf', 'senha'] as const;

export interface FormularioAcessoProps {
  /** Perfil marcado ao abrir a tela (Cliente ou Equipe). Um perfil vindo da recuperação de senha tem prioridade. */
  perfilInicial: PerfilApi;
  /** Envio em andamento: bloqueia o formulário e mostra o carregamento no botão. */
  enviando: boolean;
  /** Erro do último envio, ou null. A mensagem é exibida já convertida por `obterMensagemErro`. */
  erroEnvio: Error | null;
  /** Chamado só com valores válidos (a validação com Zod roda antes). */
  aoEnviar: (valores: ValoresAcesso) => void;
}

/** Com um envio em andamento, o link não navega (o fluxo de login ainda está em curso). */
function impedirNavegacaoEmEnvio(ocupado: boolean) {
  return (evento: MouseEvent<HTMLAnchorElement>) => {
    if (ocupado) evento.preventDefault();
  };
}

/**
 * Formulário de acesso: perfil, CPF, senha e lembrar de mim. Os textos e o link de criar conta
 * ficam aqui; o painel da marca fica em outro componente. A página decide o que acontece após o login.
 */
export function FormularioAcesso({ perfilInicial, enviando, erroEnvio, aoEnviar }: FormularioAcessoProps) {
  const localizacao = useLocation();
  // Volta da recuperação de senha: o perfil marcado lá retorna aqui (o estado traz só campos conhecidos).
  const estadoEntrada = lerEstadoRecuperacao(localizacao.state);
  const caminhoRetorno = obterCaminhoRetorno(localizacao.state, '');

  const {
    register,
    handleSubmit,
    control,
    setError,
    setValue,
    setFocus,
    formState: { errors },
  } = useForm<EntradaAcesso, unknown, ValoresAcesso>({
    resolver: zodResolver(esquemaAcesso),
    defaultValues: { perfil: estadoEntrada.perfil ?? perfilInicial, cpf: '', senha: '', lembrar: false },
  });
  const perfil = useWatch({ control, name: 'perfil' });
  const lembrar = useWatch({ control, name: 'lembrar' });
  /** Erro que a pessoa já dispensou (ao voltar a digitar). Um novo envio gera outro erro. */
  const [erroDispensado, setErroDispensado] = useState<Error | null>(null);
  /** Login com Google em andamento: o formulário de CPF fica bloqueado para não haver dois logins. */
  const [ocupadoGoogle, setOcupadoGoogle] = useState(false);
  const bloqueado = enviando || ocupadoGoogle;

  const erroApi = erroEnvio instanceof ErroApi ? erroEnvio : null;
  // Erro 400 com algum campo conhecido: a mensagem fica junto do campo, não no aviso geral.
  const campoComErro =
    erroApi?.status === 400
      ? CAMPOS_ACESSO.find((campo) => obterErroDeCampo(erroApi, campo) !== undefined)
      : undefined;
  const mensagemGeral =
    erroEnvio !== null && erroEnvio !== erroDispensado && campoComErro === undefined
      ? obterMensagemErro(erroEnvio)
      : null;

  // Depois do erro, o foco volta a um campo: o fieldset desabilitado durante o envio tira o foco.
  useEffect(() => {
    if (erroEnvio === null) return;
    if (erroApi?.status === 401) {
      setValue('senha', '');
      setFocus('senha');
      return;
    }
    if (erroApi?.status === 400) {
      aplicarErrosDeCampo(erroEnvio, CAMPOS_ACESSO, (campo, mensagem) => {
        setError(campo, { type: 'server', message: mensagem });
      });
    }
    setFocus(campoComErro ?? 'cpf');
  }, [erroEnvio, erroApi, campoComErro, setError, setFocus, setValue]);

  /** Some com o aviso geral quando a pessoa volta a digitar. */
  function limparAviso() {
    setErroDispensado(erroEnvio);
  }

  return (
    <div className="flex w-full flex-col">
      <div className="mb-8">
        <h1 className="mb-2 text-titulo-acesso text-navy">{ACESSO_TITULO}</h1>
        <p className="text-corpo-denso text-status-neutro">{ACESSO_SUBTITULO}</p>
      </div>

      <form noValidate onSubmit={handleSubmit(aoEnviar)} className="flex flex-col gap-5">
        <fieldset disabled={bloqueado} className="flex min-w-0 flex-col gap-5">
          <Controller
            name="perfil"
            control={control}
            render={({ field }) => (
              <SeletorPerfil
                valor={field.value}
                aoMudar={(novoPerfil) => {
                  field.onChange(novoPerfil);
                  limparAviso();
                }}
                erro={errors.perfil?.message}
                campoRef={field.ref}
              />
            )}
          />

          <Campo
            rotulo={ACESSO_ROTULO_CPF_CADASTRADO}
            variante="portal"
            iconeInicial="fingerprint"
            ocultarAsterisco
            placeholder={ACESSO_PLACEHOLDER_CPF}
            obrigatorio
            inputMode="numeric"
            autoComplete="username"
            maxLength={14}
            mascara={formatarCpf}
            erro={errors.cpf?.message}
            {...register('cpf', { onChange: limparAviso })}
          />

          {/* O link fica na linha do rótulo (antes do campo no Tab), por isso é uma ação do próprio CampoSenha. */}
          <CampoSenha
            rotulo={ACESSO_ROTULO_SENHA_ACESSO}
            variante="portal"
            iconeInicial="lock"
            ocultarAsterisco
            obrigatorio
            autoComplete="current-password"
            erro={errors.senha?.message}
            acaoRotulo={
              <Link
                to={CAMINHOS.recuperarSenha}
                state={montarEstadoRecuperacao(perfil, caminhoRetorno)}
                aria-disabled={bloqueado || undefined}
                onClick={impedirNavegacaoEmEnvio(bloqueado)}
                className="rounded text-rotulo text-navy transition-colors hover:text-status-pendente hover:underline focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-steel"
              >
                {ACESSO_LINK_ESQUECI_SENHA}
              </Link>
            }
            {...register('senha', { onChange: limparAviso })}
          />

          <CampoLembrarDeMim {...register('lembrar', { onChange: limparAviso })} />
        </fieldset>

        {mensagemGeral && (
          <div
            role="alert"
            className="flex items-start gap-2 rounded bg-status-perigo-fundo px-4 py-3 text-corpo-denso font-medium text-status-perigo"
          >
            <Icone nome="error" tamanho={18} className="mt-px" />
            <span>{mensagemGeral}</span>
          </div>
        )}

        <Botao
          type="submit"
          tamanho="grande"
          larguraTotal
          carregando={enviando}
          disabled={ocupadoGoogle}
          iconeDireita="arrow_forward"
          className="mt-2 !rounded shadow-md hover:shadow-lg"
        >
          {ACESSO_BOTAO_ENTRAR}
        </Botao>
      </form>

      {/*
        Bloco "Continuar com o Google" (N3, RF-51), abaixo do botão de entrar. Fica fora do <form>:
        o campo de e-mail do modo de teste não pode disparar o envio do CPF com Enter.
      */}
      {/*
        Área abaixo do botão principal com altura mínima igual nos dois perfis (evita o salto do bloco, que é
        centralizado na vertical). 149px = bloco do Google (28 de margem + 73) + linha "Criar conta" (28 de
        margem + 20 de linha). Flex coluna para as margens de cima não escaparem do mínimo.
      */}
      <div className="flex min-h-[149px] flex-col">
      <BlocoGoogle perfil={perfil} lembrar={lembrar === true} aoOcupadoChange={setOcupadoGoogle} />

      <div className="mt-7 text-center text-corpo-denso text-status-neutro">
        {perfil === 'CLIENTE' ? (
          <p>
            {ACESSO_PERGUNTA_SEM_CONTA}{' '}
            <Link
              to={CAMINHOS.cadastroDadosPessoais}
              className="ml-1 rounded font-semibold text-status-pendente decoration-2 underline-offset-4 hover:underline focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-steel"
            >
              {ACESSO_LINK_CRIAR_CONTA}
            </Link>
          </p>
        ) : (
          <p>{ACESSO_EQUIPE_SEM_CADASTRO}</p>
        )}
      </div>
      </div>
    </div>
  );
}
