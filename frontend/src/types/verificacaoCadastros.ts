/**
 * Verificação de cadastros da locadora (contrato cliente-fila-verificacao v1, tela 18). Nomes em camelCase como o JSON
 * do backend. Datas e horas da fila e do detalhe vêm como texto "dd/MM/yyyy HH:mm" (padrão do projeto); datas de
 * nascimento e validade da CNH vêm como "dd/MM/yyyy"; o envio do documento também vem como "dd/MM/yyyy HH:mm" (ver
 * `RespostaDocumentoCliente`). CPF vem formatado "000.000.000-00". Telefone e CEP vêm só com dígitos.
 */
import type { CategoriaCnh, StatusCadastroCliente } from '@/types/enums';
import type { RespostaDocumentoCliente } from '@/types/documentosCliente';

/** Decisões que a tela envia (VerificacaoCadastroVO). */
export type DecisaoVerificacaoCliente = 'APROVADO' | 'RECUSADO';

/** Linha da fila (ReturnClienteFilaVO). Status, datas e justificativa vêm do vínculo com a locadora, não do cadastro geral. */
export interface ClienteFila {
  idCliente: number;
  nome: string;
  /** CPF formatado pelo servidor ("000.000.000-00"). A tela aplica `formatarCpf`, que aceita formatado ou só dígitos. */
  cpf: string;
  /** Data e hora do cadastro ("dd/MM/yyyy HH:mm"). */
  dataCadastro: string;
  /** Status do pedido na locadora. */
  status: StatusCadastroCliente;
  /** Data e hora do pedido nesta locadora ("dd/MM/yyyy HH:mm"). */
  dataSolicitacao: string;
  /** Data e hora da última decisão nesta locadora ("dd/MM/yyyy HH:mm"), ou nulo. */
  dataVerificacao: string | null;
  /** Motivo da última recusa nesta locadora, ou nulo. */
  justificativaRecusa: string | null;
}

/** Detalhe do cadastro para o painel (ReturnClienteVerificacaoVO): a linha da fila mais os dados pessoais e de endereço. */
export interface ClienteVerificacao extends ClienteFila {
  /** Data de nascimento ("dd/MM/yyyy"). */
  dataNascimento: string;
  email: string;
  /** Telefone só com dígitos. */
  telefone: string;
  cnhNumero: string;
  cnhCategoria: CategoriaCnh;
  /** Validade da CNH ("dd/MM/yyyy"). */
  cnhValidade: string;
  logradouro: string | null;
  numero: string | null;
  complemento: string | null;
  bairro: string | null;
  cidade: string | null;
  uf: string | null;
  cep: string | null;
}

/**
 * Documento ativo do cliente para a equipe (ReturnClienteDocumentoVO, o mesmo VO do dono). `url` é o link temporário de
 * leitura (nulo quando o arquivo não pode ser assinado) e `expiraEm` o prazo dele. A tela usa o link só enquanto o
 * painel está aberto: nada vai para localStorage, sessionStorage ou cache persistente.
 */
export type DocumentoVerificacao = RespostaDocumentoCliente;

/** Filtros da fila (GET /api/clientes/verificacoes). Ausente = sem filtro; busca vazia não entra. */
export interface FiltroFilaVerificacao {
  pagina: number;
  tamanhoPagina: number;
  status?: StatusCadastroCliente;
  busca?: string;
}

/** Corpo do PATCH de decisão (VerificacaoCadastroVO). A justificativa só vai na recusa. */
export interface RequisicaoDecisaoVerificacao {
  decisao: DecisaoVerificacaoCliente;
  justificativa?: string;
}
