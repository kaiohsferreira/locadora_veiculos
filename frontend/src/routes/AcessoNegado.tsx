import { Botao, BotaoLink } from '@/components/ui/Botao';
import { Icone } from '@/components/ui/Icone';
import { useTituloDocumento } from '@/hooks/useTituloDocumento';
import { MENSAGEM_AREA_NAO_PERMITIDA } from '@/lib/mensagens';
import { useAutenticacao } from './autenticacao/useAutenticacao';
import { CAMINHOS } from './caminhos';

/** Exibido quando o usuário tem sessão, mas o perfil não permite abrir a área. */
export function AcessoNegado() {
  useTituloDocumento('Acesso não permitido');
  const { sair } = useAutenticacao();

  return (
    <div className="flex min-h-[60vh] flex-col items-center justify-center gap-3 bg-offwhite px-4 py-16 text-center">
      <span className="flex h-14 w-14 items-center justify-center rounded-full bg-status-perigo-fundo text-status-perigo">
        <Icone nome="lock" tamanho={28} />
      </span>
      <h1 className="text-titulo text-navy">Acesso não permitido</h1>
      <p className="max-w-md text-corpo text-texto-suave">{MENSAGEM_AREA_NAO_PERMITIDA}</p>
      <div className="mt-4 flex flex-wrap justify-center gap-2">
        <BotaoLink to={CAMINHOS.inicio} variante="secundario" icone="home">
          Ir para o início
        </BotaoLink>
        <Botao variante="contorno" icone="logout" onClick={sair}>
          Entrar com outra conta
        </Botao>
      </div>
    </div>
  );
}
