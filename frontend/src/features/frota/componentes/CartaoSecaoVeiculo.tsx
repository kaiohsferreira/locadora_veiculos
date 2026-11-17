import type { ReactNode } from 'react';
import { Icone } from '@/components/ui';

export interface CartaoSecaoVeiculoProps {
  /** Ícone Material Symbols da caixa do cabeçalho (decorativo: o título já diz o assunto). */
  icone: string;
  /** Título da seção, com o número (ex.: "1. Identificação Técnica do Veículo"). */
  titulo: string;
  /** Etiqueta em caixa alta à direita do cabeçalho (ex.: "Dados Obrigatórios"). */
  etiqueta: string;
  children: ReactNode;
}

/** Cartão de seção da tela 05: cabeçalho com caixa de ícone 28x28, título e etiqueta; sem linha divisória. */
export function CartaoSecaoVeiculo({ icone, titulo, etiqueta, children }: CartaoSecaoVeiculoProps) {
  return (
    <section className="relative flex flex-col gap-4 overflow-hidden rounded-cartao bg-branco p-6 shadow-sm">
      <header className="flex flex-wrap items-center justify-between gap-x-4 gap-y-2 pb-1">
        <div className="flex min-w-0 items-center gap-2">
          <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded bg-steel-alto text-navy-profundo">
            <Icone nome={icone} tamanho={18} />
          </span>
          <h2 className="text-nome-veiculo tracking-tight text-texto-desenho">{titulo}</h2>
        </div>
        <span className="text-micro font-semibold uppercase tracking-wider text-contorno">{etiqueta}</span>
      </header>
      {children}
    </section>
  );
}
