import { describe, expect, it } from 'vitest';
import { formatarMoeda } from '@/lib/formatters';
import {
  MENSAGEM_PAINEL_SEM_DADOS,
  MENSAGEM_PAINEL_SEM_PERMISSAO_VALORES,
} from '@/lib/mensagens';
import type { RespostaVisaoGeral } from '@/types/painel';
import { classificarTamanhoValor, montarCartoesIndicadores } from './indicadores';

const BASE: RespostaVisaoGeral = {
  periodo: 'TRINTA_DIAS',
  periodoInicioStr: '14/10/2026',
  periodoFimStr: '13/11/2026',
  primeiroAcesso: false,
  valoresVisiveis: true,
  receitaPeriodo: 48320,
  receitaPeriodoStr: 'R$ 48.320,00',
  variacaoReceitaPercentual: 12,
  aReceber: 9870,
  aReceberStr: 'R$ 9.870,00',
  quantidadeCobrancasEmAberto: 14,
  emAtraso: 2150,
  emAtrasoStr: 'R$ 2.150,00',
  quantidadeCobrancasVencidas: 3,
  taxaOcupacaoPercentual: 68,
  veiculosLocados: 34,
  totalVeiculos: 50,
  locacoesAtivas: 34,
  devolucoesPrevistasHoje: 5,
  frotaPorSituacao: [{ situacao: 'LOCADO', quantidade: 34 }],
};

/** Resposta sem permissão de valores: os campos em reais saem nulos, as quantidades continuam. */
const SEM_VALORES: RespostaVisaoGeral = {
  ...BASE,
  valoresVisiveis: false,
  receitaPeriodo: null,
  receitaPeriodoStr: '',
  aReceber: null,
  aReceberStr: '',
  emAtraso: null,
  emAtrasoStr: '',
};

function cartao(cartoes: ReturnType<typeof montarCartoesIndicadores>, chave: string) {
  const achado = cartoes.find((item) => item.chave === chave);
  if (!achado) throw new Error(`cartão ${chave} não encontrado`);
  return achado;
}

describe('montarCartoesIndicadores: sentido da variação da receita (seta e cor)', () => {
  it('alta: texto com sinal de mais e sentido alta', () => {
    const receita = cartao(montarCartoesIndicadores({ ...BASE, variacaoReceitaPercentual: 12 }), 'receita');

    expect(receita.variacao).toBe('+12% em relação ao período anterior');
    expect(receita.sentidoVariacao).toBe('alta');
  });

  it('queda: sinal de menos, uma casa decimal e sentido queda', () => {
    const receita = cartao(montarCartoesIndicadores({ ...BASE, variacaoReceitaPercentual: -3.5 }), 'receita');

    expect(receita.variacao).toBe('-3,5% em relação ao período anterior');
    expect(receita.sentidoVariacao).toBe('queda');
  });

  it('zero e valor que arredonda para zero: neutra, sem sinal', () => {
    const zero = cartao(montarCartoesIndicadores({ ...BASE, variacaoReceitaPercentual: 0 }), 'receita');
    const pequena = cartao(montarCartoesIndicadores({ ...BASE, variacaoReceitaPercentual: -0.04 }), 'receita');

    expect(zero.variacao).toBe('0% em relação ao período anterior');
    expect(zero.sentidoVariacao).toBe('neutra');
    expect(pequena.variacao).toBe('0% em relação ao período anterior');
    expect(pequena.sentidoVariacao).toBe('neutra');
  });

  it('sem variação (nula ou primeiro acesso), o sentido também é nulo', () => {
    expect(cartao(montarCartoesIndicadores({ ...BASE, variacaoReceitaPercentual: null }), 'receita').sentidoVariacao).toBeNull();
    expect(
      cartao(montarCartoesIndicadores(BASE, { primeiroAcesso: true }), 'receita').sentidoVariacao,
    ).toBeNull();
  });

  it('os demais cartões nunca têm sentido de variação', () => {
    const cartoes = montarCartoesIndicadores({ ...BASE, variacaoReceitaPercentual: -3 });

    expect(cartoes.filter((item) => item.chave !== 'receita').every((item) => item.sentidoVariacao === null)).toBe(true);
  });
});

describe('montarCartoesIndicadores: concordância do texto de ocupação', () => {
  it('um veículo e um locado: singular nos dois', () => {
    const ocupacao = cartao(montarCartoesIndicadores({ ...BASE, veiculosLocados: 1, totalVeiculos: 1 }), 'ocupacao');
    expect(ocupacao.detalhe).toBe('1 de 1 veículo locado');
  });

  it('nenhum locado de um veículo: "0 de 1 veículo locado"', () => {
    const ocupacao = cartao(montarCartoesIndicadores({ ...BASE, veiculosLocados: 0, totalVeiculos: 1 }), 'ocupacao');
    expect(ocupacao.detalhe).toBe('0 de 1 veículo locado');
  });

  it('dois de cinco: plural nos dois', () => {
    const ocupacao = cartao(montarCartoesIndicadores({ ...BASE, veiculosLocados: 2, totalVeiculos: 5 }), 'ocupacao');
    expect(ocupacao.detalhe).toBe('2 de 5 veículos locados');
  });

  it('um locado de cinco: o adjetivo concorda com o total, não com o locado', () => {
    const ocupacao = cartao(montarCartoesIndicadores({ ...BASE, veiculosLocados: 1, totalVeiculos: 5 }), 'ocupacao');
    expect(ocupacao.detalhe).toBe('1 de 5 veículos locados');
  });
});

describe('montarCartoesIndicadores: ordem e conteúdo', () => {
  it('traz os cinco cartões na ordem do desenho', () => {
    const cartoes = montarCartoesIndicadores(BASE);

    expect(cartoes.map((item) => item.chave)).toEqual(['receita', 'aReceber', 'emAtraso', 'ocupacao', 'locacoes']);
  });

  it('formata os valores em reais com o formatador do projeto', () => {
    const cartoes = montarCartoesIndicadores(BASE);

    expect(cartao(cartoes, 'receita').valor).toBe(formatarMoeda(48320));
    expect(cartao(cartoes, 'aReceber').valor).toBe(formatarMoeda(9870));
    expect(cartao(cartoes, 'emAtraso').valor).toBe(formatarMoeda(2150));
  });

  it('a taxa de ocupação vira percentual e o detalhe mostra locados sobre o total', () => {
    const ocupacao = cartao(montarCartoesIndicadores(BASE), 'ocupacao');

    expect(ocupacao.valor).toBe('68%');
    expect(ocupacao.detalhe).toBe('34 de 50 veículos locados');
    expect(ocupacao.monetario).toBe(false);
  });

  it('locações ativas mostra a quantidade e as devoluções previstas para hoje', () => {
    const locacoes = cartao(montarCartoesIndicadores(BASE), 'locacoes');

    expect(locacoes.valor).toBe('34');
    expect(locacoes.detalhe).toBe('5 devoluções previstas para hoje');
    expect(locacoes.tomDetalhe).toBe('destaque');
  });

  it('cobranças usam o plural certo no a receber e no atraso', () => {
    const cartoes = montarCartoesIndicadores(BASE);

    expect(cartao(cartoes, 'aReceber').detalhe).toBe('14 cobranças em aberto');
    expect(cartao(cartoes, 'emAtraso').detalhe).toBe('3 cobranças vencidas');
  });

  it('singular quando há uma cobrança, uma locação e um veículo', () => {
    const cartoes = montarCartoesIndicadores({
      ...BASE,
      quantidadeCobrancasEmAberto: 1,
      quantidadeCobrancasVencidas: 1,
      devolucoesPrevistasHoje: 1,
      veiculosLocados: 1,
      totalVeiculos: 1,
    });

    expect(cartao(cartoes, 'aReceber').detalhe).toBe('1 cobrança em aberto');
    expect(cartao(cartoes, 'emAtraso').detalhe).toBe('1 cobrança vencida');
    expect(cartao(cartoes, 'locacoes').detalhe).toBe('1 devolução prevista para hoje');
    expect(cartao(cartoes, 'ocupacao').detalhe).toBe('1 de 1 veículo locado');
  });

  it('em atraso é destacado em vermelho; os demais não', () => {
    const cartoes = montarCartoesIndicadores(BASE);

    expect(cartao(cartoes, 'emAtraso').destacarValor).toBe(true);
    expect(cartao(cartoes, 'receita').destacarValor).toBe(false);
  });
});

describe('montarCartoesIndicadores: variação da receita', () => {
  it('mostra a variação com sinal quando ela vem preenchida', () => {
    const receita = cartao(montarCartoesIndicadores(BASE), 'receita');

    expect(receita.variacao).toBe('+12% em relação ao período anterior');
  });

  it('mostra variação negativa com sinal de menos', () => {
    const receita = cartao(montarCartoesIndicadores({ ...BASE, variacaoReceitaPercentual: -3 }), 'receita');

    expect(receita.variacao).toBe('-3% em relação ao período anterior');
  });

  it('sem a variação (nula ou ausente) o cartão fica sem essa linha', () => {
    expect(cartao(montarCartoesIndicadores({ ...BASE, variacaoReceitaPercentual: null }), 'receita').variacao).toBeNull();

    const semCampo: RespostaVisaoGeral = { ...BASE };
    delete semCampo.variacaoReceitaPercentual;
    expect(cartao(montarCartoesIndicadores(semCampo), 'receita').variacao).toBeNull();
  });
});

describe('montarCartoesIndicadores: sem permissão de valores', () => {
  it('os três cartões em reais não trazem número e mostram o texto de permissão', () => {
    const cartoes = montarCartoesIndicadores(SEM_VALORES);

    for (const chave of ['receita', 'aReceber', 'emAtraso']) {
      const item = cartao(cartoes, chave);
      expect(item.valor).toBeNull();
      expect(item.semValor).toBe(MENSAGEM_PAINEL_SEM_PERMISSAO_VALORES);
    }
  });

  it('as quantidades continuam aparecendo sem permissão', () => {
    const cartoes = montarCartoesIndicadores(SEM_VALORES);

    expect(cartao(cartoes, 'ocupacao').valor).toBe('68%');
    expect(cartao(cartoes, 'locacoes').valor).toBe('34');
    expect(cartao(cartoes, 'aReceber').detalhe).toBe('14 cobranças em aberto');
  });

  it('respeita o campo: com valoresVisiveis falso, não mostra um número mesmo que ele venha preenchido', () => {
    const cartoes = montarCartoesIndicadores({ ...BASE, valoresVisiveis: false });

    expect(cartao(cartoes, 'receita').valor).toBeNull();
    expect(cartao(cartoes, 'emAtraso').valor).toBeNull();
  });
});

describe('montarCartoesIndicadores: primeiro acesso (tela 40)', () => {
  it('todos os cartões dizem "Sem dados ainda" e não mostram variação', () => {
    const cartoes = montarCartoesIndicadores({ ...BASE, primeiroAcesso: true, totalVeiculos: 0 }, { primeiroAcesso: true });

    expect(cartoes).toHaveLength(5);
    for (const item of cartoes) {
      expect(item.detalhe).toBe(MENSAGEM_PAINEL_SEM_DADOS);
    }
    expect(cartao(cartoes, 'receita').variacao).toBeNull();
  });

  it('com valores zerados, os cartões em reais mostram zero formatado', () => {
    const zerada: RespostaVisaoGeral = {
      ...BASE,
      primeiroAcesso: true,
      receitaPeriodo: 0,
      aReceber: 0,
      emAtraso: 0,
      taxaOcupacaoPercentual: 0,
      veiculosLocados: 0,
      totalVeiculos: 0,
      locacoesAtivas: 0,
      devolucoesPrevistasHoje: 0,
      quantidadeCobrancasEmAberto: 0,
      quantidadeCobrancasVencidas: 0,
    };
    const cartoes = montarCartoesIndicadores(zerada, { primeiroAcesso: true });

    expect(cartao(cartoes, 'receita').valor).toBe(formatarMoeda(0));
    expect(cartao(cartoes, 'ocupacao').valor).toBe('0%');
    expect(cartao(cartoes, 'locacoes').valor).toBe('0');
  });
});

describe('montarCartoesIndicadores: valor muito grande', () => {
  it('um valor de 9 dígitos antes da vírgula tem o menor tamanho de texto (cabe no cartão)', () => {
    const cartoes = montarCartoesIndicadores({ ...BASE, receitaPeriodo: 999_999_999.99 });
    const receita = cartao(cartoes, 'receita');

    expect(receita.valor).toBe(formatarMoeda(999_999_999.99));
    expect(receita.tamanhoValor).toBe('minimo');
  });

  it('valores pequenos usam o tamanho maior', () => {
    expect(cartao(montarCartoesIndicadores(BASE), 'aReceber').tamanhoValor).toBe('medio');
  });

  it('quantidades e percentuais usam o tamanho de título', () => {
    const cartoes = montarCartoesIndicadores(BASE);

    expect(cartao(cartoes, 'ocupacao').tamanhoValor).toBe('grande');
    expect(cartao(cartoes, 'locacoes').tamanhoValor).toBe('grande');
  });
});

describe('classificarTamanhoValor', () => {
  it('até 8 dígitos (incluindo centavos) cabe no tamanho médio', () => {
    expect(classificarTamanhoValor('R$ 9.870,00')).toBe('medio');
    expect(classificarTamanhoValor('R$ 999.999,99')).toBe('medio');
  });

  it('9 e 10 dígitos reduzem para o tamanho pequeno', () => {
    expect(classificarTamanhoValor('R$ 1.000.000,00')).toBe('pequeno');
    expect(classificarTamanhoValor('R$ 12.345.678,90')).toBe('pequeno');
  });

  it('11 dígitos (9 antes da vírgula e 2 depois) vão para o menor tamanho', () => {
    expect(classificarTamanhoValor('R$ 999.999.999,99')).toBe('minimo');
  });

  it('texto sem dígitos (valor ausente) fica no médio', () => {
    expect(classificarTamanhoValor('—')).toBe('medio');
  });
});

describe('montarCartoesIndicadores: taxa de ocupação', () => {
  it('zero veículos mostra 0% e zero de zero', () => {
    const ocupacao = cartao(
      montarCartoesIndicadores({ ...BASE, taxaOcupacaoPercentual: 0, veiculosLocados: 0, totalVeiculos: 0 }),
      'ocupacao',
    );

    expect(ocupacao.valor).toBe('0%');
    expect(ocupacao.detalhe).toBe('0 de 0 veículos locados');
  });
});
