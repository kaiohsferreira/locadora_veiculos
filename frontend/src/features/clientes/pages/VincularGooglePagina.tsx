import { useEffect, useRef, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { flushSync } from 'react-dom';
import { Navigate, useLocation, useNavigate } from 'react-router-dom';
import { Botao, BotaoLink, Campo, CampoSenha } from '@/components/ui';
import { resolverDestinoAposLogin } from '@/features/clientes/acesso/destinoAposLogin';
import { AlertaGoogle } from '@/features/clientes/google/AlertaGoogle';
import { esquemaVinculo, type ValoresVinculo } from '@/features/clientes/google/esquemaVinculo';
import {
  CAMPOS_VINCULO,
  decidirErroVinculo,
  montarRequisicaoVinculo,
} from '@/features/clientes/google/vinculoGoogle';
import { useVincularGoogle } from '@/features/clientes/hooks/useVincularGoogle';
import { useTituloDocumento } from '@/hooks/useTituloDocumento';
import {
  obterCredencialGoogle,
  limparCredencialGoogle,
  type CredencialGuardada,
} from '@/lib/credencialGoogle';
import { formatarCpf } from '@/lib/formatters';
import {
  ACESSO_ROTULO_CPF,
  ACESSO_ROTULO_SENHA,
  GOOGLE_VINCULAR_BOTAO,
  GOOGLE_VINCULAR_TITULO,
  GOOGLE_VINCULAR_TITULO_PAGINA,
  GOOGLE_VINCULAR_VOLTAR,
  mensagemVincularEmail,
} from '@/lib/mensagens';
import { CAMINHOS } from '@/routes/caminhos';

/** Alerta geral da tela. `voltarAoLogin` oferece o botão para sair da tela. */
interface AlertaVinculo {
  mensagem: string;
  voltarAoLogin: boolean;
}

/** Lê a credencial guardada. Sem e-mail, a tela não tem o que mostrar: trata como ausente. */
function lerCredencialInicial(): CredencialGuardada | null {
  const guardada = obterCredencialGoogle();
  return guardada !== null && guardada.email !== null ? guardada : null;
}

/**
 * N3 (RF-51) · Vincular conta Google: CPF e senha da conta AutoVia, em /entrar/vincular-google.
 * Só funciona com a credencial guardada em memória pelo login com Google. Sem ela (acesso direto
 * ou página recarregada), volta para /entrar. O vínculo só nasce depois de provar a senha (RN3.3b).
 */
export default function VincularGooglePagina() {
  useTituloDocumento(GOOGLE_VINCULAR_TITULO_PAGINA);
  const navigate = useNavigate();
  const localizacao = useLocation();
  const estadoRota: unknown = localizacao.state;
  // Lida uma vez, na montagem. Depois do sucesso a credencial sai da memória, e a tela não pode
  // redirecionar por isso antes de navegar ao destino.
  const [credencial] = useState<CredencialGuardada | null>(lerCredencialInicial);
  const vincular = useVincularGoogle();
  const { reset: resetarVinculo } = vincular;

  // `enviando` (estado) controla a tela. `emEnvio` (ref síncrona) impede um segundo envio
  // antes do próximo render: Enter repetido ou duplo clique.
  const [enviando, setEnviando] = useState(false);
  const [alerta, setAlerta] = useState<AlertaVinculo | null>(null);
  const emEnvio = useRef(false);
  const montada = useRef(false);
  const alertaRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    montada.current = true;
    return () => {
      montada.current = false;
      resetarVinculo();
      // O StrictMode desmonta e remonta a tela logo em seguida. A limpeza fica numa microtarefa:
      // só apaga a credencial se a tela não voltou e não está no meio de um envio. No envio, o
      // próprio sucesso já limpa a credencial.
      queueMicrotask(() => {
        if (!montada.current && !emEnvio.current) limparCredencialGoogle();
      });
    };
  }, [resetarVinculo]);

  const {
    register,
    handleSubmit,
    setError,
    setFocus,
    setValue,
    formState: { errors },
  } = useForm<ValoresVinculo>({
    resolver: zodResolver(esquemaVinculo),
    defaultValues: { cpf: '', senha: '' },
  });

  /**
   * Erro do envio: 400 de CPF ou senha vai para os campos; 401 de senha limpa a senha, foca nela e
   * mostra a mensagem ligada ao campo (aria-describedby); o resto vira alerta geral.
   */
  function tratarErro(erro: unknown) {
    const decisao = decidirErroVinculo(erro);

    if (decisao.tipo === 'campos') {
      flushSync(() => setEnviando(false));
      for (const campo of CAMPOS_VINCULO) {
        const mensagem = decisao.campos[campo];
        if (mensagem !== undefined) setError(campo, { type: 'server', message: mensagem });
      }
      const primeiroCampo = CAMPOS_VINCULO.find((campo) => decisao.campos[campo] !== undefined);
      if (primeiroCampo !== undefined) setFocus(primeiroCampo);
      return;
    }

    if (decisao.tipo === 'senha-recusada') {
      flushSync(() => {
        setEnviando(false);
        setAlerta(null);
      });
      // A credencial continua em memória: a pessoa pode tentar de novo com outra senha.
      setValue('senha', '');
      setError('senha', { type: 'server', message: decisao.mensagem });
      setFocus('senha');
      return;
    }

    flushSync(() => {
      setEnviando(false);
      setAlerta({ mensagem: decisao.mensagem, voltarAoLogin: decisao.voltarAoLogin });
    });
    alertaRef.current?.focus();
  }

  /** Envio já validado pelo React Hook Form. Só roda dentro do evento de submit. */
  async function enviar(valores: ValoresVinculo) {
    if (emEnvio.current || credencial === null) return;
    emEnvio.current = true;
    setAlerta(null);
    setEnviando(true);

    try {
      await vincular.mutateAsync(montarRequisicaoVinculo(valores, credencial.credencial, credencial.lembrar));
    } catch (erro) {
      if (!montada.current) {
        // A tela saiu durante o envio e o envio falhou: a credencial não serve mais e sai da memória.
        limparCredencialGoogle();
        return;
      }
      emEnvio.current = false;
      tratarErro(erro);
      return;
    }

    if (!montada.current) return;
    navigate(resolverDestinoAposLogin('cliente', estadoRota), { replace: true });
  }

  if (credencial === null) {
    return <Navigate to={CAMINHOS.entrar} replace />;
  }

  return (
    <div className="mx-auto w-full max-w-md px-4 py-10 sm:px-6">
      <section className="rounded-cartao bg-branco p-6 shadow-cartao sm:p-8">
        <h1 className="text-titulo text-navy">{GOOGLE_VINCULAR_TITULO}</h1>
        <p className="mt-2 text-corpo text-texto-suave">{mensagemVincularEmail(credencial.email ?? '')}</p>

        <form
          noValidate
          onSubmit={(evento) => {
            void handleSubmit(enviar)(evento);
          }}
          className="mt-8 flex flex-col gap-5"
        >
          <fieldset disabled={enviando} className="flex min-w-0 flex-col gap-5 border-0 p-0">
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
            <CampoSenha
              rotulo={ACESSO_ROTULO_SENHA}
              obrigatorio
              autoComplete="current-password"
              erro={errors.senha?.message}
              {...register('senha')}
            />
          </fieldset>

          {alerta !== null && (
            <AlertaGoogle alertaRef={alertaRef} mensagem={alerta.mensagem}>
              {alerta.voltarAoLogin && (
                <BotaoLink
                  to={CAMINHOS.entrar}
                  state={estadoRota}
                  variante="contorno"
                  tamanho="pequeno"
                  onClick={() => limparCredencialGoogle()}
                >
                  {GOOGLE_VINCULAR_VOLTAR}
                </BotaoLink>
              )}
            </AlertaGoogle>
          )}

          <Botao type="submit" larguraTotal carregando={enviando} iconeDireita="arrow_forward">
            {GOOGLE_VINCULAR_BOTAO}
          </Botao>
        </form>

        {/* Com o alerta já oferecendo "Voltar ao login", a saída aparece uma vez só. */}
        {alerta?.voltarAoLogin !== true && (
          <p className="mt-6 text-center text-corpo-denso">
            <BotaoLink
              to={CAMINHOS.entrar}
              state={estadoRota}
              variante="fantasma"
              tamanho="pequeno"
              onClick={() => limparCredencialGoogle()}
            >
              {GOOGLE_VINCULAR_VOLTAR}
            </BotaoLink>
          </p>
        )}
      </section>
    </div>
  );
}
