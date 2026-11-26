import { ImagemVeiculo } from '@/components/ui';
import { urlDaImagem } from '@/lib/fotosVeiculo';
import type { FotoCapaVeiculo } from '@/types/fotosVeiculo';

export interface FotoVeiculoProps {
  /** Capa do veículo da reserva. Nula ou ausente: mostra a caixa com o ícone de carro, como no desenho sem foto. */
  fotoCapa?: FotoCapaVeiculo | null;
  /** Texto alternativo da foto (marca e modelo). */
  descricao: string;
  /**
   * Foto pequena do cartão da reserva (telas 28 e 29). No celular (abaixo de md) a caixa é 96x64 e a foto cobre a
   * caixa (object-cover), como no desenho 28. A partir de md volta a 16:10 com a imagem inteira (object-contain),
   * como no desenho 29. Sem a prop (tela 04, LinhaVeiculo), a caixa é 16:10 com a imagem inteira, como antes.
   */
  compacta?: boolean;
  /** Chamado quando a imagem falha (ex.: URL vencida). A tela decide se refaz a consulta. */
  aoFalhar?: () => void;
  /** Classe de fundo da caixa (token). Sem a prop, a caixa usa bg-steel-fundo; só o cartão ativo do celular muda. */
  classeFundo?: string;
}

/**
 * Caixa da foto do veículo (16:10, como no desenho 29). A imagem inteira aparece, sem cortar (object-contain).
 * Sem foto, ou se a imagem falhar, mostra o ícone de carro. Com `compacta`, ver a descrição da prop.
 */
export function FotoVeiculo({ fotoCapa, descricao, compacta = false, aoFalhar, classeFundo }: FotoVeiculoProps) {
  return (
    <ImagemVeiculo
      url={fotoCapa ? urlDaImagem(fotoCapa) : null}
      alt={descricao}
      ajuste={compacta ? 'cover' : 'contain'}
      classeFundo={classeFundo}
      className={
        compacta
          ? 'h-16 w-24 shrink-0 rounded-md [&>span]:scale-50 md:aspect-[16/10] md:h-auto md:w-full md:rounded md:p-2 md:[&>img]:object-contain md:[&>span]:scale-100'
          : 'aspect-[16/10] w-full rounded p-2'
      }
      tamanhoIcone={64}
      aoFalhar={aoFalhar}
    />
  );
}
