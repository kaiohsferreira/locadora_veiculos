import { Icone } from '@/components/ui';

/**
 * Caixa da foto do veículo (16:10, como no desenho). O contrato ainda não tem campo de foto: por enquanto mostra o
 * ícone de carro. Quando a foto existir, troque só o conteúdo deste componente, sem refazer o cartão.
 */
export function FotoVeiculo() {
  return (
    <div
      aria-hidden="true"
      className="relative flex aspect-[16/10] w-full items-center justify-center overflow-hidden rounded bg-steel-fundo p-2"
    >
      <Icone nome="directions_car" tamanho={64} className="text-borda-forte" />
    </div>
  );
}
