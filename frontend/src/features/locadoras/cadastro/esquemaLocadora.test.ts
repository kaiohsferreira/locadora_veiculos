import { describe, expect, it } from 'vitest';
import {
  MENSAGEM_CADASTRO_CEP_INVALIDO,
  MENSAGEM_CADASTRO_EMAIL_INVALIDO,
  MENSAGEM_CADASTRO_TELEFONE_INVALIDO,
  MENSAGEM_LOCADORA_BAIRRO_OBRIGATORIO,
  MENSAGEM_LOCADORA_CEP_OBRIGATORIO,
  MENSAGEM_LOCADORA_CIDADE_OBRIGATORIA,
  MENSAGEM_LOCADORA_CNPJ_INVALIDO,
  MENSAGEM_LOCADORA_CNPJ_OBRIGATORIO,
  MENSAGEM_LOCADORA_EMAIL_OBRIGATORIO,
  MENSAGEM_LOCADORA_LOGRADOURO_OBRIGATORIO,
  MENSAGEM_LOCADORA_NOME_FANTASIA_OBRIGATORIO,
  MENSAGEM_LOCADORA_NUMERO_OBRIGATORIO,
  MENSAGEM_LOCADORA_RAZAO_SOCIAL_OBRIGATORIA,
  MENSAGEM_LOCADORA_TELEFONE_OBRIGATORIO,
  MENSAGEM_LOCADORA_UF_INVALIDA,
  MENSAGEM_LOCADORA_UF_OBRIGATORIA,
  mensagemCadastroCampoTamanho,
} from '@/lib/mensagens';
import {
  calcularCnpjValido,
  DADOS_EMPRESA_INICIAL,
  ESQUEMA_EMPRESA,
  LIMITES_EMPRESA,
  type FormularioEmpresa,
} from './esquemaLocadora';

/** Formulário com todos os campos válidos; cada teste troca só o campo que quer testar. */
const VALIDO: FormularioEmpresa = {
  razaoSocial: 'Rota Sul Locadora de Veículos Ltda',
  nomeFantasia: 'Rota Sul Locadora',
  cnpj: '11.222.333/0001-81',
  telefone: '(51) 3342-9000',
  email: 'contato@rotasul.com.br',
  cep: '90200-310',
  logradouro: 'Av. Severo Dullius',
  numero: '1240',
  complemento: '',
  bairro: 'São João',
  cidade: 'Porto Alegre',
  uf: 'RS',
};

/** Mensagem do primeiro erro do campo, ou null quando o campo passa. */
function erroDoCampo(
  campo: keyof FormularioEmpresa,
  valor: string,
  base: FormularioEmpresa = VALIDO,
): string | null {
  const resultado = ESQUEMA_EMPRESA.safeParse({ ...base, [campo]: valor });
  if (resultado.success) return null;
  const problema = resultado.error.issues.find((issue) => issue.path[0] === campo);
  return problema ? problema.message : null;
}

describe('ESQUEMA_EMPRESA: formulário completo', () => {
  it('aceita o formulário com todos os campos válidos', () => {
    expect(ESQUEMA_EMPRESA.safeParse(VALIDO).success).toBe(true);
  });

  it('aceita o complemento vazio (é opcional)', () => {
    expect(erroDoCampo('complemento', '')).toBeNull();
  });

  it('o formulário vazio não passa e aponta o primeiro campo obrigatório', () => {
    expect(erroDoCampo('razaoSocial', DADOS_EMPRESA_INICIAL.razaoSocial, DADOS_EMPRESA_INICIAL)).toBe(
      MENSAGEM_LOCADORA_RAZAO_SOCIAL_OBRIGATORIA,
    );
    expect(ESQUEMA_EMPRESA.safeParse(DADOS_EMPRESA_INICIAL).success).toBe(false);
  });
});

describe('ESQUEMA_EMPRESA: razão social e nome fantasia', () => {
  it('são obrigatórios e não aceitam só espaços', () => {
    expect(erroDoCampo('razaoSocial', '')).toBe(MENSAGEM_LOCADORA_RAZAO_SOCIAL_OBRIGATORIA);
    expect(erroDoCampo('razaoSocial', '   ')).toBe(MENSAGEM_LOCADORA_RAZAO_SOCIAL_OBRIGATORIA);
    expect(erroDoCampo('nomeFantasia', '  ')).toBe(MENSAGEM_LOCADORA_NOME_FANTASIA_OBRIGATORIO);
  });

  it('razão social: 150 caracteres passa e 151 não', () => {
    expect(erroDoCampo('razaoSocial', 'a'.repeat(LIMITES_EMPRESA.razaoSocial))).toBeNull();
    expect(erroDoCampo('razaoSocial', 'a'.repeat(LIMITES_EMPRESA.razaoSocial + 1))).toBe(
      mensagemCadastroCampoTamanho('razão social', 150),
    );
  });

  it('nome fantasia: 100 caracteres passa e 101 não', () => {
    expect(erroDoCampo('nomeFantasia', 'a'.repeat(100))).toBeNull();
    expect(erroDoCampo('nomeFantasia', 'a'.repeat(101))).toBe(mensagemCadastroCampoTamanho('nome fantasia', 100));
  });
});

describe('ESQUEMA_EMPRESA: CNPJ', () => {
  it('aceita CNPJ com máscara e sem máscara quando o dígito verificador confere', () => {
    expect(erroDoCampo('cnpj', '11.222.333/0001-81')).toBeNull();
    expect(erroDoCampo('cnpj', '11222333000181')).toBeNull();
  });

  it('rejeita CNPJ com o dígito verificador errado, com a mensagem de CNPJ inválido', () => {
    expect(erroDoCampo('cnpj', '11.222.333/0001-82')).toBe(MENSAGEM_LOCADORA_CNPJ_INVALIDO);
  });

  it('rejeita CNPJ incompleto e sequência repetida', () => {
    expect(erroDoCampo('cnpj', '11.222.333/0001')).toBe(MENSAGEM_LOCADORA_CNPJ_INVALIDO);
    expect(erroDoCampo('cnpj', '11111111111111')).toBe(MENSAGEM_LOCADORA_CNPJ_INVALIDO);
  });

  it('CNPJ vazio aponta a mensagem de obrigatório', () => {
    expect(erroDoCampo('cnpj', '')).toBe(MENSAGEM_LOCADORA_CNPJ_OBRIGATORIO);
  });

  it('calcularCnpjValido confere só a forma e os dígitos (sem consultar a Receita)', () => {
    expect(calcularCnpjValido('11.222.333/0001-81')).toBe(true);
    expect(calcularCnpjValido('11.222.333/0001-80')).toBe(false);
    expect(calcularCnpjValido('')).toBe(false);
  });
});

describe('ESQUEMA_EMPRESA: telefone comercial', () => {
  it('aceita 10 ou 11 dígitos, com ou sem máscara', () => {
    expect(erroDoCampo('telefone', '(51) 3342-9000')).toBeNull();
    expect(erroDoCampo('telefone', '5133342900')).toBeNull();
    expect(erroDoCampo('telefone', '(51) 98765-4321')).toBeNull();
  });

  it('rejeita 9 e 12 dígitos', () => {
    expect(erroDoCampo('telefone', '513342900')).toBe(MENSAGEM_CADASTRO_TELEFONE_INVALIDO);
    expect(erroDoCampo('telefone', '513342900012')).toBe(MENSAGEM_CADASTRO_TELEFONE_INVALIDO);
  });

  it('é obrigatório', () => {
    expect(erroDoCampo('telefone', '')).toBe(MENSAGEM_LOCADORA_TELEFONE_OBRIGATORIO);
  });
});

describe('ESQUEMA_EMPRESA: e-mail comercial', () => {
  it('aceita e-mail com formato válido, em maiúsculas ou minúsculas', () => {
    expect(erroDoCampo('email', 'contato@rotasul.com.br')).toBeNull();
    expect(erroDoCampo('email', 'Contato@RotaSul.com.br')).toBeNull();
  });

  it('rejeita formato inválido e aceita o máximo de 120 caracteres', () => {
    expect(erroDoCampo('email', 'contato.rotasul.com.br')).toBe(MENSAGEM_CADASTRO_EMAIL_INVALIDO);
    expect(erroDoCampo('email', 'contato@rotasul')).toBe(MENSAGEM_CADASTRO_EMAIL_INVALIDO);

    const local = 'a'.repeat(LIMITES_EMPRESA.email - '@x.co'.length);
    expect(erroDoCampo('email', `${local}@x.co`)).toBeNull();
    expect(erroDoCampo('email', `${local}a@x.co`)).toBe(mensagemCadastroCampoTamanho('e-mail', 120));
  });

  it('é obrigatório', () => {
    expect(erroDoCampo('email', '')).toBe(MENSAGEM_LOCADORA_EMAIL_OBRIGATORIO);
  });
});

describe('ESQUEMA_EMPRESA: endereço da sede', () => {
  it('CEP tem exatamente 8 dígitos, com ou sem traço', () => {
    expect(erroDoCampo('cep', '90200-310')).toBeNull();
    expect(erroDoCampo('cep', '90200310')).toBeNull();
    expect(erroDoCampo('cep', '9020031')).toBe(MENSAGEM_CADASTRO_CEP_INVALIDO);
    expect(erroDoCampo('cep', '')).toBe(MENSAGEM_LOCADORA_CEP_OBRIGATORIO);
  });

  it('logradouro: obrigatório, até 120 caracteres', () => {
    expect(erroDoCampo('logradouro', '')).toBe(MENSAGEM_LOCADORA_LOGRADOURO_OBRIGATORIO);
    expect(erroDoCampo('logradouro', 'a'.repeat(120))).toBeNull();
    expect(erroDoCampo('logradouro', 'a'.repeat(121))).toBe(mensagemCadastroCampoTamanho('endereço', 120));
  });

  it('número: obrigatório, até 10 caracteres', () => {
    expect(erroDoCampo('numero', '')).toBe(MENSAGEM_LOCADORA_NUMERO_OBRIGATORIO);
    expect(erroDoCampo('numero', 'a'.repeat(10))).toBeNull();
    expect(erroDoCampo('numero', 'a'.repeat(11))).toBe(mensagemCadastroCampoTamanho('número', 10));
  });

  it('complemento: opcional, até 60 caracteres', () => {
    expect(erroDoCampo('complemento', 'a'.repeat(60))).toBeNull();
    expect(erroDoCampo('complemento', 'a'.repeat(61))).toBe(mensagemCadastroCampoTamanho('complemento', 60));
  });

  it('bairro e cidade: obrigatórios, até 60 caracteres', () => {
    expect(erroDoCampo('bairro', '')).toBe(MENSAGEM_LOCADORA_BAIRRO_OBRIGATORIO);
    expect(erroDoCampo('bairro', 'a'.repeat(61))).toBe(mensagemCadastroCampoTamanho('bairro', 60));
    expect(erroDoCampo('cidade', '')).toBe(MENSAGEM_LOCADORA_CIDADE_OBRIGATORIA);
    expect(erroDoCampo('cidade', 'a'.repeat(61))).toBe(mensagemCadastroCampoTamanho('cidade', 60));
  });

  it('UF: obrigatória e uma das 27 siglas', () => {
    expect(erroDoCampo('uf', 'RS')).toBeNull();
    expect(erroDoCampo('uf', '')).toBe(MENSAGEM_LOCADORA_UF_OBRIGATORIA);
    expect(erroDoCampo('uf', 'XX')).toBe(MENSAGEM_LOCADORA_UF_INVALIDA);
    expect(erroDoCampo('uf', 'rs')).toBe(MENSAGEM_LOCADORA_UF_INVALIDA);
  });
});
