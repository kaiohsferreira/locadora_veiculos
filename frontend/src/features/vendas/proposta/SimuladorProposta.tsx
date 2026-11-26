import { Botao, Esqueleto, Icone } from '@/components/ui';
import { formatarNumero, mascararMoedaDigitada } from '@/lib/formatters';
import {
  PROPOSTA_BARRA_MAXIMO,
  PROPOSTA_BARRA_SEM_ENTRADA,
  PROPOSTA_BOTAO_ENVIANDO,
  PROPOSTA_BOTAO_ENVIAR,
  PROPOSTA_RESULTADO_SEM_JUROS,
  PROPOSTA_RESULTADO_TITULO,
  PROPOSTA_RESUMO_AGUARDANDO,
  PROPOSTA_RESUMO_CALCULANDO,
  PROPOSTA_RESUMO_FINANCIADO,
  PROPOSTA_RESUMO_NOTA,
  PROPOSTA_RESUMO_PARCELA,
  PROPOSTA_RESUMO_ULTIMA_PARCELA,
  PROPOSTA_ROTULO_ATALHOS_ENTRADA,
  PROPOSTA_ROTULO_BARRA_ENTRADA,
  PROPOSTA_ROTULO_ENTRADA,
  PROPOSTA_ROTULO_PARCELAS,
  PROPOSTA_ROTULO_VALOR_NEGOCIADO,
  PROPOSTA_SIMULADOR_SOBRETITULO,
  PROPOSTA_SIMULADOR_TITULO,
  PROPOSTA_VALOR_TRAVADO,
  textoPrazoParcelas,
} from '@/lib/mensagensProposta';
import type { SimulacaoParcelamento, VeiculoAVenda } from '@/types/vendas';
import { converterCampos, type CamposSimulacao, type ErrosSimulacao } from './camposSimulacao';
import {
  OPCOES_PARCELAS,
  PASSO_BARRA_ENTRADA,
  PERCENTUAIS_ENTRADA,
  entradaDoPercentual,
  entradaMaximaDaBarra,
  percentualDaEntrada,
  ultimaParcelaDiferente,
} from './regrasSimulacao';
import { textoReaisInteiros } from './textosProposta';

export interface SimuladorPropostaProps {
  veiculo: Pick<VeiculoAVenda, 'valorVenda' | 'valorVendaStr'>;
  campos: CamposSimulacao;
  erros: ErrosSimulacao;
  aoMudar: (campo: keyof CamposSimulacao, valor: string) => void;
  /** Resultado da API para os valores dos campos. Nulo enquanto não há resultado desses valores. */
  simulacao: SimulacaoParcelamento | null;
  /** Os campos estão válidos, mas o resultado deles ainda não voltou: mostra "Calculando…". */
  calculando: boolean;
  /** Mensagem de erro do envio ou da simulação, já em português. */
  erro?: string | null;
  /** Texto de apoio abaixo do botão (ex.: "você vai entrar na sua conta"). */
  aviso?: string | null;
  enviando: boolean;
  /** O botão só liga com a simulação dos valores atuais. */
  podeEnviar: boolean;
  aoEnviar: () => void;
}

/** Valor da entrada no formato do campo ("31.350,00"). */
function textoCampoEntrada(valor: number): string {
  return formatarNumero(valor, 2);
}

/**
 * "Simulador Inteligente" do desenho 31: valor negociado travado, entrada com atalhos de 10/20/30/50% e barra deslizante,
 * cartões de 12x a 60x e o resultado com o destaque "Nx de R$ ..." em laranja. Todos os valores do resultado são os
 * textos prontos do servidor. Sem juros (aceite da P6-FE-02): os blocos de taxa, CET, carência, IOF e total com juros
 * do desenho ficam fora.
 *
 * Fora do desenho por não existirem no sistema: "mín. recomendada", o selo "Recomendado" do 48x, "Tem um usado na
 * troca?", "Confirmar Titular da Proposta", a caixa "Autorizo...", os três selos abaixo do botão e o aviso do CPF.
 */
export function SimuladorProposta({
  veiculo,
  campos,
  erros,
  aoMudar,
  simulacao,
  calculando,
  erro,
  aviso,
  enviando,
  podeEnviar,
  aoEnviar,
}: SimuladorPropostaProps) {
  const { valorEntrada, numeroParcelas } = converterCampos(campos);
  const entradaAtual = valorEntrada ?? 0;
  const maximoBarra = entradaMaximaDaBarra(veiculo.valorVenda);
  const valorBarra = Math.min(Math.max(entradaAtual, 0), maximoBarra);

  return (
    <section
      aria-labelledby="titulo-simulador"
      className="flex flex-col gap-6 rounded-cartao bg-branco p-6 shadow-cartao"
      data-testid="simulador"
    >
      <header className="flex items-center justify-between border-b border-borda pb-2">
        <div>
          <span className="text-legenda font-semibold uppercase tracking-wider text-orange-escuro">
            {PROPOSTA_SIMULADOR_SOBRETITULO}
          </span>
          <h2 id="titulo-simulador" className="text-secao font-bold text-navy">
            {PROPOSTA_SIMULADOR_TITULO}
          </h2>
        </div>
        <span className="flex h-8 w-8 items-center justify-center rounded-full bg-steel-suave text-navy">
          <Icone nome="calculate" tamanho={18} />
        </span>
      </header>

      <div className="flex flex-col gap-4">
        <div>
          <label htmlFor="proposta-valor-negociado" className="mb-1 block text-corpo-denso font-semibold text-navy">
            {PROPOSTA_ROTULO_VALOR_NEGOCIADO}
          </label>
          <div className="relative flex items-center">
            <input
              id="proposta-valor-negociado"
              type="text"
              readOnly
              aria-readonly="true"
              value={veiculo.valorVendaStr}
              className="h-11 w-full cursor-not-allowed rounded-lg bg-offwhite pl-3 pr-10 text-corpo font-bold text-navy outline-none"
            />
            <span className="absolute inset-y-0 right-0 flex items-center pr-3 text-contorno" title={PROPOSTA_VALOR_TRAVADO}>
              <Icone nome="lock" tamanho={18} />
            </span>
          </div>
        </div>

        <div className="flex flex-col gap-2 rounded-cartao bg-steel-fundo p-4">
          <div className="flex flex-col justify-between gap-1 sm:flex-row sm:items-center">
            <label htmlFor="proposta-entrada" className="text-corpo-denso font-semibold text-navy">
              {PROPOSTA_ROTULO_ENTRADA}
            </label>
            <div className="relative w-full sm:w-48">
              <span className="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3 font-bold text-navy">R$</span>
              <input
                id="proposta-entrada"
                type="text"
                inputMode="decimal"
                autoComplete="off"
                value={campos.entrada}
                onChange={(evento) => aoMudar('entrada', mascararMoedaDigitada(evento.target.value))}
                disabled={enviando}
                aria-invalid={erros.entrada ? true : undefined}
                aria-describedby={erros.entrada ? 'proposta-entrada-erro' : undefined}
                className="h-10 w-full rounded-lg bg-branco pl-10 pr-3 text-right font-bold text-navy shadow-cartao focus:outline-none focus:ring-2 focus:ring-orange"
              />
            </div>
          </div>
          {erros.entrada && (
            <p id="proposta-entrada-erro" className="text-legenda text-status-perigo">
              {erros.entrada}
            </p>
          )}

          <fieldset className="flex flex-wrap items-center gap-1 pt-1">
            <legend className="sr-only">{PROPOSTA_ROTULO_ATALHOS_ENTRADA}</legend>
            {PERCENTUAIS_ENTRADA.map((percentual) => {
              const valor = entradaDoPercentual(veiculo.valorVenda, percentual);
              const ativo = valorEntrada !== null && valorEntrada === valor;
              return (
                <button
                  key={percentual}
                  type="button"
                  aria-pressed={ativo}
                  disabled={enviando}
                  onClick={() => aoMudar('entrada', textoCampoEntrada(valor))}
                  className={`rounded-lg px-3 py-1.5 text-legenda font-semibold transition-colors ${
                    ativo ? 'bg-navy font-bold text-branco' : 'bg-branco text-texto-suave hover:bg-steel-alto'
                  }`}
                >
                  {percentual}% ({textoReaisInteiros(valor)})
                </button>
              );
            })}
          </fieldset>

          <div className="pt-2">
            <input
              type="range"
              aria-label={PROPOSTA_ROTULO_BARRA_ENTRADA}
              min={0}
              max={maximoBarra}
              step={PASSO_BARRA_ENTRADA}
              value={valorBarra}
              disabled={enviando || maximoBarra === 0}
              onChange={(evento) => aoMudar('entrada', textoCampoEntrada(Number(evento.target.value)))}
              className="h-2 w-full cursor-pointer accent-orange"
            />
            <div className="mt-1 flex justify-between text-legenda text-texto-suave">
              <span>R$ 0 {PROPOSTA_BARRA_SEM_ENTRADA}</span>
              <span className="font-semibold text-navy" data-testid="entrada-atual-barra">
                {textoReaisInteiros(entradaAtual)} ({percentualDaEntrada(entradaAtual, veiculo.valorVenda)}%)
              </span>
              <span>
                {textoReaisInteiros(maximoBarra)} {PROPOSTA_BARRA_MAXIMO}
              </span>
            </div>
          </div>
        </div>

        <fieldset>
          <legend className="mb-2 block text-corpo-denso font-semibold text-navy">{PROPOSTA_ROTULO_PARCELAS}</legend>
          <div className="grid grid-cols-5 gap-1">
            {OPCOES_PARCELAS.map((opcao) => {
              const marcado = numeroParcelas === opcao;
              return (
                <div key={opcao}>
                  <input
                    id={`proposta-parcelas-${opcao}`}
                    type="radio"
                    name="proposta-parcelas"
                    value={opcao}
                    checked={marcado}
                    disabled={enviando}
                    onChange={() => aoMudar('parcelas', String(opcao))}
                    className="peer sr-only"
                  />
                  <label
                    htmlFor={`proposta-parcelas-${opcao}`}
                    className={`flex cursor-pointer flex-col items-center justify-center rounded-lg px-1 py-2.5 transition-colors peer-focus-visible:ring-2 peer-focus-visible:ring-orange ${
                      marcado ? 'bg-navy text-branco' : 'bg-offwhite text-texto hover:bg-steel-suave'
                    }`}
                  >
                    <span className="text-secao font-bold">{`${opcao}x`}</span>
                    <span className="text-micro opacity-80">{textoPrazoParcelas(opcao)}</span>
                  </label>
                </div>
              );
            })}
          </div>
          {erros.parcelas && <p className="mt-1 text-legenda text-status-perigo">{erros.parcelas}</p>}
        </fieldset>
      </div>

      <div className="flex flex-col gap-4 rounded-cartao bg-steel-fundo p-4" data-testid="resultado-simulacao">
        <div className="border-b border-steel-alto pb-2">
          <span className="text-legenda font-bold uppercase tracking-wider text-orange-escuro">{PROPOSTA_RESULTADO_TITULO}</span>
          <p className="text-legenda text-texto-suave">{PROPOSTA_RESULTADO_SEM_JUROS}</p>
        </div>

        {erro && (
          <div role="alert" className="flex items-start gap-2 rounded-cartao bg-status-perigo-fundo px-4 py-3 text-corpo-denso text-status-perigo">
            <Icone nome="error" tamanho={18} className="mt-0.5" />
            <span>{erro}</span>
          </div>
        )}

        {calculando ? (
          <div className="flex flex-col gap-3" aria-live="polite" aria-busy="true">
            <p className="flex items-center gap-1 text-corpo-denso text-texto-suave">
              <Icone nome="progress_activity" tamanho={16} className="motion-safe:animate-spin" />
              {PROPOSTA_RESUMO_CALCULANDO}
            </p>
            <Esqueleto className="h-4 w-1/2" />
            <Esqueleto className="h-12 w-3/4" />
          </div>
        ) : simulacao === null ? (
          <p className="text-corpo-denso text-texto-suave">{PROPOSTA_RESUMO_AGUARDANDO}</p>
        ) : (
          <div className="flex flex-col gap-4" aria-live="polite">
            <div>
              <span className="block text-legenda text-texto-suave">{PROPOSTA_RESUMO_FINANCIADO}</span>
              <span className="text-corpo font-bold text-navy tabular-nums">{simulacao.valorFinanciadoStr}</span>
            </div>
            <div className="rounded-lg bg-branco p-4">
              <span className="text-legenda font-bold uppercase text-texto-suave">{PROPOSTA_RESUMO_PARCELA}</span>
              <p className="text-titulo font-bold tracking-tight text-orange tabular-nums" data-testid="parcela-destaque">
                {simulacao.numeroParcelas}x de {simulacao.valorParcelaStr}
              </p>
              {ultimaParcelaDiferente(simulacao.valorParcela, simulacao.valorUltimaParcela) && (
                <p className="mt-1 text-corpo-denso text-texto-suave" data-testid="ultima-parcela">
                  {PROPOSTA_RESUMO_ULTIMA_PARCELA}: {simulacao.valorUltimaParcelaStr}
                </p>
              )}
            </div>
          </div>
        )}
      </div>

      <div className="flex flex-col gap-2">
        <Botao
          tamanho="grande"
          larguraTotal
          iconeDireita="arrow_forward"
          carregando={enviando}
          disabled={!podeEnviar || enviando}
          onClick={aoEnviar}
        >
          {enviando ? PROPOSTA_BOTAO_ENVIANDO : PROPOSTA_BOTAO_ENVIAR}
        </Botao>
        {aviso && <p className="text-center text-legenda text-texto-suave">{aviso}</p>}
        <p className="text-center text-legenda text-texto-suave">{PROPOSTA_RESUMO_NOTA}</p>
      </div>
    </section>
  );
}
