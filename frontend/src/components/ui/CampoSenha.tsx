import { forwardRef, useId, useState, type ReactNode } from 'react';
import type { CampoProps } from './Campo';
import { Icone } from './Icone';
import {
  CLASSES_ERRO,
  classesControle,
  classesControleCartao,
  classesControlePortal,
  classesDica,
  classesEnvoltorioCampo,
  classesRotulo,
  idsDescricao,
} from './estilosCampo';

/**
 * Mesmas props do Campo, menos `type` (fixo em senha), `mascara` (não se aplica a senha) e as props
 * de apoio visual do Campo de texto (`apoioRotulo`, `valido`, `dicaIcone`).
 */
export interface CampoSenhaProps
  extends Omit<CampoProps, 'type' | 'mascara' | 'apoioRotulo' | 'valido' | 'dicaIcone'> {
  /**
   * Ação na mesma linha do rótulo, à direita (ex.: link "Esqueci minha senha"). Fica antes do input
   * no DOM, então entra na ordem do Tab antes do campo. Sem a prop, o componente não muda.
   */
  acaoRotulo?: ReactNode;
}

const CLASSES_BOTAO_PADRAO =
  'absolute inset-y-0 right-0 flex w-11 items-center justify-center rounded-r-md text-texto-suave transition-colors hover:text-texto focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-steel/30 disabled:cursor-not-allowed disabled:opacity-50';

const CLASSES_BOTAO_PORTAL =
  'absolute right-2.5 top-1/2 flex h-8 w-8 -translate-y-1/2 items-center justify-center rounded text-texto-suave transition-colors hover:text-texto focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-steel disabled:cursor-not-allowed disabled:opacity-50';

/**
 * Campo de senha com botão para mostrar ou ocultar o texto. Mesmo visual e API do Campo.
 * Compatível com React Hook Form: `<CampoSenha rotulo="Senha" {...register('senha')} erro={errors.senha?.message} />`.
 * Use `autoComplete="current-password"` no login e `"new-password"` no cadastro.
 * O valor digitado nunca é registrado em log.
 */
export const CampoSenha = forwardRef<HTMLInputElement, CampoSenhaProps>(function CampoSenha(
  {
    rotulo,
    erro,
    dica,
    obrigatorio = false,
    ocultarRotulo = false,
    iconeInicial,
    variante = 'padrao',
    ocultarAsterisco = false,
    id,
    className = '',
    disabled,
    acaoRotulo,
    'aria-describedby': descricaoExterna,
    ...props
  },
  ref,
) {
  const [visivel, setVisivel] = useState(false);
  const idGerado = useId();
  const idCampo = id ?? `campo-senha-${idGerado}`;
  const idErro = `${idCampo}-erro`;
  const idDica = `${idCampo}-dica`;
  // O botão de mostrar/ocultar da variante portal e da cartão fica sobre o campo, com o mesmo posicionamento.
  const portal = variante === 'portal' || variante === 'cartao';

  const classesInput =
    variante === 'portal'
      ? `${classesControlePortal(Boolean(erro), iconeInicial !== undefined)} pr-12`
      : variante === 'cartao'
        ? `${classesControleCartao(Boolean(erro))} pr-12`
        : `${classesControle(Boolean(erro))} pr-11${iconeInicial !== undefined ? ' pl-10' : ''}`;

  const elementoRotulo = (
    <label htmlFor={idCampo} className={ocultarRotulo ? 'sr-only' : classesRotulo(variante)}>
      {rotulo}
      {obrigatorio && !ocultarAsterisco && (
        <span className="ml-0.5 text-status-perigo" aria-hidden="true">
          *
        </span>
      )}
    </label>
  );

  return (
    <div className={classesEnvoltorioCampo(variante)}>
      {acaoRotulo === undefined ? (
        elementoRotulo
      ) : (
        <div className="flex items-center justify-between gap-2">
          {elementoRotulo}
          {acaoRotulo}
        </div>
      )}
      <div className="relative">
        {iconeInicial !== undefined && (
          <Icone
            nome={iconeInicial}
            tamanho={20}
            className="pointer-events-none absolute left-3.5 top-1/2 -translate-y-1/2 text-texto-suave"
          />
        )}
        <input
          {...props}
          ref={ref}
          id={idCampo}
          type={visivel ? 'text' : 'password'}
          disabled={disabled}
          aria-invalid={erro ? true : undefined}
          aria-required={obrigatorio || undefined}
          aria-describedby={idsDescricao(descricaoExterna, dica && !erro && idDica, erro && idErro)}
          className={`${classesInput} ${className}`}
        />
        <button
          type="button"
          onClick={() => setVisivel((atual) => !atual)}
          aria-label="Mostrar senha"
          aria-pressed={visivel}
          disabled={disabled}
          className={portal ? CLASSES_BOTAO_PORTAL : CLASSES_BOTAO_PADRAO}
        >
          <Icone nome={visivel ? 'visibility_off' : 'visibility'} tamanho={20} />
        </button>
      </div>
      {dica && !erro && (
        <p id={idDica} className={classesDica(variante)}>
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
