import { useState, type ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { Esqueleto, Icone, ImagemVeiculo } from '@/components/ui';
import { caminhoEditarVeiculo } from '@/routes/caminhos';
import { EtiquetaStatusFrota } from '@/features/frota/componentes/EtiquetaStatusFrota';
import { MenuOperacionalVeiculo } from '@/features/frota/componentes/MenuOperacionalVeiculo';
import { PlacaMercosul } from '@/features/frota/componentes/PlacaEChassiFrota';
import type { AcoesStatusTabelaVeiculos } from '@/features/frota/componentes/TabelaVeiculos';
import { abreviarChassi } from '@/features/frota/listagem/opcoesListagemFrota';
import { montarMenuOperacional } from '@/features/frota/status/menuOperacional';
import { formatarQuilometragem } from '@/lib/formatters';
import {
  MENSAGEM_FROTA_COLUNA_ACOES,
  MENSAGEM_FROTA_COLUNA_STATUS,
  MENSAGEM_FROTA_EDITAR,
  MENSAGEM_FROTA_LISTAGEM_COLUNA_CATEGORIA,
  MENSAGEM_FROTA_LISTAGEM_COLUNA_MODELO,
  MENSAGEM_FROTA_LISTAGEM_COLUNA_ODOMETRO,
  MENSAGEM_FROTA_LISTAGEM_COLUNA_PLACA,
  MENSAGEM_FROTA_TABELA_LEGENDA,
  mensagemFotoAlt,
  mensagemFrotaAnoEChassi,
  mensagemFrotaEditarDoVeiculo,
} from '@/lib/mensagens';
import { urlDaImagem } from '@/lib/fotosVeiculo';
import type { VeiculoRetorno } from '@/types/veiculos';

/** Colunas do desenho 12 (cabeçalho e número de células). Sem olho (Ver detalhes), sem caixas de seleção. */
const COLUNAS = [
  { chave: 'placa', titulo: MENSAGEM_FROTA_LISTAGEM_COLUNA_PLACA, direita: false },
  { chave: 'modelo', titulo: MENSAGEM_FROTA_LISTAGEM_COLUNA_MODELO, direita: false },
  { chave: 'categoria', titulo: MENSAGEM_FROTA_LISTAGEM_COLUNA_CATEGORIA, direita: false },
  { chave: 'odometro', titulo: MENSAGEM_FROTA_LISTAGEM_COLUNA_ODOMETRO, direita: false },
  { chave: 'status', titulo: MENSAGEM_FROTA_COLUNA_STATUS, direita: false },
  { chave: 'acoes', titulo: MENSAGEM_FROTA_COLUNA_ACOES, direita: true },
] as const;

const LINHAS_ESQUELETO = 5;

export interface TabelaListagemVeiculosProps {
  /** Veículos da página atual. A paginação é do servidor. */
  itens: readonly VeiculoRetorno[];
  /** Mostra linhas de esqueleto no lugar dos dados (primeira carga). */
  carregando: boolean;
  /** Conteúdo quando não há linhas (ex.: estado vazio da tela). */
  vazio: ReactNode;
  /** Chamado quando a capa de um veículo falha ao abrir (a tela decide se refaz a consulta). */
  aoFalharFotoCapa?: () => void;
  /** Menu de troca de status (três pontos). Só quem pode trocar recebe a propriedade; sem ela, a coluna tem só o lápis. */
  acoesStatus?: AcoesStatusTabelaVeiculos;
}

/**
 * Tabela da listagem da frota (tela 12). Irmã da `TabelaVeiculos` da tela 11, com as colunas do desenho: placa, modelo e
 * especificação (capa, marca e modelo, ano e chassi abreviado com o chassi inteiro no título), categoria, odômetro e
 * situação. Ações: o lápis abre o cadastro do veículo para todo funcionário; os três pontos só com `acoesStatus`.
 */
export function TabelaListagemVeiculos({ itens, carregando, vazio, aoFalharFotoCapa, acoesStatus }: TabelaListagemVeiculosProps) {
  /** Um menu aberto por vez: guarda o id da linha. Se a linha sumir da página, o menu fecha sozinho. */
  const [idMenuAberto, setIdMenuAberto] = useState<number | null>(null);
  const menuAbertoValido = itens.some((veiculo) => veiculo.idVeiculo === idMenuAberto) ? idMenuAberto : null;
  return (
    <div className="w-full overflow-x-auto">
      <table className="w-full border-collapse text-corpo-denso" aria-busy={carregando || undefined}>
        <caption className="sr-only">{MENSAGEM_FROTA_TABELA_LEGENDA}</caption>
        <thead className="bg-steel-fundo">
          <tr>
            {COLUNAS.map((coluna) => (
              <th
                key={coluna.chave}
                scope="col"
                className={`px-4 py-3 text-micro font-semibold uppercase tracking-wider text-contorno ${
                  coluna.direita ? 'whitespace-nowrap text-right' : 'text-left'
                }`}
              >
                {coluna.titulo}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
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

          {!carregando && itens.length === 0 && (
            <tr>
              <td colSpan={COLUNAS.length}>{vazio}</td>
            </tr>
          )}

          {!carregando &&
            itens.map((veiculo) => (
              <LinhaListagemVeiculo
                key={veiculo.idVeiculo}
                veiculo={veiculo}
                aoFalharFotoCapa={aoFalharFotoCapa}
                acoesStatus={acoesStatus}
                menuAberto={menuAbertoValido === veiculo.idVeiculo}
                aoAbrirMenu={setIdMenuAberto}
              />
            ))}
        </tbody>
      </table>
    </div>
  );
}

interface LinhaListagemVeiculoProps {
  veiculo: VeiculoRetorno;
  aoFalharFotoCapa?: () => void;
  acoesStatus?: AcoesStatusTabelaVeiculos;
  /** Este é o menu aberto da tabela. */
  menuAberto: boolean;
  /** Abre o menu desta linha (id) ou fecha qualquer um (null). */
  aoAbrirMenu: (idVeiculo: number | null) => void;
}

function LinhaListagemVeiculo({ veiculo, aoFalharFotoCapa, acoesStatus, menuAberto, aoAbrirMenu }: LinhaListagemVeiculoProps) {
  const nome = `${veiculo.marca} ${veiculo.modelo}`;
  return (
    <tr className="transition-colors hover:bg-steel-fundo/60">
      <td className="px-4 py-3.5 align-middle">
        <PlacaMercosul placa={veiculo.placa} />
      </td>

      <td className="px-4 py-3.5 align-middle">
        <div className="flex items-center gap-2">
          <ImagemVeiculo
            url={veiculo.fotoCapa ? urlDaImagem(veiculo.fotoCapa) : null}
            alt={mensagemFotoAlt(nome)}
            className="h-10 w-12 shrink-0 rounded"
            tamanhoIcone={18}
            aoFalhar={aoFalharFotoCapa}
          />
          <div className="flex min-w-0 flex-col">
            <span className="text-corpo-denso font-semibold text-texto-desenho">{nome}</span>
            <span className="mt-0.5 text-apoio text-contorno" title={veiculo.chassi}>
              {mensagemFrotaAnoEChassi(veiculo.anoModelo, abreviarChassi(veiculo.chassi))}
            </span>
          </div>
        </div>
      </td>

      <td className="px-4 py-3.5 align-middle">
        <span className="inline-flex items-center whitespace-nowrap rounded-sm bg-steel-suave px-2 py-1 text-rotulo text-texto-apoio">
          {veiculo.categoriaNome}
        </span>
      </td>

      <td className="whitespace-nowrap px-4 py-3.5 align-middle text-dado-tabular text-texto-desenho">
        {formatarQuilometragem(veiculo.kmAtual)}
      </td>

      <td className="px-4 py-3.5 align-middle">
        <EtiquetaStatusFrota status={veiculo.status} />
      </td>

      <td className="whitespace-nowrap px-4 py-3.5 text-right align-middle">
        <div className="inline-flex items-center justify-end gap-1">
          <Link
            to={caminhoEditarVeiculo(veiculo.idVeiculo)}
            title={MENSAGEM_FROTA_EDITAR}
            aria-label={mensagemFrotaEditarDoVeiculo(veiculo.placa)}
            className="inline-flex items-center justify-center rounded p-1.5 text-contorno transition-colors hover:bg-steel-suave hover:text-texto-desenho focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-orange-acao"
          >
            <Icone nome="edit" tamanho={18} />
          </Link>
          {acoesStatus && (
            <MenuOperacionalVeiculo
              placa={veiculo.placa}
              menu={montarMenuOperacional(veiculo.status)}
              aberto={menuAberto}
              desabilitado={acoesStatus.idEmAndamento !== null}
              aoAlternar={() => aoAbrirMenu(menuAberto ? null : veiculo.idVeiculo)}
              aoFechar={() => aoAbrirMenu(null)}
              aoEscolher={(item) => acoesStatus.aoEscolher(veiculo, item)}
            />
          )}
        </div>
      </td>
    </tr>
  );
}
