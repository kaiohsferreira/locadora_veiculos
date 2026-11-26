import { generatePath } from 'react-router-dom';
import { BotaoLink } from '@/components/ui';
import { FotoVeiculo } from '@/features/area-cliente/reservas/componentes/FotoVeiculo';
import { MENSAGENS_BUSCA as texto } from '@/lib/mensagensBusca';
import { CAMINHOS } from '@/routes/caminhos';
import type { VeiculoDisponivel } from '@/types/veiculos';

interface LinhaVeiculoProps {
  veiculo: VeiculoDisponivel;
  consulta: string;
  aoFalharFoto: () => void;
}

export function LinhaVeiculo({ veiculo, consulta, aoFalharFoto }: LinhaVeiculoProps) {
  const titulo = `${veiculo.marca} ${veiculo.modelo}`;
  const parametrosRota = { veiculoId: String(veiculo.idVeiculo) };
  const local = [veiculo.locadoraCidade, veiculo.locadoraUf].filter(Boolean).join(' – ');

  return (
    <article className="grid gap-4 rounded-cartao border border-borda bg-branco p-4 shadow-cartao sm:grid-cols-[minmax(0,1fr)_minmax(0,1.2fr)] xl:grid-cols-[minmax(0,1fr)_minmax(0,1.2fr)_minmax(0,1fr)]">
      <div className="min-w-0 space-y-2 rounded bg-steel-fundo p-2">
        <p className="break-words rounded bg-steel-suave px-2 py-1 text-micro font-bold uppercase text-navy">{veiculo.categoriaNome}</p>
        <FotoVeiculo fotoCapa={veiculo.fotoCapa} descricao={titulo} aoFalhar={aoFalharFoto} />
      </div>
      <div className="min-w-0 space-y-3 sm:pb-2">
        <div>
          <h2 className="break-words text-marca text-navy">{titulo}</h2>
          <p className="mt-1 break-words text-apoio text-texto-apoio">
            {[veiculo.anoModelo, veiculo.cor].filter(Boolean).join(' · ')}
          </p>
        </div>
        <p className="break-words text-apoio text-texto-apoio">
          {[veiculo.locadoraNomeFantasia, local].filter(Boolean).join(' · ')}
        </p>
      </div>
      <div className="flex min-w-0 flex-col justify-between gap-4 rounded bg-steel-fundo p-3 text-center sm:col-span-2 xl:col-span-1 xl:text-right">
        <div>
          <p className="text-micro font-semibold uppercase text-texto-apoio">{texto.valorDiaria}</p>
          <p className="mt-1 text-rotulo text-texto-apoio">
            <strong className="break-words text-titulo-hero text-navy">{veiculo.valorDiariaStr}</strong> {texto.porDia}
          </p>
        </div>
        <div className="grid gap-1 sm:grid-cols-2 xl:grid-cols-1">
          <BotaoLink
            to={{ pathname: generatePath(CAMINHOS.checkoutReserva, parametrosRota), search: consulta }}
            iconeDireita="arrow_forward"
            className="!text-navy"
            aria-label={texto.reservarVeiculo(titulo)}
            larguraTotal
          >
            {texto.reservar}
          </BotaoLink>
          <BotaoLink
            to={{ pathname: generatePath(CAMINHOS.detalheVeiculo, parametrosRota), search: consulta }}
            variante="fantasma"
            aria-label={texto.detalhesVeiculo(titulo)}
            larguraTotal
          >
            {texto.detalhes}
          </BotaoLink>
        </div>
      </div>
    </article>
  );
}
