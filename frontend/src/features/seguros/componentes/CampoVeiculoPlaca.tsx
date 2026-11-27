import { useId, useState } from 'react';
import { Campo, Icone } from '@/components/ui';
import { MINIMO_CARACTERES_PLACA, normalizarTermoPlaca, useBuscaVeiculoPlaca } from '@/features/seguros/hooks/useBuscaVeiculoPlaca';
import {
  SEGUROS_BUSCA_BUSCANDO,
  SEGUROS_BUSCA_DIGITE,
  SEGUROS_BUSCA_SEM_RESULTADO,
  SEGUROS_FILTRO_PLACA_EXEMPLO,
  SEGUROS_TROCAR_VEICULO,
} from '@/lib/mensagensSeguros';
import { obterMensagemErro } from '@/lib/erros';
import { descricaoVeiculo } from '@/features/seguros/regras/formularioApolice';
import type { VeiculoPlaca } from '@/types/seguros';

export interface CampoVeiculoPlacaProps {
  rotulo: string;
  /** Veículo escolhido, ou nulo enquanto nenhum foi escolhido. */
  valor: VeiculoPlaca | null;
  aoEscolher: (veiculo: VeiculoPlaca | null) => void;
  erro?: string;
  desabilitado?: boolean;
  obrigatorio?: boolean;
}

/**
 * Busca de veículo pela placa (GET /api/veiculos?placa=): ao digitar 2 caracteres ou mais, lista as placas encontradas;
 * escolhida uma, o campo vira um resumo do veículo com o botão "Trocar veículo". Usada no filtro e no formulário.
 */
export function CampoVeiculoPlaca({ rotulo, valor, aoEscolher, erro, desabilitado = false, obrigatorio = false }: CampoVeiculoPlacaProps) {
  const id = useId();
  const [termo, setTermo] = useState('');
  const busca = useBuscaVeiculoPlaca(valor === null ? termo : '');
  const termoNormalizado = normalizarTermoPlaca(termo);
  const mostrarLista = valor === null && termoNormalizado.length > 0;

  if (valor !== null) {
    return (
      <div className="flex flex-col gap-1">
        <span className="text-corpo-denso font-medium text-navy">{rotulo}</span>
        <div className="flex items-center justify-between gap-2 rounded-md border border-borda-forte bg-offwhite px-3 py-2">
          <span className="flex min-w-0 items-center gap-2 text-corpo-denso text-navy">
            <Icone nome="directions_car" tamanho={18} />
            <span className="truncate font-semibold">{descricaoVeiculo(valor)}</span>
          </span>
          <button
            type="button"
            onClick={() => {
              setTermo('');
              aoEscolher(null);
            }}
            disabled={desabilitado}
            className="shrink-0 text-corpo-denso font-semibold text-navy underline-offset-2 hover:underline disabled:opacity-60"
          >
            {SEGUROS_TROCAR_VEICULO}
          </button>
        </div>
        {erro && (
          <p className="flex items-center gap-1 text-legenda text-status-perigo" role="alert">
            <Icone nome="error" tamanho={14} />
            {erro}
          </p>
        )}
      </div>
    );
  }

  const veiculos = busca.data ?? [];
  let conteudoLista;
  if (termoNormalizado.length < MINIMO_CARACTERES_PLACA) conteudoLista = <li className="px-3 py-2 text-texto-suave">{SEGUROS_BUSCA_DIGITE}</li>;
  else if (busca.isError) conteudoLista = <li className="px-3 py-2 text-status-perigo">{obterMensagemErro(busca.error)}</li>;
  else if (busca.data === undefined) conteudoLista = <li className="px-3 py-2 text-texto-suave">{SEGUROS_BUSCA_BUSCANDO}</li>;
  else if (veiculos.length === 0) conteudoLista = <li className="px-3 py-2 text-texto-suave">{SEGUROS_BUSCA_SEM_RESULTADO}</li>;
  else {
    conteudoLista = veiculos.map((veiculo) => (
      <li key={veiculo.idVeiculo}>
        <button
          type="button"
          onClick={() => {
            setTermo('');
            aoEscolher(veiculo);
          }}
          className="flex w-full items-center gap-2 px-3 py-2 text-left hover:bg-steel-claro focus-visible:bg-steel-claro focus-visible:outline-none"
        >
          <span className="font-semibold text-navy">{veiculo.placa}</span>
          <span className="truncate text-texto-suave">
            {veiculo.marca} {veiculo.modelo}
          </span>
        </button>
      </li>
    ));
  }

  return (
    <div className="relative">
      <Campo
        id={id}
        rotulo={rotulo}
        iconeInicial="search"
        placeholder={SEGUROS_FILTRO_PLACA_EXEMPLO}
        autoComplete="off"
        value={termo}
        onChange={(evento) => setTermo(evento.target.value)}
        erro={erro}
        disabled={desabilitado}
        obrigatorio={obrigatorio}
        ocultarAsterisco={!obrigatorio}
        aria-controls={`${id}-lista`}
        aria-expanded={mostrarLista}
      />
      {mostrarLista && (
        <ul
          id={`${id}-lista`}
          className="absolute inset-x-0 top-full z-20 mt-1 max-h-64 overflow-y-auto rounded-md border border-borda bg-branco py-1 text-corpo-denso shadow-elevado"
        >
          {conteudoLista}
        </ul>
      )}
    </div>
  );
}
