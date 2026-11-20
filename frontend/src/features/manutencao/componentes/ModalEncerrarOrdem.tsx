import { useId, useMemo, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Botao, Campo, Modal, useAviso } from '@/components/ui';
import { CLASSES_ERRO } from '@/components/ui/estilosCampo';
import { aplicarErrosDeCampo, obterMensagemErro } from '@/lib/erros';
import { mascararMoedaDigitada } from '@/lib/formatters';
import {
  MENSAGEM_MANUTENCAO_BOTAO_CANCELAR,
  MENSAGEM_MANUTENCAO_BOTAO_ENCERRAR_ORDEM,
  MENSAGEM_MANUTENCAO_CAMPO_CUSTO,
  MENSAGEM_MANUTENCAO_CAMPO_DATA_SAIDA,
  MENSAGEM_MANUTENCAO_CAMPO_KM_SAIDA,
  MENSAGEM_MANUTENCAO_SUCESSO_ENCERRAR,
  mensagemManutencaoEncerrarTitulo,
} from '@/lib/mensagens';
import type { ManutencaoListaRetorno } from '@/types/manutencao';
import { useEncerrarManutencao } from '@/features/manutencao/hooks/useEncerrarManutencao';
import {
  criarEsquemaEncerrarOrdem,
  dataIsoLocal,
  montarRequisicaoEncerrar,
  type ValoresEncerrarOrdem,
} from '@/features/manutencao/formularios/esquemasOrdem';

/** Campos que o servidor pode recusar com mensagem por campo (400). */
const CAMPOS_SERVIDOR_ENCERRAR = ['kmAtual', 'dataSaida', 'custo'] as const;

export interface ModalEncerrarOrdemProps {
  /** Ordem aberta que será encerrada (linha da tabela). */
  ordem: ManutencaoListaRetorno;
  /** Fecha o modal (sucesso ou cancelamento). O pai desmonta o componente. */
  aoFechar: () => void;
}

/**
 * Modal "Encerrar ordem #n": quilometragem na saída (obrigatória), data de saída (vazia é hoje) e custo (opcional, em
 * reais, com máscara). Valida antes e mostra as mensagens do servidor no campo certo; 404 e 409 aparecem no topo.
 */
export function ModalEncerrarOrdem({ ordem, aoFechar }: ModalEncerrarOrdemProps) {
  const aviso = useAviso();
  const idFormulario = useId();
  const encerrar = useEncerrarManutencao();
  const [hoje] = useState(() => dataIsoLocal(new Date()));
  const [erroGeral, setErroGeral] = useState<string | null>(null);
  const esquema = useMemo(() => criarEsquemaEncerrarOrdem(hoje, ordem.dataEntrada), [hoje, ordem.dataEntrada]);

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<ValoresEncerrarOrdem>({
    resolver: zodResolver(esquema),
    defaultValues: { kmAtual: '', dataSaida: hoje, custo: '' },
  });

  const enviar = handleSubmit((valores) => {
    setErroGeral(null);
    encerrar.mutate(
      { idManutencao: ordem.idManutencao, requisicao: montarRequisicaoEncerrar(valores) },
      {
        onSuccess: () => {
          aviso.sucesso(MENSAGEM_MANUTENCAO_SUCESSO_ENCERRAR);
          aoFechar();
        },
        onError: (erro) => {
          const aplicou = aplicarErrosDeCampo(erro, CAMPOS_SERVIDOR_ENCERRAR, (campo, mensagem) =>
            setError(campo, { type: 'server', message: mensagem }),
          );
          if (!aplicou) setErroGeral(obterMensagemErro(erro));
        },
      },
    );
  });

  return (
    <Modal
      aberto
      aoFechar={() => {
        if (!encerrar.isPending) aoFechar();
      }}
      titulo={mensagemManutencaoEncerrarTitulo(ordem.idManutencao)}
      tamanho="pequeno"
      fecharAoClicarFora={!encerrar.isPending}
      rodape={
        <>
          <Botao variante="contorno" onClick={aoFechar} disabled={encerrar.isPending}>
            {MENSAGEM_MANUTENCAO_BOTAO_CANCELAR}
          </Botao>
          <Botao type="submit" form={idFormulario} carregando={encerrar.isPending}>
            {MENSAGEM_MANUTENCAO_BOTAO_ENCERRAR_ORDEM}
          </Botao>
        </>
      }
    >
      <form id={idFormulario} noValidate onSubmit={enviar} className="flex flex-col gap-4">
        <Campo
          type="number"
          min={0}
          step={1}
          inputMode="numeric"
          rotulo={MENSAGEM_MANUTENCAO_CAMPO_KM_SAIDA}
          obrigatorio
          erro={errors.kmAtual?.message}
          {...register('kmAtual')}
        />

        <Campo
          type="date"
          rotulo={MENSAGEM_MANUTENCAO_CAMPO_DATA_SAIDA}
          max={hoje}
          erro={errors.dataSaida?.message}
          {...register('dataSaida')}
        />

        <Campo
          inputMode="decimal"
          rotulo={MENSAGEM_MANUTENCAO_CAMPO_CUSTO}
          mascara={mascararMoedaDigitada}
          erro={errors.custo?.message}
          {...register('custo')}
        />

        {erroGeral !== null && (
          <p role="alert" className={CLASSES_ERRO}>
            {erroGeral}
          </p>
        )}
      </form>
    </Modal>
  );
}
