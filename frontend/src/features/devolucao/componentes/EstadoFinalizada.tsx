import { Icone } from '@/components/ui';
import { VALOR_AUSENTE, formatarQuilometragem } from '@/lib/formatters';
import { MENSAGENS_COBRANCAS_EXTRAS } from '@/lib/mensagensCobrancasExtras';
import { MENSAGENS_DEVOLUCAO as M } from '@/lib/mensagensDevolucao';
import type { ContextoDevolucao, ResultadoEncerramento } from '@/types/devolucao';
import { textoCaucaoEncerrada } from '../regras/regrasDevolucao';

export interface EstadoFinalizadaProps {
  contexto: ContextoDevolucao;
  /** Resposta do encerramento desta sessão. Nula ao reabrir a locação: o contexto traz só o que o GET devolve. */
  resultado: ResultadoEncerramento | null;
}

/** Locação encerrada (anexo, tela 06, estado D): só leitura, sem formulário e sem botão de encerrar. */
export function EstadoFinalizada({ contexto, resultado }: EstadoFinalizadaProps) {
  const dataEfetiva = resultado?.locacao.dataDevolucaoEfetivaStr ?? contexto.comparacao?.saida.dataHoraStr ?? null;
  const total = resultado?.locacao.valorTotalStr ?? contexto.valorTotalPrevistoStr ?? VALOR_AUSENTE;
  const caucao = resultado ? textoCaucaoEncerrada(resultado) : null;
  const pagamentos = resultado
    ? [resultado.cobrancas.idPagamentoDiaria, resultado.cobrancas.idPagamentoCobrancaExtra].filter((id): id is number => id !== null)
    : [];
  return (
    <section aria-labelledby="locacao-encerrada" className="flex min-w-0 flex-col gap-4 rounded-cartao bg-branco p-6 shadow-sm">
      <div className="flex flex-wrap items-center gap-3">
        <span className="inline-flex items-center gap-1 rounded bg-steel-alto px-2 py-1 text-rotulo font-bold uppercase text-navy-profundo">
          <Icone nome="check_circle" tamanho={16} />
          <span>{M.finalizadaSelo}</span>
        </span>
        <h2 id="locacao-encerrada" className="text-titulo-md font-bold text-navy-profundo break-words">
          {dataEfetiva !== null ? M.finalizadaTitulo(dataEfetiva) : M.finalizadaSelo}
        </h2>
      </div>
      <p className="text-corpo-denso font-semibold text-navy-profundo">{M.valorTotalLocacao(total)}</p>
      {resultado && (
        <p className="text-corpo-denso text-navy-profundo">
          {M.veiculoEstado(resultado.veiculo.status === 'DISPONIVEL', formatarQuilometragem(resultado.veiculo.kmAtual))}
        </p>
      )}
      {caucao !== null && <p className="text-corpo-denso text-navy-profundo">{caucao}</p>}
      {pagamentos.map(id => <p key={id} className="text-corpo-denso text-navy-profundo">{MENSAGENS_COBRANCAS_EXTRAS.pagamentoLancado(id)}</p>)}
      {resultado && resultado.avisos.length > 0 && (
        <output className="block rounded-lg bg-status-pendente-fundo p-3 text-apoio text-status-pendente">
          <ul className="flex flex-col gap-2">
            {resultado.avisos.map(aviso => <li key={aviso} className="break-words">{aviso}</li>)}
          </ul>
        </output>
      )}
    </section>
  );
}
