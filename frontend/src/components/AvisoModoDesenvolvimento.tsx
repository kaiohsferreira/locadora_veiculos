import { MENSAGEM_AUTENTICACAO_IGNORADA } from '@/lib/mensagens';
import { useAutenticacao } from '@/routes/autenticacao/useAutenticacao';
import { Icone } from './ui/Icone';

/** Faixa de alerta exibida enquanto o desvio de autenticação de desenvolvimento estiver ativo. */
export function AvisoModoDesenvolvimento() {
  const { autenticacaoIgnorada } = useAutenticacao();
  if (!autenticacaoIgnorada) return null;

  return (
    <div className="flex items-center justify-center gap-2 bg-orange-claro px-4 py-1 text-center text-legenda font-medium text-navy">
      <Icone nome="construction" tamanho={16} />
      {MENSAGEM_AUTENTICACAO_IGNORADA}
    </div>
  );
}
