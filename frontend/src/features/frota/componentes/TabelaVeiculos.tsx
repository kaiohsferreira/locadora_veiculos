import { useState, type ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { Esqueleto, ImagemVeiculo } from '@/components/ui';
import { caminhoEditarVeiculo } from '@/routes/caminhos';
import { MenuOperacionalVeiculo } from '@/features/frota/componentes/MenuOperacionalVeiculo';
import { ChassiEAno, PlacaMercosul } from '@/features/frota/componentes/PlacaEChassiFrota';
import { montarMenuOperacional, type ItemMenuOperacional } from '@/features/frota/status/menuOperacional';
import { formatarQuilometragem } from '@/lib/formatters';
import {
  MENSAGEM_FROTA_BOTAO_DETALHES,
  MENSAGEM_FROTA_COLUNA_ACOES,
  MENSAGEM_FROTA_COLUNA_CHASSI,
  MENSAGEM_FROTA_COLUNA_KM,
  MENSAGEM_FROTA_COLUNA_PLACA,
  MENSAGEM_FROTA_COLUNA_STATUS,
  MENSAGEM_FROTA_COLUNA_VEICULO,
  MENSAGEM_FROTA_TABELA_LEGENDA,
  mensagemFotoAlt,
  mensagemFrotaDetalhesDoVeiculo,
} from '@/lib/mensagens';
import { urlDaImagem } from '@/lib/fotosVeiculo';
import type { CategoriaVeiculo } from '@/types/categorias';
import type { VeiculoRetorno } from '@/types/veiculos';
import { EtiquetaStatusFrota } from './EtiquetaStatusFrota';

/** Colunas da tabela da frota (cabeçalho e número de células). Sem câmbio, motor, pátio e caixas de seleção. */
const COLUNAS = [
  { chave: 'veiculo', titulo: MENSAGEM_FROTA_COLUNA_VEICULO, alinhamento: 'esquerda' },
  { chave: 'placa', titulo: MENSAGEM_FROTA_COLUNA_PLACA, alinhamento: 'esquerda' },
  { chave: 'chassi', titulo: MENSAGEM_FROTA_COLUNA_CHASSI, alinhamento: 'esquerda' },
  { chave: 'km', titulo: MENSAGEM_FROTA_COLUNA_KM, alinhamento: 'esquerda' },
  { chave: 'status', titulo: MENSAGEM_FROTA_COLUNA_STATUS, alinhamento: 'esquerda' },
  { chave: 'acoes', titulo: MENSAGEM_FROTA_COLUNA_ACOES, alinhamento: 'direita' },
] as const;

const LINHAS_ESQUELETO = 5;

/**
 * Ações de troca de status da linha (menu de três pontos da tela 11). Sem esta propriedade, a coluna fica só com o botão
 * Detalhes. A tela 12 tem o mesmo menu na tabela irmã (TabelaListagemVeiculos), que usa este mesmo tipo. A tela já confere
 * o cargo antes de passar.
 */
export interface AcoesStatusTabelaVeiculos {
  /** Veículo com troca em andamento: a linha dele fica bloqueada. Com um pedido em curso, todos os menus ficam bloqueados. */
  idEmAndamento: number | null;
  /** Chamado com o veículo e o item escolhido no menu. */
  aoEscolher: (veiculo: VeiculoRetorno, item: ItemMenuOperacional) => void;
}

export interface TabelaVeiculosProps {
  /** Veículos da página atual. A paginação é do servidor. */
  itens: readonly VeiculoRetorno[];
  /** Mostra linhas de esqueleto no lugar dos dados (primeira carga). */
  carregando: boolean;
  /** Conteúdo quando não há linhas (ex.: estado vazio da tela). */
  vazio: ReactNode;
  /** Chamado quando a capa de um veículo falha ao abrir (a tela decide se refaz a consulta). */
  aoFalharFotoCapa?: () => void;
  /**
   * Lista de categorias, para mostrar a descrição ao lado do grupo (ex.: "Hatch Econômico"). Sem a lista, ou sem a
   * categoria nela, a linha mostra só o grupo.
   */
  categorias?: readonly CategoriaVeiculo[];
  /** Menu de troca de status em cada linha. Opcional: sem ela, a tabela fica igual à de antes. */
  acoesStatus?: AcoesStatusTabelaVeiculos;
}

/**
 * Tabela da frota (tela 11). A tela 12 tem a tabela irmã TabelaListagemVeiculos (decisão 4 do contrato da tela 12); o tipo
 * AcoesStatusTabelaVeiculos é compartilhado. Cada linha tem a capa (com ícone de carro quando não há foto ou ela falha), a
 * placa, o chassi e ano, a quilometragem, a situação e o botão Detalhes, que abre o cadastro do veículo. Com `acoesStatus`,
 * o botão de três pontos fica ao lado do Detalhes.
 */
export function TabelaVeiculos({
  itens,
  carregando,
  vazio,
  aoFalharFotoCapa,
  categorias,
  acoesStatus,
}: TabelaVeiculosProps) {
  const descricaoPorCategoria = new Map<number, string | null>(
    (categorias ?? []).map((categoria) => [categoria.idCategoria, categoria.descricao]),
  );
  /** Um menu aberto por vez: guarda o id da linha. Se a linha sumir da página, o menu fecha sozinho. */
  const [idMenuAberto, setIdMenuAberto] = useState<number | null>(null);
  const menuAbertoValido = itens.some((veiculo) => veiculo.idVeiculo === idMenuAberto) ? idMenuAberto : null;
  return (
    <div className="w-full overflow-x-auto">
      <table className="min-w-full border-collapse text-corpo-denso" aria-busy={carregando || undefined}>
        <caption className="sr-only">{MENSAGEM_FROTA_TABELA_LEGENDA}</caption>
        <thead className="bg-steel-suave">
          <tr>
            {COLUNAS.map((coluna) => (
              <th
                key={coluna.chave}
                scope="col"
                className={`px-4 py-3 text-micro font-medium uppercase tracking-wider text-texto-apoio ${
                  coluna.alinhamento === 'direita' ? 'text-right' : 'text-left'
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
              <LinhaVeiculo
                key={veiculo.idVeiculo}
                veiculo={veiculo}
                aoFalharFotoCapa={aoFalharFotoCapa}
                descricaoCategoria={descricaoPorCategoria.get(veiculo.idCategoria) ?? null}
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

interface LinhaVeiculoProps {
  veiculo: VeiculoRetorno;
  aoFalharFotoCapa?: () => void;
  /** Descrição da categoria do veículo (vinda da lista de categorias), ou nula quando não há. */
  descricaoCategoria: string | null;
  acoesStatus?: AcoesStatusTabelaVeiculos;
  /** Este é o menu aberto da tabela. */
  menuAberto: boolean;
  /** Abre o menu desta linha (id) ou fecha qualquer um (null). */
  aoAbrirMenu: (idVeiculo: number | null) => void;
}

function LinhaVeiculo({
  veiculo,
  aoFalharFotoCapa,
  descricaoCategoria,
  acoesStatus,
  menuAberto,
  aoAbrirMenu,
}: LinhaVeiculoProps) {
  const nome = `${veiculo.marca} ${veiculo.modelo}`;
  const botaoDetalhes = (
    <Link
      to={caminhoEditarVeiculo(veiculo.idVeiculo)}
      className="inline-flex h-8 items-center rounded-sm bg-steel-suave px-2.5 text-rotulo font-semibold text-texto-desenho transition-colors hover:bg-steel-alto"
    >
      {MENSAGEM_FROTA_BOTAO_DETALHES}
      <span className="sr-only"> {mensagemFrotaDetalhesDoVeiculo(veiculo.placa)}</span>
    </Link>
  );
  return (
    <tr className="transition-colors hover:bg-steel-fundo/60">
      <td className="px-4 py-3.5 align-middle">
        <div className="flex items-center gap-2">
          <ImagemVeiculo
            url={veiculo.fotoCapa ? urlDaImagem(veiculo.fotoCapa) : null}
            alt={mensagemFotoAlt(nome)}
            className="h-9 w-12 shrink-0 rounded-sm shadow-sm"
            tamanhoIcone={18}
            aoFalhar={aoFalharFotoCapa}
          />
          <div className="flex min-w-0 flex-col">
            <span className="truncate text-corpo-denso font-semibold text-texto-desenho">{nome}</span>
            <span className="flex items-center gap-1 text-micro text-texto-apoio">
              <span className="rounded-sm bg-steel-suave px-1 font-semibold text-navy">{veiculo.categoriaNome}</span>
              {descricaoCategoria && <span>{descricaoCategoria}</span>}
            </span>
          </div>
        </div>
      </td>

      <td className="px-4 py-3.5 align-middle">
        <PlacaMercosul placa={veiculo.placa} />
      </td>

      <td className="px-4 py-3.5 align-middle">
        <ChassiEAno chassi={veiculo.chassi} anoFabricacao={veiculo.anoFabricacao} anoModelo={veiculo.anoModelo} />
      </td>

      <td className="px-4 py-3.5 align-middle">
        <span className="whitespace-nowrap text-corpo-denso font-bold text-texto-desenho tabular-nums">
          {formatarQuilometragem(veiculo.kmAtual)}
        </span>
      </td>

      <td className="px-4 py-3.5 align-middle">
        <EtiquetaStatusFrota status={veiculo.status} />
      </td>

      <td className="px-4 py-3.5 text-right align-middle">
        {acoesStatus ? (
          <div className="inline-flex items-center justify-end gap-1">
            {botaoDetalhes}
            <MenuOperacionalVeiculo
              placa={veiculo.placa}
              menu={montarMenuOperacional(veiculo.status)}
              aberto={menuAberto}
              desabilitado={acoesStatus.idEmAndamento !== null}
              aoAlternar={() => aoAbrirMenu(menuAberto ? null : veiculo.idVeiculo)}
              aoFechar={() => aoAbrirMenu(null)}
              aoEscolher={(item) => acoesStatus.aoEscolher(veiculo, item)}
            />
          </div>
        ) : (
          botaoDetalhes
        )}
      </td>
    </tr>
  );
}

