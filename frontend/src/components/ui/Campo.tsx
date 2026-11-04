import { forwardRef, useId, type ChangeEvent, type InputHTMLAttributes } from 'react';
import { Icone } from './Icone';
import { CLASSES_DICA, CLASSES_ERRO, CLASSES_ROTULO, classesControle, idsDescricao } from './estilosCampo';

export interface CampoProps extends InputHTMLAttributes<HTMLInputElement> {
  /** Texto do rótulo (sempre obrigatório, por acessibilidade). */
  rotulo: string;
  /** Mensagem de erro exibida abaixo do campo (ex.: `errors.cpf?.message`). */
  erro?: string;
  /** Texto de ajuda exibido abaixo do campo quando não há erro. */
  dica?: string;
  /** Mostra o asterisco no rótulo e marca o campo como obrigatório para leitores de tela. */
  obrigatorio?: boolean;
  /** Mantém o rótulo só para leitores de tela (ex.: barra de filtros densa). */
  ocultarRotulo?: boolean;
  /** Máscara aplicada a cada digitação (ex.: `formatarCpf`, `formatarPlaca` de `@/lib/formatters`). */
  mascara?: (valor: string) => string;
}

/**
 * Campo de texto com rótulo, obrigatório (*) e mensagem de erro.
 * Compatível com React Hook Form: `<Campo rotulo="CPF" {...register('cpf')} erro={errors.cpf?.message} />`.
 */
export const Campo = forwardRef<HTMLInputElement, CampoProps>(function Campo(
  { rotulo, erro, dica, obrigatorio = false, ocultarRotulo = false, mascara, id, className = '', onChange, ...props },
  ref,
) {
  const idGerado = useId();
  const idCampo = id ?? `campo-${idGerado}`;
  const idErro = `${idCampo}-erro`;
  const idDica = `${idCampo}-dica`;

  function aoAlterar(evento: ChangeEvent<HTMLInputElement>) {
    if (mascara) {
      evento.target.value = mascara(evento.target.value);
    }
    onChange?.(evento);
  }

  return (
    <div className="flex flex-col gap-1">
      <label htmlFor={idCampo} className={ocultarRotulo ? 'sr-only' : CLASSES_ROTULO}>
        {rotulo}
        {obrigatorio && (
          <span className="ml-0.5 text-status-perigo" aria-hidden="true">
            *
          </span>
        )}
      </label>
      <input
        {...props}
        ref={ref}
        id={idCampo}
        onChange={aoAlterar}
        aria-invalid={erro ? true : undefined}
        aria-required={obrigatorio || undefined}
        aria-describedby={idsDescricao(dica && !erro && idDica, erro && idErro)}
        className={`${classesControle(Boolean(erro))} ${className}`}
      />
      {dica && !erro && (
        <p id={idDica} className={CLASSES_DICA}>
          {dica}
        </p>
      )}
      {erro && (
        <p id={idErro} role="alert" className={CLASSES_ERRO}>
          <Icone nome="error" tamanho={14} />
          {erro}
        </p>
      )}
    </div>
  );
});
