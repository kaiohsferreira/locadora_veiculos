import { Icone } from '@/components/ui';
import { formatarPlaca } from '@/lib/formatters';
import { MENSAGEM_FROTA_BRASIL } from '@/lib/mensagens';

/**
 * Placa e chassi das listas da frota (telas 11 e 12). Saíram da tabela da tela 11 sem mudança visual: a tela 11 importa
 * daqui, e a tabela da tela 12 reaproveita a placa.
 */

/** Chassi em cima e os anos (fabricação / modelo) embaixo, como no desenho. */
export function ChassiEAno({ chassi, anoFabricacao, anoModelo }: { chassi: string; anoFabricacao: number; anoModelo: number }) {
  return (
    <span className="flex flex-col">
      <span className="text-apoio font-medium text-texto-desenho">{chassi}</span>
      <span className="text-micro text-texto-apoio">{`${anoFabricacao} / ${anoModelo}`}</span>
    </span>
  );
}

/** Placa no emblema do desenho: faixa azul com "BRASIL" e a placa embaixo, no padrão de `@/lib/formatters`. */
export function PlacaMercosul({ placa }: { placa: string }) {
  return (
    <div className="inline-flex flex-col overflow-hidden rounded-sm border border-contorno-suave bg-branco shadow-sm">
      <div className="flex items-center justify-between gap-1 bg-navy-marinho px-2 py-0.5 leading-none text-branco">
        <span className="inline-block h-1.5 w-2 rounded-sm bg-frota-disponivel-ponto" aria-hidden="true" />
        <span className="text-emblema-brasil tracking-widest">{MENSAGEM_FROTA_BRASIL}</span>
        <Icone nome="flag" tamanho={8} />
      </div>
      <div className="px-2 py-0.5 text-center text-corpo-denso font-bold leading-none tracking-wider text-texto-desenho">
        {formatarPlaca(placa)}
      </div>
    </div>
  );
}
