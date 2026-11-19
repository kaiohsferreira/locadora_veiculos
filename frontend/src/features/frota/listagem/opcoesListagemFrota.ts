/**
 * Opções dos filtros e textos da frota que as duas telas (11 e 12) usam. Funções puras, sem React.
 */
import type { OpcaoSelect } from '@/components/ui';
import { mensagemVeiculoRotuloCategoria } from '@/lib/mensagens';
import type { CategoriaVeiculo } from '@/types/categorias';
import { ROTULO_STATUS_VEICULO, type StatusVeiculo } from '@/types/enums';

/** Opções da categoria (valor = id), com a descrição do grupo. Vazio enquanto as categorias carregam. */
export function montarOpcoesCategoriaFrota(categorias: readonly CategoriaVeiculo[] | undefined): OpcaoSelect[] {
  return (categorias ?? []).map((categoria) => ({
    valor: String(categoria.idCategoria),
    rotulo: mensagemVeiculoRotuloCategoria(categoria.nome, categoria.descricao),
  }));
}

/**
 * Situações que o filtro de status da tela 12 oferece (com "Todos os Status" por cima, como opção vazia). Vendido e
 * baixado só aparecem em "Todos": a lista sem filtro os traz.
 */
export const STATUS_FILTRO_LISTAGEM_FROTA: readonly StatusVeiculo[] = ['DISPONIVEL', 'LOCADO', 'MANUTENCAO', 'RESERVADO'];

/** Opções do filtro de status da tela 12 (rótulos singulares do projeto). */
export function opcoesStatusListagemFrota(): OpcaoSelect[] {
  return STATUS_FILTRO_LISTAGEM_FROTA.map((status) => ({ valor: status, rotulo: ROTULO_STATUS_VEICULO[status] }));
}

/** Chassi com menos caracteres que isto aparece inteiro (abreviar não economiza nada). */
export const TAMANHO_MINIMO_CHASSI_ABREVIADO = 10;

/**
 * Chassi abreviado como no desenho 12: os 5 primeiros caracteres, reticências e os 4 últimos ("9BR58...7821"). Chassi
 * curto sai inteiro; vazio sai vazio. O chassi inteiro fica no `title` da célula.
 */
export function abreviarChassi(chassi: string): string {
  const texto = chassi.trim();
  if (texto.length < TAMANHO_MINIMO_CHASSI_ABREVIADO) return texto;
  return `${texto.slice(0, 5)}...${texto.slice(-4)}`;
}
