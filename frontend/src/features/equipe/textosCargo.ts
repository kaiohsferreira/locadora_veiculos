/**
 * O que cada cargo pode fazer (caixa "O que este cargo pode fazer" do desenho 41). Texto fixo por cargo.
 *
 * Origem: responsabilidades de cada perfil na seção 2.2 de Especificacao_AutoVia_Requisitos.txt. O atendente bate com
 * as três marcas do desenho. Os demais saem da mesma tabela, pela ordem dos blocos de texto. Os dois textos do
 * Administrador foram passados pelo dono do produto. O servidor usa o cargo em poucas regras: os valores em reais da
 * visão geral (só ADMINISTRADOR e FINANCEIRO). A troca de status da frota (menu da tela 11, etapa 2) usa
 * GESTOR_FROTA e ADMINISTRADOR. Os demais textos descrevem a intenção do perfil, não uma regra já aplicada.
 */
import type { CargoFuncionario } from '@/types/enums';

/** Responsabilidades de cada cargo, em frases curtas (uma linha da caixa por item). */
export const RESPONSABILIDADES_CARGO: Readonly<Record<CargoFuncionario, readonly string[]>> = {
  ATENDENTE: [
    'Verificar cadastros de clientes',
    'Fazer check-in e check-out',
    'Registrar vistorias e emitir contratos',
  ],
  VENDEDOR: ['Conduzir a negociação e registrar a venda de veículos'],
  GESTOR_FROTA: ['Cadastrar veículos', 'Abrir e encerrar manutenções', 'Contratar apólices'],
  FINANCEIRO: ['Lançar e baixar cobranças', 'Acompanhar inadimplência'],
  ADMINISTRATIVO: ['Cuidar da documentação de transferência', 'Acompanhar sinistros'],
  ADMINISTRADOR: ['Gerencia a equipe e os dados da locadora', 'Acessa todos os módulos do painel'],
};

/** Ícone Material Symbols de cada cargo (o mesmo do desenho 41). */
export const ICONE_CARGO: Readonly<Record<CargoFuncionario, string>> = {
  ATENDENTE: 'support_agent',
  VENDEDOR: 'sell',
  GESTOR_FROTA: 'local_shipping',
  FINANCEIRO: 'account_balance_wallet',
  ADMINISTRATIVO: 'corporate_fare',
  ADMINISTRADOR: 'shield_person',
};
