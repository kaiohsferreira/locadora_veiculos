import { useEffect, useRef, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { flushSync } from 'react-dom';
import { useNavigate } from 'react-router-dom';
import { Botao, CampoSenha, useAviso } from '@/components/ui';
import { useRedefinirSenha } from '@/features/clientes/hooks/useRedefinirSenha';
import { AlertaRecuperacao } from '@/features/clientes/recuperacao/AlertaRecuperacao';
import { CartaoRecuperacao } from '@/features/clientes/recuperacao/CartaoRecuperacao';
import { EstadoLinkInvalido } from '@/features/clientes/recuperacao/EstadoLinkInvalido';
import { esquemaRedefinicao, type ValoresRedefinicao } from '@/features/clientes/recuperacao/esquemaRedefinicao';
import {
  CAMPOS_REDEFINICAO,
  decidirErroRedefinicao,
  montarRequisicaoRedefinicao,
} from '@/features/clientes/recuperacao/fluxoRedefinicao';
import { useTituloDocumento } from '@/hooks/useTituloDocumento';
import {
  MENSAGEM_CADASTRO_DICA_SENHA,
  REDEFINICAO_BOTAO_SALVAR,
  REDEFINICAO_ROTULO_CONFIRMAR_SENHA,
  REDEFINICAO_ROTULO_NOVA_SENHA,
  REDEFINICAO_SUBTITULO,
  REDEFINICAO_SUCESSO,
  REDEFINICAO_TITULO,
} from '@/lib/mensagens';
import { lerTokenDoFragmento, removerFragmentoDaBarra } from '@/lib/tokenRecuperacao';
import { useAutenticacao } from '@/routes/autenticacao/useAutenticacao';
import { CAMINHOS } from '@/routes/caminhos';

/**
 * N1 (RF-49) · Definir nova senha, em /redefinir-senha. O token vem do fragmento (#token=) do link.
 * Ele é lido UMA vez, na montagem, guardado só em memória e tirado da barra. Sem login automático:
 * depois do 204 a pessoa vai para /entrar.
 */
export default function RedefinirSenhaPagina() {
  useTituloDocumento(REDEFINICAO_TITULO);
  const navigate = useNavigate();
  const aviso = useAviso();
  const { autenticado, sair } = useAutenticacao();
  const redefinir = useRedefinirSenha();
  const { reset: resetarRedefinicao } = redefinir;

  // Inicializador do estado: lê o fragmento na primeira renderização. O StrictMode repete o
  // inicializador e os efeitos, mas o estado é preservado, então o token não se perde.
  const [token, setToken] = useState<string | null>(() => lerTokenDoFragmento(window.location.hash));
  const [concluida, setConcluida] = useState(false);
  const [enviando, setEnviando] = useState(false);
  const [alertaGeral, setAlertaGeral] = useState<string | null>(null);
  const emEnvio = useRef(false);
  const montada = useRef(false);
  const alertaRef = useRef<HTMLDivElement>(null);
  const tituloInvalidoRef = useRef<HTMLHeadingElement>(null);

  // Tira o fragmento da barra. É idempotente: na segunda execução do StrictMode não há mais fragmento.
  useEffect(() => {
    removerFragmentoDaBarra();
  }, []);

  // Ao sair da tela, descarta a mutação (senha e token não ficam em cache).
  useEffect(() => {
    montada.current = true;
    return () => {
      montada.current = false;
      resetarRedefinicao();
    };
  }, [resetarRedefinicao]);

  const {
    register,
    handleSubmit,
    setError,
    setFocus,
    reset: resetarFormulario,
    formState: { errors },
  } = useForm<ValoresRedefinicao>({
    resolver: zodResolver(esquemaRedefinicao),
    defaultValues: { novaSenha: '', confirmacaoSenha: '' },
  });

  /** Os valores de senha saem da memória do formulário quando a tela deixa de precisar deles. */
  function descartarSenhas() {
    resetarFormulario({ novaSenha: '', confirmacaoSenha: '' });
  }

  /**
   * Erro do envio: 409 e 400 com o token trocam para o estado de link inválido; 400 com campo vai para o
   * campo; 429 e rede mantêm o formulário e o token para nova tentativa.
   */
  function tratarErro(erro: unknown) {
    const decisao = decidirErroRedefinicao(erro);

    if (decisao.tipo === 'link-invalido') {
      descartarSenhas();
      resetarRedefinicao();
      flushSync(() => {
        setEnviando(false);
        setToken(null);
      });
      tituloInvalidoRef.current?.focus();
      return;
    }

    if (decisao.tipo === 'campos') {
      flushSync(() => setEnviando(false));
      for (const campo of CAMPOS_REDEFINICAO) {
        const mensagem = decisao.campos[campo];
        if (mensagem !== undefined) setError(campo, { type: 'server', message: mensagem });
      }
      const primeiroCampo = CAMPOS_REDEFINICAO.find((campo) => decisao.campos[campo] !== undefined);
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
  async function enviar(dados: ValoresRedefinicao) {
    if (emEnvio.current || token === null) return;
    emEnvio.current = true;
    setAlertaGeral(null);
    setEnviando(true);

    try {
      await redefinir.mutateAsync(montarRequisicaoRedefinicao(token, dados));
    } catch (erro) {
      if (!montada.current) return;
      emEnvio.current = false;
      tratarErro(erro);
      return;
    }

    if (!montada.current) return;
    // Sucesso (204): o token e as senhas saem da memória antes de sair da tela.
    descartarSenhas();
    resetarRedefinicao();
    setToken(null);
    setConcluida(true);
    // O back-end invalidou as sessões desta conta (sv): se este navegador tem uma aberta, ela está morta.
    // Encerra antes de ir para /entrar, senão a guarda do login mandaria a pessoa à home com sessão inválida.
    if (autenticado) sair();
    aviso.sucesso(REDEFINICAO_SUCESSO);
    navigate(CAMINHOS.entrar, { replace: true });
  }

  // Depois do 204 a tela só espera a navegação para /entrar: não mostra nada no meio.
  if (concluida) return null;

  return (
    <CartaoRecuperacao>
      {token === null ? (
        <EstadoLinkInvalido tituloRef={tituloInvalidoRef} />
      ) : (
        <>
          <h1 className="text-titulo text-navy">{REDEFINICAO_TITULO}</h1>
          <p className="mt-2 text-corpo text-texto-suave">{REDEFINICAO_SUBTITULO}</p>

          <form
            noValidate
            onSubmit={(evento) => {
              void handleSubmit(enviar)(evento);
            }}
            className="mt-8 flex flex-col gap-5"
          >
            <fieldset disabled={enviando} className="flex min-w-0 flex-col gap-5 border-0 p-0">
              <CampoSenha
                rotulo={REDEFINICAO_ROTULO_NOVA_SENHA}
                obrigatorio
                autoComplete="new-password"
                dica={MENSAGEM_CADASTRO_DICA_SENHA}
                erro={errors.novaSenha?.message}
                {...register('novaSenha', { deps: ['confirmacaoSenha'] })}
              />

              <CampoSenha
                rotulo={REDEFINICAO_ROTULO_CONFIRMAR_SENHA}
                obrigatorio
                autoComplete="new-password"
                erro={errors.confirmacaoSenha?.message}
                {...register('confirmacaoSenha')}
              />
            </fieldset>

            <AlertaRecuperacao alertaRef={alertaRef} mensagem={alertaGeral} />

            <Botao type="submit" larguraTotal carregando={enviando} iconeDireita="arrow_forward">
              {REDEFINICAO_BOTAO_SALVAR}
            </Botao>
          </form>
        </>
      )}
    </CartaoRecuperacao>
  );
}
