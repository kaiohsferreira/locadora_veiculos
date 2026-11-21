import type { MutableRefObject, ReactNode } from 'react';
import { Gaveta } from '@/components/ui';
import { MENSAGEM_EQUIPE_FECHAR } from '@/lib/mensagens';

export interface GavetaFuncionarioProps {
  /** Título da gaveta (anunciado pelos leitores de tela). */
  titulo: string;
  /** Ícone Material Symbols do cabeçalho. */
  icone: string;
  /** Enquanto grava, nada fecha a gaveta (X, Cancelar, Esc e clique no fundo). */
  bloqueada: boolean;
  aoFechar: () => void;
  /** Botão que abriu a gaveta: recebe o foco de volta quando ela fecha. */
  refGatilho: MutableRefObject<HTMLElement | null>;
  children: ReactNode;
}

/**
 * Gaveta de funcionário (tela 41). O casco (diálogo, foco, Esc, clique no fundo e largura) vem do componente
 * compartilhado `Gaveta`; aqui só fica o texto de fechar da equipe.
 */
export function GavetaFuncionario({ titulo, icone, bloqueada, aoFechar, refGatilho, children }: GavetaFuncionarioProps) {
  return (
    <Gaveta
      titulo={titulo}
      icone={icone}
      bloqueada={bloqueada}
      aoFechar={aoFechar}
      refGatilho={refGatilho}
      rotuloFechar={MENSAGEM_EQUIPE_FECHAR}
    >
      {children}
    </Gaveta>
  );
}
