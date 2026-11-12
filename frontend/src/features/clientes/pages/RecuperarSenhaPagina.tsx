import { useEffect, useRef, useState } from 'react';
import { Controller, useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { flushSync } from 'react-dom';
import { Link, useLocation } from 'react-router-dom';
import { Botao, Campo } from '@/components/ui';
import { SeletorPerfil } from '@/features/clientes/acesso/SeletorPerfil';
import { useSolicitarRecuperacaoSenha } from '@/features/clientes/hooks/useSolicitarRecuperacaoSenha';
import { AlertaRecuperacao } from '@/features/clientes/recuperacao/AlertaRecuperacao';
import { CartaoRecuperacao } from '@/features/clientes/recuperacao/CartaoRecuperacao';
import { ConfirmacaoRecuperacao } from '@/features/clientes/recuperacao/ConfirmacaoRecuperacao';
import { esquemaSolicitacao, type ValoresSolicitacao } from '@/features/clientes/recuperacao/esquemaSolicitacao';
import {
  CAMPOS_SOLICITACAO,
  decidirErroSolicitacao,
  montarRequisicaoSolicitacao,
  perfilInicialRecuperacao,
} from '@/features/clientes/recuperacao/fluxoRecuperacao';
import { montarEstadoRecuperacao } from '@/features/clientes/recuperacao/estadoRecuperacao';
import { useTituloDocumento } from '@/hooks/useTituloDocumento';
import { formatarCpf } from '@/lib/formatters';
import {
  ACESSO_ROTULO_CPF,
  RECUPERACAO_BOTAO_ENVIAR,
  RECUPERACAO_LINK_VOLTAR_LOGIN,
  RECUPERACAO_SUBTITULO,
  RECUPERACAO_TITULO,
} from '@/lib/mensagens';
import { CAMINHOS, obterCaminhoRetorno } from '@/routes/caminhos';
import type { PerfilApi } from '@/types/autenticacao';

/**
 * N1 (RF-49) · Esqueci minha senha: pedido do link de redefinição, em /recuperar-senha.
 * A resposta é sempre a mesma (202): a tela nunca diz se a conta existe.
 * Quem já tem sessão pode abrir a tela normalmente (trocar a senha em outro dispositivo é um caso válido).
 * O perfil marcado e o caminho de retorno do login ficam em `location.state` e voltam com "Voltar ao login".
 */
export default function RecuperarSenhaPagina() {
  useTituloDocumento(RECUPERACAO_TITULO);
  const localizacao = useLocation();
  const solicitar = useSolicitarRecuperacaoSenha();
  const { reset: resetarSolicitacao } = solicitar;
  /** Caminho de retorno do login, já validado (vazio quando não há). */
  const deEntrada = obterCaminhoRetorno(localizacao.state, '');

  // `enviando` (estado) controla a tela. `emEnvio` (ref síncrona) impede um segundo envio
  // antes do próximo render: Enter repetido ou duplo clique.
  const [enviando, setEnviando] = useState(false);
  const [enviado, setEnviado] = useState(false);
  /** Perfil do pedido enviado: a confirmação devolve esse perfil ao login. */
  const [perfilEnviado, setPerfilEnviado] = useState<PerfilApi>('CLIENTE');
  const [alertaGeral, setAlertaGeral] = useState<string | null>(null);
  const emEnvio = useRef(false);
  const montada = useRef(false);
  const alertaRef = useRef<HTMLDivElement>(null);
  const tituloConfirmacaoRef = useRef<HTMLHeadingElement>(null);

  // Ao sair da tela, descarta a mutação (o CPF e a resposta não ficam em cache).
  useEffect(() => {
    montada.current = true;
    return () => {
      montada.current = false;
      resetarSolicitacao();
    };
  }, [resetarSolicitacao]);

  const {
    control,
    register,
    handleSubmit,
    setError,
    setFocus,
    reset: resetarFormulario,
    formState: { errors },
  } = useForm<ValoresSolicitacao>({
    resolver: zodResolver(esquemaSolicitacao),
    defaultValues: { perfil: perfilInicialRecuperacao(localizacao.state), cpf: '' },
  });
  const perfilMarcado = useWatch({ control, name: 'perfil' });

  /**
   * Erro do envio: 400 com campo vai para o campo; o resto (429, rede) vira alerta geral.
   * Só mexe no formulário depois de ele ser reabilitado, para o foco funcionar.
   */
  function tratarErro(erro: unknown) {
    const decisao = decidirErroSolicitacao(erro);

    if (decisao.tipo === 'campos') {
      flushSync(() => setEnviando(false));
      for (const campo of CAMPOS_SOLICITACAO) {
        const mensagem = decisao.campos[campo];
        if (mensagem !== undefined) setError(campo, { type: 'server', message: mensagem });
      }
      const primeiroCampo = CAMPOS_SOLICITACAO.find((campo) => decisao.campos[campo] !== undefined);
      if (primeiroCampo !== undefined) setFocus(primeiroCampo);
      return;
    }

    flushSync(() => {
      setEnviando(false);
      setAlertaGeral(decisao.mensagem);
    });
    alertaRef.current?.focus();
  }

  /** Envio já validado pelo React Hook Form. Só roda dentro do evento de submit. */
  async function enviar(dados: ValoresSolicitacao) {
    if (emEnvio.current) return;
    emEnvio.current = true;
    setAlertaGeral(null);
    setEnviando(true);

    try {
      await solicitar.mutateAsync(montarRequisicaoSolicitacao(dados));
    } catch (erro) {
      if (!montada.current) return;
      emEnvio.current = false;
      tratarErro(erro);
      return;
    }

    if (!montada.current) return;
    // Sucesso: o CPF sai da memória do formulário e a confirmação recebe o foco.
    resetarFormulario();
    flushSync(() => {
      setEnviando(false);
      setPerfilEnviado(dados.perfil);
      setEnviado(true);
    });
    tituloConfirmacaoRef.current?.focus();
  }

  if (enviado) {
    return (
      <CartaoRecuperacao>
        <ConfirmacaoRecuperacao
          tituloRef={tituloConfirmacaoRef}
          estadoVoltar={montarEstadoRecuperacao(perfilEnviado, deEntrada)}
        />
      </CartaoRecuperacao>
    );
  }

  return (
    <CartaoRecuperacao>
      <h1 className="text-titulo text-navy">{RECUPERACAO_TITULO}</h1>
      <p className="mt-2 text-corpo text-texto-suave">{RECUPERACAO_SUBTITULO}</p>

      <form
        noValidate
        onSubmit={(evento) => {
          void handleSubmit(enviar)(evento);
        }}
        className="mt-8 flex flex-col gap-5"
      >
        <fieldset disabled={enviando} className="flex min-w-0 flex-col gap-5 border-0 p-0">
          <Controller
            name="perfil"
            control={control}
            render={({ field }) => (
              <SeletorPerfil
                valor={field.value}
                aoMudar={(novoPerfil) => field.onChange(novoPerfil)}
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
            {...register('cpf')}
          />
        </fieldset>

        <AlertaRecuperacao alertaRef={alertaRef} mensagem={alertaGeral} />

        <Botao type="submit" larguraTotal carregando={enviando} iconeDireita="arrow_forward">
          {RECUPERACAO_BOTAO_ENVIAR}
        </Botao>
      </form>

      <p className="mt-6 text-center text-corpo-denso">
        <Link
          to={CAMINHOS.entrar}
          state={montarEstadoRecuperacao(perfilMarcado, deEntrada)}
          className="font-semibold text-navy underline underline-offset-4 hover:text-navy-claro"
        >
          {RECUPERACAO_LINK_VOLTAR_LOGIN}
        </Link>
      </p>
    </CartaoRecuperacao>
  );
}
