import type { ReactNode } from 'react';
import { Esqueleto, Icone, ImagemVeiculo } from '@/components/ui';
import { urlDaImagem } from '@/lib/fotosVeiculo';
import { VALOR_AUSENTE, formatarData, formatarMoeda, formatarPlaca } from '@/lib/formatters';
import {
  MENSAGEM_MANUTENCAO_COLUNA_ACOES,
  MENSAGEM_MANUTENCAO_COLUNA_CUSTO,
  MENSAGEM_MANUTENCAO_COLUNA_ENTRADA,
  MENSAGEM_MANUTENCAO_COLUNA_OFICINA,
  MENSAGEM_MANUTENCAO_COLUNA_SAIDA,
  MENSAGEM_MANUTENCAO_COLUNA_STATUS,
  MENSAGEM_MANUTENCAO_COLUNA_TIPO,
  MENSAGEM_MANUTENCAO_COLUNA_VEICULO,
  MENSAGEM_MANUTENCAO_EM_ABERTO,
  MENSAGEM_MANUTENCAO_ENCERRAR_OS,
  MENSAGEM_MANUTENCAO_HISTORICO_TITULO,
  MENSAGEM_MANUTENCAO_STATUS_CONCLUIDA,
  MENSAGEM_MANUTENCAO_STATUS_EXECUCAO,
  mensagemFotoAlt,
  mensagemManutencaoEncerrarTitulo,
  mensagemManutencaoOs,
} from '@/lib/mensagens';
import { ROTULO_TIPO_MANUTENCAO, type TipoManutencao } from '@/types/enums';
import type { ManutencaoListaRetorno } from '@/types/manutencao';

const LINHAS_ESQUELETO = 5;

const COLUNAS: readonly { chave: string; titulo: string; direita?: boolean }[] = [
  { chave: 'veiculo', titulo: MENSAGEM_MANUTENCAO_COLUNA_VEICULO },
  { chave: 'tipo', titulo: MENSAGEM_MANUTENCAO_COLUNA_TIPO },
  { chave: 'entrada', titulo: MENSAGEM_MANUTENCAO_COLUNA_ENTRADA },
  { chave: 'saida', titulo: MENSAGEM_MANUTENCAO_COLUNA_SAIDA },
  { chave: 'oficina', titulo: MENSAGEM_MANUTENCAO_COLUNA_OFICINA },
  { chave: 'custo', titulo: MENSAGEM_MANUTENCAO_COLUNA_CUSTO, direita: true },
  { chave: 'status', titulo: MENSAGEM_MANUTENCAO_COLUNA_STATUS },
  { chave: 'acoes', titulo: MENSAGEM_MANUTENCAO_COLUNA_ACOES, direita: true },
];

export interface TabelaOrdensManutencaoProps {
  /** Ordens da página atual. A paginação é do servidor. */
  itens: readonly ManutencaoListaRetorno[];
  carregando: boolean;
  /** Conteúdo quando não há linhas (estado vazio da página). */
  vazio: ReactNode;
  /** Chamado na primeira falha de capa (a página refaz a consulta, uma vez por visita). */
  aoFalharFotoCapa: () => void;
  /** Abre o modal de encerrar a ordem (só para ordem aberta). */
  aoEncerrar: (ordem: ManutencaoListaRetorno) => void;
}

/**
 * Tabela de ordens (desenho 14, seção 2). As colunas são as do desenho, sem previsão, sem hora e sem "Aguardando Peças".
 * A segunda linha da oficina é a descrição da ordem. Ações: só "Encerrar OS", e só em ordem aberta (P7 cortou o resto).
 */
export function TabelaOrdensManutencao({ itens, carregando, vazio, aoFalharFotoCapa, aoEncerrar }: TabelaOrdensManutencaoProps) {
  return (
    <div className="w-full overflow-x-auto">
      <table className="w-full border-collapse text-apoio" aria-busy={carregando || undefined}>
        <caption className="sr-only">{MENSAGEM_MANUTENCAO_HISTORICO_TITULO}</caption>
        <thead className="bg-steel-fundo">
          <tr>
            {COLUNAS.map((coluna) => (
              <th
                key={coluna.chave}
                scope="col"
                className={`whitespace-nowrap px-4 py-2 first:px-6 last:px-6 text-micro font-semibold uppercase tracking-wider text-texto-apoio ${
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
                  <td key={coluna.chave} className="px-4 py-4 first:px-6 last:px-6">
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
            itens.map((ordem) => (
              <LinhaOrdemManutencao
                key={ordem.idManutencao}
                ordem={ordem}
                aoFalharFotoCapa={aoFalharFotoCapa}
                aoEncerrar={aoEncerrar}
              />
            ))}
        </tbody>
      </table>
    </div>
  );
}

function TipoOrdem({ tipo }: { tipo: TipoManutencao }) {
  const preventiva = tipo === 'PREVENTIVA';
  return (
    <span
      className={`inline-flex items-center gap-1 whitespace-nowrap rounded-sm px-1.5 py-0.5 text-micro font-semibold ${
        preventiva ? 'bg-steel-alto text-navy-profundo' : 'bg-frota-oficina-fundo text-pagamento-texto-pendente'
      }`}
    >
      <Icone nome={preventiva ? 'verified' : 'build'} tamanho={14} className={preventiva ? 'text-navy-profundo' : 'text-frota-oficina'} />
      {ROTULO_TIPO_MANUTENCAO[tipo]}
    </span>
  );
}

function EtiquetaSituacaoOrdem({ aberta }: { aberta: boolean }) {
  return (
    <span
      className={`inline-flex items-center gap-1.5 whitespace-nowrap rounded-full px-2 py-0.5 text-micro font-semibold ${
        aberta ? 'bg-frota-oficina-fundo text-manutencao-execucao-texto' : 'bg-manutencao-concluida-fundo text-pagamento-texto-compensado'
      }`}
    >
      <span aria-hidden="true" className={`h-1.5 w-1.5 rounded-full ${aberta ? 'bg-frota-oficina' : 'bg-navy'}`} />
      {aberta ? MENSAGEM_MANUTENCAO_STATUS_EXECUCAO : MENSAGEM_MANUTENCAO_STATUS_CONCLUIDA}
    </span>
  );
}

interface LinhaOrdemManutencaoProps {
  ordem: ManutencaoListaRetorno;
  aoFalharFotoCapa: () => void;
  aoEncerrar: (ordem: ManutencaoListaRetorno) => void;
}

function LinhaOrdemManutencao({ ordem, aoFalharFotoCapa, aoEncerrar }: LinhaOrdemManutencaoProps) {
  const nome = `${ordem.marca} ${ordem.modelo}`;
  return (
    <tr className="transition-colors hover:bg-steel-fundo/60">
      <td className="px-4 py-4 align-middle first:px-6 last:px-6">
        <div className="flex items-center gap-3">
          <ImagemVeiculo
            url={ordem.fotoCapa ? urlDaImagem(ordem.fotoCapa) : null}
            alt={mensagemFotoAlt(nome)}
            className="h-9 w-12 shrink-0 rounded"
            tamanhoIcone={18}
            aoFalhar={aoFalharFotoCapa}
          />
          <div className="flex min-w-0 flex-col">
            <span className="text-corpo-denso font-semibold text-texto-desenho">{nome}</span>
            <div className="mt-0.5 flex flex-wrap items-center gap-1.5">
              <span className="rounded-sm bg-steel-suave px-1 text-dado-tabular font-semibold text-texto-apoio">
                {formatarPlaca(ordem.placa)}
              </span>
              <span className="text-micro text-contorno">{mensagemManutencaoOs(ordem.idManutencao)}</span>
            </div>
          </div>
        </div>
      </td>

      <td className="px-4 py-4 align-middle first:px-6 last:px-6">
        <TipoOrdem tipo={ordem.tipo} />
      </td>

      <td className="whitespace-nowrap px-4 py-4 align-middle text-dado-tabular text-texto-desenho">
        {formatarData(ordem.dataEntrada)}
      </td>

      <td className="whitespace-nowrap px-4 py-4 align-middle text-dado-tabular text-texto-desenho">
        {ordem.aberta ? (
          <span className="text-contorno">{MENSAGEM_MANUTENCAO_EM_ABERTO}</span>
        ) : (
          <span className="font-medium">{formatarData(ordem.dataSaida)}</span>
        )}
      </td>

      <td className="px-4 py-4 align-middle first:px-6 last:px-6">
        <div className="flex flex-col">
          <span className="font-medium text-texto-desenho">{ordem.oficina ?? VALOR_AUSENTE}</span>
          {ordem.descricao && <span className="text-micro text-contorno">{ordem.descricao}</span>}
        </div>
      </td>

      <td className="whitespace-nowrap px-4 py-4 text-right align-middle text-dado-tabular font-bold text-texto-desenho">
        {ordem.custo === null ? VALOR_AUSENTE : formatarMoeda(ordem.custo)}
      </td>

      <td className="px-4 py-4 text-center align-middle">
        <EtiquetaSituacaoOrdem aberta={ordem.aberta} />
      </td>

      <td className="whitespace-nowrap px-4 py-4 text-right align-middle last:px-6">
        {ordem.aberta && (
          <button
            type="button"
            title={MENSAGEM_MANUTENCAO_ENCERRAR_OS}
            aria-label={mensagemManutencaoEncerrarTitulo(ordem.idManutencao)}
            onClick={() => aoEncerrar(ordem)}
            className="inline-flex items-center justify-center rounded p-1.5 text-contorno transition-colors hover:bg-steel-suave hover:text-frota-oficina focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-orange-acao"
          >
            <Icone nome="check_circle" tamanho={18} />
          </button>
        )}
      </td>
    </tr>
  );
}
