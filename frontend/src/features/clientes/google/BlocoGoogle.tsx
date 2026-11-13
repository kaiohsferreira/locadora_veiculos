import { lazy, Suspense, useEffect, useRef, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { useConfiguracaoGoogle } from '@/features/clientes/hooks/useConfiguracaoGoogle';
import { useLoginGoogle } from '@/features/clientes/hooks/useLoginGoogle';
import { guardarCredencialGoogle } from '@/lib/credencialGoogle';
import { GOOGLE_ENTRANDO, GOOGLE_SEPARADOR } from '@/lib/mensagens';
import { guardarPreenchimentoGoogle } from '@/lib/preenchimentoGoogle';
import { CAMINHOS } from '@/routes/caminhos';
import type { PerfilApi } from '@/types/autenticacao';
import { AlertaGoogle } from './AlertaGoogle';
import { BotaoGoogle } from './BotaoGoogle';
import { decidirAcaoErroGoogle, decidirAcaoResultadoGoogle, type AcaoLoginGoogle } from './decisaoLoginGoogle';
import { obterModoBlocoGoogle } from './modoBlocoGoogle';

/**
 * Bloco de teste (FAKE). Só existe no build de desenvolvimento: fora dele a constante é null e o
 * módulo nem entra no pacote de produção.
 */
const BlocoGoogleTeste = import.meta.env.DEV
  ? lazy(() => import('./BlocoGoogleTeste').then((modulo) => ({ default: modulo.BlocoGoogleTeste })))
  : null;

/**
 * Altura reservada ao bloco do Google (sem a margem de cima). Valor único, igual para todos os estados:
 * divisor 1px + espaço 28px (gap-7) + botão do Google 44px (iframe `large`) = 73px. Mais a margem `mt-7`
 * (28px), a área ocupa 101px. O modo de teste (só em desenvolvimento) é mais alto e cresce.
 */
const CLASSES_RESERVA_GOOGLE = 'mt-7 min-h-[73px]';

export interface BlocoGoogleProps {
  /** Perfil escolhido no seletor. O bloco só existe para Cliente (RN3.1). */
  perfil: PerfilApi;
  /** Valor atual da caixa "Lembrar de mim" (RN2.7). Lido no momento do callback. */
  lembrar: boolean;
  /** Avisa a tela de acesso que um login com Google está em andamento (bloqueia o formulário de CPF). */
  aoOcupadoChange: (ocupado: boolean) => void;
}

/** Alerta do bloco. O objeto novo a cada erro faz o foco ir ao alerta mesmo com a mesma mensagem. */
interface AlertaBloco {
  mensagem: string;
}

/**
 * "Continuar com o Google" na tela de acesso (N3, RF-51). Fica abaixo do botão de entrar, depois
 * de um separador "ou". Sem configuração habilitada, não aparece e não mostra erro (RN3.1).
 * A situação devolvida pela API decide o próximo passo (AUTENTICADO, vínculo, cadastro, conta ambígua).
 */
export function BlocoGoogle({ perfil, lembrar, aoOcupadoChange }: BlocoGoogleProps) {
  const ativo = perfil === 'CLIENTE';
  const configuracao = useConfiguracaoGoogle(ativo);
  const login = useLoginGoogle();
  const { reset: resetarLogin } = login;
  const navegar = useNavigate();
  const localizacao = useLocation();
  const estadoRota: unknown = localizacao.state;
  const entrandoGoogle = login.isPending;

  const [alerta, setAlerta] = useState<AlertaBloco | null>(null);
  const alertaRef = useRef<HTMLDivElement>(null);
  const emEnvio = useRef(false);
  // O callback do Google é criado uma vez, então lê a caixa "Lembrar de mim" e o perfil pela ref.
  const lembrarRef = useRef(lembrar);
  const perfilRef = useRef(perfil);

  useEffect(() => {
    lembrarRef.current = lembrar;
  }, [lembrar]);

  useEffect(() => {
    perfilRef.current = perfil;
  }, [perfil]);

  // Ao trocar de perfil, o alerta deste bloco não vale mais. O ajuste é feito na renderização
  // (padrão do React para estado derivado), sem efeito.
  const [perfilDoAlerta, setPerfilDoAlerta] = useState<PerfilApi>(perfil);
  if (perfilDoAlerta !== perfil) {
    setPerfilDoAlerta(perfil);
    setAlerta(null);
  }

  useEffect(() => {
    if (alerta !== null) alertaRef.current?.focus();
  }, [alerta]);

  useEffect(() => {
    aoOcupadoChange(entrandoGoogle);
  }, [entrandoGoogle, aoOcupadoChange]);

  // A mutação não guarda a credencial depois que a tela deixa o bloco.
  useEffect(() => () => resetarLogin(), [resetarLogin]);

  /** Executa a decisão da tela. Só guarda a credencial no caso de vínculo (memória do módulo). */
  function executarAcao(acao: AcaoLoginGoogle, credencial: string, lembrarEnvio: boolean) {
    if (acao.tipo === 'concluido') return;
    if (acao.tipo === 'alerta') {
      setAlerta({ mensagem: acao.mensagem });
      return;
    }
    if (acao.tipo === 'confirmar-vinculo') {
      guardarCredencialGoogle({ credencial, lembrar: lembrarEnvio, nome: acao.nome, email: acao.email });
      navegar(CAMINHOS.vincularGoogle, { state: estadoRota });
      return;
    }
    // Cadastro: nada é criado aqui. Nome e e-mail vão para a memória (não para a URL) e a tela de
    // cadastro mostra a faixa informativa ao abrir.
    guardarPreenchimentoGoogle({ nome: acao.nome, email: acao.email });
    navegar(CAMINHOS.cadastroDadosPessoais);
  }

  /** Envia a credencial recebida do Google (ou do modo de teste). Envio único por ref síncrona. */
  function tratarCredencial(credencial: string) {
    if (emEnvio.current || perfilRef.current !== 'CLIENTE') return;
    emEnvio.current = true;
    setAlerta(null);
    const lembrarEnvio = lembrarRef.current;
    const perfilEnvio = perfilRef.current;

    /** Libera o envio. Devolve true quando o perfil mudou durante o envio: o resultado é ignorado. */
    function encerrarEnvio(): boolean {
      emEnvio.current = false;
      return perfilRef.current !== perfilEnvio;
    }

    login.mutate(
      { credencial, lembrar: lembrarEnvio },
      {
        onSuccess: (resultado) => {
          if (encerrarEnvio()) return;
          executarAcao(decidirAcaoResultadoGoogle(resultado), credencial, lembrarEnvio);
        },
        onError: (erro) => {
          if (encerrarEnvio()) return;
          executarAcao(decidirAcaoErroGoogle(erro), credencial, lembrarEnvio);
        },
      },
    );
  }

  const configuracaoPronta = configuracao.data;
  const modo = obterModoBlocoGoogle(ativo ? configuracaoPronta : undefined, import.meta.env.DEV);
  // Equipe: o bloco não existe e a tela reserva a altura por conta própria (FormularioAcesso).
  if (!ativo) return null;
  // Cliente sem Google (carregando a configuração ou indisponível): mantém o espaço vazio para a linha
  // "Criar conta" não subir nem descer quando o botão aparecer.
  if (modo.tipo === 'oculto') return <div aria-hidden="true" className={CLASSES_RESERVA_GOOGLE} />;

  return (
    <div className={`${CLASSES_RESERVA_GOOGLE} flex flex-col gap-7`}>
      <div className="relative flex items-center justify-center">
        <div aria-hidden="true" className="h-px w-full bg-borda" />
        <span className="absolute bg-offwhite px-4 text-legenda text-status-neutro">{GOOGLE_SEPARADOR}</span>
      </div>

      {modo.tipo === 'teste' && BlocoGoogleTeste !== null ? (
        <Suspense fallback={null}>
          <BlocoGoogleTeste ocupado={entrandoGoogle} aoCredencial={tratarCredencial} />
        </Suspense>
      ) : modo.tipo === 'real' ? (
        <BotaoGoogle clientId={modo.clientId} ocupado={entrandoGoogle} aoCredencial={tratarCredencial} />
      ) : null}

      {/* Região de status: existe sempre; sem texto não ocupa espaço (empty:hidden). */}
      <output className="block text-center text-corpo-denso text-status-neutro empty:hidden">
        {entrandoGoogle ? GOOGLE_ENTRANDO : null}
      </output>

      {alerta !== null && <AlertaGoogle alertaRef={alertaRef} mensagem={alerta.mensagem} />}
    </div>
  );
}
