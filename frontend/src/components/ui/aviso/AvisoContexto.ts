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
