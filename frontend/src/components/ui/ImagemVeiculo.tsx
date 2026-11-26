import { useState } from 'react';
import { urlImagemExibida } from '@/lib/fotosVeiculo';
import { Icone } from './Icone';

const ICONE_SEM_FOTO = 'directions_car';
const TAMANHO_ICONE_PADRAO = 64;

const CLASSE_AJUSTE: Readonly<Record<AjusteImagemVeiculo, string>> = {
  cover: 'object-cover',
  contain: 'object-contain',
};

export type AjusteImagemVeiculo = 'cover' | 'contain';

export interface ImagemVeiculoProps {
  /** URL da imagem. Nula ou ausente: mostra o ícone de carro, como nas telas sem foto. */
  url?: string | null;
  /** Texto alternativo. Vazio quando a imagem é só decorativa. */
  alt: string;
  /** "cover" preenche a caixa cortando a imagem; "contain" mostra a imagem inteira. Padrão: cover. */
  ajuste?: AjusteImagemVeiculo;
  /** Tamanho e proporção da caixa (ex.: "aspect-[16/10] w-full"). */
  className?: string;
  /** Cor de fundo da caixa, como classe de token (ex.: "bg-steel-alto"). Padrão: bg-steel-fundo. */
  classeFundo?: string;
  /** Tamanho do ícone de carro quando não há foto. Padrão: 64. */
  tamanhoIcone?: number;
  /** Chamado quando a imagem carrega. Opcional: quem precisa esperar a imagem (ex.: prévia por cima) usa. */
  aoCarregar?: () => void;
  /** Chamado quando a imagem falha (antes do ícone de carro entrar). Opcional. */
  aoFalhar?: () => void;
}

/**
 * Foto do veículo numa caixa de tamanho e proporção fixos. Sem URL, ou quando a URL falha ao carregar (ex.: link
 * vencido), mostra o ícone de carro. A tela decide se consulta de novo; este componente não consulta nada.
 */
export function ImagemVeiculo({
  url,
  alt,
  ajuste = 'cover',
  className = '',
  classeFundo = 'bg-steel-fundo',
  tamanhoIcone = TAMANHO_ICONE_PADRAO,
  aoCarregar,
  aoFalhar,
}: ImagemVeiculoProps) {
  const [urlComFalha, setUrlComFalha] = useState<string | null>(null);
  const urlExibida = urlImagemExibida(url, urlComFalha);

  return (
    <div className={`relative flex items-center justify-center overflow-hidden ${classeFundo} ${className}`}>
      {urlExibida ? (
        <img
          src={urlExibida}
          alt={alt}
          loading="lazy"
          decoding="async"
          referrerPolicy="no-referrer"
          onLoad={aoCarregar}
          onError={() => {
            setUrlComFalha(urlExibida);
            aoFalhar?.();
          }}
          className={`h-full w-full ${CLASSE_AJUSTE[ajuste]}`}
        />
      ) : (
        <Icone nome={ICONE_SEM_FOTO} tamanho={tamanhoIcone} className="text-borda-forte" />
      )}
    </div>
  );
}
