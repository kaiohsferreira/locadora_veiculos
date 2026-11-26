import { useRef, useState } from 'react';
import { Botao, SeloGravidade } from '@/components/ui';
import { formatarMoeda } from '@/lib/formatters';
import { MENSAGENS_DEVOLUCAO } from '@/lib/mensagensDevolucao';
import { MENSAGEM_FOTOS_TENTAR_DE_NOVO, MENSAGEM_FOTOS_VAZIO } from '@/lib/mensagensFotosVeiculo';
import {
  MENSAGEM_AVARIA_AVISO_NAO_REMOVE,
  MENSAGEM_AVARIA_CUSTO,
  MENSAGEM_AVARIA_ENVIAR_FOTO,
  MENSAGEM_AVARIA_FOTO_COM_ERRO,
  MENSAGEM_AVARIA_LISTA_VAZIA,
  MENSAGEM_AVARIA_LISTA_VAZIA_LEITURA,
  MENSAGEM_VISTORIA_REGISTRO_FOTOS_APOIO,
  MENSAGEM_VISTORIA_REGISTRO_FOTOS_TITULO,
  mensagemNomeAcessivelAvaria,
  mensagemRotuloAvaria,
  mensagemSeloMarcador,
} from '@/lib/mensagensVistoriaAvarias';
import { decidirFalhaDaImagem, textoSituacaoItemLocal, type LinhaAvaria } from './avariasVistoria';
import { modeloLinhaAvaria, type ModeloLinhaAvaria } from './modeloLinhaAvaria';

export interface ListaAvariasProps {
  linhas: readonly LinhaAvaria[];
  somenteLeitura: boolean;
  enviando: boolean;
  aoTentarDeNovo(chave: number): void;
}

/** Cartões das avarias: número, título (rótulo do ângulo), selo de gravidade, descrição, custo e situação do envio. */
export function ListaAvarias({ linhas, somenteLeitura, enviando, aoTentarDeNovo }: ListaAvariasProps) {
  const modelos = linhas.map(modeloLinhaAvaria);
  const temGravada = modelos.some((modelo) => modelo.chaveLocal === null && modelo.idAvaria !== null);

  if (modelos.length === 0) {
    return <p className="text-corpo-denso text-texto-apoio">{somenteLeitura ? MENSAGEM_AVARIA_LISTA_VAZIA_LEITURA : MENSAGEM_AVARIA_LISTA_VAZIA}</p>;
  }

  return (
    <div className="flex flex-col gap-3">
      <ul className="flex flex-col gap-2">
        {modelos.map((modelo) => (
          <li key={modelo.chaveLocal !== null ? `local-${modelo.chaveLocal}` : `servidor-${modelo.idAvaria}`} className="flex flex-col gap-2 rounded-lg bg-steel-fundo p-4 transition-colors hover:bg-steel-suave">
            <div className="flex items-start justify-between gap-2">
              <div className="flex items-center gap-2">
                <span aria-hidden="true" className="flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-orange-acao text-dado-tabular font-bold text-branco">
                  {modelo.numero}
                </span>
                <span className="text-corpo-denso font-bold text-texto-desenho">{modelo.titulo}</span>
              </div>
              <SeloGravidade gravidade={modelo.gravidade} />
            </div>
            <p className="pl-8 text-apoio text-texto-apoio">{modelo.descricao}</p>
            <div className="flex items-center justify-between pl-8">
              <span className="text-micro font-medium uppercase text-contorno">{MENSAGEM_AVARIA_CUSTO}</span>
              <span className="text-dado-tabular font-bold text-texto-desenho">{formatarMoeda(modelo.valorEstimado)}</span>
            </div>
            {modelo.situacao !== null && (
              <p className="pl-8 text-apoio text-texto-apoio">{modelo.mensagemErro ?? textoSituacaoItemLocal(modelo.situacao)}</p>
            )}
            {modelo.podeRepetir && modelo.chaveLocal !== null && (
              <div className="pl-8">
                <Botao type="button" variante="contorno" tamanho="pequeno" disabled={enviando} onClick={() => aoTentarDeNovo(modelo.chaveLocal as number)}>
                  {MENSAGEM_FOTOS_TENTAR_DE_NOVO}
                </Botao>
              </div>
            )}
          </li>
        ))}
      </ul>
      {temGravada && <p className="text-apoio text-texto-apoio">{MENSAGEM_AVARIA_AVISO_NAO_REMOVE}</p>}
    </div>
  );
}

export interface GradeFotosAvariasProps {
  linhas: readonly LinhaAvaria[];
  enviando: boolean;
  /** Foto com erro de avaria gravada: envia a nova foto (PUT), que substitui a anterior. Vale também com a vistoria registrada. */
  aoEnviarFotoGravada(idAvaria: number, arquivo: File): void;
  /** Pede a lista de novo quando a imagem falha (a URL da foto expira em 15 min). Chamado uma vez por avaria gravada. */
  aoRecarregarLista?(): void;
}

/** Chave da imagem com falha: muda quando a URL muda, para uma foto nova não herdar o erro antigo. */
function chaveFoto(modelo: ModeloLinhaAvaria, url: string): string {
  return `${modelo.chaveLocal ?? 'servidor'}|${modelo.idAvaria ?? ''}|${url}`;
}

/**
 * Fotos das avarias: legenda "Avaria n" e selo "Marcador n", como no desenho. Sem arquivo, ou com a imagem que não carrega,
 * o erro da foto aparece no lugar; a avaria gravada ganha o envio de nova foto (também na leitura, sem mapa nem lista de edição).
 */
export function GradeFotosAvarias({ linhas, enviando, aoEnviarFotoGravada, aoRecarregarLista }: GradeFotosAvariasProps) {
  const modelos = linhas.map(modeloLinhaAvaria);
  const comFoto = modelos.filter((modelo) => modelo.urlFoto !== null || modelo.semArquivo);
  const [falhas, setFalhas] = useState<ReadonlySet<string>>(() => new Set());
  const recarregadas = useRef<Set<number>>(new Set());

  function aoFalharImagem(modelo: ModeloLinhaAvaria, url: string): void {
    const idAvaria = modelo.idAvaria;
    if (aoRecarregarLista !== undefined && modelo.chaveLocal === null && idAvaria !== null) {
      const jaRecarregou = recarregadas.current.has(idAvaria);
      if (decidirFalhaDaImagem(jaRecarregou) === 'recarregar_lista') {
        recarregadas.current.add(idAvaria);
        aoRecarregarLista();
        return;
      }
    }
    setFalhas((anteriores) => new Set(anteriores).add(chaveFoto(modelo, url)));
  }

  return (
    <section aria-labelledby="titulo-fotos-vistoria" className="flex flex-col gap-4 rounded-xl bg-branco p-6 shadow-sm">
      <div>
        <h3 id="titulo-fotos-vistoria" className="text-titulo-md font-bold text-texto-desenho">{MENSAGEM_VISTORIA_REGISTRO_FOTOS_TITULO}</h3>
        <p className="text-apoio text-texto-apoio">{MENSAGEM_VISTORIA_REGISTRO_FOTOS_APOIO}</p>
      </div>
      {comFoto.length === 0 ? (
        <p className="text-corpo-denso text-texto-apoio">{MENSAGEM_FOTOS_VAZIO}</p>
      ) : (
        <ul className="grid grid-cols-3 gap-2 sm:grid-cols-4">
          {comFoto.map((modelo) => {
            const url = modelo.urlFoto;
            const semImagem = url === null || falhas.has(chaveFoto(modelo, url));
            const podeEnviar = modelo.chaveLocal === null && modelo.idAvaria !== null;
            return (
              <li
                key={modelo.chaveLocal !== null ? `local-${modelo.chaveLocal}` : `servidor-${modelo.idAvaria}`}
                className={`relative flex flex-col overflow-hidden rounded-lg bg-steel-suave shadow-sm ${semImagem ? 'col-span-2 min-h-24' : 'h-24'}`}
              >
                {url !== null && !semImagem ? (
                  <img
                    src={url}
                    alt={mensagemNomeAcessivelAvaria(modelo.numero, modelo.descricao, MENSAGENS_DEVOLUCAO.gravidades[modelo.gravidade])}
                    className="h-full w-full object-cover"
                    onError={() => aoFalharImagem(modelo, url)}
                    onLoad={() => {
                      if (modelo.idAvaria !== null) recarregadas.current.delete(modelo.idAvaria);
                    }}
                  />
                ) : (
                  // Foto com erro: o texto aprovado pode ocupar várias linhas (o item cresce); pb-12 deixa espaço para o rótulo da base.
                  <div className="flex flex-1 flex-col items-center justify-center gap-1 px-2 pb-12 pt-7 text-center">
                    <span className="text-apoio text-status-perigo">{MENSAGEM_AVARIA_FOTO_COM_ERRO}</span>
                    {podeEnviar && (
                      <label className="cursor-pointer rounded text-micro font-semibold text-frota-oficina focus-within:outline focus-within:outline-2 focus-within:outline-navy">
                        <span className="sr-only">{`${MENSAGEM_AVARIA_ENVIAR_FOTO} ${mensagemRotuloAvaria(modelo.numero)}`}</span>
                        <span aria-hidden="true">{MENSAGEM_AVARIA_ENVIAR_FOTO}</span>
                        <input
                          type="file"
                          accept="image/jpeg,image/png"
                          disabled={enviando}
                          className="sr-only"
                          onChange={(evento) => {
                            const arquivo = evento.target.files?.[0];
                            if (arquivo && modelo.idAvaria !== null) aoEnviarFotoGravada(modelo.idAvaria, arquivo);
                          }}
                        />
                      </label>
                    )}
                  </div>
                )}
                <span className="pointer-events-none absolute inset-x-0 bottom-0 truncate bg-gradient-to-t from-navy-profundo/80 via-transparent to-transparent px-1.5 pb-1.5 pt-6 text-micro font-semibold text-branco">
                  {mensagemRotuloAvaria(modelo.numero)}
                </span>
                <span className="absolute left-1 top-1 rounded bg-orange-acao px-1 text-chip-frota font-bold text-branco">{mensagemSeloMarcador(modelo.numero)}</span>
              </li>
            );
          })}
        </ul>
      )}
    </section>
  );
}
