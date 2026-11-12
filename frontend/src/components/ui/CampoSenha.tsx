import { forwardRef, useId, useState, type ReactNode } from 'react';
import type { CampoProps } from './Campo';
import { Icone } from './Icone';
import { CLASSES_DICA, CLASSES_ERRO, CLASSES_ROTULO, classesControle, idsDescricao } from './estilosCampo';

/** Mesmas props do Campo, menos `type` (fixo em senha) e `mascara` (não se aplica a senha). */
export interface CampoSenhaProps extends Omit<CampoProps, 'type' | 'mascara'> {
  /**
   * Ação na mesma linha do rótulo, à direita (ex.: link "Esqueci minha senha"). Fica antes do input
   * no DOM, então entra na ordem do Tab antes do campo. Sem a prop, o componente não muda.
   */
  acaoRotulo?: ReactNode;
}

/**
 * Campo de senha com botão para mostrar ou ocultar o texto. Mesmo visual e API do Campo.
 * Compatível com React Hook Form: `<CampoSenha rotulo="Senha" {...register('senha')} erro={errors.senha?.message} />`.
 * Use `autoComplete="current-password"` no login e `"new-password"` no cadastro.
 * O valor digitado nunca é registrado em log.
 */
export const CampoSenha = forwardRef<HTMLInputElement, CampoSenhaProps>(function CampoSenha(
  { rotulo, erro, dica, obrigatorio = false, ocultarRotulo = false, id, className = '', disabled, acaoRotulo, ...props },
  ref,
) {
  const [visivel, setVisivel] = useState(false);
  const idGerado = useId();
  const idCampo = id ?? `campo-senha-${idGerado}`;
  const idErro = `${idCampo}-erro`;
  const idDica = `${idCampo}-dica`;

  const elementoRotulo = (
    <label htmlFor={idCampo} className={ocultarRotulo ? 'sr-only' : CLASSES_ROTULO}>
      {rotulo}
      {obrigatorio && (
        <span className="ml-0.5 text-status-perigo" aria-hidden="true">
          *
        </span>
      )}
    </label>
  );

  return (
    <div className="flex flex-col gap-1">
      {acaoRotulo === undefined ? (
        elementoRotulo
      ) : (
        <div className="flex items-center justify-between gap-2">
          {elementoRotulo}
          {acaoRotulo}
        </div>
      )}
      <div className="relative">
        <input
          {...props}
          ref={ref}
          id={idCampo}
          type={visivel ? 'text' : 'password'}
          disabled={disabled}
          aria-invalid={erro ? true : undefined}
          aria-required={obrigatorio || undefined}
          aria-describedby={idsDescricao(dica && !erro && idDica, erro && idErro)}
          className={`${classesControle(Boolean(erro))} pr-11 ${className}`}
        />
        <button
          type="button"
          onClick={() => setVisivel((atual) => !atual)}
          aria-label="Mostrar senha"
          aria-pressed={visivel}
          disabled={disabled}
          className="absolute inset-y-0 right-0 flex w-11 items-center justify-center rounded-r-md text-texto-suave transition-colors hover:text-texto focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-steel/30 disabled:cursor-not-allowed disabled:opacity-50"
        >
          <Icone nome={visivel ? 'visibility_off' : 'visibility'} tamanho={20} />
        </button>
      </div>
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
