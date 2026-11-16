import { mensagemReservasBoasVindas, MENSAGEM_RESERVAS_DESCRICAO } from '@/lib/mensagens';

export interface CabecalhoReservasProps {
  /** Nome do cliente, vindo da sessão. */
  nome: string;
}

/** Título e descrição da página. Só o que a sessão e o texto verdadeiro sustentam (sem pontos, sem selo de fidelidade). */
export function CabecalhoReservas({ nome }: CabecalhoReservasProps) {
  return (
    <header className="flex flex-col gap-1">
      <h1 className="break-words text-titulo tracking-tight text-navy sm:text-titulo-hero">
        {mensagemReservasBoasVindas(nome)}
      </h1>
      <p className="max-w-2xl text-corpo-denso text-status-neutro">{MENSAGEM_RESERVAS_DESCRICAO}</p>
    </header>
  );
}
