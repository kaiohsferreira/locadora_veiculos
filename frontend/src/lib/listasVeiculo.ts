/**
 * Listas fechadas da tela de cadastro de veículo (tela 05), decididas pelo Kaio em 16/11/2026. Sem React.
 *
 * - Marca: lista do Kaio (ordem alfabética). O servidor aceita qualquer texto: um valor gravado fora da lista aparece
 *   na edição como opção extra, sem ser trocado em silêncio.
 * - Cor: as quatro amostras do desenho. Clicar na cor marcada de novo desmarca (a cor é opcional).
 * - Status inicial: travado em "Disponível para Locação Imediata" (RF-02: o veículo nasce disponível; nada é enviado).
 */
import type { OpcaoSelect } from '@/components/ui';
import type { CategoriaVeiculo } from '@/types/categorias';
import {
  MENSAGEM_VEICULO_COR_BRANCO,
  MENSAGEM_VEICULO_COR_CINZA,
  MENSAGEM_VEICULO_COR_PRATA,
  MENSAGEM_VEICULO_COR_PRETO,
  MENSAGEM_VEICULO_STATUS_INICIAL_DISPONIVEL,
  mensagemVeiculoRotuloCategoria,
} from '@/lib/mensagensVeiculos';

/** Marcas da lista fechada, em ordem alfabética. */
export const MARCAS_VEICULO = [
  'Audi',
  'BMW',
  'BYD',
  'Caoa Chery',
  'Chevrolet',
  'Citroën',
  'Fiat',
  'Ford',
  'GWM',
  'Honda',
  'Hyundai',
  'Jeep',
  'Kia',
  'Land Rover',
  'Mercedes-Benz',
  'Mitsubishi',
  'Nissan',
  'Peugeot',
  'Ram',
  'Renault',
  'Toyota',
  'Volkswagen',
  'Volvo',
] as const;

/** Cor da lista: valor enviado, texto da amostra e classe da amostra e do ícone de marcado (tokens do tema). */
export interface CorVeiculo {
  valor: string;
  rotulo: string;
  classeAmostra: string;
  /** Cor do ícone de "marcado" sobre a amostra: branco nas escuras, escura na branca. */
  classeMarcado: string;
}

/** As quatro cores do desenho 05. O valor enviado da cinza é "Cinza Silverstone" (o rótulo mostra "Cinza Silver"). */
export const CORES_VEICULO: readonly CorVeiculo[] = [
  {
    valor: MENSAGEM_VEICULO_COR_PRETO,
    rotulo: MENSAGEM_VEICULO_COR_PRETO,
    classeAmostra: 'bg-amostra-preto',
    classeMarcado: 'text-branco',
  },
  {
    valor: MENSAGEM_VEICULO_COR_BRANCO,
    rotulo: MENSAGEM_VEICULO_COR_BRANCO,
    classeAmostra: 'bg-branco',
    classeMarcado: 'text-texto-desenho',
  },
  {
    valor: MENSAGEM_VEICULO_COR_PRATA,
    rotulo: MENSAGEM_VEICULO_COR_PRATA,
    classeAmostra: 'bg-amostra-prata',
    classeMarcado: 'text-texto-desenho',
  },
  {
    valor: 'Cinza Silverstone',
    rotulo: MENSAGEM_VEICULO_COR_CINZA,
    classeAmostra: 'bg-amostra-cinza',
    classeMarcado: 'text-branco',
  },
];

/** Status inicial travado: a única opção, que é o status que o veículo recebe no cadastro. */
export const STATUS_INICIAL_VEICULO_TRAVADO = {
  valor: 'DISPONIVEL',
  rotulo: MENSAGEM_VEICULO_STATUS_INICIAL_DISPONIVEL,
} as const;

/**
 * Opções da marca: as da lista, e o valor atual logo no fim quando ele não está na lista (edição de veículo com marca
 * gravada por outra via). Vazio não vira opção.
 */
export function opcoesMarcaVeiculo(valorAtual: string): OpcaoSelect[] {
  const opcoes: OpcaoSelect[] = MARCAS_VEICULO.map((marca) => ({ valor: marca, rotulo: marca }));
  const atual = valorAtual.trim();
  if (atual.length > 0 && !(MARCAS_VEICULO as readonly string[]).includes(atual)) {
    opcoes.push({ valor: atual, rotulo: atual });
  }
  return opcoes;
}

/** A cor atual é uma das quatro? Se não, é um valor gravado fora da lista (ele aparece como opção extra). */
export function corForaDaListaVeiculo(valorAtual: string): boolean {
  const atual = valorAtual.trim();
  return atual.length > 0 && !CORES_VEICULO.some((cor) => cor.valor === atual);
}

/** Categoria gravada no veículo que está sendo editado (pode estar desativada, então não vem na lista de ativas). */
export interface CategoriaAtualVeiculo {
  idCategoria: number;
  nome: string;
}

/**
 * Opções do seletor de categoria: as ativas, em ordem da API. Se a categoria do veículo não está entre elas (ex.: foi
 * desativada), ela entra no fim como opção extra, para a edição não trocar o valor em silêncio.
 */
export function opcoesCategoriaVeiculo(
  categorias: readonly CategoriaVeiculo[],
  atual: CategoriaAtualVeiculo | null,
): OpcaoSelect[] {
  const opcoes: OpcaoSelect[] = categorias.map((categoria) => ({
    valor: String(categoria.idCategoria),
    rotulo: mensagemVeiculoRotuloCategoria(categoria.nome, categoria.descricao),
  }));
  if (atual !== null && !categorias.some((categoria) => categoria.idCategoria === atual.idCategoria)) {
    opcoes.push({ valor: String(atual.idCategoria), rotulo: mensagemVeiculoRotuloCategoria(atual.nome, null) });
  }
  return opcoes;
}

/** Situação do seletor de categoria: carregando, erro (com "Tentar de novo"), vazio ou pronto. */
export type SituacaoCategoriasVeiculo = 'carregando' | 'erro' | 'vazio' | 'pronto';

/** Escolhe a situação do seletor: erro e carregamento têm prioridade; vazio é quando não há nenhuma opção. */
export function situacaoCategoriasVeiculo(estado: {
  carregando: boolean;
  erro: boolean;
  quantidadeOpcoes: number;
}): SituacaoCategoriasVeiculo {
  if (estado.carregando) return 'carregando';
  if (estado.erro) return 'erro';
  return estado.quantidadeOpcoes === 0 ? 'vazio' : 'pronto';
}
