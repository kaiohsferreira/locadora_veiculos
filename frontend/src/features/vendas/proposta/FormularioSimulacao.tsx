import { Campo, Cartao } from '@/components/ui';
import { mascararMoedaDigitada } from '@/lib/formatters';
import {
  PROPOSTA_DICA_ENTRADA,
  PROPOSTA_DICA_PARCELAS,
  PROPOSTA_ROTULO_ENTRADA,
  PROPOSTA_ROTULO_PARCELAS,
  PROPOSTA_TITULO_SIMULACAO,
} from '@/lib/mensagensProposta';
import { mascararParcelas, type CamposSimulacao, type ErrosSimulacao } from './camposSimulacao';

export interface FormularioSimulacaoProps {
  campos: CamposSimulacao;
  erros: ErrosSimulacao;
  aoMudar: (campo: keyof CamposSimulacao, valor: string) => void;
  /** Desliga os campos durante o envio da proposta. */
  desabilitado?: boolean;
}

/** Cartão "Simule seu financiamento": entrada em reais (com máscara) e parcelas de 1 a 60, lado a lado no computador. */
export function FormularioSimulacao({ campos, erros, aoMudar, desabilitado = false }: FormularioSimulacaoProps) {
  return (
    <Cartao titulo={PROPOSTA_TITULO_SIMULACAO}>
      <div className="grid grid-cols-1 gap-6 sm:grid-cols-2">
        <Campo
          id="proposta-entrada"
          rotulo={PROPOSTA_ROTULO_ENTRADA}
          inputMode="decimal"
          autoComplete="off"
          mascara={mascararMoedaDigitada}
          value={campos.entrada}
          onChange={(evento) => aoMudar('entrada', evento.target.value)}
          erro={erros.entrada}
          dica={erros.entrada ? undefined : PROPOSTA_DICA_ENTRADA}
          disabled={desabilitado}
          obrigatorio
          ocultarAsterisco
        />
        <Campo
          id="proposta-parcelas"
          rotulo={PROPOSTA_ROTULO_PARCELAS}
          inputMode="numeric"
          autoComplete="off"
          mascara={mascararParcelas}
          value={campos.parcelas}
          onChange={(evento) => aoMudar('parcelas', evento.target.value)}
          erro={erros.parcelas}
          dica={erros.parcelas ? undefined : PROPOSTA_DICA_PARCELAS}
          disabled={desabilitado}
          obrigatorio
          ocultarAsterisco
        />
      </div>
    </Cartao>
  );
}
