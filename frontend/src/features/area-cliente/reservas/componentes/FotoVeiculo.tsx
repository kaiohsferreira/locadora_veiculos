import { ImagemVeiculo } from '@/components/ui';
import { urlDaImagem } from '@/lib/fotosVeiculo';
import type { FotoCapaVeiculo } from '@/types/fotosVeiculo';

export interface FotoVeiculoProps {
  /** Capa do veículo da reserva. Nula ou ausente: mostra a caixa com o ícone de carro, como no desenho sem foto. */
  fotoCapa?: FotoCapaVeiculo | null;
  /** Texto alternativo da foto (marca e modelo). */
  descricao: string;
}

/**
 * Caixa da foto do veículo (16:10, como no desenho 29). A imagem inteira aparece, sem cortar (object-contain).
 * Sem foto, ou se a imagem falhar, mostra o ícone de carro.
 */
export function FotoVeiculo({ fotoCapa, descricao }: FotoVeiculoProps) {
  return (
    <ImagemVeiculo
      url={fotoCapa ? urlDaImagem(fotoCapa) : null}
      alt={descricao}
      ajuste="contain"
      className="aspect-[16/10] w-full rounded p-2"
      tamanhoIcone={64}
    />
  );
}
