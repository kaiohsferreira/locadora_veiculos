import { isValidElement, type ReactElement, type ReactNode } from 'react';
import { describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import type { AlertaRevisaoRetorno, AlertasRevisaoRetorno } from '@/types/manutencao';
import { CartaoAlertaRevisao } from './CartaoAlertaRevisao';
import { ListaAlertasRevisao } from './ListaAlertasRevisao';

// Os cartões só montam marcação: nenhum clique acontece aqui (o comportamento dos botões é da página).
const ACOES = { aoAbrirOrdem: vi.fn(), aoVerHistorico: vi.fn(), aoFalharFotoCapa: vi.fn() };

function alerta(parcial: Partial<AlertaRevisaoRetorno>): AlertaRevisaoRetorno {
  return {
    idVeiculo: 1,
    placa: 'BRA2E19',
    marca: 'Chevrolet',
    modelo: 'Onix Plus LTZ',
    categoriaNome: 'Grupo B',
    kmAtual: 19820,
    kmUltimaPreventiva: 10000,
    kmProximaRevisao: 20000,
    faltamKm: 180,
    percentualCiclo: 98.2,
    situacao: 'PROXIMA',
    fotoCapa: null,
    erroFotoCapa: null,
    ...parcial,
  };
}

/** Textos que o desenho mostrava e que o P7 cortou: nenhum pode aparecer, nem desligado. */
const TEXTOS_CORTADOS = [
  'Programar',
  'Revisão Nível 3',
  'Revisão Nível 5',
  'Mais Opções',
  'Ver Telemetria',
  'Agendar Revisão',
  'Agendar manutenção',
  'Exportar Relatório',
];

describe('CartaoAlertaRevisao: faixa vermelha', () => {
  const marcado = renderToStaticMarkup(<CartaoAlertaRevisao alerta={alerta({ faltamKm: 180 })} {...ACOES} />);

  it('mostra o selo, o texto de quanto falta e o ciclo limite', () => {
    expect(marcado).toContain('Revisão Iminente');
    expect(marcado).toContain('Faltam apenas 180 km!');
    expect(marcado).toContain('98% do ciclo limite');
    expect(marcado).toContain('Crítico');
  });

  it('tem o botão Bloquear p/ Oficina e o Ver Histórico, e não tem Abrir ordem', () => {
    expect(marcado).toContain('Bloquear p/ Oficina');
    expect(marcado).toContain('title="Ver Histórico"');
    expect(marcado).not.toContain('Abrir ordem');
  });

  it('usa as cores de alerta do tema na barra, no selo e no botão', () => {
    expect(marcado).toContain('bg-manutencao-alerta');
    expect(marcado).toContain('hover:bg-manutencao-alerta-escuro');
    expect(marcado).toContain('ring-manutencao-alerta/20');
  });

  it('não mostra a categoria (o selo ocupa a linha dela, como no desenho)', () => {
    expect(marcado).not.toContain('Categoria Grupo B');
  });

  it('a revisão vencida tem selo próprio e diz quanto passou do limite', () => {
    const vencido = renderToStaticMarkup(
      <CartaoAlertaRevisao alerta={alerta({ situacao: 'VENCIDA', faltamKm: -320, percentualCiclo: 103.2 })} {...ACOES} />,
    );

    expect(vencido).toContain('Revisão vencida');
    expect(vencido).toContain('Passou 320 km do limite');
    expect(vencido).not.toContain('Revisão Iminente');
  });
});

describe('CartaoAlertaRevisao: faixa laranja', () => {
  const marcado = renderToStaticMarkup(
    <CartaoAlertaRevisao alerta={alerta({ faltamKm: 1060, percentualCiclo: 89.4, kmAtual: 38940 })} {...ACOES} />,
  );

  it('mostra categoria, placa formatada, km e o que falta com o ciclo atingido', () => {
    expect(marcado).toContain('Categoria Grupo B');
    expect(marcado).toContain('Faltam 1.060 km');
    expect(marcado).toContain('89% do ciclo atingido');
    expect(marcado).toContain('38.940 km');
  });

  it('o botão é Abrir ordem (ocupa a linha) e não há Bloquear nem Ver Histórico', () => {
    expect(marcado).toContain('Abrir ordem');
    expect(marcado).not.toContain('Bloquear p/ Oficina');
    expect(marcado).not.toContain('Ver Histórico');
  });

  it('não tem o rodapé vermelho (Crítico) nem selo', () => {
    expect(marcado).not.toContain('Crítico');
    expect(marcado).not.toContain('Revisão Iminente');
  });
});

describe('CartaoAlertaRevisao: faixa azul-cinza (neutra)', () => {
  const marcado = renderToStaticMarkup(
    <CartaoAlertaRevisao alerta={alerta({ faltamKm: 1500, percentualCiclo: 85 })} {...ACOES} />,
  );

  it('mostra Faltam X km com o ciclo atingido, sem selo e sem Crítico', () => {
    expect(marcado).toContain('Faltam 1.500 km');
    expect(marcado).toContain('85% do ciclo atingido');
    expect(marcado).not.toContain('Crítico');
  });

  it('usa a cor neutra do tema', () => {
    expect(marcado).toContain('bg-manutencao-neutro');
    expect(marcado).not.toContain('bg-manutencao-alerta');
  });
});

describe('CartaoAlertaRevisao: textos cortados e foto', () => {
  it('nenhuma faixa mostra os textos que o P7 cortou', () => {
    for (const faltam of [180, 1060, 1500]) {
      const marcado = renderToStaticMarkup(<CartaoAlertaRevisao alerta={alerta({ faltamKm: faltam })} {...ACOES} />);
      for (const texto of TEXTOS_CORTADOS) expect(marcado).not.toContain(texto);
    }
  });

  it('usa a capa do veículo na foto, com o nome no texto alternativo', () => {
    const marcado = renderToStaticMarkup(
      <CartaoAlertaRevisao
        alerta={alerta({ fotoCapa: { url: 'https://arquivo.exemplo/capa.jpg', urlMiniatura: null, expiraEm: '2026-11-20T10:00:00Z' } })}
        {...ACOES}
      />,
    );

    expect(marcado).toContain('src="https://arquivo.exemplo/capa.jpg"');
    expect(marcado).toContain('alt="Foto do veículo Chevrolet Onix Plus LTZ"');
  });

  it('sem capa, mostra o ícone de carro no lugar da foto', () => {
    const marcado = renderToStaticMarkup(<CartaoAlertaRevisao alerta={alerta({})} {...ACOES} />);

    expect(marcado).not.toContain('<img');
    expect(marcado).toContain('directions_car');
  });
});

describe('CartaoAlertaRevisao: medidas e raios do desenho (tela 14)', () => {
  const marcado = renderToStaticMarkup(<CartaoAlertaRevisao alerta={alerta({ faltamKm: 180 })} {...ACOES} />);

  it('o bloco dos botões tem mt-2 e pt-4, como o desenho', () => {
    expect(marcado).toContain('mt-2 flex gap-2 pt-4');
    expect(marcado).not.toContain('mt-3');
  });

  it('a placa tem px-1 (o desenho usa px-space-xs)', () => {
    expect(marcado).toContain('rounded-sm bg-steel-alto px-1 py-0.5');
    expect(marcado).not.toContain('px-1.5');
  });

  it('a foto e os botões usam rounded (0,25rem do desenho); a placa e o km usam rounded-sm', () => {
    expect(marcado).not.toContain('rounded-lg');
    expect(marcado).toMatch(/h-28 w-full rounded(?![\w-])/);
    expect(marcado).toContain('rounded bg-manutencao-alerta');
    expect(marcado).toContain('rounded-sm px-2 py-0.5');
  });

  it('não usa font-tabular', () => {
    expect(marcado).not.toContain('font-tabular');
  });
});

describe('ListaAlertasRevisao: estados', () => {
  const dados: AlertasRevisaoRetorno = {
    total: 2,
    intervaloRevisaoKm: 10000,
    itens: [alerta({ idVeiculo: 1, faltamKm: 180 }), alerta({ idVeiculo: 2, placa: 'ONX9A11', faltamKm: 1060, percentualCiclo: 89 })],
  };
  const base = {
    mensagemErro: null,
    tentando: false,
    aoTentarNovamente: vi.fn(),
    ...ACOES,
  };

  it('com dados: título, selo com a contagem, intervalo padrão e os cartões', () => {
    const marcado = renderToStaticMarkup(<ListaAlertasRevisao {...base} carregando={false} alertas={dados} />);

    expect(marcado).toContain('Alertas de Veículos Próximos da Revisão');
    expect(marcado).toContain('2 Requerem Atenção');
    expect(marcado).toContain('Intervalo Padrão: 10.000 km');
    expect(marcado).toContain('Faltam apenas 180 km!');
    expect(marcado).toContain('Faltam 1.060 km');
  });

  it('um só alerta usa o singular no selo', () => {
    const marcado = renderToStaticMarkup(
      <ListaAlertasRevisao {...base} carregando={false} alertas={{ ...dados, total: 1, itens: [dados.itens[0]] }} />,
    );

    expect(marcado).toContain('1 Requer Atenção');
  });

  it('com foto de capa que falhou, mostra o aviso com a mensagem do servidor', () => {
    const marcado = renderToStaticMarkup(
      <ListaAlertasRevisao
        {...base}
        carregando={false}
        alertas={{ ...dados, itens: [alerta({ erroFotoCapa: 'Não foi possível carregar as fotos agora.' })] }}
      />,
    );

    expect(marcado).toContain('Não foi possível carregar as fotos agora.');
  });

  it('carregando: esqueletos e aria-busy, sem cartões', () => {
    const marcado = renderToStaticMarkup(<ListaAlertasRevisao {...base} carregando alertas={undefined} />);

    expect(marcado).toContain('aria-busy="true"');
    expect(marcado).not.toContain('Requerem Atenção');
  });

  it('erro: mensagem do alerta com Tentar de novo, e os cartões somem', () => {
    const marcado = renderToStaticMarkup(
      <ListaAlertasRevisao
        {...base}
        carregando={false}
        alertas={undefined}
        mensagemErro="Não foi possível carregar os alertas de revisão."
      />,
    );

    expect(marcado).toContain('Não foi possível carregar os alertas de revisão.');
    expect(marcado).toContain('Tentar de novo');
    expect(marcado).not.toContain('Faltam');
  });

  it('vazio: a mensagem de nenhum alerta, sem o selo', () => {
    const marcado = renderToStaticMarkup(
      <ListaAlertasRevisao {...base} carregando={false} alertas={{ total: 0, itens: [], intervaloRevisaoKm: 10000 }} />,
    );

    expect(marcado).toContain('Nenhum veículo próximo da revisão.');
    expect(marcado).not.toContain('Requerem Atenção');
    expect(marcado).not.toContain('Requer Atenção');
  });
});

/** Texto visível de um nó do React (sem os elementos de ícone). */
function textoDoNo(no: ReactNode): string {
  if (typeof no === 'string' || typeof no === 'number') return String(no);
  if (Array.isArray(no)) return no.map(textoDoNo).join('');
  if (isValidElement<{ children?: ReactNode }>(no)) return textoDoNo(no.props.children);
  return '';
}

/** Primeiro botão da árvore cujo texto contém o rótulo. */
function acharBotao(no: ReactNode, rotulo: string): ReactElement<{ onClick: () => void }> | undefined {
  if (Array.isArray(no)) {
    for (const item of no) {
      const achado = acharBotao(item, rotulo);
      if (achado) return achado;
    }
    return undefined;
  }
  if (!isValidElement<{ children?: ReactNode; onClick: () => void }>(no)) return undefined;
  if (no.type === 'button' && textoDoNo(no.props.children).includes(rotulo)) {
    return no as ReactElement<{ onClick: () => void }>;
  }
  return acharBotao(no.props.children, rotulo);
}

describe('CartaoAlertaRevisao: o que o botão manda para o formulário de ordem', () => {
  it('Bloquear p/ Oficina (cartão vermelho) pede a ordem com o tipo Preventiva', () => {
    const aoAbrirOrdem = vi.fn();
    const arvore = CartaoAlertaRevisao({
      alerta: alerta({ idVeiculo: 9, faltamKm: 180 }),
      aoAbrirOrdem,
      aoVerHistorico: vi.fn(),
      aoFalharFotoCapa: vi.fn(),
    });
    acharBotao(arvore, 'Bloquear p/ Oficina')?.props.onClick();
    expect(aoAbrirOrdem).toHaveBeenCalledWith(9, 'PREVENTIVA');
  });

  it('Abrir ordem (cartão laranja) pede a ordem só com o veículo, sem tipo', () => {
    const aoAbrirOrdem = vi.fn();
    const arvore = CartaoAlertaRevisao({
      alerta: alerta({ idVeiculo: 9, faltamKm: 1060, percentualCiclo: 89.4 }),
      aoAbrirOrdem,
      aoVerHistorico: vi.fn(),
      aoFalharFotoCapa: vi.fn(),
    });
    acharBotao(arvore, 'Abrir ordem')?.props.onClick();
    expect(aoAbrirOrdem).toHaveBeenCalledTimes(1);
    expect(aoAbrirOrdem.mock.calls[0]).toHaveLength(1);
    expect(aoAbrirOrdem.mock.calls[0][0]).toBe(9);
  });
});
