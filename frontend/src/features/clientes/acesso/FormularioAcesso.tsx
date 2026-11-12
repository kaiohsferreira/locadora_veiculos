import { useEffect, useState } from 'react';
import { Controller, useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Link } from 'react-router-dom';
import { Botao, Campo, CampoSenha, Icone } from '@/components/ui';
import { aplicarErrosDeCampo, obterErroDeCampo, obterMensagemErro } from '@/lib/erros';
import { formatarCpf } from '@/lib/formatters';
import {
  ACESSO_BOTAO_ENTRAR,
  ACESSO_EQUIPE_SEM_CADASTRO,
  ACESSO_LINK_CRIAR_CONTA,
  ACESSO_PERGUNTA_SEM_CONTA,
  ACESSO_ROTULO_CPF,
  ACESSO_ROTULO_SENHA,
  ACESSO_SUBTITULO,
  ACESSO_TITULO,
} from '@/lib/mensagens';
import { CAMINHOS } from '@/routes/caminhos';
import { ErroApi } from '@/types/api';
import type { PerfilApi } from '@/types/autenticacao';
import { esquemaAcesso, type ValoresAcesso } from './esquemaAcesso';
import { SeletorPerfil } from './SeletorPerfil';

/** Campos que o backend pode apontar num erro 400 (ProblemDetails.errors). */
const CAMPOS_ACESSO = ['perfil', 'cpf', 'senha'] as const;

export interface FormularioAcessoProps {
  /** Perfil marcado ao abrir a tela (Cliente ou Equipe). */
  perfilInicial: PerfilApi;
  /** Envio em andamento: bloqueia o formulário e mostra o carregamento no botão. */
  enviando: boolean;
  /** Erro do último envio, ou null. A mensagem é exibida já convertida por `obterMensagemErro`. */
  erroEnvio: Error | null;
  /** Chamado só com valores válidos (a validação com Zod roda antes). */
  aoEnviar: (valores: ValoresAcesso) => void;
}

/**
 * Formulário de acesso: perfil, CPF e senha. Os textos e o link de criar conta ficam
 * aqui; o painel da marca fica em outro componente. A página decide o que acontece após o login.
 */
export function FormularioAcesso({ perfilInicial, enviando, erroEnvio, aoEnviar }: FormularioAcessoProps) {
  const {
    register,
    handleSubmit,
    control,
    setError,
    setValue,
    setFocus,
    formState: { errors },
  } = useForm<ValoresAcesso>({
    resolver: zodResolver(esquemaAcesso),
    defaultValues: { perfil: perfilInicial, cpf: '', senha: '' },
  });
  const perfil = useWatch({ control, name: 'perfil' });
  /** Erro que a pessoa já dispensou (ao voltar a digitar). Um novo envio gera outro erro. */
  const [erroDispensado, setErroDispensado] = useState<Error | null>(null);

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
      <h1 className="text-titulo text-navy">{ACESSO_TITULO}</h1>
      <p className="mt-2 text-corpo text-texto-suave">{ACESSO_SUBTITULO}</p>

      <form noValidate onSubmit={handleSubmit(aoEnviar)} className="mt-8 flex flex-col gap-5">
        <fieldset disabled={enviando} className="flex min-w-0 flex-col gap-5">
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
            rotulo={ACESSO_ROTULO_CPF}
            obrigatorio
            inputMode="numeric"
            autoComplete="username"
            maxLength={14}
            mascara={formatarCpf}
            erro={errors.cpf?.message}
            {...register('cpf', { onChange: limparAviso })}
          />

          <CampoSenha
            rotulo={ACESSO_ROTULO_SENHA}
            obrigatorio
            autoComplete="current-password"
            erro={errors.senha?.message}
            {...register('senha', { onChange: limparAviso })}
          />
        </fieldset>

        {mensagemGeral && (
          <div
            role="alert"
            className="flex items-start gap-2 rounded-md bg-status-perigo-fundo px-4 py-3 text-corpo-denso font-medium text-status-perigo"
          >
            <Icone nome="error" tamanho={18} className="mt-px" />
            <span>{mensagemGeral}</span>
          </div>
        )}

        <Botao type="submit" larguraTotal carregando={enviando} iconeDireita="arrow_forward">
          {ACESSO_BOTAO_ENTRAR}
        </Botao>
      </form>

      <div className="mt-6 text-center text-corpo-denso text-texto-suave">
        {perfil === 'CLIENTE' ? (
          <p>
            {ACESSO_PERGUNTA_SEM_CONTA}{' '}
            <Link
              to={CAMINHOS.cadastroDadosPessoais}
              className="font-semibold text-navy underline underline-offset-4 hover:text-navy-claro"
            >
              {ACESSO_LINK_CRIAR_CONTA}
            </Link>
          </p>
        ) : (
          <p>{ACESSO_EQUIPE_SEM_CADASTRO}</p>
        )}
      </div>
    </div>
  );
}
