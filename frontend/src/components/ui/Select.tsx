import { forwardRef, useId, type SelectHTMLAttributes } from 'react';
import { Icone } from './Icone';
import {
  CLASSES_ERRO,
  classesControle,
  classesControleCartao,
  classesDica,
  classesEnvoltorioCampo,
  classesRotulo,
  classesSelectDesenho,
  classesSelectFrota,
  idsDescricao,
  type VarianteCampo,
} from './estilosCampo';

export interface OpcaoSelect {
  valor: string;
  rotulo: string;
  desabilitada?: boolean;
}

export interface SelectProps extends SelectHTMLAttributes<HTMLSelectElement> {
  /** Texto do rótulo (sempre obrigatório, por acessibilidade). */
  rotulo: string;
  /** Opções da lista. Para enums do domínio, use `criarOpcoes()` de `@/types/enums`. */
  opcoes: readonly OpcaoSelect[];
  /** Mensagem de erro exibida abaixo do campo. */
  erro?: string;
  /** Texto de ajuda exibido abaixo do campo quando não há erro. */
  dica?: string;
  /** Mostra o asterisco no rótulo e marca o campo como obrigatório para leitores de tela. */
  obrigatorio?: boolean;
  /** Esconde só o asterisco visual; `aria-required` segue para leitores de tela. */
  ocultarAsterisco?: boolean;
  /** Mantém o rótulo só para leitores de tela. */
  ocultarRotulo?: boolean;
  /** Texto da primeira opção, de valor vazio. `false` remove essa opção. Padrão: "Selecione". */
  opcaoVazia?: string | false;
  /**
   * `padrao` (visual de sempre), `cartao` (cartão-campo da etapa de documentos do cadastro), `desenho` (back office
   * do cadastro de veículo, tela 05) ou `frota` (filtros da tela 11, Gestão de Frota). Padrão: `padrao`.
   */
  variante?: Extract<VarianteCampo, 'padrao' | 'cartao' | 'desenho' | 'frota'>;
  /** Só na variante `desenho`: texto em destaque laranja e seminegrito (status do veículo). */
  destaque?: boolean;
  /** Nome do ícone Material Symbols mostrado dentro da lista, à esquerda (variante `padrao`). Opcional. */
  iconeInicial?: string;
}

/**
 * Lista de seleção nativa, estilizada, com rótulo, obrigatório (*) e erro.
 * Compatível com React Hook Form: `<Select rotulo="Status" opcoes={...} {...register('status')} />`.
 */
export const Select = forwardRef<HTMLSelectElement, SelectProps>(function Select(
  {
    rotulo,
    opcoes,
    erro,
    dica,
    obrigatorio = false,
    ocultarAsterisco = false,
    ocultarRotulo = false,
    opcaoVazia = 'Selecione',
    variante = 'padrao',
    destaque = false,
    iconeInicial,
    id,
    className = '',
    ...props
  },
  ref,
) {
  const idGerado = useId();
  const idCampo = id ?? `select-${idGerado}`;
  const idErro = `${idCampo}-erro`;
  const idDica = `${idCampo}-dica`;
  const desenho = variante === 'desenho';
  const frota = variante === 'frota';
  const comIconeInicial = iconeInicial !== undefined && variante === 'padrao';

  return (
    <div className={classesEnvoltorioCampo(variante)}>
      <label htmlFor={idCampo} className={ocultarRotulo ? 'sr-only' : classesRotulo(variante)}>
        {rotulo}
        {obrigatorio && !ocultarAsterisco && (
          <span className="ml-0.5 text-status-perigo" aria-hidden="true">
            *
          </span>
        )}
      </label>
      <div className="relative">
        <select
          {...props}
          ref={ref}
          id={idCampo}
          aria-invalid={erro ? true : undefined}
          aria-required={obrigatorio || undefined}
          aria-describedby={idsDescricao(dica && !erro && idDica, erro && idErro)}
          className={
            desenho
              ? `${classesSelectDesenho(Boolean(erro), destaque)} ${className}`
              : frota
                ? `${classesSelectFrota(Boolean(erro))} ${className}`
                : `${variante === 'cartao' ? classesControleCartao(Boolean(erro)) : classesControle(Boolean(erro))} appearance-none pr-10${comIconeInicial ? ' pl-10' : ''} ${className}`
          }
        >
          {opcaoVazia !== false && <option value="">{opcaoVazia}</option>}
          {opcoes.map((opcao) => (
            <option key={opcao.valor} value={opcao.valor} disabled={opcao.desabilitada}>
              {opcao.rotulo}
            </option>
          ))}
        </select>
        {comIconeInicial && (
          <Icone
            nome={iconeInicial}
            tamanho={20}
            className="pointer-events-none absolute left-3.5 top-1/2 -translate-y-1/2 text-texto-suave"
          />
        )}
        <Icone
          nome="expand_more"
          tamanho={frota ? 18 : undefined}
          className={
            frota
              ? 'pointer-events-none absolute right-2.5 top-1/2 -translate-y-1/2 text-contorno'
              : `pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 ${desenho ? 'text-contorno' : 'text-steel'}`
          }
        />
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
