import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter } from 'react-router-dom';
import {
  MENSAGEM_PAINEL_ERRO_CARREGAR,
  MENSAGEM_PAINEL_SEM_PERMISSAO_VALORES,
} from '@/lib/mensagens';
import { ErroApi } from '@/types/api';
import type { RespostaVisaoGeral } from '@/types/painel';
import { AutenticacaoContexto, type AutenticacaoContextoValor } from '@/routes/autenticacao/AutenticacaoContexto';
import { usePainelVisaoGeral } from '@/features/visao-geral/hooks/usePainelVisaoGeral';
import VisaoGeralPagina from './VisaoGeralPagina';

// Nenhum teste chama a API: a consulta vira um dublê, como nas telas de equipe.
vi.mock('@/features/visao-geral/hooks/usePainelVisaoGeral', () => ({
  usePainelVisaoGeral: vi.fn(),
}));

type ConsultaVisao = ReturnType<typeof usePainelVisaoGeral>;

const AUTENTICACAO: AutenticacaoContextoValor = {
  usuario: {
    nome: 'Marina Costa',
    perfil: 'funcionario',
    cargo: 'ADMINISTRADOR',
    empresaNomeFantasia: 'Rota Sul Locadora',
  },
  autenticado: true,
  autenticacaoIgnorada: false,
  entrar: () => undefined,
  sair: () => undefined,
  possuiPerfil: () => true,
};

/** Frota com movimento: tela 39. */
const RESPOSTA_39: RespostaVisaoGeral = {
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
  frotaPorSituacao: [
    { situacao: 'LOCADO', quantidade: 34 },
    { situacao: 'DISPONIVEL', quantidade: 9 },
    { situacao: 'RESERVADO', quantidade: 3 },
    { situacao: 'MANUTENCAO', quantidade: 4 },
    { situacao: 'VENDIDO', quantidade: 1 },
  ],
};

/** Locadora nova, sem veículo: tela 40. */
const RESPOSTA_40: RespostaVisaoGeral = {
  ...RESPOSTA_39,
  periodo: 'TRINTA_DIAS',
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
  variacaoReceitaPercentual: null,
  frotaPorSituacao: [],
};

function consultaCom(parcial: Partial<ConsultaVisao>): ConsultaVisao {
  return {
    data: undefined,
    error: null,
    isPending: false,
    isError: false,
    isFetching: false,
    refetch: vi.fn(),
    ...parcial,
  } as unknown as ConsultaVisao;
}

function renderizar(consulta: ConsultaVisao): string {
  vi.mocked(usePainelVisaoGeral).mockReturnValue(consulta);
  return renderToStaticMarkup(
    <AutenticacaoContexto.Provider value={AUTENTICACAO}>
      <MemoryRouter>
        <VisaoGeralPagina />
      </MemoryRouter>
    </AutenticacaoContexto.Provider>,
  );
}

/** Textos que ficaram de fora desta fatia (desenho 39 e 40): nenhum pode aparecer. */
const FORA_DA_FATIA_39 = [
  'Receita dos últimos 6 meses',
  'Agenda de hoje',
  'Pendências',
  'Veículos mais alugados',
  'Últimas movimentações',
  'Exportar relatório',
  'Relatório completo da frota',
  'Acessar central de tarefas',
  'Ver log de auditoria completo',
  'Buscar por placa',
  'Nova Locação',
  'Notificações',
  'Dados da locadora',
];

const FORA_DA_FATIA_40 = [
  'Complete os dados da locadora',
  'Publique a frota no catálogo',
  'Verificação validada',
  'Tempo estimado restante',
  'Configurar taxas',
  'Baixar modelo CSV',
  'Falar com especialista',
  'Saber mais',
  'Nenhuma retirada ou devolução',
  'Os valores aparecem aqui',
];

describe('VisaoGeralPagina: tela 39 (com dados)', () => {
  beforeEach(() => {
    vi.mocked(usePainelVisaoGeral).mockReset();
  });

  it('mostra o rótulo com a locadora, a saudação com o primeiro nome e a data de hoje', () => {
    const html = renderizar(consultaCom({ data: RESPOSTA_39 }));

    expect(html).toContain('Rota Sul Locadora');
    expect(html).toMatch(/Bom dia, Marina|Boa tarde, Marina|Boa noite, Marina/);
    expect(html).toMatch(/Resumo da operação em \d{2}\/\d{2}\/\d{4}/);
  });

  it('mostra os cinco indicadores e a situação da frota', () => {
    const html = renderizar(consultaCom({ data: RESPOSTA_39 }));

    for (const titulo of ['Receita do período', 'A receber', 'Em atraso', 'Taxa de ocupação', 'Locações ativas']) {
      expect(html).toContain(titulo);
    }
    expect(html).toContain('68%');
    expect(html).toContain('34 de 50 veículos locados');
    expect(html).toContain('5 devoluções previstas para hoje');
    expect(html).toContain('Situação da frota');
    expect(html).toContain('50 veículos');
  });

  it('a legenda da frota traz só a frota em operação, com as quantidades', () => {
    const html = renderizar(consultaCom({ data: RESPOSTA_39 }));

    expect(html).toContain('Locado');
    expect(html).toContain('Disponível');
    expect(html).toContain('Em manutenção');
    expect(html).not.toContain('Baixado');
    expect(html).not.toContain('Vendido');
    expect(html).not.toContain('À venda (seminovos)');
  });

  it('o centro da rosca é o total de veículos da resposta (o mesmo do cartão de ocupação)', () => {
    const html = renderizar(consultaCom({ data: { ...RESPOSTA_39, totalVeiculos: 50 } }));

    expect(html).toContain('34 de 50 veículos locados');
    expect(html).toContain('>50</span>');
  });

  it('a variação aparece quando vem preenchida', () => {
    const html = renderizar(consultaCom({ data: RESPOSTA_39 }));

    expect(html).toContain('+12% em relação ao período anterior');
  });

  it('sem variação, o cartão de receita fica sem essa linha', () => {
    const html = renderizar(consultaCom({ data: { ...RESPOSTA_39, variacaoReceitaPercentual: null } }));

    expect(html).not.toContain('em relação ao período anterior');
  });

  it('o seletor de período mostra as quatro opções e marca os 30 dias como escolhido', () => {
    const html = renderizar(consultaCom({ data: RESPOSTA_39 }));

    expect(html).toContain('Hoje');
    expect(html).toContain('7 dias');
    expect(html).toContain('Este mês');
    expect(html).toMatch(/aria-pressed="true"[^>]*>30 dias<\/button>/);
    expect(html).toMatch(/aria-pressed="false"[^>]*>Hoje<\/button>/);
  });

  it('o botão de cadastrar veículo aponta para o cadastro de veículo', () => {
    const html = renderizar(consultaCom({ data: RESPOSTA_39 }));
    const botao = html.match(/<a[^>]*>\+ Cadastrar veículo<\/a>/)?.[0] ?? '';

    expect(botao).toContain('href="/back-office/frota/veiculos/novo"');
  });

  it('não mostra o que ficou de fora desta fatia', () => {
    const html = renderizar(consultaCom({ data: RESPOSTA_39 }));

    for (const termo of FORA_DA_FATIA_39) {
      expect(html).not.toContain(termo);
    }
  });

  it('não mostra a tela de primeiro acesso', () => {
    const html = renderizar(consultaCom({ data: RESPOSTA_39 }));

    expect(html).not.toContain('Bem-vinda à AutoVia');
    expect(html).not.toContain('Primeiros passos');
  });
});

describe('VisaoGeralPagina: sem permissão de valores', () => {
  beforeEach(() => {
    vi.mocked(usePainelVisaoGeral).mockReset();
  });

  it('os três cartões em reais aparecem sem valor, com o texto de permissão', () => {
    const html = renderizar(
      consultaCom({
        data: {
          ...RESPOSTA_39,
          valoresVisiveis: false,
          receitaPeriodo: null,
          aReceber: null,
          emAtraso: null,
        },
      }),
    );

    const ocorrencias = html.split(MENSAGEM_PAINEL_SEM_PERMISSAO_VALORES).length - 1;
    expect(ocorrencias).toBe(3);
    expect(html).not.toContain('R$');
    expect(html).toContain('68%');
  });
});

describe('VisaoGeralPagina: tela 40 (primeiro acesso)', () => {
  beforeEach(() => {
    vi.mocked(usePainelVisaoGeral).mockReset();
  });

  it('é escolhida quando primeiroAcesso é verdadeiro: boas-vindas com o nome da locadora', () => {
    const html = renderizar(consultaCom({ data: RESPOSTA_40 }));

    expect(html).toContain('Bem-vinda à AutoVia, Rota Sul Locadora');
    expect(html).toContain('Primeiros passos');
    expect(html).not.toContain('Situação da frota');
    expect(html).not.toContain('Resumo da operação em');
  });

  it('mostra os dois passos, com o progresso calculado de 1 de 2', () => {
    const html = renderizar(consultaCom({ data: RESPOSTA_40 }));

    expect(html).toContain('Conta criada');
    expect(html).toContain('Cadastre o primeiro veículo');
    expect(html).toContain('1 de 2 concluído');
    expect(html).toContain('Etapa 2 em andamento');
    expect(html).toContain('Falta 1 passo para começar a operar.');
  });

  it('o passo do veículo tem o botão laranja que abre o cadastro de veículo', () => {
    const html = renderizar(consultaCom({ data: RESPOSTA_40 }));
    const botao = html.match(/<a[^>]*>(?:<[^>]*>)*\s*add\s*(?:<[^>]*>)*\s*Cadastrar veículo<\/a>/)?.[0] ?? '';

    expect(botao).toContain('href="/back-office/frota/veiculos/novo"');
  });

  it('os cinco indicadores aparecem zerados com "Sem dados ainda"', () => {
    const html = renderizar(consultaCom({ data: RESPOSTA_40 }));

    expect(html.split('Sem dados ainda').length - 1).toBe(5);
    expect(html).toContain('0%');
    expect(html).not.toContain('em relação ao período anterior');
  });

  it('não tem o seletor de período nem o botão do cabeçalho', () => {
    const html = renderizar(consultaCom({ data: RESPOSTA_40 }));

    expect(html).not.toContain('aria-pressed');
    expect(html.match(/\+ Cadastrar veículo/g)).toBeNull();
  });

  it('não mostra o que ficou de fora desta fatia', () => {
    const html = renderizar(consultaCom({ data: RESPOSTA_40 }));

    for (const termo of [...FORA_DA_FATIA_40, ...FORA_DA_FATIA_39]) {
      expect(html).not.toContain(termo);
    }
  });
});

describe('VisaoGeralPagina: carregando, erro e troca de período', () => {
  beforeEach(() => {
    vi.mocked(usePainelVisaoGeral).mockReset();
  });

  it('carregando: cabeçalho, esqueletos dos cinco indicadores e da frota; sem números', () => {
    const html = renderizar(consultaCom({ isPending: true, isFetching: true }));

    expect(html).toContain('Carregando o resumo da locadora');
    expect(html).toContain('Receita do período');
    expect(html).toContain('Situação da frota');
    expect(html).toContain('aria-busy="true"');
    expect(html).not.toContain('68%');
  });

  it('erro sem dados: mostra a mensagem da tela e o botão de tentar de novo, sem a técnica', () => {
    const erro = new ErroApi({ status: 500, mensagem: 'Ocorreu um erro inesperado no servidor. Tente novamente em instantes.' });
    const html = renderizar(consultaCom({ isError: true, error: erro }));

    expect(html).toContain(MENSAGEM_PAINEL_ERRO_CARREGAR);
    expect(html).toContain('Tentar novamente');
    expect(html).not.toContain('Situação da frota');
  });

  it('erro durante a troca de período não mostra os números do período anterior', () => {
    const erro = new ErroApi({ status: 0, mensagem: 'Não foi possível conectar ao servidor.' });
    const html = renderizar(consultaCom({ isError: true, error: erro, data: RESPOSTA_39 }));

    expect(html).toContain(MENSAGEM_PAINEL_ERRO_CARREGAR);
    expect(html).not.toContain('68%');
  });

  it('a página mantém o cabeçalho e o seletor quando a consulta de um novo período está em andamento', () => {
    const html = renderizar(consultaCom({ data: RESPOSTA_39, isFetching: true }));

    expect(html).toContain('Resumo da operação em');
    expect(html).toContain('aria-busy="true"');
    expect(html).toContain('68%');
  });
});
