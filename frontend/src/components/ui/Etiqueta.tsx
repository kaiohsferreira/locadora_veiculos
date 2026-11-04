import { ROTULO_STATUS, type StatusDominio } from '@/types/enums';

/** sucesso = verde, pendente = laranja, perigo = vermelho, neutro = cinza. */
export type TomEtiqueta = 'sucesso' | 'pendente' | 'perigo' | 'neutro';

interface EtiquetaBaseProps {
  className?: string;
}

interface EtiquetaDeStatusProps extends EtiquetaBaseProps {
  /** Valor de status do domínio (ex.: "DISPONIVEL", "EM_ANDAMENTO"): define texto e tom. */
  status: StatusDominio;
  /** Substitui o rótulo padrão do status. */
  rotulo?: string;
  /** Substitui o tom padrão do status. */
  tom?: TomEtiqueta;
}

interface EtiquetaLivreProps extends EtiquetaBaseProps {
  status?: undefined;
  /** Texto da etiqueta (ex.: "Vence em 30 dias"). */
  rotulo: string;
  tom: TomEtiqueta;
}

export type EtiquetaProps = EtiquetaDeStatusProps | EtiquetaLivreProps;

/**
 * Tom de cada status. Verde = concluído ou disponível; laranja = pendente ou
 * em andamento; vermelho = atrasado ou bloqueado; cinza = cancelado ou fora da frota.
 */
const TOM_POR_STATUS: Readonly<Record<StatusDominio, TomEtiqueta>> = {
  // Cadastro do cliente
  PENDENTE: 'pendente',
  APROVADO: 'sucesso',
  RECUSADO: 'perigo',
  // Veículo
  DISPONIVEL: 'sucesso',
  RESERVADO: 'pendente',
  LOCADO: 'pendente',
  MANUTENCAO: 'perigo',
  VENDIDO: 'neutro',
  BAIXADO: 'neutro',
  // Reserva
  ATIVA: 'pendente',
  CONFIRMADA: 'sucesso',
  CANCELADA: 'neutro',
  CONCLUIDA: 'sucesso',
  // Locação
  EM_ANDAMENTO: 'pendente',
  FINALIZADA: 'sucesso',
  ATRASADA: 'perigo',
  // Documentação da venda
  EM_TRANSFERENCIA: 'pendente',
  // Pagamento
  PAGO: 'sucesso',
  ATRASADO: 'perigo',
  CANCELADO: 'neutro',
  // Sinistro
  ABERTO: 'pendente',
  EM_ANALISE: 'pendente',
  NEGADO: 'perigo',
  ENCERRADO: 'sucesso',
};

const CLASSES_TOM: Readonly<Record<TomEtiqueta, string>> = {
  sucesso: 'bg-status-sucesso-fundo text-status-sucesso',
  pendente: 'bg-status-pendente-fundo text-status-pendente',
  perigo: 'bg-status-perigo-fundo text-status-perigo',
  neutro: 'bg-status-neutro-fundo text-status-neutro',
};

/**
 * Etiqueta de status: sempre exibe TEXTO; a cor apenas reforça a informação.
 * Uso: `<Etiqueta status={veiculo.status} />` ou `<Etiqueta rotulo="Vence em 30 dias" tom="pendente" />`.
 */
export function Etiqueta(props: EtiquetaProps) {
  const tom: TomEtiqueta = props.tom ?? (props.status ? TOM_POR_STATUS[props.status] : 'neutro');
  const rotulo = props.rotulo ?? (props.status ? ROTULO_STATUS[props.status] : '');

  return (
    <span
      className={`inline-flex items-center gap-1.5 whitespace-nowrap rounded-md px-2 py-0.5 text-legenda font-semibold ${CLASSES_TOM[tom]} ${props.className ?? ''}`}
    >
      <span className="h-1.5 w-1.5 rounded-full bg-current" aria-hidden="true" />
      {rotulo}
    </span>
  );
}
