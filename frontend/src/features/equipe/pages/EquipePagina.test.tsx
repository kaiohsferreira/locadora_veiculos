import { describe, expect, it, vi, beforeEach } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { AvisoProvider } from '@/components/ui';
import {
  MENSAGEM_EQUIPE_ERRO_LISTA,
  MENSAGEM_EQUIPE_VAZIA_COM_FILTRO,
  MENSAGEM_FALHA_CONEXAO as MENSAGEM_EQUIPE_FALHA_TESTE,
} from '@/lib/mensagens';
import { ErroApi } from '@/types/api';
import type { RespostaEquipe } from '@/types/equipe';
import { useAlterarSituacaoFuncionario } from '@/features/equipe/hooks/useAlterarSituacaoFuncionario';
import { useEquipe } from '@/features/equipe/hooks/useEquipe';
import { AutenticacaoContexto, type AutenticacaoContextoValor } from '@/routes/autenticacao/AutenticacaoContexto';
import EquipePagina from './EquipePagina';

// Nenhum teste chama a API: o cliente HTTP vira dublê (ele lê o navegador ao carregar, que não existe aqui).
vi.mock('@/api/client', () => ({
  api: { get: vi.fn(), post: vi.fn(), put: vi.fn(), patch: vi.fn() },
}));
// A página só lê os hooks de consulta e de situação.
vi.mock('@/features/equipe/hooks/useEquipe', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@/features/equipe/hooks/useEquipe')>()),
  useEquipe: vi.fn(),
}));
vi.mock('@/features/equipe/hooks/useAlterarSituacaoFuncionario', () => ({
  useAlterarSituacaoFuncionario: vi.fn(),
}));

type ConsultaEquipe = ReturnType<typeof useEquipe>;

const RESPOSTA: RespostaEquipe = {
  itens: [
    {
      idFuncionario: 1,
      nome: 'Marina Costa',
      cpfMascarado: '341.***.***-12',
      matricula: 'MAT-0001',
      email: 'marina@rotasul.com.br',
      cargo: 'ADMINISTRADOR',
      ativo: true,
      dataAdmissao: '2024-10-01',
      dataAdmissaoStr: '01/10/2024',
      ehVoce: true,
    },
    {
      idFuncionario: 2,
      nome: 'Carlos Eduardo Silveira',
      cpfMascarado: '892.***.***-45',
      matricula: 'MAT-0102',
      email: 'carlos.silveira@rotasul.com.br',
      cargo: 'ATENDENTE',
      ativo: true,
      dataAdmissao: '2024-10-15',
      dataAdmissaoStr: '15/10/2024',
      ehVoce: false,
    },
    {
      idFuncionario: 3,
      nome: 'Luciano Ramos',
      cpfMascarado: '215.***.***-74',
      matricula: 'MAT-0089',
      email: 'luciano.ramos@rotasul.com.br',
      cargo: 'VENDEDOR',
      ativo: false,
      dataAdmissao: '2023-06-18',
      dataAdmissaoStr: '18/06/2023',
      ehVoce: false,
    },
  ],
  total: 3,
  pagina: 1,
  tamanhoPagina: 20,
  totalAtivos: 2,
  totalInativos: 1,
  totalAdministradores: 1,
  totalCadastrados: 3,
};

const AUTENTICACAO: AutenticacaoContextoValor = {
  usuario: { nome: 'Marina Costa', perfil: 'funcionario', cargo: 'ADMINISTRADOR', empresaNomeFantasia: 'Rota Sul Locadora' },
  autenticado: true,
  autenticacaoIgnorada: false,
  entrar: () => undefined,
  sair: () => undefined,
  possuiPerfil: () => true,
};

function consultaCom(parcial: Partial<ConsultaEquipe>): ConsultaEquipe {
  return {
    data: undefined,
    error: null,
    isPending: false,
    isError: false,
    isFetching: false,
    refetch: vi.fn(),
    ...parcial,
  } as unknown as ConsultaEquipe;
}

/** Renderiza a página com a consulta dada e uma situação de mutação em repouso. */
function renderizar(consulta: ConsultaEquipe): string {
  vi.mocked(useEquipe).mockReturnValue(consulta);
  vi.mocked(useAlterarSituacaoFuncionario).mockReturnValue({
    mutate: vi.fn(),
    isPending: false,
    variables: undefined,
  } as unknown as ReturnType<typeof useAlterarSituacaoFuncionario>);
  return renderToStaticMarkup(
    <AutenticacaoContexto.Provider value={AUTENTICACAO}>
      <AvisoProvider>
        <EquipePagina />
      </AvisoProvider>
    </AutenticacaoContexto.Provider>,
  );
}

describe('EquipePagina (tela 41)', () => {
  beforeEach(() => {
    vi.mocked(useEquipe).mockReset();
    vi.mocked(useAlterarSituacaoFuncionario).mockReset();
  });

  it('carregando: mostra cabeçalho, indicadores, filtros e a tabela com a legenda', () => {
    const html = renderizar(consultaCom({ isPending: true }));

    expect(html).toContain('Equipe');
    expect(html).toContain('Adicionar funcionário');
    expect(html).toContain('Funcionários ativos');
    expect(html).toContain('Administradores');
    expect(html).toContain('Funcionários da locadora');
    expect(html).toContain('aria-busy="true"');
  });

  it('com dados: mostra os três indicadores com os totais e a linha "Você"', () => {
    const html = renderizar(consultaCom({ data: RESPOSTA }));

    expect(html).toContain('de 3 cadastrados');
    expect(html).toContain('acesso revogado');
    expect(html).toContain('gestor master');
    expect(html).toContain('Marina Costa');
    expect(html).toContain('>Você<');
    expect(html).toContain('341.***.***-12');
    expect(html).toContain('MAT-0001');
    expect(html).toContain('Administrador');
    expect(html).toContain('Reativar acesso');
  });

  it('a linha do próprio usuário não tem Desativar; outro ativo tem exatamente uma vez', () => {
    const html = renderizar(consultaCom({ data: RESPOSTA }));

    expect(html.match(/title="Desativar"/g)?.length).toBe(1);
    expect(html.match(/title="Editar"/g)?.length).toBe(3);
  });

  it('mostra o rodapé de paginação com o resumo dos funcionários', () => {
    const html = renderizar(consultaCom({ data: RESPOSTA }));

    expect(html).toContain('Paginação');
    expect(html).toContain('funcionários');
  });

  it('erro da lista: mensagem da tela e botão de tentar de novo, sem a mensagem técnica', () => {
    const erro = new ErroApi({ status: 0, mensagem: MENSAGEM_EQUIPE_FALHA_TESTE });
    const html = renderizar(consultaCom({ isError: true, error: erro }));

    expect(html).toContain(MENSAGEM_EQUIPE_ERRO_LISTA);
    expect(html).toContain(MENSAGEM_EQUIPE_FALHA_TESTE);
    expect(html).toContain('Tentar novamente');
    expect(html).not.toContain('<table');
  });

  it('lista vazia com o filtro padrão (Ativos): mensagem de vazio com filtro e Limpar filtros', () => {
    const vazia: RespostaEquipe = { ...RESPOSTA, itens: [], total: 0 };
    const html = renderizar(consultaCom({ data: vazia }));

    expect(html).toContain(MENSAGEM_EQUIPE_VAZIA_COM_FILTRO);
    expect(html).toContain('Limpar filtros');
  });

  it('não existe convite em nenhum estado da tela', () => {
    const estados = [
      consultaCom({ isPending: true }),
      consultaCom({ data: RESPOSTA }),
      consultaCom({ isError: true, error: new ErroApi({ status: 0, mensagem: MENSAGEM_EQUIPE_FALHA_TESTE }) }),
      consultaCom({ data: { ...RESPOSTA, itens: [], total: 0 } }),
    ];
    for (const consulta of estados) {
      const html = renderizar(consulta);
      expect(html.toLowerCase()).not.toContain('convite');
      expect(html).not.toContain('Enviar');
      expect(html).not.toContain('Reenviar');
      expect(html).not.toContain('Excluir convite');
    }
  });

  it('a busca e os filtros existem com os rótulos do desenho', () => {
    const html = renderizar(consultaCom({ data: RESPOSTA }));

    expect(html).toContain('Buscar por nome, CPF ou matrícula...');
    expect(html).toContain('Todos os cargos');
    expect(html).toContain('Situação dos funcionários');
    expect(html).toContain('>Todos<');
    expect(html).toContain('>Ativos<');
    expect(html).toContain('>Inativos<');
  });
});
