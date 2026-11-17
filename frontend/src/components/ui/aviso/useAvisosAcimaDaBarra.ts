import { useContext, useEffect } from 'react';
import { AvisoElevacaoContexto } from './AvisoContexto';

/**
 * Pede que os avisos desta tela apareçam acima da barra de ações fixa. O pedido vale enquanto a tela estiver montada.
 * Quem não usa o hook continua com os avisos no canto de sempre.
 * Uso: `useAvisosAcimaDaBarra();` na página que tem a barra fixa inferior.
 */
export function useAvisosAcimaDaBarra(): void {
  const contexto = useContext(AvisoElevacaoContexto);
  const registrar = contexto?.registrar;

  useEffect(() => {
    if (registrar === undefined) return;
    return registrar();
  }, [registrar]);
}
