import type { FetchStatus } from '@tanstack/react-query';
import { useState, type FormEvent } from 'react';
import { Botao, BotaoLink, Campo } from '@/components/ui';
import { MENSAGENS_DETALHE_VEICULO as texto } from '@/lib/mensagensDetalheVeiculo';
import type { PeriodoLocacaoVeiculo, VeiculoLocacao } from '@/types/veiculos';
import { avaliarPeriodo } from '../busca/parametrosBusca';

interface ResumoLocacaoVeiculoProps {
  datas: PeriodoLocacaoVeiculo;
  hoje: string;
  detalhe?: VeiculoLocacao;
  diarias: number;
  situacaoConsulta: FetchStatus;
  falhou: boolean;
  destinoCheckout: string;
  aoAplicar: (datas: PeriodoLocacaoVeiculo) => void;
  aoTentarNovamente: () => void;
}

interface EdicaoPeriodo {
  origem: PeriodoLocacaoVeiculo;
  datas: PeriodoLocacaoVeiculo;
  tentouAplicar: boolean;
}

function mesmasDatas(a: PeriodoLocacaoVeiculo, b: PeriodoLocacaoVeiculo): boolean {
  return a.dataRetirada === b.dataRetirada && a.dataDevolucao === b.dataDevolucao;
}

const FORMATADOR_TOTAL = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });

function formatarTotalLocacao(valorDiaria: number, diarias: number): string {
  // A diária cabe em Number (decimal de precisão 10 e escala 2); o total do período pode exceder esse limite.
  const centavosDaDiaria = BigInt(Math.round(valorDiaria * 100));
  const totalEmCentavos = centavosDaDiaria * BigInt(diarias);
  const centavos = (totalEmCentavos % 100n).toString().padStart(2, '0');
  return FORMATADOR_TOTAL.formatToParts(totalEmCentavos / 100n)
    .map((parte) => parte.type === 'fraction' ? centavos : parte.value)
    .join('');
}

export function ResumoLocacaoVeiculo({
  datas, hoje, detalhe, diarias, situacaoConsulta, falhou, destinoCheckout, aoAplicar, aoTentarNovamente,
}: ResumoLocacaoVeiculoProps) {
  const [edicao, definirEdicao] = useState<EdicaoPeriodo>({ origem: datas, datas, tentouAplicar: false });
  const mudouPeriodoAplicado = !mesmasDatas(edicao.origem, datas);
  const periodo = mudouPeriodoAplicado ? datas : edicao.datas;
  // Atualiza o rascunho ao navegar pelo histórico, preservando os campos montados e o foco ao aplicar.
  if (mudouPeriodoAplicado) definirEdicao({ origem: datas, datas, tentouAplicar: false });

  const alterado = !mesmasDatas(periodo, datas);
  const avaliacao = avaliarPeriodo(periodo, hoje);
  const urlComDatas = Boolean(datas.dataRetirada || datas.dataDevolucao);
  const mostrarErros = edicao.tentouAplicar || (urlComDatas && !alterado);
  const erros = mostrarErros ? avaliacao.erros : {};
  const consultando = situacaoConsulta !== 'idle';
  const tarifaAtual = alterado || consultando || falhou ? undefined : detalhe;
  const disponibilidade = diarias > 0 ? tarifaAtual?.disponivelNoPeriodo : null;
  const disponivel = disponibilidade === true;
  const indisponivel = disponibilidade === false;
  const valorTotal = disponivel && tarifaAtual
    ? formatarTotalLocacao(tarifaAtual.valorDiaria, diarias)
    : undefined;

  function editar(campo: keyof PeriodoLocacaoVeiculo, valor: string) {
    definirEdicao((atual) => ({ ...atual, datas: { ...atual.datas, [campo]: valor } }));
  }

  function enviar(evento: FormEvent<HTMLFormElement>) {
    evento.preventDefault();
    if (avaliacao.diarias === 0) {
      definirEdicao((atual) => ({ ...atual, tentouAplicar: true }));
      return;
    }
    definirEdicao((atual) => ({ ...atual, tentouAplicar: false }));
    aoAplicar(periodo);
  }

  let mensagem: string = texto.informarDatas;
  if (situacaoConsulta === 'paused') mensagem = texto.aguardandoConexao;
  else if (alterado) mensagem = texto.aplicarEdicao;
  else if (consultando) mensagem = texto.verificando;
  else if (falhou) mensagem = texto.erroPeriodo;
  else if (disponivel) mensagem = texto.disponivel;
  else if (indisponivel) mensagem = texto.indisponivel;

  return (
    <section aria-labelledby="titulo-resumo" className="min-w-0 rounded-cartao border border-borda bg-branco shadow-cartao">
      <h2 id="titulo-resumo" className="border-b border-borda px-4 py-4 text-marca text-navy sm:px-6">{texto.resumo}</h2>
      <div className="space-y-5 p-4 sm:p-6">
        <form noValidate onSubmit={enviar} className="space-y-4">
          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-1 xl:grid-cols-2">
            <Campo
              rotulo={texto.retirada}
              name="dataRetirada"
              type="date"
              required
              obrigatorio
              min={hoje}
              max="9999-12-30"
              erro={erros.dataRetirada}
              value={periodo.dataRetirada}
              onChange={(evento) => editar('dataRetirada', evento.target.value)}
              className="min-w-0"
            />
            <Campo
              rotulo={texto.devolucao}
              name="dataDevolucao"
              type="date"
              required
              obrigatorio
              min={avaliacao.minimoDevolucao}
              max="9999-12-31"
              erro={erros.dataDevolucao}
              value={periodo.dataDevolucao}
              onChange={(evento) => editar('dataDevolucao', evento.target.value)}
              className="min-w-0"
            />
          </div>
          <Botao type="submit" variante="contorno" icone="event" larguraTotal carregando={consultando && !alterado}>
            {texto.aplicar}
          </Botao>
        </form>
        <output
          className={`block rounded-cartao px-3 py-2 text-corpo-denso ${
            disponivel ? 'bg-status-sucesso-fundo text-status-sucesso' : indisponivel
              ? 'bg-status-pendente-fundo text-status-pendente'
              : 'bg-steel-fundo text-navy'
          }`}
        >
          {mensagem}
        </output>
        {falhou && !alterado && !consultando && (
          <Botao variante="fantasma" icone="refresh" onClick={aoTentarNovamente}>{texto.tentarNovamente}</Botao>
        )}
        {tarifaAtual && (
          <dl className="space-y-2 text-corpo-denso">
            <div className="flex justify-between gap-3">
              <dt className="text-texto-apoio">{texto.diaria}</dt>
              <dd className="shrink-0 font-semibold text-navy">{tarifaAtual.valorDiariaStr}</dd>
            </div>
            {valorTotal && (
              <>
                <div className="flex justify-between gap-3">
                  <dt className="text-texto-apoio">{texto.periodo}</dt>
                  <dd className="font-semibold text-navy">{texto.numeroDiarias(diarias)}</dd>
                </div>
                <div className="flex items-baseline justify-between gap-3 border-t border-borda pt-3">
                  <dt className="font-semibold text-navy">{texto.total}</dt>
                  <dd className="min-w-0 break-words text-right text-marca text-navy">{valorTotal}</dd>
                </div>
              </>
            )}
          </dl>
        )}
        {disponivel && (
          <BotaoLink to={destinoCheckout} larguraTotal iconeDireita="arrow_forward" className="!text-navy">
            {texto.continuar}
          </BotaoLink>
        )}
      </div>
    </section>
  );
}
