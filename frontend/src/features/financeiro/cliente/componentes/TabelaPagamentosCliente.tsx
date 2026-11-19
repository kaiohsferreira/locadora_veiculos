import { EstadoErro, EstadoVazio, Esqueleto, Icone, Paginacao } from '@/components/ui';
import {
  ehPendente,
  ID_PAINEL_PAGAMENTOS,
  podePagarNaLinha,
  rotuloOrigem,
  subtextoVencimento,
  iconeDoTipo,
} from '@/features/financeiro/cliente/regrasPagamentosCliente';
import { VALOR_AUSENTE } from '@/lib/formatters';
import {
  MENSAGEM_PAGAMENTOS_CABECALHO_ACOES,
  MENSAGEM_PAGAMENTOS_CABECALHO_DESCRICAO,
  MENSAGEM_PAGAMENTOS_CABECALHO_LOCACAO,
  MENSAGEM_PAGAMENTOS_CABECALHO_STATUS,
  MENSAGEM_PAGAMENTOS_CABECALHO_VALOR,
  MENSAGEM_PAGAMENTOS_CABECALHO_VENCIMENTO,
  MENSAGEM_PAGAMENTOS_LEGENDA_TABELA,
  MENSAGEM_PAGAMENTOS_PAGAR_AGORA,
  MENSAGEM_PAGAMENTOS_RODAPE_ITENS,
  MENSAGEM_PAGAMENTOS_VAZIO_FILTRO_TITULO,
  MENSAGEM_PAGAMENTOS_VAZIO_TITULO,
  mensagemCobrancaNumero,
} from '@/lib/mensagens';
import { obterMensagemErro } from '@/lib/erros';
import { ROTULO_TIPO_PAGAMENTO } from '@/types/enums';
import type { ErroApi } from '@/types/api';
import type { MeuPagamento } from '@/types/pagamentos';
import { BotaoPagarDesligado } from './BotaoPagarDesligado';
import { EtiquetaSituacaoPagamento } from './EtiquetaSituacaoPagamento';

export interface TabelaPagamentosClienteProps {
  itens: readonly MeuPagamento[];
  carregando: boolean;
  erro: ErroApi | Error | null;
  total: number;
  pagina: number;
  tamanhoPagina: number;
  /** Consulta em andamento (bloqueia a paginação e mostra "tentando"). */
  atualizando: boolean;
  /** Há aba ou período fora do padrão (decide o texto do vazio). */
  comFiltro: boolean;
  aoMudarPagina: (pagina: number) => void;
  aoTentarNovamente: () => void;
}

const CABECALHOS: readonly { chave: string; titulo: string; alinhamento: 'esquerda' | 'centro' | 'direita' }[] = [
  { chave: 'descricao', titulo: MENSAGEM_PAGAMENTOS_CABECALHO_DESCRICAO, alinhamento: 'esquerda' },
  { chave: 'locacao', titulo: MENSAGEM_PAGAMENTOS_CABECALHO_LOCACAO, alinhamento: 'esquerda' },
  { chave: 'vencimento', titulo: MENSAGEM_PAGAMENTOS_CABECALHO_VENCIMENTO, alinhamento: 'esquerda' },
  { chave: 'valor', titulo: MENSAGEM_PAGAMENTOS_CABECALHO_VALOR, alinhamento: 'direita' },
  { chave: 'status', titulo: MENSAGEM_PAGAMENTOS_CABECALHO_STATUS, alinhamento: 'centro' },
  { chave: 'acoes', titulo: MENSAGEM_PAGAMENTOS_CABECALHO_ACOES, alinhamento: 'direita' },
];

const CLASSE_ALINHAMENTO: Readonly<Record<'esquerda' | 'centro' | 'direita', string>> = {
  esquerda: 'text-left',
  centro: 'text-center',
  direita: 'text-right',
};

const LINHAS_ESQUELETO = 5;

/** Tabela do desenho (6 colunas), com carregando, vazio, erro e paginação. Em tela estreita rola dentro do cartão. */
export function TabelaPagamentosCliente({
  itens,
  carregando,
  erro,
  total,
  pagina,
  tamanhoPagina,
  atualizando,
  comFiltro,
  aoMudarPagina,
  aoTentarNovamente,
}: TabelaPagamentosClienteProps) {
  if (erro && itens.length === 0 && !carregando) {
    return (
      <section id={ID_PAINEL_PAGAMENTOS} role="tabpanel" className="overflow-hidden rounded-cartao bg-branco shadow-sm">
        <EstadoErro mensagem={obterMensagemErro(erro)} aoTentarNovamente={aoTentarNovamente} tentando={atualizando} />
      </section>
    );
  }

  const vazio = !carregando && total === 0;

  return (
    <section
      id={ID_PAINEL_PAGAMENTOS}
      role="tabpanel"
      aria-busy={carregando || atualizando}
      className="overflow-hidden rounded-cartao bg-branco shadow-sm"
    >
      <div className="w-full overflow-x-auto">
        <table className="w-full border-collapse text-corpo-denso">
          <caption className="sr-only">{MENSAGEM_PAGAMENTOS_LEGENDA_TABELA}</caption>
          <thead>
            <tr className="bg-steel-fundo text-texto-apoio">
              {CABECALHOS.map((cabecalho, indice) => (
                <th
                  key={cabecalho.chave}
                  scope="col"
                  className={`whitespace-nowrap py-3.5 text-micro font-medium uppercase tracking-wider ${
                    indice === 0 || indice === CABECALHOS.length - 1 ? 'px-6' : 'px-4'
                  } ${CLASSE_ALINHAMENTO[cabecalho.alinhamento]}`}
                >
                  {cabecalho.titulo}
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-steel-suave/60">
            {carregando &&
              Array.from({ length: LINHAS_ESQUELETO }, (_, indiceLinha) => (
                <tr key={`esqueleto-${indiceLinha}`}>
                  {CABECALHOS.map((cabecalho) => (
                    <td key={cabecalho.chave} className="px-4 py-4">
                      <Esqueleto className="h-4 w-full" />
                    </td>
                  ))}
                </tr>
              ))}
            {!carregando && itens.map((item) => <LinhaPagamento key={item.idPagamento} pagamento={item} />)}
          </tbody>
        </table>
      </div>

      {vazio && (
        <EstadoVazio titulo={comFiltro ? MENSAGEM_PAGAMENTOS_VAZIO_FILTRO_TITULO : MENSAGEM_PAGAMENTOS_VAZIO_TITULO} />
      )}

      {!carregando && total > 0 && (
        <div className="border-t border-borda bg-steel-fundo">
          <Paginacao
            pagina={pagina}
            tamanhoPagina={tamanhoPagina}
            total={total}
            aoMudarPagina={aoMudarPagina}
            desabilitada={atualizando}
            rotuloItens={MENSAGEM_PAGAMENTOS_RODAPE_ITENS}
          />
        </div>
      )}
    </section>
  );
}

interface LinhaPagamentoProps {
  pagamento: MeuPagamento;
}

/** Uma cobrança: descrição, origem, vencimento, valor, situação e ação (sempre desligada). */
function LinhaPagamento({ pagamento }: LinhaPagamentoProps) {
  const pendente = ehPendente(pagamento);
  const idDescricao = `pagamento-${pagamento.idPagamento}-descricao`;
  const origem = rotuloOrigem(pagamento);
  const subtexto = subtextoVencimento(pagamento);
  /** Caução não é dívida: a data usa a cor neutra das linhas pagas, qualquer que seja a situação. */
  const classeData =
    pagamento.tipo === 'CAUCAO'
      ? 'text-texto-desenho'
      : pagamento.situacao === 'PENDENTE'
      ? 'font-semibold text-frota-oficina'
      : pagamento.situacao === 'ATRASADO'
        ? 'font-semibold text-status-perigo'
        : 'text-texto-desenho';
  const classeSubtexto =
    pagamento.situacao === 'PENDENTE'
      ? 'font-medium text-frota-oficina'
      : pagamento.situacao === 'ATRASADO'
        ? 'font-medium text-status-perigo'
        : 'text-texto-apoio';

  return (
    <tr className="transition-colors hover:bg-steel-fundo/40">
      <td className="px-6 py-4 align-middle" aria-labelledby={idDescricao}>
        <div className="flex items-center gap-2">
          <span
            className={`flex h-10 w-10 shrink-0 items-center justify-center rounded ${
              pendente ? 'bg-frota-oficina-fundo text-pagamento-texto-pendente' : 'bg-steel-alto text-navy-profundo'
            }`}
          >
            <Icone nome={iconeDoTipo(pagamento.tipo)} tamanho={20} />
          </span>
          <div>
            <p id={idDescricao} className="font-semibold text-navy-profundo">{ROTULO_TIPO_PAGAMENTO[pagamento.tipo]}</p>
            <p className="mt-0.5 text-apoio text-texto-apoio">{mensagemCobrancaNumero(pagamento.idPagamento)}</p>
          </div>
        </div>
      </td>

      <td className="px-4 py-4 align-middle">
        <div className="flex flex-col">
          <span className="text-dado-tabular font-bold text-navy-profundo">{origem ?? VALOR_AUSENTE}</span>
          {pagamento.veiculoDescricao && (
            <span className="text-apoio text-texto-apoio">{pagamento.veiculoDescricao}</span>
          )}
        </div>
      </td>

      <td className="px-4 py-4 align-middle">
        <div className="flex flex-col">
          <span className={`text-dado-tabular ${classeData}`}>{pagamento.dataVencimentoStr}</span>
          {subtexto && <span className={`text-micro ${classeSubtexto}`}>{subtexto}</span>}
        </div>
      </td>

      <td
        className={`whitespace-nowrap px-4 py-4 text-right align-middle text-valor-destaque ${
          pendente ? 'text-navy-profundo' : 'text-texto-desenho'
        }`}
      >
        {pagamento.valorStr}
      </td>

      <td className="px-4 py-4 text-center align-middle">
        <EtiquetaSituacaoPagamento situacao={pagamento.situacao} />
      </td>

      <td className="whitespace-nowrap px-6 py-4 text-right align-middle">
        {podePagarNaLinha(pagamento) && (
          <BotaoPagarDesligado
            variante="linha"
            rotulo={MENSAGEM_PAGAMENTOS_PAGAR_AGORA}
            icone="bolt"
            tamanhoIcone={18}
          />
        )}
      </td>
    </tr>
  );
}
