import { useId } from 'react';
import { Paginacao } from '@/components/ui';
import { MENSAGEM_FROTA_LINHAS_POR_PAGINA, MENSAGEM_FROTA_RODAPE_ITENS } from '@/lib/mensagens';
import { OPCOES_LINHAS_POR_PAGINA_FROTA } from '@/features/frota/listagem/parametrosListagemFrota';

export interface RodapeListagemFrotaProps {
  pagina: number;
  tamanhoPagina: number;
  total: number;
  /** Bloqueia a paginação e o seletor enquanto a próxima página carrega. */
  desabilitada: boolean;
  aoMudarPagina: (pagina: number) => void;
  aoMudarLinhas: (tamanhoPagina: number) => void;
}

/**
 * Rodapé da tabela (tela 11): resumo, seletor de linhas por página (10, 25, 50 e 100) e a paginação do servidor. O
 * seletor é um `select` nativo, como no desenho, com o rótulo visível ligado a ele.
 */
export function RodapeListagemFrota({
  pagina,
  tamanhoPagina,
  total,
  desabilitada,
  aoMudarPagina,
  aoMudarLinhas,
}: RodapeListagemFrotaProps) {
  const idLinhas = useId();
  return (
    <Paginacao
      variante="frota"
      pagina={pagina}
      tamanhoPagina={tamanhoPagina}
      total={total}
      aoMudarPagina={aoMudarPagina}
      desabilitada={desabilitada}
      rotuloItens={MENSAGEM_FROTA_RODAPE_ITENS}
      complemento={
        <div className="flex items-center gap-1">
          <label htmlFor={idLinhas} className="text-rotulo text-texto-apoio">
            {MENSAGEM_FROTA_LINHAS_POR_PAGINA}
          </label>
          <select
            id={idLinhas}
            value={String(tamanhoPagina)}
            disabled={desabilitada}
            onChange={(evento) => aoMudarLinhas(Number(evento.target.value))}
            className="h-7 cursor-pointer rounded-sm bg-branco px-1.5 text-rotulo text-texto-desenho outline-none focus-visible:ring-2 focus-visible:ring-orange-acao disabled:cursor-not-allowed disabled:opacity-60"
          >
            {OPCOES_LINHAS_POR_PAGINA_FROTA.map((tamanho) => (
              <option key={tamanho} value={String(tamanho)}>
                {tamanho}
              </option>
            ))}
          </select>
        </div>
      }
    />
  );
}
