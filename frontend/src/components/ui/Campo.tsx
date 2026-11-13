import { forwardRef, useId, type ChangeEvent, type InputHTMLAttributes } from 'react';
import { Icone } from './Icone';
import {
  CLASSES_APOIO_ROTULO,
  CLASSES_ERRO,
  classesControle,
  classesControleCadastro,
  classesControleCartao,
  classesControlePortal,
  classesDica,
  classesEnvoltorioCampo,
  classesRotulo,
  idsDescricao,
  type VarianteCampo,
} from './estilosCampo';

export interface CampoProps extends InputHTMLAttributes<HTMLInputElement> {
  /** Texto do rótulo (sempre obrigatório, por acessibilidade). */
  rotulo: string;
  /** Mensagem de erro exibida abaixo do campo (ex.: `errors.cpf?.message`). */
  erro?: string;
  /** Texto de ajuda exibido abaixo do campo quando não há erro. */
  dica?: string;
  /** Marca o campo como obrigatório para leitores de tela e mostra o asterisco no rótulo (veja `ocultarAsterisco`). */
  obrigatorio?: boolean;
  /** Mantém o rótulo só para leitores de tela (ex.: barra de filtros densa). */
  ocultarRotulo?: boolean;
  /** Máscara aplicada a cada digitação (ex.: `formatarCpf`, `formatarPlaca` de `@/lib/formatters`). */
  mascara?: (valor: string) => string;
  /** Nome do ícone Material Symbols mostrado dentro do campo, à esquerda. Opcional. */
  iconeInicial?: string;
  /**
   * `padrao` (visual de sempre), `portal` (tela de acesso), `cadastro` (etapa de dados pessoais do
   * cadastro) ou `cartao` (cartão-campo da etapa de documentos). Padrão: `padrao`.
   */
  variante?: VarianteCampo;
  /** Esconde só o asterisco visual; `aria-required` segue para leitores de tela. */
  ocultarAsterisco?: boolean;
  /** Texto de apoio na linha do rótulo, à direita (ex.: "Conforme documento oficial"). Opcional. */
  apoioRotulo?: string;
  /** Mostra o ícone de "conferido" à direita, dentro do campo (valor válido). Opcional. */
  valido?: boolean;
  /** Ícone Material Symbols antes do texto de ajuda (`dica`). Opcional. */
  dicaIcone?: string;
}

/** Classes do input conforme a variante (a padrão só ganha `pl-10` quando há ícone inicial). */
function classesDoControle(variante: VarianteCampo, temErro: boolean, comIcone: boolean): string {
  if (variante === 'portal') return classesControlePortal(temErro, comIcone);
  if (variante === 'cadastro') return classesControleCadastro(temErro, comIcone);
  if (variante === 'cartao') return classesControleCartao(temErro);
  return `${classesControle(temErro)}${comIcone ? ' pl-10' : ''}`;
}

/**
 * Campo de texto com rótulo, obrigatório (*) e mensagem de erro.
 * Compatível com React Hook Form: `<Campo rotulo="CPF" {...register('cpf')} erro={errors.cpf?.message} />`.
 */
export const Campo = forwardRef<HTMLInputElement, CampoProps>(function Campo(
  {
    rotulo,
    erro,
    dica,
    obrigatorio = false,
    ocultarRotulo = false,
    mascara,
    iconeInicial,
    variante = 'padrao',
    ocultarAsterisco = false,
    apoioRotulo,
    valido,
    dicaIcone,
    id,
    className = '',
    onChange,
    ...props
  },
  ref,
) {
  const idGerado = useId();
  const idCampo = id ?? `campo-${idGerado}`;
  const idErro = `${idCampo}-erro`;
  const idDica = `${idCampo}-dica`;
  const idApoio = `${idCampo}-apoio`;

  function aoAlterar(evento: ChangeEvent<HTMLInputElement>) {
    if (mascara) {
      evento.target.value = mascara(evento.target.value);
    }
    onChange?.(evento);
  }

  const cadastro = variante === 'cadastro';
  const classesInput = classesDoControle(variante, Boolean(erro), iconeInicial !== undefined);
  const comEnvoltorio = iconeInicial !== undefined || valido === true;

  const input = (
    <input
      {...props}
      ref={ref}
      id={idCampo}
      onChange={aoAlterar}
      aria-invalid={erro ? true : undefined}
      aria-required={obrigatorio || undefined}
      aria-describedby={idsDescricao(apoioRotulo && idApoio, dica && !erro && idDica, erro && idErro)}
      className={`${classesInput} ${className}`}
    />
  );

  const elementoRotulo = (
    <label htmlFor={idCampo} className={ocultarRotulo ? 'sr-only' : classesRotulo(variante)}>
      {rotulo}
      {obrigatorio && !ocultarAsterisco && (
        <span className={`ml-0.5 text-status-perigo${cadastro ? ' font-bold' : ''}`} aria-hidden="true">
          *
        </span>
      )}
    </label>
  );

  return (
    <div className={classesEnvoltorioCampo(variante)}>
      {apoioRotulo === undefined ? (
        elementoRotulo
      ) : (
        <div className="flex flex-col gap-0.5 sm:flex-row sm:items-center sm:justify-between sm:gap-2">
          {elementoRotulo}
          <span id={idApoio} className={CLASSES_APOIO_ROTULO}>{apoioRotulo}</span>
        </div>
      )}
      {!comEnvoltorio ? (
        input
      ) : (
        <div className="relative flex items-center">
          {iconeInicial !== undefined && (
            <Icone
              nome={iconeInicial}
              tamanho={cadastro ? 18 : 20}
              className={`pointer-events-none absolute text-texto-suave ${cadastro ? 'left-3' : 'left-3.5'}`}
            />
          )}
          {input}
          {valido === true && !erro && (
            // Campo de data nativo tem o próprio ícone de calendário na borda direita: o "conferido" fica ao lado dele.
            <Icone
              nome="check_circle"
              tamanho={20}
              preenchido
              className={`pointer-events-none absolute text-status-sucesso ${props.type === 'date' ? 'right-10' : 'right-3'}`}
            />
          )}
        </div>
      )}
      {dica && !erro && (
        <p id={idDica} className={classesDica(variante)}>
          {dicaIcone !== undefined && <Icone nome={dicaIcone} tamanho={14} className="text-status-pendente" />}
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
