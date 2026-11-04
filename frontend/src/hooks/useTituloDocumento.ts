import { useEffect } from 'react';

const NOME_APLICACAO = 'AutoVia';

/** Define o título da aba do navegador ("Título | AutoVia") enquanto a tela estiver aberta. */
export function useTituloDocumento(titulo: string): void {
  useEffect(() => {
    const tituloAnterior = document.title;
    document.title = titulo ? `${titulo} | ${NOME_APLICACAO}` : NOME_APLICACAO;
    return () => {
      document.title = tituloAnterior;
    };
  }, [titulo]);
}
