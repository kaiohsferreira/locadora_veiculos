import { MENSAGEM_ERRO_CARREGAMENTO } from '@/lib/mensagens';
import { Botao } from './Botao';
import { Icone } from './Icone';

export interface EstadoErroProps {
  /** Padrão: "Não foi possível carregar as informações." */
  titulo?: string;
  /** Mensagem amigável do erro (use `obterMensagemErro(erro)` de `@/lib/erros`). */
  mensagem?: string;
  /** Quando informado, mostra o botão "Tentar novamente" (ex.: `refetch` da query). */
  aoTentarNovamente?: () => void;
  /** Indica que a nova tentativa está em andamento. */
  tentando?: boolean;
}

/** Estado de erro ao carregar dados, com opção de tentar de novo. */
export function EstadoErro({
  titulo = MENSAGEM_ERRO_CARREGAMENTO,
  mensagem,
  aoTentarNovamente,
  tentando = false,
}: EstadoErroProps) {
  return (
    <div role="alert" className="flex flex-col items-center justify-center gap-2 px-4 py-12 text-center">
      <span className="mb-2 flex h-12 w-12 items-center justify-center rounded-full bg-status-perigo-fundo text-status-perigo">
        <Icone nome="error" tamanho={24} />
      </span>
      <p className="text-corpo font-semibold text-navy">{titulo}</p>
      {mensagem && <p className="max-w-md text-corpo-denso text-texto-suave">{mensagem}</p>}
      {aoTentarNovamente && (
        <Botao variante="contorno" icone="refresh" className="mt-4" carregando={tentando} onClick={aoTentarNovamente}>
          Tentar novamente
        </Botao>
      )}
    </div>
  );
}
