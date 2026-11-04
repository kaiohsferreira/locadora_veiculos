import type { ReactNode } from 'react';
import { MENSAGEM_LISTA_VAZIA } from '@/lib/mensagens';
import { Icone } from './Icone';

export interface EstadoVazioProps {
  /** Padrão: "Nenhum registro encontrado." */
  titulo?: string;
  /** Explicação ou sugestão (ex.: "Ajuste os filtros e tente de novo."). */
  descricao?: string;
  /** Ícone Material Symbols. Padrão: "inbox". */
  icone?: string;
  /** Ação opcional, como um Botao ou BotaoLink. */
  acao?: ReactNode;
}

/** Estado vazio de listagens e painéis. */
export function EstadoVazio({ titulo = MENSAGEM_LISTA_VAZIA, descricao, icone = 'inbox', acao }: EstadoVazioProps) {
  return (
    <div className="flex flex-col items-center justify-center gap-2 px-4 py-12 text-center">
      <span className="mb-2 flex h-12 w-12 items-center justify-center rounded-full bg-steel-claro text-steel">
        <Icone nome={icone} tamanho={24} />
      </span>
      <p className="text-corpo font-semibold text-navy">{titulo}</p>
      {descricao && <p className="max-w-md text-corpo-denso text-texto-suave">{descricao}</p>}
      {acao && <div className="mt-4">{acao}</div>}
    </div>
  );
}
