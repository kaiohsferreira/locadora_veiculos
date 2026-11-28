import { Botao } from '@/components/ui';
import { MENSAGEM_FROTA_TENTAR_NOVAMENTE } from '@/lib/mensagens';
import { MENSAGEM_VEICULO_CATEGORIAS_ERRO } from '@/lib/mensagensVeiculos';

export interface AvisoCategoriasFrotaProps {
  /** Tenta carregar as categorias de novo (o `refetch` da consulta). */
  aoTentarNovamente: () => void;
}

/**
 * Aviso do filtro de categoria das telas 11 e 12 quando GET /api/categorias falha. Mesmo padrão da tela 05 (texto pequeno,
 * em vermelho, com o botão de tentar de novo ao lado). A lista de veículos segue funcionando sem a categoria.
 */
export function AvisoCategoriasFrota({ aoTentarNovamente }: AvisoCategoriasFrotaProps) {
  return (
    <div role="alert" className="flex flex-wrap items-center gap-2 text-micro text-status-perigo">
      <span>{MENSAGEM_VEICULO_CATEGORIAS_ERRO}</span>
      <Botao variante="contorno" tamanho="pequeno" onClick={aoTentarNovamente}>
        {MENSAGEM_FROTA_TENTAR_NOVAMENTE}
      </Botao>
    </div>
  );
}
