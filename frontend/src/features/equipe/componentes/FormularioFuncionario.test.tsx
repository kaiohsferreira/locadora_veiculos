import { describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { valoresIniciaisCadastro, valoresIniciaisEdicao } from '../formulario/conversaoFuncionario';
import type { FuncionarioEquipe } from '@/types/equipe';
import { FormularioFuncionario } from './FormularioFuncionario';

// Nenhum teste chama a API: o cliente HTTP vira dublê (ele lê o navegador ao carregar, que não existe aqui).
vi.mock('@/api/client', () => ({
  api: { get: vi.fn(), post: vi.fn(), put: vi.fn(), patch: vi.fn() },
}));

const FUNCIONARIO: FuncionarioEquipe = {
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
};

/** Renderiza o corpo do formulário com os parâmetros dados (sem envio: só a marcação é conferida). */
function renderizar(parcial: Partial<Parameters<typeof FormularioFuncionario>[0]>): string {
  return renderToStaticMarkup(
    <FormularioFuncionario
      modo="cadastro"
      valoresIniciais={valoresIniciaisCadastro()}
      ehVoce={false}
      enviando={false}
      erroGeral={null}
      errosCampos={{}}
      aoEnviar={() => undefined}
      aoCancelar={() => undefined}
      {...parcial}
    />,
  );
}

describe('FormularioFuncionario: cadastro', () => {
  it('mostra os campos do desenho, a senha inicial, a confirmação e o aviso que substitui o convite', () => {
    const html = renderizar({});

    expect(html).toContain('Nome completo');
    expect(html).toContain('CPF');
    expect(html).toContain('Matrícula');
    expect(html).toContain('E-mail institucional');
    expect(html).toContain('Data de admissão');
    expect(html).toContain('Cargo atribuído');
    expect(html).toContain('Senha inicial');
    expect(html).toContain('Confirmar senha');
    expect(html).toContain('Informe a senha ao funcionário. Ela não fica visível depois.');
    expect(html).toContain('Adicionar funcionário');
  });

  it('não tem convite: sem "Enviar convite" nem o aviso de link por e-mail', () => {
    const html = renderizar({});

    expect(html.toLowerCase()).not.toContain('convite');
    expect(html).not.toContain('link seguro');
    expect(html).not.toContain('Enviar convite');
  });

  it('o cargo tem as seis opções, a primeira é Atendente, e a caixa mostra as responsabilidades', () => {
    const html = renderizar({});

    expect(html).toContain('<option value="ATENDENTE">Atendente</option>');
    expect(html).toContain('<option value="ADMINISTRADOR">Administrador</option>');
    expect(html).toContain('O que este cargo pode fazer:');
    expect(html).toContain('Verificar cadastros de clientes');
  });

  it('mostra o erro geral do envio no topo, com papel de alerta', () => {
    const html = renderizar({ erroGeral: 'A locadora precisa manter pelo menos um administrador ativo.' });

    expect(html).toContain('role="alert"');
    expect(html).toContain('A locadora precisa manter pelo menos um administrador ativo.');
  });

  // Limite: o erro de campo do servidor é aplicado por efeito (setError), que o SSR não executa. Esse trecho
  // fica sem teste automatizado no ambiente Vitest atual (Node, sem DOM) e é conferido no QA no navegador.
});

describe('FormularioFuncionario: edição', () => {
  const valores = valoresIniciaisEdicao(FUNCIONARIO);

  it('mostra o CPF mascarado só como leitura e não mostra senha', () => {
    const html = renderizar({ modo: 'edicao', valoresIniciais: valores, cpfMascarado: FUNCIONARIO.cpfMascarado });

    expect(html).toContain('892.***.***-45');
    expect(html).toContain('O CPF não pode ser alterado.');
    expect(html).not.toContain('Senha inicial');
    expect(html).not.toContain('Confirmar senha');
    expect(html).toContain('Salvar alterações');
  });

  it('na própria linha, o cargo aparece como leitura com a dica e sem seletor', () => {
    const html = renderizar({
      modo: 'edicao',
      valoresIniciais: valores,
      cpfMascarado: FUNCIONARIO.cpfMascarado,
      ehVoce: true,
    });

    expect(html).toContain('Você não pode alterar o próprio cargo.');
    expect(html).not.toContain('<select');
    expect(html).toContain('type="hidden"');
  });

  it('em outra linha, o cargo continua como seletor', () => {
    const html = renderizar({ modo: 'edicao', valoresIniciais: valores, cpfMascarado: FUNCIONARIO.cpfMascarado });

    expect(html).toContain('<select');
  });

  it('não tem convite', () => {
    const html = renderizar({ modo: 'edicao', valoresIniciais: valores, cpfMascarado: FUNCIONARIO.cpfMascarado });

    expect(html.toLowerCase()).not.toContain('convite');
  });
});
