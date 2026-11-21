import { EstadoVazio, Esqueleto, Icone } from '@/components/ui';
import {
  MENSAGEM_FINANCEIRO_ACAO_DAR_BAIXA,
  MENSAGEM_FINANCEIRO_CARREGANDO,
  MENSAGEM_FINANCEIRO_COLUNA_ACAO,
  MENSAGEM_FINANCEIRO_COLUNA_CLIENTE,
  MENSAGEM_FINANCEIRO_COLUNA_ORIGEM,
  MENSAGEM_FINANCEIRO_COLUNA_STATUS,
  MENSAGEM_FINANCEIRO_COLUNA_TIPO,
  MENSAGEM_FINANCEIRO_COLUNA_VALOR,
  MENSAGEM_FINANCEIRO_COLUNA_VENCIMENTO,
  MENSAGEM_FINANCEIRO_LEGENDA_TABELA,
  MENSAGEM_FINANCEIRO_STATUS_ATRASADO,
  MENSAGEM_FINANCEIRO_STATUS_PAGO,
  MENSAGEM_FINANCEIRO_STATUS_PENDENTE,
  MENSAGEM_FINANCEIRO_TABELA_VAZIA,
  mensagemFinanceiroCpf,
  mensagemFinanceiroPagoEm,
} from '@/lib/mensagens';
import {
  podeMostrarDarBaixa,
  situacaoExibida,
  textoVencimento,
} from '@/features/financeiro/painel/regrasPainelFinanceiro';
import type { SituacaoTituloFinanceiro, TituloFinanceiro } from '@/types/painelFinanceiro';

const LINHAS_ESQUELETO = 5;

const COLUNAS: readonly { chave: string; titulo: string; direita?: boolean }[] = [
  { chave: 'origem', titulo: MENSAGEM_FINANCEIRO_COLUNA_ORIGEM },
  { chave: 'cliente', titulo: MENSAGEM_FINANCEIRO_COLUNA_CLIENTE },
  { chave: 'tipo', titulo: MENSAGEM_FINANCEIRO_COLUNA_TIPO },
  { chave: 'valor', titulo: MENSAGEM_FINANCEIRO_COLUNA_VALOR },
  { chave: 'vencimento', titulo: MENSAGEM_FINANCEIRO_COLUNA_VENCIMENTO },
  { chave: 'status', titulo: MENSAGEM_FINANCEIRO_COLUNA_STATUS },
  { chave: 'acao', titulo: MENSAGEM_FINANCEIRO_COLUNA_ACAO, direita: true },
];

export interface TabelaTitulosFinanceirosProps {
  /** Títulos da página atual, já sem caução. A paginação é do servidor. */
  itens: readonly TituloFinanceiro[];
  carregando: boolean;
  /** Abre o diálogo de dar baixa numa linha. */
  aoDarBaixa: (titulo: TituloFinanceiro) => void;
}

/**
 * Tabela de títulos (desenho 09, seção de títulos). Colunas do desenho, com o botão "Dar baixa" no lugar das ações que
 * foram cortadas. Linhas em atraso ganham o fundo de alerta do desenho.
 */
export function TabelaTitulosFinanceiras({ itens, carregando, aoDarBaixa }: TabelaTitulosFinanceirosProps) {
  return (
    <div className="w-full overflow-x-auto">
      <table className="w-full border-collapse text-apoio" aria-busy={carregando || undefined}>
        <caption className="sr-only">{MENSAGEM_FINANCEIRO_LEGENDA_TABELA}</caption>
        <thead className="bg-steel-fundo">
          <tr>
            {COLUNAS.map((coluna) => (
              <th
                key={coluna.chave}
                scope="col"
                className={`whitespace-nowrap px-4 py-3 text-micro font-semibold uppercase tracking-wider text-texto-apoio ${
                  coluna.direita ? 'text-right' : 'text-left'
                }`}
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
                  <td key={coluna.chave} className="px-4 py-3.5">
                    <Esqueleto className="h-4 w-full" />
                  </td>
                ))}
              </tr>
            ))}

          {carregando && (
            <tr>
              <td colSpan={COLUNAS.length} className="sr-only">
                {MENSAGEM_FINANCEIRO_CARREGANDO}
              </td>
            </tr>
          )}

          {!carregando && itens.length === 0 && (
            <tr>
              <td colSpan={COLUNAS.length}>
                <EstadoVazio titulo={MENSAGEM_FINANCEIRO_TABELA_VAZIA} />
              </td>
            </tr>
          )}

          {!carregando &&
            itens.map((titulo) => (
              <LinhaTitulo key={titulo.idPagamento} titulo={titulo} aoDarBaixa={aoDarBaixa} />
            ))}
        </tbody>
      </table>
    </div>
  );
}

/** Selo de origem (Locação ou Venda), com o ícone do desenho e o texto que o servidor manda. */
function SeloOrigem({ titulo }: { titulo: TituloFinanceiro }) {
  const locacao = titulo.origem === 'LOCACAO';
  return (
    <span
      className={`inline-flex items-center gap-1 whitespace-nowrap rounded-sm px-2 py-0.5 text-micro font-semibold text-branco ${
        locacao ? 'bg-navy' : 'bg-financeiro-origem-venda'
      }`}
    >
      <Icone nome={locacao ? 'key' : 'sell'} tamanho={14} />
      {titulo.origemStr}
    </span>
  );
}

/** Selo de situação: texto e ponto, com as cores do desenho (Em Atraso, Pendente e Compensado / Pago). */
function SeloSituacao({ situacao }: { situacao: SituacaoTituloFinanceiro }) {
  const configuracao: Record<SituacaoTituloFinanceiro, { rotulo: string; classe: string; ponto: string }> = {
    ATRASADO: {
      rotulo: MENSAGEM_FINANCEIRO_STATUS_ATRASADO,
      classe: 'bg-manutencao-alerta text-branco',
      ponto: 'bg-branco',
    },
    PENDENTE: {
      rotulo: MENSAGEM_FINANCEIRO_STATUS_PENDENTE,
      classe: 'bg-frota-oficina-fundo text-pagamento-texto-pendente',
      ponto: 'bg-frota-oficina',
    },
    PAGO: {
      rotulo: MENSAGEM_FINANCEIRO_STATUS_PAGO,
      classe: 'bg-steel-alto text-navy-profundo',
      ponto: 'bg-orange-acao',
    },
  };
  const { rotulo, classe, ponto } = configuracao[situacao];
  return (
    <span
      className={`inline-flex items-center gap-1.5 whitespace-nowrap rounded-sm px-2.5 py-1 text-rotulo font-semibold ${classe}`}
    >
      <span aria-hidden="true" className={`h-1.5 w-1.5 rounded-full ${ponto}`} />
      {rotulo}
    </span>
  );
}

/** Linha do vencimento: a data e, embaixo, o texto do prazo (cor de alerta quando vence hoje ou amanhã, vermelho quando venceu). */
function CelulaVencimento({ titulo, situacao }: { titulo: TituloFinanceiro; situacao: SituacaoTituloFinanceiro }) {
  if (situacao === 'PAGO') {
    return (
      <div className="flex flex-col">
        <span className="text-dado-tabular font-medium text-texto-desenho">{titulo.dataVencimentoStr}</span>
        <span className="text-micro font-medium text-texto-apoio">
          {titulo.dataPagamentoStr !== null ? mensagemFinanceiroPagoEm(titulo.dataPagamentoStr) : null}
        </span>
      </div>
    );
  }

  const dias = titulo.diasParaVencer;
  const vencido = dias < 0;
  const proximo = dias === 0 || dias === 1;
  const classeTexto = vencido ? 'text-manutencao-alerta' : proximo ? 'text-frota-oficina' : 'text-texto-apoio';
  return (
    <div className="flex flex-col">
      <span className="text-dado-tabular font-medium text-texto-desenho">{titulo.dataVencimentoStr}</span>
      <span className={`inline-flex items-center gap-1 text-micro font-semibold ${classeTexto}`}>
        <Icone nome="schedule" tamanho={12} />
        {textoVencimento(dias)}
      </span>
    </div>
  );
}

function LinhaTitulo({ titulo, aoDarBaixa }: { titulo: TituloFinanceiro; aoDarBaixa: (titulo: TituloFinanceiro) => void }) {
  const situacao = situacaoExibida(titulo);
  const atrasado = situacao === 'ATRASADO';
  return (
    <tr className={`transition-colors hover:bg-steel-fundo/60 ${atrasado ? 'bg-manutencao-alerta-trilho/10' : ''}`}>
      <td className="whitespace-nowrap px-4 py-3.5 align-middle">
        <SeloOrigem titulo={titulo} />
      </td>
      <td className="px-4 py-3.5 align-middle">
        <div className="flex flex-col">
          <span className="text-corpo-denso font-semibold text-texto-desenho">{titulo.clienteNome}</span>
          {titulo.clienteCpfMascarado !== null && (
            <span className="text-micro font-medium text-texto-apoio">
              {mensagemFinanceiroCpf(titulo.clienteCpfMascarado)}
            </span>
          )}
        </div>
      </td>
      <td className="px-4 py-3.5 align-middle text-apoio text-texto-desenho">{titulo.tipoStr}</td>
      <td className="whitespace-nowrap px-4 py-3.5 align-middle">
        <span className="text-valor-destaque font-bold text-navy-profundo">{titulo.valorStr}</span>
      </td>
      <td className="whitespace-nowrap px-4 py-3.5 align-middle">
        <CelulaVencimento titulo={titulo} situacao={situacao} />
      </td>
      <td className="whitespace-nowrap px-4 py-3.5 align-middle">
        <SeloSituacao situacao={situacao} />
      </td>
      <td className="whitespace-nowrap px-4 py-3.5 text-right align-middle">
        {podeMostrarDarBaixa(titulo) && (
          <button
            type="button"
            onClick={() => aoDarBaixa(titulo)}
            className="inline-flex items-center gap-1.5 rounded-sm bg-frota-oficina px-3 py-1.5 text-rotulo font-semibold text-branco shadow-sm transition-colors hover:bg-financeiro-acao-hover"
          >
            {MENSAGEM_FINANCEIRO_ACAO_DAR_BAIXA}
          </button>
        )}
      </td>
    </tr>
  );
}
