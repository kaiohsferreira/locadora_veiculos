import { useId, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Botao, Modal, Select, useAviso } from '@/components/ui';
import { CLASSES_ERRO } from '@/components/ui/estilosCampo';
import {
  MENSAGEM_FINANCEIRO_BOTAO_CANCELAR,
  MENSAGEM_FINANCEIRO_CAIXA_PIX,
  MENSAGEM_FINANCEIRO_DIALOGO_CONFIRMAR,
  MENSAGEM_FINANCEIRO_DIALOGO_FORMA,
  MENSAGEM_FINANCEIRO_DIALOGO_TITULO,
  MENSAGEM_FINANCEIRO_SEPARADOR_RESUMO,
  MENSAGEM_FINANCEIRO_SUCESSO_BAIXA,
} from '@/lib/mensagens';
import { FORMA_PAGAMENTO, ROTULO_FORMA_PAGAMENTO } from '@/types/enums';
import type { TituloFinanceiro } from '@/types/painelFinanceiro';
import { useBaixarPagamento } from '@/features/financeiro/hooks/usePainelFinanceiro';
import {
  criarEsquemaBaixa,
  decidirAposErroBaixa,
  montarRequisicaoBaixa,
  VALORES_INICIAIS_BAIXA,
  type ValoresBaixa,
} from '@/features/financeiro/formularios/regrasBaixa';

const ESQUEMA_BAIXA = criarEsquemaBaixa();

/** Opções do campo Forma, na ordem da lista fechada do contrato, com os rótulos do projeto. */
const OPCOES_FORMA = FORMA_PAGAMENTO.map((valor) => ({ valor, rotulo: ROTULO_FORMA_PAGAMENTO[valor] }));

export interface ModalDarBaixaProps {
  /** Título escolhido na tabela. */
  titulo: TituloFinanceiro;
  /** Fecha o diálogo (sucesso ou cancelamento). O pai desmonta o componente. */
  aoFechar: () => void;
}

/**
 * Diálogo "Dar baixa no pagamento": escolhe a forma (obrigatória) e confirma. Quando o servidor recusa por haver PIX
 * gerado (409), mostra a mensagem dele e a caixa "Confirmo a baixa mesmo com o PIX gerado."; marcando a caixa e
 * confirmando de novo, a baixa vai com a confirmação. Qualquer outro erro aparece com a mensagem do servidor.
 */
export function ModalDarBaixa({ titulo, aoFechar }: ModalDarBaixaProps) {
  const aviso = useAviso();
  const idFormulario = useId();
  const baixa = useBaixarPagamento();
  const [erro, setErro] = useState<string | null>(null);
  const [caixaPixVisivel, setCaixaPixVisivel] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ValoresBaixa>({
    resolver: zodResolver(ESQUEMA_BAIXA),
    defaultValues: VALORES_INICIAIS_BAIXA,
  });

  const enviar = handleSubmit((valores) => {
    setErro(null);
    baixa.mutate(
      { idPagamento: titulo.idPagamento, requisicao: montarRequisicaoBaixa(valores) },
      {
        onSuccess: () => {
          aviso.sucesso(MENSAGEM_FINANCEIRO_SUCESSO_BAIXA);
          aoFechar();
        },
        onError: (falha) => {
          const decisao = decidirAposErroBaixa(falha);
          setErro(decisao.mensagem);
          if (decisao.mostrarCaixaPix) setCaixaPixVisivel(true);
        },
      },
    );
  });

  return (
    <Modal
      aberto
      aoFechar={() => {
        if (!baixa.isPending) aoFechar();
      }}
      titulo={MENSAGEM_FINANCEIRO_DIALOGO_TITULO}
      tamanho="pequeno"
      fecharAoClicarFora={!baixa.isPending}
      rodape={
        <>
          <Botao variante="contorno" onClick={aoFechar} disabled={baixa.isPending}>
            {MENSAGEM_FINANCEIRO_BOTAO_CANCELAR}
          </Botao>
          <Botao type="submit" form={idFormulario} carregando={baixa.isPending}>
            {MENSAGEM_FINANCEIRO_DIALOGO_CONFIRMAR}
          </Botao>
        </>
      }
    >
      <form id={idFormulario} noValidate onSubmit={enviar} className="flex flex-col gap-4">
        <p className="break-words rounded-cartao bg-steel-fundo p-4 text-texto-apoio">
          <span className="font-semibold text-navy">{titulo.origemStr}</span>
          {MENSAGEM_FINANCEIRO_SEPARADOR_RESUMO}
          <span>{titulo.clienteNome}</span>
          {MENSAGEM_FINANCEIRO_SEPARADOR_RESUMO}
          <span className="font-semibold tabular-nums text-navy">{titulo.valorStr}</span>
        </p>

        <Select
          rotulo={MENSAGEM_FINANCEIRO_DIALOGO_FORMA}
          obrigatorio
          opcoes={OPCOES_FORMA}
          erro={errors.forma?.message}
          {...register('forma')}
        />

        {caixaPixVisivel && (
          <label className="flex items-start gap-2 rounded-md bg-steel-fundo p-3 text-corpo-denso text-texto-desenho">
            <input type="checkbox" className="mt-0.5 h-4 w-4 accent-navy" {...register('confirmarPix')} />
            <span>{MENSAGEM_FINANCEIRO_CAIXA_PIX}</span>
          </label>
        )}

        {erro !== null && (
          <p role="alert" className={CLASSES_ERRO}>
            {erro}
          </p>
        )}
      </form>
    </Modal>
  );
}
