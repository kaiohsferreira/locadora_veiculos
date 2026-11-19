import { Icone } from '@/components/ui';

export interface AvisoFotoCapaFrotaProps {
  /** Mensagem que o servidor mandou em erroFotoCapa. Texto pronto: a tela não traduz nem acrescenta nada. */
  mensagem: string;
}

/**
 * Aviso em linha, acima da tabela, quando a listagem trouxe erro de capa em algum veículo. Ícone e texto (nunca só cor);
 * papel de status para leitor de tela. Mesmo padrão do aviso de capa de Minhas reservas.
 */
export function AvisoFotoCapaFrota({ mensagem }: AvisoFotoCapaFrotaProps) {
  return (
    <output className="flex items-center gap-2 border-b border-borda bg-status-pendente-fundo px-4 py-3 text-corpo-denso text-status-pendente">
      <Icone nome="image_not_supported" tamanho={18} />
      <span>{mensagem}</span>
    </output>
  );
}
