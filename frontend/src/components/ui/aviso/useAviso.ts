import { useContext } from 'react';
import { AvisoContexto, type AvisoContextoValor } from './AvisoContexto';

/**
 * Notificações (toast) da aplicação.
 * Uso: `const aviso = useAviso(); aviso.sucesso('Reserva confirmada.'); aviso.erro(obterMensagemErro(erro));`
 */
export function useAviso(): AvisoContextoValor {
  const contexto = useContext(AvisoContexto);
  if (!contexto) {
    throw new Error('useAviso precisa ser usado dentro de <AvisoProvider>.');
  }
  return contexto;
}
