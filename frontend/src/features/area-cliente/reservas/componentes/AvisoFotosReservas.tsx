import { Icone } from '@/components/ui';

export interface AvisoFotosReservasProps {
  /** Mensagem que o servidor mandou em erroFotoCapa. Texto pronto: o front não traduz nem acrescenta nada. */
  mensagem: string;
}

/**
 * Aviso em linha, perto da lista, quando a resposta da aba trouxe erro de foto em algum cartão. Não é flutuante e não
 * se repete: a tela só o mostra enquanto a resposta exibida tiver o erro. Ícone e texto (nunca só cor); papel de status
 * para leitor de tela.
 */
export function AvisoFotosReservas({ mensagem }: AvisoFotosReservasProps) {
  return (
    <output className="mb-6 flex items-center gap-2 rounded-cartao bg-status-pendente-fundo px-4 py-3 text-corpo-denso text-status-pendente">
      <Icone nome="image_not_supported" tamanho={18} />
      <span>{mensagem}</span>
    </output>
  );
}
