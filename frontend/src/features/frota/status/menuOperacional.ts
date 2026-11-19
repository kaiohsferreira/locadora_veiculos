/**
 * Menu de três pontos da linha da tela 11 (etapa 2): quais trocas de status cada situação oferece. Função pura, sem React.
 *
 * Só três trocas manuais existem (contrato veiculo-status v1): enviar para manutenção, voltar para disponível e dar baixa.
 * RESERVADO, LOCADO, VENDIDO e BAIXADO não têm troca: o menu mostra a frase do servidor no lugar dos itens.
 */
import {
  MENSAGEM_FROTA_FRASE_STATUS_POR_FLUXO,
  MENSAGEM_FROTA_FRASE_VENDIDO_OU_BAIXADO,
  MENSAGEM_FROTA_ITEM_BAIXA,
  MENSAGEM_FROTA_ITEM_DISPONIVEL,
  MENSAGEM_FROTA_ITEM_MANUTENCAO,
  MENSAGEM_FROTA_SUCESSO_BAIXA,
  MENSAGEM_FROTA_SUCESSO_DISPONIVEL,
  MENSAGEM_FROTA_SUCESSO_MANUTENCAO,
} from '@/lib/mensagens';
import { cargoPermitido } from '@/routes/autenticacao/usePermissoes';
import type { CargoFuncionario } from '@/types/enums';
import type { DestinoManualStatusVeiculo, StatusVeiculo } from '@/types/veiculos';

/** Cargos que veem e usam o menu (o servidor confere de novo). */
export const CARGOS_TROCA_STATUS_VEICULO: readonly CargoFuncionario[] = ['GESTOR_FROTA', 'ADMINISTRADOR'];

/** Um item do menu. `perigoso` marca a baixa (cor de perigo) e `destino` é o status que a API recebe. */
export interface ItemMenuOperacional {
  chave: 'manutencao' | 'disponivel' | 'baixa';
  rotulo: string;
  destino: DestinoManualStatusVeiculo;
  perigoso: boolean;
}

/** Conteúdo do menu de uma linha: os itens (pode ser vazio) e, quando não há item, a frase que explica o porquê. */
export interface MenuOperacional {
  itens: readonly ItemMenuOperacional[];
  frase: string | null;
}

const ITEM_MANUTENCAO: ItemMenuOperacional = {
  chave: 'manutencao',
  rotulo: MENSAGEM_FROTA_ITEM_MANUTENCAO,
  destino: 'MANUTENCAO',
  perigoso: false,
};

const ITEM_DISPONIVEL: ItemMenuOperacional = {
  chave: 'disponivel',
  rotulo: MENSAGEM_FROTA_ITEM_DISPONIVEL,
  destino: 'DISPONIVEL',
  perigoso: false,
};

const ITEM_BAIXA: ItemMenuOperacional = {
  chave: 'baixa',
  rotulo: MENSAGEM_FROTA_ITEM_BAIXA,
  destino: 'BAIXADO',
  perigoso: true,
};

const MENU_POR_STATUS: Readonly<Record<StatusVeiculo, MenuOperacional>> = {
  DISPONIVEL: { itens: [ITEM_MANUTENCAO, ITEM_BAIXA], frase: null },
  MANUTENCAO: { itens: [ITEM_DISPONIVEL, ITEM_BAIXA], frase: null },
  RESERVADO: { itens: [], frase: MENSAGEM_FROTA_FRASE_STATUS_POR_FLUXO },
  LOCADO: { itens: [], frase: MENSAGEM_FROTA_FRASE_STATUS_POR_FLUXO },
  VENDIDO: { itens: [], frase: MENSAGEM_FROTA_FRASE_VENDIDO_OU_BAIXADO },
  BAIXADO: { itens: [], frase: MENSAGEM_FROTA_FRASE_VENDIDO_OU_BAIXADO },
};

const MENU_VAZIO: MenuOperacional = { itens: [], frase: null };

/** Itens e frase do menu de um veículo, pela situação dele. Status fora dos seis: menu vazio (a linha não quebra). */
export function montarMenuOperacional(status: StatusVeiculo): MenuOperacional {
  return MENU_POR_STATUS[status] ?? MENU_VAZIO;
}

/** Só a baixa pede confirmação; as outras trocas vão direto para a API. */
export function itemPedeConfirmacao(item: ItemMenuOperacional): boolean {
  return item.destino === 'BAIXADO';
}

/** Só GESTOR_FROTA e ADMINISTRADOR veem o botão. Sem cargo (cliente ou sessão vazia), nunca. */
export function podeTrocarStatusVeiculo(cargo: CargoFuncionario | null): boolean {
  return cargoPermitido(cargo, CARGOS_TROCA_STATUS_VEICULO);
}

/** Aviso de sucesso de cada troca (um texto por ação). */
const SUCESSO_POR_DESTINO: Readonly<Record<DestinoManualStatusVeiculo, string>> = {
  MANUTENCAO: MENSAGEM_FROTA_SUCESSO_MANUTENCAO,
  DISPONIVEL: MENSAGEM_FROTA_SUCESSO_DISPONIVEL,
  BAIXADO: MENSAGEM_FROTA_SUCESSO_BAIXA,
};

export function mensagemSucessoTrocaStatus(destino: DestinoManualStatusVeiculo): string {
  return SUCESSO_POR_DESTINO[destino];
}
