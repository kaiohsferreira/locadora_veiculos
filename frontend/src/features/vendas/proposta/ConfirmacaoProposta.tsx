import type { ReactNode } from 'react';
import { BotaoLink, Etiqueta, Icone } from '@/components/ui';
import { formatarDataHora, formatarMoeda } from '@/lib/formatters';
import {
  PROPOSTA_CONFIRMACAO_DATA,
  PROPOSTA_CONFIRMACAO_INICIO,
  PROPOSTA_CONFIRMACAO_LOCADORA_PADRAO,
  PROPOSTA_CONFIRMACAO_PARCELAS,
  PROPOSTA_CONFIRMACAO_SITUACAO,
  PROPOSTA_CONFIRMACAO_TEXTO,
  PROPOSTA_CONFIRMACAO_TITULO,
  PROPOSTA_CONFIRMACAO_VEICULO,
  PROPOSTA_CONFIRMACAO_VER_SEMINOVOS,
  PROPOSTA_RESUMO_ENTRADA,
  PROPOSTA_RESUMO_FINANCIADO,
  PROPOSTA_RESUMO_ULTIMA_PARCELA,
  PROPOSTA_ROTULO_STATUS,
} from '@/lib/mensagensProposta';
import { CAMINHOS } from '@/routes/caminhos';
import type { PropostaCompra } from '@/types/vendas';
import { ultimaParcelaDiferente } from './regrasSimulacao';

export interface ConfirmacaoPropostaProps {
  proposta: PropostaCompra;
  /** Nome fantasia da locadora do veículo, para a frase de confirmação. */
  locadora: string;
}

function Item({ rotulo, children }: { rotulo: string; children: ReactNode }) {
  return (
    <div className="flex items-baseline justify-between gap-4 py-2 text-corpo-denso">
      <dt className="text-texto-suave">{rotulo}</dt>
      <dd className="text-right font-medium text-navy tabular-nums">{children}</dd>
    </div>
  );
}

/** Confirmação depois do envio: os números gravados pelo servidor, a data e a situação, com os caminhos de saída. */
export function ConfirmacaoProposta({ proposta, locadora }: ConfirmacaoPropostaProps) {
  return (
    <section
      aria-labelledby="titulo-confirmacao-proposta"
      className="mx-auto flex w-full max-w-xl flex-col items-center gap-6 rounded-cartao bg-branco p-6 text-center shadow-cartao sm:p-8"
    >
      <span className="flex h-16 w-16 items-center justify-center rounded-full bg-status-sucesso-fundo text-status-sucesso">
        <Icone nome="check_circle" tamanho={40} preenchido />
      </span>
      <div className="flex flex-col gap-2">
        <h2 id="titulo-confirmacao-proposta" className="text-titulo text-navy">
          {PROPOSTA_CONFIRMACAO_TITULO}
        </h2>
        <p className="text-corpo text-texto-suave">
          {locadora || PROPOSTA_CONFIRMACAO_LOCADORA_PADRAO} {PROPOSTA_CONFIRMACAO_TEXTO}
        </p>
      </div>

      <dl className="w-full divide-y divide-borda rounded-cartao border border-borda px-4 text-left">
        <Item rotulo={PROPOSTA_CONFIRMACAO_VEICULO}>
          {proposta.marca} {proposta.modelo} {proposta.anoModelo}
        </Item>
        <Item rotulo={PROPOSTA_RESUMO_ENTRADA}>{formatarMoeda(proposta.valorEntrada)}</Item>
        <Item rotulo={PROPOSTA_RESUMO_FINANCIADO}>{formatarMoeda(proposta.valorFinanciado)}</Item>
        <Item rotulo={PROPOSTA_CONFIRMACAO_PARCELAS}>
          {proposta.numeroParcelas} × {formatarMoeda(proposta.valorParcela)}
        </Item>
        {ultimaParcelaDiferente(proposta.valorParcela, proposta.valorUltimaParcela) && (
          <Item rotulo={PROPOSTA_RESUMO_ULTIMA_PARCELA}>{formatarMoeda(proposta.valorUltimaParcela)}</Item>
        )}
        <Item rotulo={PROPOSTA_CONFIRMACAO_DATA}>{formatarDataHora(proposta.dataProposta)}</Item>
        <Item rotulo={PROPOSTA_CONFIRMACAO_SITUACAO}>
          <Etiqueta rotulo={PROPOSTA_ROTULO_STATUS[proposta.status]} tom="sucesso" />
        </Item>
      </dl>

      <div className="flex w-full flex-col gap-3 sm:flex-row sm:justify-center">
        <BotaoLink to={CAMINHOS.seminovos} icone="directions_car">
          {PROPOSTA_CONFIRMACAO_VER_SEMINOVOS}
        </BotaoLink>
        <BotaoLink to={CAMINHOS.inicio} variante="contorno">
          {PROPOSTA_CONFIRMACAO_INICIO}
        </BotaoLink>
      </div>
    </section>
  );
}
