import { Botao, Modal } from '@/components/ui';
import {
  MENSAGEM_FROTA_BAIXA_TEXTO,
  MENSAGEM_FROTA_CANCELAR,
  MENSAGEM_FROTA_ITEM_BAIXA,
  mensagemFrotaBaixaTitulo,
} from '@/lib/mensagens';
import type { VeiculoRetorno } from '@/types/veiculos';

export interface ConfirmacaoBaixaFrotaProps {
  /** Veículo cuja baixa espera a confirmação; nulo deixa o Modal fechado. */
  paraBaixa: VeiculoRetorno | null;
  /** Há uma troca em andamento: o Modal não fecha até a resposta. */
  pendente: boolean;
  aoCancelar: () => void;
  aoConfirmar: () => void;
}

/** Confirmação da baixa (a única troca de status que pede confirmação). Igual nas telas 11 e 12. */
export function ConfirmacaoBaixaFrota({ paraBaixa, pendente, aoCancelar, aoConfirmar }: ConfirmacaoBaixaFrotaProps) {
  return (
    <Modal
      aberto={paraBaixa !== null}
      aoFechar={() => {
        if (!pendente) aoCancelar();
      }}
      titulo={paraBaixa ? mensagemFrotaBaixaTitulo(paraBaixa.placa) : ''}
      descricao={MENSAGEM_FROTA_BAIXA_TEXTO}
      tamanho="pequeno"
      fecharAoClicarFora={!pendente}
      rodape={
        <>
          <Botao variante="contorno" onClick={aoCancelar} disabled={pendente}>
            {MENSAGEM_FROTA_CANCELAR}
          </Botao>
          <Botao variante="perigo" onClick={aoConfirmar} carregando={pendente}>
            {MENSAGEM_FROTA_ITEM_BAIXA}
          </Botao>
        </>
      }
    >
      {null}
    </Modal>
  );
}
