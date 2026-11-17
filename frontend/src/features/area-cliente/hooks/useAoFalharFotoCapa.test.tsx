import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { useAoFalharFotoCapa } from './useAoFalharFotoCapa';

interface SondaProps {
  recarregar: () => void;
  aoReceber: (aviso: () => void) => void;
}

/** Componente de teste: chama o hook e entrega o aviso devolvido para quem monta a sonda. */
function Sonda({ recarregar, aoReceber }: SondaProps) {
  aoReceber(useAoFalharFotoCapa(recarregar));
  return null;
}

/** Monta a sonda uma vez (uma "visita" à tela) e devolve o aviso de falha de imagem. */
function montarAviso(recarregar: () => void): () => void {
  let aviso: (() => void) | undefined;
  renderToStaticMarkup(
    <Sonda
      recarregar={recarregar}
      aoReceber={(recebido) => {
        aviso = recebido;
      }}
    />,
  );
  if (aviso === undefined) throw new Error('o hook não devolveu o aviso de falha');
  return aviso;
}

describe('useAoFalharFotoCapa', () => {
  it('na visita, a primeira falha refaz a consulta uma vez; as seguintes não', () => {
    let chamadas = 0;
    const aviso = montarAviso(() => {
      chamadas += 1;
    });

    aviso();
    expect(chamadas).toBe(1);

    aviso();
    aviso();
    expect(chamadas).toBe(1);
  });

  it('cada visita (cada montagem da tela) tem o próprio controle', () => {
    let chamadas = 0;
    const recarregar = (): void => {
      chamadas += 1;
    };

    montarAviso(recarregar)();
    montarAviso(recarregar)();

    expect(chamadas).toBe(2);
  });
});
