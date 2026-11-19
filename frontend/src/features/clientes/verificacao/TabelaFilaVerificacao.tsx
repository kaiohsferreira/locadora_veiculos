import type { MouseEvent, ReactNode } from 'react';
import { Esqueleto, Etiqueta, Icone } from '@/components/ui';
import { formatarCpf } from '@/lib/formatters';
import {
  MENSAGEM_VERIFICACAO_COLUNA_ACAO,
  MENSAGEM_VERIFICACAO_COLUNA_CLIENTE,
  MENSAGEM_VERIFICACAO_COLUNA_CPF,
  MENSAGEM_VERIFICACAO_COLUNA_DATA,
  MENSAGEM_VERIFICACAO_COLUNA_STATUS,
  MENSAGEM_VERIFICACAO_TABELA_LEGENDA,
  mensagemVerificacaoAbrirCadastro,
} from '@/lib/mensagens';
import type { ClienteFila } from '@/types/verificacaoCadastros';
import { dataHoraExibicao, iniciaisCliente } from './regras';

/**
 * Colunas da fila (tela 18): sem documentos, sem protocolo e sem ponto de prioridade. Espaçamento do desenho: 16px na
 * primeira e na última coluna e 8px nas do meio.
 */
const COLUNAS = [
  { chave: 'cliente', titulo: MENSAGEM_VERIFICACAO_COLUNA_CLIENTE, alinhamento: 'esquerda', espacamento: 'px-4' },
  { chave: 'cpf', titulo: MENSAGEM_VERIFICACAO_COLUNA_CPF, alinhamento: 'esquerda', espacamento: 'px-2' },
  { chave: 'data', titulo: MENSAGEM_VERIFICACAO_COLUNA_DATA, alinhamento: 'esquerda', espacamento: 'px-2' },
  { chave: 'status', titulo: MENSAGEM_VERIFICACAO_COLUNA_STATUS, alinhamento: 'esquerda', espacamento: 'px-2' },
  { chave: 'acao', titulo: MENSAGEM_VERIFICACAO_COLUNA_ACAO, alinhamento: 'direita', espacamento: 'px-4' },
] as const;

type ChaveColuna = (typeof COLUNAS)[number]['chave'];

/**
 * Classes extras da coluna. O cliente absorve a largura que sobra (w-full com max-w-0 não empurra a tabela). A data some
 * quando o painel está aberto abaixo de 1400px: as cinco colunas não cabem sem cortar o nome.
 */
function classeDaColuna(chave: ChaveColuna, comPainel: boolean): string {
  if (chave === 'cliente') return 'w-full max-w-0';
  if (chave === 'data' && comPainel) return 'hidden min-[1400px]:table-cell';
  return '';
}

const LINHAS_ESQUELETO = 5;

export interface TabelaFilaVerificacaoProps {
  /** Linhas da página atual. A paginação é do servidor. */
  itens: readonly ClienteFila[];
  /** Mostra linhas de esqueleto no lugar dos dados (primeira carga). */
  carregando: boolean;
  /** Conteúdo quando não há linhas (lista vazia, com ou sem filtro). */
  vazio: ReactNode;
  /** Cliente cujo painel está aberto: a linha dele fica destacada. */
  idSelecionado: number | null;
  /** Abre o painel do cliente. */
  aoAbrir: (idCliente: number) => void;
  /**
   * Painel lateral aberto: a coluna de data some abaixo de 1400px (o painel já mostra a data do pedido) e o nome do cliente
   * corta com reticências quando não cabe. Sem painel, a coluna de data aparece sempre.
   */
  comPainel?: boolean;
}

/**
 * Tabela da fila de verificação (tela 18). Cada linha tem as iniciais e o nome do cliente, o CPF, a data do cadastro, o
 * status e o botão que abre o painel (o olho na linha aberta, a seta nas outras). A linha inteira também abre o painel;
 * o botão continua sendo o caminho do teclado e do leitor de tela. O e-mail não entra na fila: aparece no painel (decisão 9).
 */
export function TabelaFilaVerificacao({
  itens,
  carregando,
  vazio,
  idSelecionado,
  aoAbrir,
  comPainel = false,
}: TabelaFilaVerificacaoProps) {
  return (
    <div className="w-full overflow-x-auto">
      <table className="w-full border-collapse text-corpo-denso" aria-busy={carregando || undefined}>
        <caption className="sr-only">{MENSAGEM_VERIFICACAO_TABELA_LEGENDA}</caption>
        <thead className="bg-steel-fundo">
          <tr>
            {COLUNAS.map((coluna) => (
              <th
                key={coluna.chave}
                scope="col"
                className={`${coluna.espacamento} py-3 text-micro uppercase tracking-wider text-contorno ${
                  coluna.alinhamento === 'direita' ? 'text-right' : 'text-left'
                } ${classeDaColuna(coluna.chave, comPainel)}`}
              >
                {coluna.titulo}
              </th>
            ))}
          </tr>
        </thead>
        <tbody className="divide-y divide-steel-suave">
          {carregando &&
            Array.from({ length: LINHAS_ESQUELETO }, (_, indice) => (
              <tr key={`esqueleto-${indice}`}>
                {COLUNAS.map((coluna) => (
                  <td key={coluna.chave} className={`${coluna.espacamento} py-3.5 ${classeDaColuna(coluna.chave, comPainel)}`}>
                    <Esqueleto className="h-4 w-full" />
                  </td>
                ))}
              </tr>
            ))}

          {!carregando && itens.length === 0 && (
            <tr>
              <td colSpan={COLUNAS.length}>{vazio}</td>
            </tr>
          )}

          {!carregando &&
            itens.map((cliente) => (
              <LinhaFila
                key={cliente.idCliente}
                cliente={cliente}
                selecionada={cliente.idCliente === idSelecionado}
                aoAbrir={aoAbrir}
                comPainel={comPainel}
              />
            ))}
        </tbody>
      </table>
    </div>
  );
}

interface LinhaFilaProps {
  cliente: ClienteFila;
  selecionada: boolean;
  aoAbrir: (idCliente: number) => void;
  comPainel: boolean;
}

function LinhaFila({ cliente, selecionada, aoAbrir, comPainel }: LinhaFilaProps) {
  const nome = cliente.nome;
  // Clique em qualquer ponto da linha abre o painel. A linha não é controle: sem role e sem tabindex (o botão é o caminho do teclado).
  return (
    <tr
      aria-selected={selecionada || undefined}
      onClick={() => aoAbrir(cliente.idCliente)}
      className={`cursor-pointer transition-colors ${selecionada ? 'bg-steel-alto/60' : 'hover:bg-steel-fundo'}`}
    >
      <th scope="row" aria-label={nome} className="w-full max-w-0 px-4 py-3.5 text-left font-normal">
        <div className="flex min-w-0 items-center gap-2">
          <span
            className={`flex h-9 w-9 shrink-0 items-center justify-center rounded-full text-corpo-denso font-semibold shadow-sm ${
              selecionada ? 'bg-orange-acao text-branco' : 'bg-steel-linha text-navy-profundo'
            }`}
          >
            {iniciaisCliente(nome)}
          </span>
          <span title={nome} className="min-w-0 truncate font-semibold text-navy-profundo">
            {nome}
          </span>
        </div>
      </th>
      <td className="whitespace-nowrap px-2 py-3.5 font-medium tabular-nums text-dado-tabular text-texto-desenho">
        {formatarCpf(cliente.cpf)}
      </td>
      <td className={`whitespace-nowrap px-2 py-3.5 text-apoio text-texto-apoio ${classeDaColuna('data', comPainel)}`}>
        {dataHoraExibicao(cliente.dataCadastro)}
      </td>
      <td className="whitespace-nowrap px-2 py-3.5">
        <Etiqueta status={cliente.status} />
      </td>
      <td className="whitespace-nowrap px-4 py-3.5 text-right">
        <button
          type="button"
          aria-label={mensagemVerificacaoAbrirCadastro(nome)}
          aria-pressed={selecionada}
          onClick={(evento: MouseEvent<HTMLButtonElement>) => {
            // O clique também chega à linha: parar a propagação garante uma única abertura.
            evento.stopPropagation();
            aoAbrir(cliente.idCliente);
          }}
          className={`inline-flex items-center justify-center rounded-md p-1.5 transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-orange-acao ${
            selecionada ? 'bg-navy-profundo text-branco' : 'text-texto-apoio hover:bg-steel-alto'
          }`}
        >
          <Icone nome={selecionada ? 'visibility' : 'arrow_forward'} tamanho={16} />
        </button>
      </td>
    </tr>
  );
}
