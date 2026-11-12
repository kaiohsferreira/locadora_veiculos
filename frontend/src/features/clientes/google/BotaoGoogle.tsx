import { useEffect, useRef, useState } from 'react';
import { Botao } from '@/components/ui';
import { carregarScriptGoogle } from '@/lib/googleIdentity';
import { GOOGLE_CARREGANDO, GOOGLE_FALHA_SCRIPT, GOOGLE_TENTAR_NOVAMENTE } from '@/lib/mensagens';
import { AlertaGoogle } from './AlertaGoogle';

export interface BotaoGoogleProps {
  /** Client ID público da AutoVia (vem da configuração). Só é passado para `initialize`. */
  clientId: string;
  /** Recebe a credencial (JWT) quando o usuário conclui o login no Google. */
  aoCredencial: (credencial: string) => void;
  /** Login em andamento: o botão deixa de receber cliques (o contêiner ignora o ponteiro). */
  ocupado?: boolean;
}

/**
 * `inicial`: primeiro carregamento (a região de status segue vazia). `carregando`: após "Tentar novamente",
 * quando o texto de carregamento entra na região de status.
 */
type EstadoScript = 'inicial' | 'carregando' | 'pronto' | 'falhou';

/**
 * Botão oficial "Continuar com o Google" (modo real, G3). O script é carregado sob demanda e
 * o botão é desenhado pelo próprio Google num iframe. Enquanto carrega, um espaço com a altura
 * do botão evita salto de layout. Se o script falhar, aparece o alerta com "Tentar novamente" e
 * o formulário de CPF segue funcionando.
 */
export function BotaoGoogle({ clientId, aoCredencial, ocupado = false }: BotaoGoogleProps) {
  const [estado, setEstado] = useState<EstadoScript>('inicial');
  // Muda a cada "Tentar novamente": o efeito de carregamento roda de novo.
  const [tentativa, setTentativa] = useState(0);

  /** "Tentar novamente": o texto de carregamento entra agora, na troca de estado. */
  function tentarNovamente() {
    setEstado('carregando');
    setTentativa((atual) => atual + 1);
  }
  const elementoRef = useRef<HTMLDivElement>(null);
  // O Google guarda o callback da inicialização: a ref sempre aponta para a versão atual da tela.
  const aoCredencialRef = useRef(aoCredencial);

  useEffect(() => {
    aoCredencialRef.current = aoCredencial;
  }, [aoCredencial]);

  useEffect(() => {
    // Marca só esta montagem (variável local ao efeito, não compartilhada). Com o StrictMode o efeito
    // roda duas vezes: a primeira fica inativa e não inicializa o Google de novo.
    const montagem = { ativa: true };

    carregarScriptGoogle().then(
      () => {
        if (!montagem.ativa) return;
        const api = window.google?.accounts?.id;
        const elemento = elementoRef.current;
        if (!api || !elemento) {
          setEstado('falhou');
          return;
        }
        try {
          api.initialize({
            client_id: clientId,
            callback: (resposta) => {
              const credencial = resposta.credential;
              // Popup cancelado não chama o callback. Credencial vazia é ignorada.
              if (typeof credencial === 'string' && credencial !== '') {
                aoCredencialRef.current(credencial);
              }
            },
          });
          // Remove o que uma montagem anterior (StrictMode ou nova tentativa) deixou antes de desenhar.
          elemento.replaceChildren();
          api.renderButton(elemento, {
            type: 'standard',
            theme: 'outline',
            size: 'large',
            text: 'continue_with',
            locale: 'pt-BR',
          });
          setEstado('pronto');
        } catch {
          setEstado('falhou');
        }
      },
      () => {
        if (montagem.ativa) setEstado('falhou');
      },
    );

    return () => {
      montagem.ativa = false;
    };
  }, [clientId, tentativa]);

  const falhou = estado === 'falhou';
  const aguardando = estado === 'inicial' || estado === 'carregando';

  return (
    <div
      aria-disabled={ocupado || undefined}
      className={ocupado ? 'pointer-events-none opacity-60' : undefined}
    >
      {falhou && (
        <AlertaGoogle mensagem={GOOGLE_FALHA_SCRIPT}>
          <Botao variante="contorno" tamanho="pequeno" onClick={tentarNovamente}>
            {GOOGLE_TENTAR_NOVAMENTE}
          </Botao>
        </AlertaGoogle>
      )}

      {/* O contêiner do botão fica montado sempre (a ref precisa dele); só some quando há falha. */}
      <div className={falhou ? 'hidden' : 'relative min-h-10 w-full'}>
        <div ref={elementoRef} className="flex min-h-10 w-full justify-center" />
        {aguardando && <div aria-hidden="true" className="absolute inset-0 animate-pulse rounded-md bg-offwhite" />}
        {/* A região de status existe desde o início, vazia: o texto entra quando o estado muda. */}
        <output className="sr-only">{estado === 'carregando' ? GOOGLE_CARREGANDO : ''}</output>
      </div>
    </div>
  );
}
