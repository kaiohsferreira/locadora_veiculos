import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import {
  CartaoLocacaoAtiva,
  CLASSE_CAIXA_FOTO_GRANDE,
} from '@/features/area-cliente/locacoes/componentes/CartaoLocacaoAtiva';
import { LOCACAO_ATIVA } from '@/features/area-cliente/locacoes/fixturasLocacoes';
import type { LocacaoCliente } from '@/types/locacoesCliente';

/** Todos os campos anuláveis do contrato locacao-cliente v3 em null. */
const COM_ANULAVEIS_NULOS: LocacaoCliente = {
  ...LOCACAO_ATIVA,
  codigoReserva: null,
  dataDevolucaoEfetiva: null,
  dataDevolucaoEfetivaStr: null,
  valorTotal: null,
  valorTotalStr: null,
  cor: null,
  fotoCapa: null,
  erroFotoCapa: null,
  toleranciaAtrasoMinutos: null,
  combustivelRetirada: null,
  combustivelRetiradaStr: null,
};

/** Os mesmos campos, mas com as chaves ausentes na resposta (undefined em tempo de execução). */
function comChavesAusentes(): LocacaoCliente {
  const parcial: Partial<LocacaoCliente> = { ...COM_ANULAVEIS_NULOS };
  delete parcial.toleranciaAtrasoMinutos;
  delete parcial.combustivelRetirada;
  delete parcial.combustivelRetiradaStr;
  delete parcial.fotoCapa;
  delete parcial.erroFotoCapa;
  delete parcial.codigoReserva;
  return parcial as LocacaoCliente;
}

function renderizar(locacao: LocacaoCliente): string {
  return renderToStaticMarkup(<CartaoLocacaoAtiva locacao={locacao} />);
}

describe('CartaoLocacaoAtiva (marcação, tela 27)', () => {
  it('bloco grande da foto: altura limitada de md a xl, com a foto inteira (contain); a partir de xl volta ao corte', () => {
    expect(CLASSE_CAIXA_FOTO_GRANDE).toContain('md:max-h-80');
    expect(CLASSE_CAIXA_FOTO_GRANDE).toContain('xl:max-h-none');
    expect(CLASSE_CAIXA_FOTO_GRANDE).toContain('md:[&_img]:!object-contain');
    expect(CLASSE_CAIXA_FOTO_GRANDE).toContain('xl:[&_img]:!object-cover');
    expect(CLASSE_CAIXA_FOTO_GRANDE).toContain('aspect-[16/10]');
  });

  it('a caixa da foto no HTML traz o limite de altura e o bloco segue escondido no celular', () => {
    const html = renderizar(LOCACAO_ATIVA);
    expect(html).toContain('relative hidden aspect-[16/10] w-full overflow-hidden rounded-xl bg-steel-fundo p-2 md:block md:max-h-80');
    expect(html).toContain('xl:max-h-none');
  });

  it('com todos os anuláveis em null: sem NaN, undefined ou null na tela, e o texto de "não informado" no combustível', () => {
    const html = renderizar(COM_ANULAVEIS_NULOS);
    expect(html).not.toContain('NaN');
    expect(html).not.toContain('undefined');
    expect(html).not.toContain('>null<');
    expect(html).toContain('Não informado');
    expect(html).not.toContain('Tolerância');
    expect(html).toContain('Dentro do prazo');
  });

  it('com as chaves anuláveis ausentes (undefined): mesma tela, sem NaN, undefined nem "min" solto', () => {
    const html = renderizar(comChavesAusentes());
    expect(html).not.toContain('NaN');
    expect(html).not.toContain('undefined');
    expect(html).not.toContain('undefined min');
    expect(html).not.toContain('Tolerância');
    expect(html).toContain('Não informado');
    expect(html).toContain('Dentro do prazo');
  });
});
