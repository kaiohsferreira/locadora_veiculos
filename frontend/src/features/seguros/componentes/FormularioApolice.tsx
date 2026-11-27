import { useEffect } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Botao, Campo, Icone } from '@/components/ui';
import {
  esquemaApolice,
  TAMANHO_MAXIMO_COBERTURA,
  TAMANHO_MAXIMO_NUMERO_APOLICE,
  TAMANHO_MAXIMO_SEGURADORA,
  type CampoApolice,
  type ValoresApolice,
} from '@/features/seguros/regras/formularioApolice';
import { mascararMoedaDigitada } from '@/lib/formatters';
import {
  SEGUROS_BOTAO_CANCELAR,
  SEGUROS_BOTAO_SALVAR,
  SEGUROS_EXEMPLO_COBERTURA,
  SEGUROS_EXEMPLO_SEGURADORA,
  SEGUROS_ROTULO_COBERTURA,
  SEGUROS_ROTULO_FIM,
  SEGUROS_ROTULO_INICIO,
  SEGUROS_ROTULO_NUMERO,
  SEGUROS_ROTULO_PREMIO,
  SEGUROS_ROTULO_SEGURADORA,
  SEGUROS_ROTULO_VEICULO,
} from '@/lib/mensagensSeguros';
import { CampoVeiculoPlaca } from './CampoVeiculoPlaca';

/** Ordem dos campos na tela (o primeiro com erro do servidor recebe o foco). */
const CAMPOS_EM_ORDEM: readonly CampoApolice[] = [
  'veiculo',
  'seguradora',
  'numeroApolice',
  'dataInicio',
  'dataFim',
  'valorPremio',
  'cobertura',
];

export interface FormularioApoliceProps {
  valoresIniciais: ValoresApolice;
  enviando: boolean;
  /** Aviso geral no topo (falha que não é de um campo). */
  erroGeral: string | null;
  /** Erros do servidor por campo (409 no número, 404 no veículo, 400 na vigência...). */
  errosCampos: Partial<Record<CampoApolice, string>>;
  aoEnviar: (valores: ValoresApolice) => void;
  aoCancelar: () => void;
}

/**
 * Formulário da apólice, dentro da gaveta: veículo (busca por placa), seguradora, número, vigência (início e fim),
 * prêmio e cobertura. As regras da API aparecem no campo antes de enviar; os erros do servidor vão para o campo certo.
 */
export function FormularioApolice({ valoresIniciais, enviando, erroGeral, errosCampos, aoEnviar, aoCancelar }: FormularioApoliceProps) {
  const {
    control,
    register,
    handleSubmit,
    setError,
    setFocus,
    formState: { errors },
  } = useForm<ValoresApolice>({
    mode: 'onTouched',
    resolver: zodResolver(esquemaApolice),
    defaultValues: valoresIniciais,
  });

  // Erros do servidor viram erro do campo; o primeiro na ordem da tela recebe o foco depois do envio.
  useEffect(() => {
    let primeiro: CampoApolice | null = null;
    for (const campo of CAMPOS_EM_ORDEM) {
      const mensagem = errosCampos[campo];
      if (mensagem === undefined) continue;
      setError(campo, { type: 'server', message: mensagem });
      primeiro ??= campo;
    }
    if (primeiro !== null && primeiro !== 'veiculo' && !enviando) setFocus(primeiro);
  }, [errosCampos, setError, setFocus, enviando]);

  return (
    <form onSubmit={handleSubmit(aoEnviar)} noValidate className="flex min-h-0 flex-1 flex-col" aria-busy={enviando || undefined}>
      <fieldset disabled={enviando} className="min-w-0 flex-1 space-y-4 overflow-y-auto border-0 px-6 py-4">
        {erroGeral !== null && (
          <div role="alert" className="flex items-start gap-2 rounded-lg bg-status-perigo-fundo p-3 text-corpo-denso text-status-perigo">
            <Icone nome="error" tamanho={18} className="mt-0.5" />
            <span>{erroGeral}</span>
          </div>
        )}

        <Controller
          name="veiculo"
          control={control}
          render={({ field }) => (
            <CampoVeiculoPlaca
              rotulo={SEGUROS_ROTULO_VEICULO}
              valor={field.value}
              aoEscolher={(veiculo) => field.onChange(veiculo)}
              erro={errors.veiculo?.message}
              desabilitado={enviando}
              obrigatorio
            />
          )}
        />

        <Campo
          rotulo={SEGUROS_ROTULO_SEGURADORA}
          placeholder={SEGUROS_EXEMPLO_SEGURADORA}
          maxLength={TAMANHO_MAXIMO_SEGURADORA}
          autoComplete="off"
          obrigatorio
          erro={errors.seguradora?.message}
          {...register('seguradora')}
        />

        <Campo
          rotulo={SEGUROS_ROTULO_NUMERO}
          maxLength={TAMANHO_MAXIMO_NUMERO_APOLICE}
          autoComplete="off"
          obrigatorio
          erro={errors.numeroApolice?.message}
          {...register('numeroApolice')}
        />

        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <Campo type="date" rotulo={SEGUROS_ROTULO_INICIO} obrigatorio erro={errors.dataInicio?.message} {...register('dataInicio')} />
          <Campo type="date" rotulo={SEGUROS_ROTULO_FIM} obrigatorio erro={errors.dataFim?.message} {...register('dataFim')} />
        </div>

        <Campo
          rotulo={SEGUROS_ROTULO_PREMIO}
          inputMode="decimal"
          autoComplete="off"
          mascara={mascararMoedaDigitada}
          obrigatorio
          erro={errors.valorPremio?.message}
          {...register('valorPremio')}
        />

        <Campo
          rotulo={SEGUROS_ROTULO_COBERTURA}
          placeholder={SEGUROS_EXEMPLO_COBERTURA}
          maxLength={TAMANHO_MAXIMO_COBERTURA}
          autoComplete="off"
          erro={errors.cobertura?.message}
          {...register('cobertura')}
        />
      </fieldset>

      <footer className="flex flex-wrap items-center justify-end gap-2 border-t border-borda bg-steel-fundo px-6 py-4">
        <Botao variante="contorno" onClick={aoCancelar} disabled={enviando}>
          {SEGUROS_BOTAO_CANCELAR}
        </Botao>
        <Botao type="submit" icone="save" carregando={enviando}>
          {SEGUROS_BOTAO_SALVAR}
        </Botao>
      </footer>
    </form>
  );
}
