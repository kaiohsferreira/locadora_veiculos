import { forwardRef, useId, type ChangeEvent, type InputHTMLAttributes, type ReactNode } from 'react';
import { Icone } from './Icone';
import {
  CLASSES_ERRO,
  CLASSES_PREFIXO_DESENHO,
  CLASSES_SUFIXO_DESENHO,
  classesApoioRotulo,
  classesControle,
  classesControleCadastro,
  classesControleCartao,
  classesControleDesenho,
  classesControleFrota,
  classesControlePortal,
  classesDica,
  classesEnvoltorioCampo,
  classesRotulo,
  idsDescricao,
  type TipografiaCampo,
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
   * cadastro), `cartao` (cartão-campo da etapa de documentos) ou `desenho` (back office do cadastro de veículo).
   * Padrão: `padrao`.
   */
  variante?: VarianteCampo;
  /** Esconde só o asterisco visual; `aria-required` segue para leitores de tela. */
  ocultarAsterisco?: boolean;
  /** Texto de apoio na linha do rótulo, à direita (ex.: "Conforme documento oficial"). Opcional. */
  apoioRotulo?: string;
  /** Classes do texto de apoio do rótulo (ex.: a pílula "4 diárias" da busca). Sem ela, vale o estilo padrão do apoio. Opcional. */
  apoioRotuloClasse?: string;
  /** Mantém o apoio do rótulo na mesma linha em qualquer largura (também abaixo de `sm`, como nos desenhos). Opcional. */
  apoioRotuloEmLinha?: boolean;
  /** Mostra o ícone de "conferido" à direita, dentro do campo (valor válido). Opcional. */
  valido?: boolean;
  /** Ícone Material Symbols antes do texto de ajuda (`dica`). Opcional. */
  dicaIcone?: string;
  /** Só na variante `desenho`: texto ou elemento dentro do campo, à esquerda (ex.: "R$"). */
  prefixo?: ReactNode;
  /** Só na variante `desenho`: texto ou ícone dentro do campo, à direita (ex.: "KM"). */
  sufixo?: ReactNode;
  /**
   * Só na variante `desenho`: o sufixo é um ícone decorativo sobre a borda direita e não reserva espaço no texto
   * (o desenho do chassi usa 16px de recuo; o "KM" do odômetro precisa do recuo padrão e não usa esta opção).
   */
  sufixoSemRecuo?: boolean;
  /** Só na variante `desenho`: recuo maior à esquerda para o emblema da placa (56px em vez de 44px). */
  prefixoLargo?: boolean;
  /** Só na variante `desenho`: fonte do texto digitado (corpo, dado, placa ou valor). Padrão: corpo. */
  tipografia?: TipografiaCampo;
}

/** Classes do input conforme a variante (a padrão só ganha `pl-10` quando há ícone inicial). */
function classesDoControle(
  variante: VarianteCampo,
  temErro: boolean,
  comIcone: boolean,
  desenho: { recuoEsquerdo: 'nenhum' | 'prefixo' | 'prefixoLargo'; sufixo: boolean; tipografia: TipografiaCampo },
): string {
  if (variante === 'portal') return classesControlePortal(temErro, comIcone);
  if (variante === 'cadastro') return classesControleCadastro(temErro, comIcone);
  if (variante === 'cartao') return classesControleCartao(temErro);
  if (variante === 'desenho') return classesControleDesenho(temErro, desenho);
  if (variante === 'frota') return classesControleFrota(temErro, desenho.recuoEsquerdo !== 'nenhum');
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
    apoioRotuloClasse,
    apoioRotuloEmLinha = false,
    valido,
    dicaIcone,
    prefixo,
    sufixo,
    sufixoSemRecuo = false,
    prefixoLargo = false,
    tipografia = 'corpo',
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
  const desenho = variante === 'desenho';
  const frota = variante === 'frota';
  const recuoEsquerdo = prefixo === undefined ? 'nenhum' : prefixoLargo ? 'prefixoLargo' : 'prefixo';
  const classesInput = classesDoControle(variante, Boolean(erro), iconeInicial !== undefined, {
    recuoEsquerdo,
    sufixo: sufixo !== undefined && !sufixoSemRecuo,
    tipografia,
  });
  const comEnvoltorio =
    iconeInicial !== undefined || valido === true || ((desenho || frota) && (prefixo !== undefined || sufixo !== undefined));

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

  // Na variante desenho, rótulo e apoio ficam numa linha só, nas pontas, sem quebra de linha e sem espaço fixo entre eles.
  const semQuebraComApoio = desenho && apoioRotulo !== undefined ? ' whitespace-nowrap' : '';
  const classeLinhaRotulo = apoioRotuloEmLinha
    ? 'flex items-center justify-between gap-2'
    : desenho
      ? 'flex flex-col gap-0.5 sm:flex-row sm:items-center sm:justify-between'
      : 'flex flex-col gap-0.5 sm:flex-row sm:items-center sm:justify-between sm:gap-2';

  const elementoRotulo = (
    <label htmlFor={idCampo} className={(ocultarRotulo ? 'sr-only' : classesRotulo(variante)) + semQuebraComApoio}>
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
        <div className={classeLinhaRotulo}>
          {elementoRotulo}
          <span id={idApoio} className={(apoioRotuloClasse ?? classesApoioRotulo(variante)) + semQuebraComApoio}>{apoioRotulo}</span>
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
          {prefixo !== undefined && <span className={CLASSES_PREFIXO_DESENHO}>{prefixo}</span>}
          {input}
          {sufixo !== undefined && <span className={CLASSES_SUFIXO_DESENHO}>{sufixo}</span>}
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
