import { forwardRef, useId, useMemo, useState, type TextareaHTMLAttributes } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Botao, Campo, Modal, Select, useAviso, type OpcaoSelect } from '@/components/ui';
import { CLASSES_ERRO, classesEnvoltorioCampo, classesRotulo } from '@/components/ui/estilosCampo';
import { aplicarErrosDeCampo, obterMensagemErro } from '@/lib/erros';
import { formatarPlaca } from '@/lib/formatters';
import {
  MENSAGEM_MANUTENCAO_BOTAO_ABRIR_CONFIRMAR,
  MENSAGEM_MANUTENCAO_BOTAO_CANCELAR,
  MENSAGEM_MANUTENCAO_CAMPO_DATA_ENTRADA,
  MENSAGEM_MANUTENCAO_CAMPO_DESCRICAO,
  MENSAGEM_MANUTENCAO_CAMPO_OFICINA,
  MENSAGEM_MANUTENCAO_CAMPO_TIPO,
  MENSAGEM_MANUTENCAO_CAMPO_VEICULO,
  MENSAGEM_MANUTENCAO_MODAL_ABRIR_TITULO,
  MENSAGEM_MANUTENCAO_SUCESSO_ABRIR,
} from '@/lib/mensagens';
import { ROTULO_TIPO_MANUTENCAO, TIPO_MANUTENCAO } from '@/types/enums';
import { useAbrirManutencao } from '@/features/manutencao/hooks/useAbrirManutencao';
import { useVeiculosParaOrdem } from '@/features/manutencao/hooks/useVeiculosParaOrdem';
import {
  TAMANHO_MAXIMO_DESCRICAO_MANUTENCAO,
  TAMANHO_MAXIMO_OFICINA_MANUTENCAO,
  criarEsquemaAbrirOrdem,
  dataIsoLocal,
  montarRequisicaoAbrir,
  type ValoresAbrirOrdem,
} from '@/features/manutencao/formularios/esquemasOrdem';

/** Campos que o servidor pode recusar com mensagem por campo (400). */
const CAMPOS_SERVIDOR_ABRIR = ['idVeiculo', 'tipo', 'oficina', 'descricao', 'dataEntrada'] as const;

const OPCOES_TIPO: readonly OpcaoSelect[] = TIPO_MANUTENCAO.map((tipo) => ({
  valor: tipo,
  rotulo: ROTULO_TIPO_MANUTENCAO[tipo],
}));

/** Descrição da ordem: campo de texto longo (o desenho não tem um componente próprio para isso). */
const CampoTextoLongo = forwardRef<HTMLTextAreaElement, TextareaHTMLAttributes<HTMLTextAreaElement> & { rotulo: string; erro?: string }>(
  function CampoTextoLongo({ rotulo, erro, id, className = '', ...props }, ref) {
    const idGerado = useId();
    const idCampo = id ?? `texto-${idGerado}`;
    return (
      <div className={classesEnvoltorioCampo('padrao')}>
        <label htmlFor={idCampo} className={classesRotulo('padrao')}>
          {rotulo}
        </label>
        <textarea
          {...props}
          ref={ref}
          id={idCampo}
          rows={3}
          aria-invalid={erro ? true : undefined}
          className={`min-h-20 w-full resize-y rounded-md border bg-branco px-3 py-2 text-corpo-denso text-texto placeholder:text-texto-suave focus:outline-none focus:ring-2 ${
            erro ? 'border-status-perigo focus:ring-status-perigo/20' : 'border-borda-forte focus:border-steel focus:ring-steel/30'
          } ${className}`}
        />
        {erro && (
          <p role="alert" className={CLASSES_ERRO}>
            {erro}
          </p>
        )}
      </div>
    );
  },
);

export interface ModalAbrirOrdemProps {
  /** Veículo já escolhido (Bloquear p/ Oficina e Abrir ordem do cartão), ou nulo para escolher na lista. */
  idVeiculoInicial: number | null;
  /** Fecha o modal (sucesso ou cancelamento). O pai desmonta o componente. */
  aoFechar: () => void;
}

/**
 * Modal "Abrir ordem de manutenção". Veículo e tipo são obrigatórios; oficina, descrição e data de entrada são opcionais
 * (a data vazia é hoje). Valida antes (Zod) e mostra as mensagens do servidor no campo certo; 404 e 409 aparecem no topo.
 * Depois do sucesso, o aviso é dado pela página e o modal fecha.
 */
export function ModalAbrirOrdem({ idVeiculoInicial, aoFechar }: ModalAbrirOrdemProps) {
  const aviso = useAviso();
  const idFormulario = useId();
  const veiculos = useVeiculosParaOrdem();
  const abrir = useAbrirManutencao();
  const [hoje] = useState(() => dataIsoLocal(new Date()));
  const [erroGeral, setErroGeral] = useState<string | null>(null);
  const esquema = useMemo(() => criarEsquemaAbrirOrdem(hoje), [hoje]);

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<ValoresAbrirOrdem>({
    resolver: zodResolver(esquema),
    defaultValues: {
      idVeiculo: idVeiculoInicial === null ? '' : String(idVeiculoInicial),
      tipo: '',
      oficina: '',
      descricao: '',
      dataEntrada: hoje,
    },
  });

  const opcoesVeiculos: OpcaoSelect[] = (veiculos.data ?? []).map((veiculo) => ({
    valor: String(veiculo.idVeiculo),
    rotulo: `${formatarPlaca(veiculo.placa)} · ${veiculo.marca} ${veiculo.modelo}`,
  }));

  const enviar = handleSubmit((valores) => {
    setErroGeral(null);
    abrir.mutate(montarRequisicaoAbrir(valores), {
      onSuccess: () => {
        aviso.sucesso(MENSAGEM_MANUTENCAO_SUCESSO_ABRIR);
        aoFechar();
      },
      onError: (erro) => {
        const aplicou = aplicarErrosDeCampo(erro, CAMPOS_SERVIDOR_ABRIR, (campo, mensagem) =>
          setError(campo, { type: 'server', message: mensagem }),
        );
        if (!aplicou) setErroGeral(obterMensagemErro(erro));
      },
    });
  });

  return (
    <Modal
      aberto
      aoFechar={() => {
        if (!abrir.isPending) aoFechar();
      }}
      titulo={MENSAGEM_MANUTENCAO_MODAL_ABRIR_TITULO}
      tamanho="medio"
      fecharAoClicarFora={!abrir.isPending}
      rodape={
        <>
          <Botao variante="contorno" onClick={aoFechar} disabled={abrir.isPending}>
            {MENSAGEM_MANUTENCAO_BOTAO_CANCELAR}
          </Botao>
          <Botao type="submit" form={idFormulario} carregando={abrir.isPending}>
            {MENSAGEM_MANUTENCAO_BOTAO_ABRIR_CONFIRMAR}
          </Botao>
        </>
      }
    >
      <form id={idFormulario} noValidate onSubmit={enviar} className="flex flex-col gap-4">
        {veiculos.isError && (
          <p role="alert" className={CLASSES_ERRO}>
            {obterMensagemErro(veiculos.error)}
          </p>
        )}

        {/* A chave troca quando a lista chega: o select precisa nascer com as opções para mostrar o veículo escolhido. */}
        <Select
          key={veiculos.isSuccess ? 'veiculos-prontos' : 'veiculos-carregando'}
          rotulo={MENSAGEM_MANUTENCAO_CAMPO_VEICULO}
          obrigatorio
          opcoes={opcoesVeiculos}
          erro={errors.idVeiculo?.message}
          {...register('idVeiculo')}
        />

        <div className="grid gap-4 sm:grid-cols-2">
          <Select
            rotulo={MENSAGEM_MANUTENCAO_CAMPO_TIPO}
            obrigatorio
            opcoes={OPCOES_TIPO}
            erro={errors.tipo?.message}
            {...register('tipo')}
          />
          <Campo
            rotulo={MENSAGEM_MANUTENCAO_CAMPO_OFICINA}
            maxLength={TAMANHO_MAXIMO_OFICINA_MANUTENCAO}
            erro={errors.oficina?.message}
            {...register('oficina')}
          />
        </div>

        <CampoTextoLongo
          rotulo={MENSAGEM_MANUTENCAO_CAMPO_DESCRICAO}
          maxLength={TAMANHO_MAXIMO_DESCRICAO_MANUTENCAO}
          erro={errors.descricao?.message}
          {...register('descricao')}
        />

        <Campo
          type="date"
          rotulo={MENSAGEM_MANUTENCAO_CAMPO_DATA_ENTRADA}
          max={hoje}
          erro={errors.dataEntrada?.message}
          {...register('dataEntrada')}
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
