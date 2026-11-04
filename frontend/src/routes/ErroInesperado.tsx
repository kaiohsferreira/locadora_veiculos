import { useEffect } from 'react';
import { useRouteError } from 'react-router-dom';
import { Botao, BotaoLink } from '@/components/ui/Botao';
import { Icone } from '@/components/ui/Icone';
import { useTituloDocumento } from '@/hooks/useTituloDocumento';
import { MENSAGEM_ERRO_INESPERADO } from '@/lib/mensagens';
import { CAMINHOS } from './caminhos';

/**
 * Tela de erro das rotas (errorElement). Nunca mostra o detalhe técnico ao
 * usuário; no desenvolvimento, o erro vai para o console para depuração.
 */
export function ErroInesperado() {
  useTituloDocumento('Algo deu errado');
  const erro = useRouteError();

  useEffect(() => {
    if (import.meta.env.DEV) {
      console.error(erro);
    }
  }, [erro]);

  return (
    <div className="flex min-h-screen flex-col items-center justify-center gap-3 bg-offwhite px-4 py-16 text-center">
      <span className="flex h-14 w-14 items-center justify-center rounded-full bg-status-perigo-fundo text-status-perigo">
        <Icone nome="error" tamanho={28} />
      </span>
      <h1 className="text-titulo text-navy">Algo deu errado</h1>
      <p className="max-w-md text-corpo text-texto-suave">{MENSAGEM_ERRO_INESPERADO}</p>
      <div className="mt-4 flex flex-wrap justify-center gap-2">
        <Botao variante="secundario" icone="refresh" onClick={() => window.location.reload()}>
          Recarregar página
        </Botao>
        <BotaoLink to={CAMINHOS.inicio} variante="contorno" icone="home" reloadDocument>
          Ir para o início
        </BotaoLink>
      </div>
    </div>
  );
}
