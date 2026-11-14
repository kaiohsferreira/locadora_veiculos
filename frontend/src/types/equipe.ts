/**
 * Equipe da locadora (contrato equipe.contract.json, P10-BE-03, tela 41). Nomes em português como no contrato.
 * Nenhum tipo aqui traz CPF completo, senha ou hash: o CPF chega mascarado do servidor.
 */
import type { CargoFuncionario } from '@/types/enums';

/** Um funcionário da equipe (ReturnFuncionarioVO). Usado na lista e na consulta de edição. */
export interface FuncionarioEquipe {
  idFuncionario: number;
  nome: string;
  /** Formato 341.***.***-12, mascarado pelo servidor. */
  cpfMascarado: string;
  matricula: string;
  email: string | null;
  cargo: CargoFuncionario;
  /** Falso quando o acesso foi desativado. A linha continua na lista, como inativa. */
  ativo: boolean;
  /** Data de admissão no formato "AAAA-MM-DD" (DateOnly). */
  dataAdmissao: string;
  /** Data de admissão já formatada pelo servidor (dd/MM/yyyy). A tela usa `formatarData` do front. */
  dataAdmissaoStr: string;
  /** Verdadeiro quando o funcionário é quem está logado. */
  ehVoce: boolean;
}

/** Resposta de GET /api/equipe (ReturnEquipeVO). Os totais vêm da empresa inteira, sem os filtros da busca. */
export interface RespostaEquipe {
  itens: FuncionarioEquipe[];
  /** Contagem COM os filtros (é a que serve para a paginação). */
  total: number;
  pagina: number;
  tamanhoPagina: number;
  totalAtivos: number;
  totalInativos: number;
  /** Administradores ativos. */
  totalAdministradores: number;
  /** Ativos e inativos juntos. */
  totalCadastrados: number;
}

/** Totais dos indicadores da tela (o mesmo recorte de RespostaEquipe, sem a lista). */
export type TotaisEquipe = Pick<
  RespostaEquipe,
  'totalAtivos' | 'totalInativos' | 'totalAdministradores' | 'totalCadastrados'
>;

/** Filtros e paginação já prontos para a chamada. Campos ausentes significam "sem filtro". */
export interface FiltroEquipe {
  /** Base 1. */
  pagina: number;
  /** Padrão do contrato: 20; máximo 100. */
  tamanhoPagina: number;
  /** Até 60 caracteres. Ausente quando vazia ou só espaços. */
  busca?: string;
  cargo?: CargoFuncionario;
  /** true = só ativos; false = só inativos; ausente = todos. */
  ativo?: boolean;
}

/** Corpo do POST /api/equipe (FuncionarioVO). A empresa vem do servidor, nunca daqui. */
export interface RequisicaoCadastroFuncionario {
  nome: string;
  /** Só dígitos. */
  cpf: string;
  matricula: string;
  email: string;
  /** "AAAA-MM-DD". */
  dataAdmissao: string;
  cargo: CargoFuncionario;
  /** Existe só nesta chamada. Nunca volta na resposta. */
  senhaInicial: string;
}

/** Corpo do PUT /api/equipe/{id} (FuncionarioEdicaoVO). Sem CPF e sem senha. */
export interface RequisicaoEdicaoFuncionario {
  nome: string;
  matricula: string;
  email: string;
  /** "AAAA-MM-DD". */
  dataAdmissao: string;
  cargo: CargoFuncionario;
}

/** Corpo do PATCH /api/equipe/{id}/situacao (SituacaoFuncionarioVO). */
export interface RequisicaoSituacaoFuncionario {
  ativo: boolean;
}
