import { describe, expect, it } from 'vitest';
import {
  MENSAGEM_ERRO_API_PADRAO,
  MENSAGEM_LOCADORA_CADASTRO_INDISPONIVEL,
  MENSAGEM_LOCADORA_CNPJ_INVALIDO,
  MENSAGEM_LOCADORA_CNPJ_JA_CADASTRADO,
  MENSAGEM_LOCADORA_EMAIL_JA_CADASTRADO,
  MENSAGEM_LOCADORA_FALHA_ENVIO,
  MENSAGEM_LOCADORA_LIMITE_TENTATIVAS,
  MENSAGEM_LOCADORA_RESPONSAVEL_CPF_JA_CADASTRADO,
} from '@/lib/mensagens';
import { ErroApi } from '@/types/api';
import type { RequisicaoCadastroLocadora } from '@/types/empresas';
import {
  camposEmConflitoLocadora,
  conflitosDoEnvio,
  errosDaEtapaLocadora,
  interpretarErroCadastroLocadora,
} from './mapeamentoErroLocadora';

function erroApi(status: number, mensagem: string, erros?: Record<string, string[]>): ErroApi {
  return new ErroApi({ status, mensagem, erros });
}

const REQUISICAO: RequisicaoCadastroLocadora = {
  razaoSocial: 'Rota Sul Locadora de Veículos Ltda',
  nomeFantasia: 'Rota Sul Locadora',
  cnpj: '11222333000181',
  telefone: '5133429000',
  email: 'contato@rotasul.com.br',
  cep: '90200310',
  logradouro: 'Av. Severo Dullius',
  numero: '1240',
  complemento: null,
  bairro: 'São João',
  cidade: 'Porto Alegre',
  uf: 'RS',
  responsavelNome: 'Marina Costa',
  responsavelCpf: '52998224725',
  responsavelEmail: 'marina@rotasul.com.br',
  senha: 'Rotasul@2025!',
  aceiteTermos: true,
};

describe('interpretarErroCadastroLocadora: 409 (conflito) aponta o campo certo', () => {
  it('CNPJ já cadastrado volta para a etapa 1, no campo do CNPJ', () => {
    const resultado = interpretarErroCadastroLocadora(
      erroApi(409, 'Já existe uma locadora cadastrada com este CNPJ.'),
    );
    expect(resultado.erros).toEqual([{ etapa: 1, campo: 'cnpj', mensagem: MENSAGEM_LOCADORA_CNPJ_JA_CADASTRADO }]);
    expect(resultado.mensagemGeral).toBeNull();
    expect(resultado.emUso).toBe(true);
  });

  it('e-mail da empresa já cadastrado volta para a etapa 1, no campo do e-mail comercial', () => {
    const resultado = interpretarErroCadastroLocadora(
      erroApi(409, 'Já existe uma locadora cadastrada com este e-mail.'),
    );
    expect(resultado.erros).toEqual([{ etapa: 1, campo: 'email', mensagem: MENSAGEM_LOCADORA_EMAIL_JA_CADASTRADO }]);
  });

  it('CPF do responsável já cadastrado fica na etapa 2, no campo do CPF', () => {
    const resultado = interpretarErroCadastroLocadora(
      erroApi(409, 'Já existe um funcionário cadastrado com este CPF.'),
    );
    expect(resultado.erros).toEqual([
      { etapa: 2, campo: 'cpf', mensagem: MENSAGEM_LOCADORA_RESPONSAVEL_CPF_JA_CADASTRADO },
    ]);
  });

  it('um 409 sem texto conhecido vira aviso geral (sem campo)', () => {
    const resultado = interpretarErroCadastroLocadora(erroApi(409, 'Outro conflito qualquer.'));
    expect(resultado.erros).toEqual([]);
    expect(resultado.mensagemGeral).toBe('Outro conflito qualquer.');
    expect(resultado.emUso).toBe(false);
  });
});

/**
 * Textos reais dos 409 da locadora (ConstantsMessage.Locadora.cs e Equipe.cs), fixados como literais: se o back mudar
 * a frase, este teste quebra aqui e não a tela.
 */
describe('interpretarErroCadastroLocadora: textos reais dos 409 do backend, fixados', () => {
  it('ErroLocadoraCnpjDuplicado vai para o campo CNPJ da etapa 1', () => {
    const resultado = interpretarErroCadastroLocadora(erroApi(409, 'Já existe uma locadora cadastrada com este CNPJ.'));
    expect(resultado.erros).toEqual([{ etapa: 1, campo: 'cnpj', mensagem: MENSAGEM_LOCADORA_CNPJ_JA_CADASTRADO }]);
  });

  it('ErroLocadoraEmailDuplicado vai para o campo e-mail da etapa 1', () => {
    const resultado = interpretarErroCadastroLocadora(erroApi(409, 'Já existe uma locadora cadastrada com este e-mail.'));
    expect(resultado.erros).toEqual([{ etapa: 1, campo: 'email', mensagem: MENSAGEM_LOCADORA_EMAIL_JA_CADASTRADO }]);
  });

  it('ErroLocadoraResponsavelCpfDuplicado vai para o campo CPF da etapa 2', () => {
    const resultado = interpretarErroCadastroLocadora(erroApi(409, 'Já existe um funcionário cadastrado com este CPF.'));
    expect(resultado.erros).toEqual([{ etapa: 2, campo: 'cpf', mensagem: MENSAGEM_LOCADORA_RESPONSAVEL_CPF_JA_CADASTRADO }]);
  });
});

describe('interpretarErroCadastroLocadora: 400 sem errors que cita CNPJ volta para a etapa 1', () => {
  it('o CNPJ recusado no serviço (ex.: operadora) aponta o campo CNPJ da etapa 1, sem mensagem técnica', () => {
    const resultado = interpretarErroCadastroLocadora(erroApi(400, 'O CNPJ informado é inválido.'));
    expect(resultado.erros).toEqual([{ etapa: 1, campo: 'cnpj', mensagem: MENSAGEM_LOCADORA_CNPJ_INVALIDO }]);
    expect(resultado.mensagemGeral).toBeNull();
    expect(resultado.emUso).toBe(false);
  });

  it('um 400 sem errors e sem CNPJ continua como aviso geral', () => {
    const resultado = interpretarErroCadastroLocadora(erroApi(400, 'Dados inválidos'));
    expect(resultado.erros).toEqual([]);
    expect(resultado.mensagemGeral).toBe('Dados inválidos');
  });
});

describe('interpretarErroCadastroLocadora: 400 (validação) leva cada campo à etapa dele', () => {
  it('chaves da etapa 1 vão para a etapa 1 e as do responsável para a 2, sem diferença de maiúsculas', () => {
    const resultado = interpretarErroCadastroLocadora(
      erroApi(400, 'Dados inválidos', {
        razaoSocial: ['Informe a razão social da locadora.'],
        ResponsavelCpf: ['O CPF do responsável é inválido.'],
        aceiteTermos: ['É preciso aceitar os termos de uso e a política de privacidade.'],
      }),
    );
    expect(resultado.erros).toEqual([
      { etapa: 1, campo: 'razaoSocial', mensagem: 'Informe a razão social da locadora.' },
      { etapa: 2, campo: 'cpf', mensagem: 'O CPF do responsável é inválido.' },
      { etapa: 2, campo: 'aceiteTermos', mensagem: 'É preciso aceitar os termos de uso e a política de privacidade.' },
    ]);
    expect(resultado.emUso).toBe(false);
  });

  it('e-mail da empresa (email) e e-mail do responsável (responsavelEmail) são campos diferentes', () => {
    const resultado = interpretarErroCadastroLocadora(
      erroApi(400, 'Dados inválidos', {
        email: ['O e-mail da locadora é inválido.'],
        responsavelEmail: ['O e-mail do responsável é inválido.'],
      }),
    );
    expect(resultado.erros).toEqual([
      { etapa: 1, campo: 'email', mensagem: 'O e-mail da locadora é inválido.' },
      { etapa: 2, campo: 'email', mensagem: 'O e-mail do responsável é inválido.' },
    ]);
  });

  it('chaves desconhecidas são ignoradas; sem nenhuma conhecida, vira aviso geral', () => {
    const resultado = interpretarErroCadastroLocadora(erroApi(400, 'Dados inválidos', { algoEstranho: ['x'] }));
    expect(resultado.erros).toEqual([]);
    expect(resultado.mensagemGeral).toBe('Dados inválidos');
  });
});

describe('interpretarErroCadastroLocadora: avisos gerais com texto próprio', () => {
  it('404 (cadastro público desligado) mostra o aviso de indisponibilidade', () => {
    expect(interpretarErroCadastroLocadora(erroApi(404, 'ErroLocadoraCadastroIndisponivel')).mensagemGeral).toBe(
      MENSAGEM_LOCADORA_CADASTRO_INDISPONIVEL,
    );
  });

  it('429 (limite de tentativas) mostra o aviso de limite', () => {
    expect(interpretarErroCadastroLocadora(erroApi(429, 'ErroLimiteRequisicoes')).mensagemGeral).toBe(
      MENSAGEM_LOCADORA_LIMITE_TENTATIVAS,
    );
  });

  it('falha de rede (sem resposta, status 0) mostra o aviso de envio', () => {
    expect(interpretarErroCadastroLocadora(erroApi(0, 'falha de conexão')).mensagemGeral).toBe(
      MENSAGEM_LOCADORA_FALHA_ENVIO,
    );
  });

  it('500 usa a mensagem que o cliente HTTP já deixou pronta, nunca o texto técnico', () => {
    const resultado = interpretarErroCadastroLocadora(erroApi(500, 'Não foi possível concluir a operação.'));
    expect(resultado.mensagemGeral).toBe('Não foi possível concluir a operação.');
  });

  it('erro que não é ErroApi vira a mensagem padrão, sem o texto cru', () => {
    const resultado = interpretarErroCadastroLocadora(new Error('NullReferenceException em EmpresaService'));
    expect(resultado.mensagemGeral).toBe(MENSAGEM_ERRO_API_PADRAO);
    expect(resultado.mensagemGeral).not.toContain('NullReference');
  });
});

describe('conflitosDoEnvio e camposEmConflitoLocadora: não avançar com o valor recusado', () => {
  it('guarda o CNPJ e o e-mail recusados já normalizados', () => {
    const resultado = interpretarErroCadastroLocadora(
      erroApi(409, 'Já existe uma locadora cadastrada com este CNPJ.'),
    );
    expect(conflitosDoEnvio(resultado, { ...REQUISICAO, email: 'Contato@RotaSul.com.br' })).toEqual({
      cnpj: '11222333000181',
    });
  });

  it('erro de validação não cria conflito', () => {
    const resultado = interpretarErroCadastroLocadora(erroApi(400, 'x', { cnpj: ['O CNPJ é inválido.'] }));
    expect(conflitosDoEnvio(resultado, REQUISICAO)).toEqual({});
  });

  it('o campo em conflito continua bloqueado com a máscara; mudar o valor libera', () => {
    const conflitos = { cnpj: '11222333000181' };
    expect(camposEmConflitoLocadora({ cnpj: '11.222.333/0001-81', email: 'x@y.com' }, conflitos)).toEqual(['cnpj']);
    expect(camposEmConflitoLocadora({ cnpj: '11.222.333/0001-82', email: 'x@y.com' }, conflitos)).toEqual([]);
  });

  it('e-mail em conflito compara sem maiúsculas e sem espaços nas pontas', () => {
    const conflitos = { email: 'contato@rotasul.com.br' };
    expect(camposEmConflitoLocadora({ cnpj: '', email: '  CONTATO@rotasul.com.br ' }, conflitos)).toEqual(['email']);
  });
});

describe('errosDaEtapaLocadora: a etapa 2 espera a etapa 1', () => {
  const pendentes = [
    { etapa: 1 as const, campo: 'cnpj', mensagem: 'a' },
    { etapa: 2 as const, campo: 'cpf', mensagem: 'b' },
  ];

  it('a etapa 1 aplica só os seus erros', () => {
    expect(errosDaEtapaLocadora(1, pendentes)).toEqual([pendentes[0]]);
  });

  it('a etapa 2 espera enquanto houver erro da etapa 1', () => {
    expect(errosDaEtapaLocadora(2, pendentes)).toEqual([]);
  });

  it('a etapa 2 aplica os seus quando não resta erro da etapa 1', () => {
    expect(errosDaEtapaLocadora(2, [pendentes[1]])).toEqual([pendentes[1]]);
  });
});
