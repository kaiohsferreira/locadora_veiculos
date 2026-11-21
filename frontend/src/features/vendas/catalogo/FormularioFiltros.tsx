import type { FormEvent, ReactNode } from 'react';
import { Botao, Campo } from '@/components/ui';
import { mascararMoedaDigitada } from '@/lib/formatters';
import {
  CATALOGO_BOTAO_APLICAR,
  CATALOGO_BOTAO_LIMPAR,
  CATALOGO_EXEMPLO_KM,
  CATALOGO_EXEMPLO_MARCA,
  CATALOGO_ROTULO_ANO,
  CATALOGO_ROTULO_ATE,
  CATALOGO_ROTULO_DE,
  CATALOGO_ROTULO_FAIXA_PRECO,
  CATALOGO_ROTULO_KM,
  CATALOGO_ROTULO_MARCA,
} from '@/lib/mensagensCatalogo';
import { mascararAno, mascararKm, type CamposFiltro, type ChaveFiltroCatalogo, type ErrosFiltro } from './filtrosCatalogo';

export interface FormularioFiltrosProps {
  /** Prefixo dos ids dos campos (o cartão do computador e a janela do celular ficam na mesma página). */
  idBase: string;
  campos: CamposFiltro;
  erros: ErrosFiltro;
  aoMudar: (campo: ChaveFiltroCatalogo, valor: string) => void;
  aoAplicar: () => void;
  aoLimpar: () => void;
}

/**
 * Título de um grupo de dois campos ("De" e "Até"). Valores em reais não cabem lado a lado na coluna estreita dos
 * filtros, então o grupo de preço empilha os campos (`empilhado`); os anos, com 4 dígitos, ficam lado a lado.
 */
function Grupo({ titulo, empilhado = false, children }: { titulo: string; empilhado?: boolean; children: ReactNode }) {
  return (
    <fieldset className="flex flex-col gap-2">
      <legend className="mb-2 text-corpo-denso font-semibold text-navy">{titulo}</legend>
      <div className={`grid gap-3 ${empilhado ? 'grid-cols-1' : 'grid-cols-2'}`}>{children}</div>
    </fieldset>
  );
}

/**
 * Campos dos filtros do catálogo: marca, faixa de preço, ano do modelo (de/até) e km máximo. Os filtros só valem ao
 * aplicar (botão ou Enter), para não buscar a cada tecla. Usado no cartão do computador e na janela do celular.
 */
export function FormularioFiltros({ idBase, campos, erros, aoMudar, aoAplicar, aoLimpar }: FormularioFiltrosProps) {
  function enviar(evento: FormEvent<HTMLFormElement>) {
    evento.preventDefault();
    aoAplicar();
  }

  return (
    <form className="flex flex-col gap-6" onSubmit={enviar} noValidate>
      <Campo
        id={`${idBase}-marca`}
        rotulo={CATALOGO_ROTULO_MARCA}
        placeholder={CATALOGO_EXEMPLO_MARCA}
        autoComplete="off"
        value={campos.marca}
        onChange={(evento) => aoMudar('marca', evento.target.value)}
        erro={erros.marca}
      />
      <Grupo titulo={CATALOGO_ROTULO_FAIXA_PRECO} empilhado>
        <Campo
          id={`${idBase}-preco-minimo`}
          rotulo={CATALOGO_ROTULO_DE}
          inputMode="decimal"
          autoComplete="off"
          mascara={mascararMoedaDigitada}
          value={campos.precoMinimo}
          onChange={(evento) => aoMudar('precoMinimo', evento.target.value)}
          erro={erros.precoMinimo}
        />
        <Campo
          id={`${idBase}-preco-maximo`}
          rotulo={CATALOGO_ROTULO_ATE}
          inputMode="decimal"
          autoComplete="off"
          mascara={mascararMoedaDigitada}
          value={campos.precoMaximo}
          onChange={(evento) => aoMudar('precoMaximo', evento.target.value)}
          erro={erros.precoMaximo}
        />
      </Grupo>
      <Grupo titulo={CATALOGO_ROTULO_ANO}>
        <Campo
          id={`${idBase}-ano-minimo`}
          rotulo={CATALOGO_ROTULO_DE}
          inputMode="numeric"
          autoComplete="off"
          mascara={mascararAno}
          value={campos.anoMinimo}
          onChange={(evento) => aoMudar('anoMinimo', evento.target.value)}
          erro={erros.anoMinimo}
        />
        <Campo
          id={`${idBase}-ano-maximo`}
          rotulo={CATALOGO_ROTULO_ATE}
          inputMode="numeric"
          autoComplete="off"
          mascara={mascararAno}
          value={campos.anoMaximo}
          onChange={(evento) => aoMudar('anoMaximo', evento.target.value)}
          erro={erros.anoMaximo}
        />
      </Grupo>
      <Campo
        id={`${idBase}-km-maximo`}
        rotulo={CATALOGO_ROTULO_KM}
        placeholder={CATALOGO_EXEMPLO_KM}
        inputMode="numeric"
        autoComplete="off"
        mascara={mascararKm}
        value={campos.kmMaximo}
        onChange={(evento) => aoMudar('kmMaximo', evento.target.value)}
        erro={erros.kmMaximo}
      />
      <div className="flex flex-col gap-2">
        <Botao type="submit" larguraTotal icone="filter_alt">
          {CATALOGO_BOTAO_APLICAR}
        </Botao>
        <Botao type="button" variante="fantasma" larguraTotal onClick={aoLimpar}>
          {CATALOGO_BOTAO_LIMPAR}
        </Botao>
      </div>
    </form>
  );
}
