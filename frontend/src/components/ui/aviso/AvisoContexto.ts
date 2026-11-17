import { createContext } from 'react';

export type TipoAviso = 'sucesso' | 'erro';

export interface ItemAviso {
  id: number;
  tipo: TipoAviso;
  mensagem: string;
}

/** Funções expostas pelo `useAviso()`. */
export interface AvisoContextoValor {
  /** Mostra uma notificação de sucesso (ex.: "Veículo cadastrado com sucesso."). */
  sucesso: (mensagem: string) => void;
  /** Mostra uma notificação de erro. Para erros de API: `erro(obterMensagemErro(e))`. */
  erro: (mensagem: string) => void;
}

export const AvisoContexto = createContext<AvisoContextoValor | null>(null);

/**
 * Pedido de uma tela para que os avisos fiquem acima da barra de ações fixa (ex.: a tela de cadastro de veículo).
 * `registrar` devolve a função que desfaz o pedido. Sem provedor por perto, o pedido não faz nada.
 */
export interface AvisoElevacaoContextoValor {
  registrar: () => () => void;
}

export const AvisoElevacaoContexto = createContext<AvisoElevacaoContextoValor | null>(null);
