import { Botao, Icone } from '@/components/ui';
import { MENSAGEM_FROTA_ERRO_RESUMO, MENSAGEM_FROTA_TENTAR_NOVAMENTE } from '@/lib/mensagens';

export interface AvisoResumoFrotaProps {
  /** Repetindo a consulta do resumo: o botão mostra o carregamento. */
  carregando: boolean;
  aoTentarNovamente: () => void;
}

/** Aviso acima dos cartões quando o resumo da frota (visão geral) não carrega. Igual nas telas 11 e 12. */
export function AvisoResumoFrota({ carregando, aoTentarNovamente }: AvisoResumoFrotaProps) {
  return (
    <output className="flex flex-wrap items-center justify-between gap-2 rounded-cartao bg-status-perigo-fundo px-4 py-3 text-corpo-denso text-status-perigo">
      <span className="flex items-center gap-2">
        <Icone nome="error" tamanho={18} />
        <span>{MENSAGEM_FROTA_ERRO_RESUMO}</span>
      </span>
      <Botao variante="contorno" tamanho="pequeno" carregando={carregando} onClick={aoTentarNovamente}>
        {MENSAGEM_FROTA_TENTAR_NOVAMENTE}
      </Botao>
    </output>
  );
}
