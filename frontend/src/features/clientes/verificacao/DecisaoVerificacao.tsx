import { useId } from 'react';
import { Botao } from '@/components/ui';
import {
  MENSAGEM_VERIFICACAO_BOTAO_APROVAR,
  MENSAGEM_VERIFICACAO_BOTAO_RECUSAR,
  MENSAGEM_VERIFICACAO_JA_APROVADO,
  MENSAGEM_VERIFICACAO_JUSTIFICATIVA_APOIO,
  MENSAGEM_VERIFICACAO_JUSTIFICATIVA_PLACEHOLDER,
  MENSAGEM_VERIFICACAO_JUSTIFICATIVA_ROTULO,
  MENSAGEM_VERIFICACAO_SECAO_DECISAO,
} from '@/lib/mensagens';
import type { StatusCadastroCliente } from '@/types/enums';
import type { DecisaoVerificacaoCliente } from '@/types/verificacaoCadastros';
import { mostrarDecisao, recusaHabilitada, TAMANHO_MAXIMO_JUSTIFICATIVA } from './regras';

export interface DecisaoVerificacaoProps {
  status: StatusCadastroCliente;
  justificativa: string;
  aoMudarJustificativa: (texto: string) => void;
  /** Há um envio de decisão em andamento: os botões ficam bloqueados. */
  enviando: boolean;
  /** Qual decisão está sendo enviada (só o botão dela mostra o carregamento). */
  decisaoEnviada: DecisaoVerificacaoCliente | null;
  aoAprovar: () => void;
  aoRecusar: () => void;
}

/**
 * Decisão do cadastro (tela 18). Aprovado não recebe decisão: aparece só o aviso. Para pendente e recusado, a justificativa
 * tem texto de apoio que diz que ela só vale para a recusa (decisão 7). Recusar só fica habilitado com justificativa.
 */
export function DecisaoVerificacao({
  status,
  justificativa,
  aoMudarJustificativa,
  enviando,
  decisaoEnviada,
  aoAprovar,
  aoRecusar,
}: DecisaoVerificacaoProps) {
  const idJustificativa = useId();
  const idApoio = `${idJustificativa}-apoio`;

  if (!mostrarDecisao(status)) {
    return <p className="text-corpo-denso text-texto-apoio">{MENSAGEM_VERIFICACAO_JA_APROVADO}</p>;
  }

  return (
    <section className="flex flex-col gap-3">
      <h3 className="text-legenda font-semibold uppercase tracking-wide text-contorno">
        {MENSAGEM_VERIFICACAO_SECAO_DECISAO}
      </h3>
      <div className="flex flex-col gap-1">
        <label htmlFor={idJustificativa} className="text-legenda font-semibold uppercase tracking-wide text-contorno">
          {MENSAGEM_VERIFICACAO_JUSTIFICATIVA_ROTULO}
        </label>
        <textarea
          id={idJustificativa}
          rows={2}
          maxLength={TAMANHO_MAXIMO_JUSTIFICATIVA}
          value={justificativa}
          disabled={enviando}
          placeholder={MENSAGEM_VERIFICACAO_JUSTIFICATIVA_PLACEHOLDER}
          aria-describedby={idApoio}
          onChange={(evento) => aoMudarJustificativa(evento.target.value)}
          className="w-full resize-none rounded bg-steel-fundo p-2.5 text-corpo-denso text-texto-desenho placeholder:text-contorno focus:outline-none focus:ring-2 focus:ring-steel disabled:opacity-60"
        />
        <p id={idApoio} className="text-legenda text-texto-apoio">
          {MENSAGEM_VERIFICACAO_JUSTIFICATIVA_APOIO}
        </p>
      </div>
      <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
        <Botao
          variante="secundario"
          icone="check_circle"
          larguraTotal
          className="h-11"
          disabled={enviando}
          carregando={enviando && decisaoEnviada === 'APROVADO'}
          onClick={aoAprovar}
        >
          {MENSAGEM_VERIFICACAO_BOTAO_APROVAR}
        </Botao>
        <Botao
          variante="perigo"
          icone="cancel"
          larguraTotal
          className="h-11"
          disabled={!recusaHabilitada(justificativa, enviando)}
          carregando={enviando && decisaoEnviada === 'RECUSADO'}
          onClick={aoRecusar}
        >
          {MENSAGEM_VERIFICACAO_BOTAO_RECUSAR}
        </Botao>
      </div>
    </section>
  );
}
