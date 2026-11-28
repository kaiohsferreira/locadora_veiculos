import { useState } from 'react';
import { ImagemVeiculo } from '@/components/ui';
import { AvisoFotosReservas } from '@/features/area-cliente/reservas/componentes/AvisoFotosReservas';
import { urlDaImagem } from '@/lib/fotosVeiculo';
import { MENSAGENS_DETALHE_VEICULO as texto } from '@/lib/mensagensDetalheVeiculo';
import type { FotoCapaVeiculo } from '@/types/fotosVeiculo';

interface GaleriaVeiculoProps {
  fotos: FotoCapaVeiculo[];
  titulo: string;
  erroFotos: string | null;
  aoFalharFoto: () => void;
}

export function GaleriaVeiculo({ fotos, titulo, erroFotos, aoFalharFoto }: GaleriaVeiculoProps) {
  const [escolhida, escolher] = useState(0);
  const [urlComFalha, marcarFalha] = useState<string>();
  const indiceAtual = escolhida < fotos.length ? escolhida : 0;
  const selecionada = fotos[indiceAtual];

  return (
    <section aria-label={texto.galeria} className="space-y-3">
      <ImagemVeiculo
        url={selecionada?.url}
        alt={texto.foto(indiceAtual + 1, titulo)}
        ajuste="contain"
        classeFundo="bg-steel-fundo"
        className="aspect-[16/9] w-full rounded-cartao border border-borda"
        aoFalhar={() => {
          marcarFalha(selecionada?.url);
          aoFalharFoto();
        }}
      />
      {!selecionada && <p className="text-center text-apoio text-texto-apoio">{texto.semFoto}</p>}
      {selecionada && urlComFalha === selecionada.url && (
        <output className="block text-center text-apoio text-texto-apoio">{texto.falhaFoto}</output>
      )}
      {fotos.length > 1 && (
        <div className="grid grid-cols-3 gap-2 sm:grid-cols-5">
          {fotos.map((foto, indice) => (
            <button
              key={indice}
              type="button"
              aria-label={texto.selecionarFoto(indice + 1)}
              aria-pressed={indice === indiceAtual}
              onClick={() => escolher(indice)}
              className="min-w-0 overflow-hidden rounded-cartao border-2 border-borda hover:border-navy focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-navy aria-pressed:border-navy"
            >
              <ImagemVeiculo
                url={urlDaImagem(foto)}
                alt=""
                className="aspect-[16/10] w-full"
                tamanhoIcone={24}
                aoFalhar={aoFalharFoto}
              />
            </button>
          ))}
        </div>
      )}
      {erroFotos && <AvisoFotosReservas mensagem={erroFotos} />}
    </section>
  );
}
