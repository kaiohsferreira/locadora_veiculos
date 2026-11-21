import { Icone, ImagemVeiculo } from '@/components/ui';
import { urlDaImagem } from '@/lib/fotosVeiculo';
import type { VeiculoAVenda } from '@/types/vendas';

export interface FotoCapaVeiculoVendaProps {
  veiculo: Pick<VeiculoAVenda, 'marca' | 'modelo' | 'fotoCapa' | 'erroFotoCapa'>;
  /** Tamanho e proporção da caixa da foto. Padrão: paisagem 16:9 na largura toda. */
  className?: string;
  /** Tamanho do ícone de carro quando não há foto. Padrão: 72. */
  tamanhoIcone?: number;
  /** Chamado quando a imagem falha ao carregar (a tela pode refazer a consulta uma vez). */
  aoFalhar?: () => void;
}

/**
 * Foto de capa de um seminovo, usada na proposta (P6-FE-02) e no catálogo (P6-FE-01). Com foto, mostra a foto; sem foto,
 * o ícone de carro; quando o servidor manda `erroFotoCapa` (fotoCapa nula), mostra o ícone e, embaixo, uma faixa com o
 * texto do servidor (ícone e texto, nunca só cor).
 */
export function FotoCapaVeiculoVenda({ veiculo, className = 'aspect-[16/9] w-full', tamanhoIcone = 72, aoFalhar }: FotoCapaVeiculoVendaProps) {
  return (
    <div className="bg-offwhite">
      <ImagemVeiculo
        url={veiculo.fotoCapa ? urlDaImagem(veiculo.fotoCapa) : null}
        alt={`${veiculo.marca} ${veiculo.modelo}`}
        ajuste="cover"
        className={className}
        tamanhoIcone={tamanhoIcone}
        aoFalhar={aoFalhar}
      />
      {veiculo.erroFotoCapa && (
        <output className="flex items-center gap-2 bg-status-pendente-fundo px-4 py-2 text-corpo-denso text-status-pendente">
          <Icone nome="image_not_supported" tamanho={18} />
          <span>{veiculo.erroFotoCapa}</span>
        </output>
      )}
    </div>
  );
}
