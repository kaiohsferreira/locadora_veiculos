import { useState } from 'react';
import { Botao, ImagemVeiculo, Icone } from '@/components/ui';
import { PERCENTUAL_MAXIMO } from '@/lib/fotosVeiculo';
import {
  MENSAGEM_FOTOS_AGUARDANDO,
  MENSAGEM_FOTOS_ERRO_ENVIO,
  MENSAGEM_FOTOS_PRINCIPAL,
  MENSAGEM_FOTOS_REMOVER_IMAGEM,
  MENSAGEM_FOTOS_TENTAR_DE_NOVO,
  mensagemFotoEnviando,
  mensagemFotoRemoverNumero,
} from '@/lib/mensagensFotosVeiculo';

/** Estado visual de uma miniatura: foto gravada, ou da fila (aguardando, enviando, com erro). */
export type EstadoMiniatura = 'gravada' | 'aguardando' | 'enviando' | 'erro';

export interface MiniaturaFotoVeiculoProps {
  /** Posição na grade, a partir de 1 (usada no nome acessível do botão de remover). */
  posicao: number;
  /**
   * Imagem a mostrar: URL da foto gravada ou prévia local da fila. Nula quando a foto gravada não tem arquivo: aparece
   * o ícone de carro no lugar da imagem.
   */
  url: string | null;
  /** Texto alternativo da foto. */
  alt: string;
  estado: EstadoMiniatura;
  /** Selo "Foto Principal": só na primeira foto gravada. */
  principal?: boolean;
  /** Progresso do envio, de 0 a 100 (só no estado "enviando"). */
  percentual?: number;
  /** Mensagem do erro de envio (só no estado "erro"). */
  mensagemErro?: string | null;
  /**
   * Prévia local da foto que acabou de ser enviada (só no estado "gravada"). Continua visível por cima da imagem do
   * servidor até ela carregar ou falhar; depois sai. A fila libera a prévia por conta própria.
   */
  previaAteCarregar?: string | null;
  /**
   * Só a foto gravada sem arquivo: o botão de remover fica sempre visível, sem depender do mouse (toque, foco ou hover).
   * Padrão falso: as demais miniaturas seguem com o botão escondido fora do hover.
   */
  removerSempreVisivel?: boolean;
  aoRemover: () => void;
  aoTentarDeNovo?: () => void;
}

/**
 * Miniatura da grade de fotos (desenho 05). A caixa da foto é sempre 4:3, como no desenho. Foto gravada não mostra
 * rodapé. Foto da fila mostra o estado de verdade (aguardando ou progresso dentro da caixa). Foto com erro mantém a
 * caixa 4:3 com uma faixa de ícone; a mensagem completa e o "Tentar de novo" ficam logo abaixo, na mesma célula da
 * grade, com quebra de linha: nada sai da caixa nem sobrepõe a foto vizinha. O botão de remover fica visível no foco e
 * em telas sem hover; some enquanto a foto está enviando. A foto gravada sem arquivo usa o mesmo estado "erro", sem
 * imagem (url nula), com o texto do servidor e sem "Tentar de novo" (só o erro de envio tem o botão); nela o botão de
 * remover fica sempre visível (`removerSempreVisivel`).
 */
export function MiniaturaFotoVeiculo({
  posicao,
  url,
  alt,
  estado,
  principal = false,
  percentual = 0,
  mensagemErro = null,
  previaAteCarregar = null,
  removerSempreVisivel = false,
  aoRemover,
  aoTentarDeNovo,
}: MiniaturaFotoVeiculoProps) {
  const podeRemover = estado !== 'enviando';
  const comErro = estado === 'erro';
  // A prévia fica guardada aqui: a fila a libera depois do fim do lote, mas a miniatura só a deixa ao carregar a imagem do servidor.
  const [previaVisivel, setPreviaVisivel] = useState<string | null>(previaAteCarregar);
  const encerrarPrevia = () => setPreviaVisivel(null);

  return (
    <li className={`group relative min-w-0 ${comErro ? 'flex flex-col gap-2' : ''}`}>
      <div className="relative aspect-[4/3] overflow-hidden rounded bg-steel-suave shadow-sm">
        <ImagemVeiculo
          url={url}
          alt={alt}
          aoCarregar={encerrarPrevia}
          aoFalhar={encerrarPrevia}
          className="h-full w-full transition-transform duration-300 group-hover:scale-105"
        />
        {previaVisivel !== null && (
          <img src={previaVisivel} alt="" aria-hidden="true" className="pointer-events-none absolute inset-0 h-full w-full object-cover" />
        )}
        <div
          aria-hidden="true"
          className={`pointer-events-none absolute inset-0 bg-gradient-to-t via-transparent to-transparent ${
            principal ? 'from-navy-profundo/80' : 'from-navy-profundo/70'
          }`}
        />

        {principal && (
          <span className="absolute left-1.5 top-1.5 flex items-center gap-0.5 rounded-sm bg-status-pendente px-1.5 py-0.5 text-micro font-bold text-branco shadow-sm">
            <Icone nome="star" tamanho={12} />
            {MENSAGEM_FOTOS_PRINCIPAL}
          </span>
        )}

        {podeRemover && (
          <button
            type="button"
            onClick={aoRemover}
            title={MENSAGEM_FOTOS_REMOVER_IMAGEM}
            aria-label={mensagemFotoRemoverNumero(posicao)}
            className={`absolute right-1.5 top-1.5 z-10 flex h-6 w-6 items-center justify-center rounded-sm bg-navy/80 text-branco transition-opacity focus-visible:opacity-100${
              removerSempreVisivel ? '' : ' [@media(hover:hover)]:opacity-0 [@media(hover:hover)]:group-hover:opacity-100'
            }`}
          >
            <Icone nome="close" tamanho={14} />
          </button>
        )}

        {estado === 'aguardando' && (
          <p className="absolute inset-x-0 bottom-0 bg-navy/80 px-2 py-1 text-micro font-medium text-branco">
            {MENSAGEM_FOTOS_AGUARDANDO}
          </p>
        )}

        {estado === 'enviando' && (
          <div className="absolute inset-x-0 bottom-0 bg-navy/80 px-2 pb-1.5 pt-1 text-micro font-medium text-branco">
            <p>{mensagemFotoEnviando(percentual)}</p>
            <progress
              value={percentual}
              max={PERCENTUAL_MAXIMO}
              aria-label={mensagemFotoEnviando(percentual)}
              className="mt-1 block h-1 w-full appearance-none overflow-hidden rounded-full border-0 bg-branco/30 [&::-moz-progress-bar]:bg-orange [&::-webkit-progress-bar]:bg-branco/30 [&::-webkit-progress-value]:bg-orange"
            />
          </div>
        )}

        {comErro && (
          <span className="absolute inset-x-0 bottom-0 flex items-center justify-center bg-status-perigo py-1 text-branco">
            <Icone nome="error" tamanho={14} />
          </span>
        )}
      </div>

      {comErro && (
        <div className="flex flex-col items-start gap-2">
          <p role="alert" className="break-words text-legenda font-medium text-status-perigo">
            {mensagemErro ?? MENSAGEM_FOTOS_ERRO_ENVIO}
          </p>
          {aoTentarDeNovo && (
            <Botao
              variante="contorno"
              tamanho="pequeno"
              icone="refresh"
              onClick={aoTentarDeNovo}
              className="w-full !h-auto py-1.5 !whitespace-normal"
            >
              {MENSAGEM_FOTOS_TENTAR_DE_NOVO}
            </Botao>
          )}
        </div>
      )}
    </li>
  );
}
