import { describe, expect, it } from 'vitest';
import {
  MENSAGEM_CADASTRO_CEP_INVALIDO,
  MENSAGEM_CADASTRO_CNH_CATEGORIA_INVALIDA,
  MENSAGEM_CADASTRO_CNH_NUMERO_OBRIGATORIO,
  MENSAGEM_CADASTRO_CNH_VALIDADE_OBRIGATORIA,
  MENSAGEM_CADASTRO_CNH_VENCIDA,
  MENSAGEM_CADASTRO_CPF_INVALIDO,
  MENSAGEM_CADASTRO_CPF_OBRIGATORIO,
  MENSAGEM_CADASTRO_DATA_NASCIMENTO_FUTURA,
  MENSAGEM_CADASTRO_DATA_NASCIMENTO_OBRIGATORIA,
  MENSAGEM_CADASTRO_EMAIL_INVALIDO,
  MENSAGEM_CADASTRO_EMAIL_OBRIGATORIO,
  MENSAGEM_CADASTRO_NOME_OBRIGATORIO,
  MENSAGEM_CADASTRO_SENHA_FRACA,
  MENSAGEM_CADASTRO_SENHA_OBRIGATORIA,
  MENSAGEM_CADASTRO_SENHAS_NAO_CONFEREM,
  MENSAGEM_CADASTRO_TELEFONE_INVALIDO,
  MENSAGEM_CADASTRO_TELEFONE_OBRIGATORIO,
  MENSAGEM_CADASTRO_UF_INVALIDA,
  mensagemCadastroCampoTamanho,
  mensagemCadastroCnhNumeroInvalido,
  mensagemCadastroMenorDeIdade,
  mensagemCadastroSenhaTamanho,
} from '@/lib/mensagens';
import {
  criarEsquemaCadastro,
  type FormularioCadastro,
} from '@/features/clientes/cadastro/esquemaCadastro';

// Referência fixa: 12/11/2026 (hora local). O esquema recebe este "hoje" em vez do relógio.
const HOJE = new Date(2026, 10, 12, 10, 0);
const esquema = criarEsquemaCadastro(() => HOJE);

/** Formulário totalmente válido, com endereço preenchido. */
const VALIDO: FormularioCadastro = {
  nome: 'Mariana Ribeiro de Souza',
  cpf: '529.982.247-25',
  dataNascimento: '1992-08-14',
  email: 'mariana@email.com',
  telefone: '(11) 98765-4321',
  cnhNumero: '12345678901',
  cnhCategoria: 'B',
  cnhValidade: '2028-05-10',
  cep: '01310-100',
  logradouro: 'Avenida Paulista',
  numero: '1000',
  complemento: 'Apto 12',
  bairro: 'Bela Vista',
  cidade: 'São Paulo',
  uf: 'SP',
  senha: 'Senha1234',
  confirmarSenha: 'Senha1234',
};

/** Resultado do esquema para o formulário válido com os campos alterados. */
function resultadoCom(alteracoes: Partial<FormularioCadastro>) {
  return esquema.safeParse({ ...VALIDO, ...alteracoes });
}

/** Primeira mensagem de erro do campo, ou undefined quando o campo está válido. */
function mensagemDo(campo: keyof FormularioCadastro, alteracoes: Partial<FormularioCadastro>) {
  const resultado = resultadoCom(alteracoes);
  if (resultado.success) return undefined;
  return resultado.error.issues.find((issue) => issue.path[0] === campo)?.message;
}

describe('esquemaCadastro: caso válido', () => {
  it('aceita o formulário completo', () => {
    expect(resultadoCom({}).success).toBe(true);
  });

  it('aceita o endereço vazio (todo o bloco é opcional)', () => {
    const semEndereco = resultadoCom({
      cep: '',
      logradouro: '',
      numero: '',
      complemento: '',
      bairro: '',
      cidade: '',
      uf: '',
    });
    expect(semEndereco.success).toBe(true);
  });

  it('aceita CPF e telefone com ou sem máscara', () => {
    expect(resultadoCom({ cpf: '52998224725', telefone: '11987654321' }).success).toBe(true);
  });
});

describe('esquemaCadastro: nome', () => {
  it('recusa vazio e só espaços', () => {
    expect(mensagemDo('nome', { nome: '' })).toBe(MENSAGEM_CADASTRO_NOME_OBRIGATORIO);
    expect(mensagemDo('nome', { nome: '   ' })).toBe(MENSAGEM_CADASTRO_NOME_OBRIGATORIO);
  });

  it('recusa mais de 120 caracteres e aceita 120', () => {
    expect(mensagemDo('nome', { nome: 'a'.repeat(121) })).toBe(
      mensagemCadastroCampoTamanho('nome', 120),
    );
    expect(mensagemDo('nome', { nome: 'a'.repeat(120) })).toBeUndefined();
  });
});

describe('esquemaCadastro: CPF', () => {
  it('recusa vazio', () => {
    expect(mensagemDo('cpf', { cpf: '' })).toBe(MENSAGEM_CADASTRO_CPF_OBRIGATORIO);
  });

  it('recusa dígito verificador errado e sequência repetida', () => {
    expect(mensagemDo('cpf', { cpf: '529.982.247-26' })).toBe(MENSAGEM_CADASTRO_CPF_INVALIDO);
    expect(mensagemDo('cpf', { cpf: '111.111.111-11' })).toBe(MENSAGEM_CADASTRO_CPF_INVALIDO);
  });
});

describe('esquemaCadastro: data de nascimento', () => {
  it('recusa vazia', () => {
    expect(mensagemDo('dataNascimento', { dataNascimento: '' })).toBe(
      MENSAGEM_CADASTRO_DATA_NASCIMENTO_OBRIGATORIA,
    );
  });

  it('recusa data futura', () => {
    expect(mensagemDo('dataNascimento', { dataNascimento: '2026-11-13' })).toBe(
      MENSAGEM_CADASTRO_DATA_NASCIMENTO_FUTURA,
    );
  });

  it('recusa menor de 18 anos (17 anos e 364 dias) e aceita 18 completos hoje', () => {
    expect(mensagemDo('dataNascimento', { dataNascimento: '2008-11-13' })).toBe(
      mensagemCadastroMenorDeIdade(18),
    );
    expect(mensagemDo('dataNascimento', { dataNascimento: '2008-11-12' })).toBeUndefined();
  });
});

describe('esquemaCadastro: e-mail', () => {
  it('recusa vazio', () => {
    expect(mensagemDo('email', { email: '' })).toBe(MENSAGEM_CADASTRO_EMAIL_OBRIGATORIO);
  });

  it('recusa formato inválido', () => {
    expect(mensagemDo('email', { email: 'sem-arroba.com' })).toBe(MENSAGEM_CADASTRO_EMAIL_INVALIDO);
  });

  it('recusa mais de 120 caracteres', () => {
    expect(mensagemDo('email', { email: `${'a'.repeat(115)}@x.com` })).toBe(
      mensagemCadastroCampoTamanho('e-mail', 120),
    );
  });
});

describe('esquemaCadastro: telefone', () => {
  it('recusa vazio', () => {
    expect(mensagemDo('telefone', { telefone: '' })).toBe(MENSAGEM_CADASTRO_TELEFONE_OBRIGATORIO);
  });

  it('recusa menos de 10 dígitos ou mais de 11', () => {
    expect(mensagemDo('telefone', { telefone: '(11) 9876' })).toBe(MENSAGEM_CADASTRO_TELEFONE_INVALIDO);
    expect(mensagemDo('telefone', { telefone: '119876543210' })).toBe(MENSAGEM_CADASTRO_TELEFONE_INVALIDO);
  });

  it('aceita 10 dígitos (fixo com DDD)', () => {
    expect(mensagemDo('telefone', { telefone: '1134567890' })).toBeUndefined();
  });
});

describe('esquemaCadastro: CNH', () => {
  it('recusa o número vazio e com outro tamanho que não 11 dígitos', () => {
    expect(mensagemDo('cnhNumero', { cnhNumero: '' })).toBe(MENSAGEM_CADASTRO_CNH_NUMERO_OBRIGATORIO);
    expect(mensagemDo('cnhNumero', { cnhNumero: '1234567890' })).toBe(
      mensagemCadastroCnhNumeroInvalido(11),
    );
    expect(mensagemDo('cnhNumero', { cnhNumero: '123456789012' })).toBe(
      mensagemCadastroCnhNumeroInvalido(11),
    );
  });

  it('recusa categoria vazia ou fora de A, B, AB, C, D e E', () => {
    expect(mensagemDo('cnhCategoria', { cnhCategoria: '' })).toBe(MENSAGEM_CADASTRO_CNH_CATEGORIA_INVALIDA);
    expect(mensagemDo('cnhCategoria', { cnhCategoria: 'F' })).toBe(MENSAGEM_CADASTRO_CNH_CATEGORIA_INVALIDA);
    expect(mensagemDo('cnhCategoria', { cnhCategoria: 'AB' })).toBeUndefined();
  });

  it('recusa validade vazia', () => {
    expect(mensagemDo('cnhValidade', { cnhValidade: '' })).toBe(MENSAGEM_CADASTRO_CNH_VALIDADE_OBRIGATORIA);
  });

  it('recusa CNH vencida (ontem) e aceita a validade de hoje', () => {
    expect(mensagemDo('cnhValidade', { cnhValidade: '2026-11-11' })).toBe(MENSAGEM_CADASTRO_CNH_VENCIDA);
    expect(mensagemDo('cnhValidade', { cnhValidade: '2026-11-12' })).toBeUndefined();
  });
});

describe('esquemaCadastro: endereço (opcional)', () => {
  it('recusa CEP preenchido com menos de 8 dígitos', () => {
    expect(mensagemDo('cep', { cep: '01310-10' })).toBe(MENSAGEM_CADASTRO_CEP_INVALIDO);
    expect(mensagemDo('cep', { cep: '-' })).toBe(MENSAGEM_CADASTRO_CEP_INVALIDO);
  });

  it('aceita CEP com 8 dígitos, com ou sem máscara', () => {
    expect(mensagemDo('cep', { cep: '01310-100' })).toBeUndefined();
    expect(mensagemDo('cep', { cep: '01310100' })).toBeUndefined();
  });

  it('recusa UF fora da lista de siglas (inclui minúsculas)', () => {
    expect(mensagemDo('uf', { uf: 'XX' })).toBe(MENSAGEM_CADASTRO_UF_INVALIDA);
    expect(mensagemDo('uf', { uf: 'sp' })).toBe(MENSAGEM_CADASTRO_UF_INVALIDA);
  });

  it('respeita os limites de tamanho de logradouro, número, complemento, bairro e cidade', () => {
    expect(mensagemDo('logradouro', { logradouro: 'a'.repeat(121) })).toBe(
      mensagemCadastroCampoTamanho('logradouro', 120),
    );
    expect(mensagemDo('numero', { numero: 'a'.repeat(11) })).toBe(mensagemCadastroCampoTamanho('número', 10));
    expect(mensagemDo('complemento', { complemento: 'a'.repeat(61) })).toBe(
      mensagemCadastroCampoTamanho('complemento', 60),
    );
    expect(mensagemDo('bairro', { bairro: 'a'.repeat(61) })).toBe(mensagemCadastroCampoTamanho('bairro', 60));
    expect(mensagemDo('cidade', { cidade: 'a'.repeat(61) })).toBe(mensagemCadastroCampoTamanho('cidade', 60));
  });
});

describe('esquemaCadastro: senha', () => {
  it('recusa vazia', () => {
    expect(mensagemDo('senha', { senha: '' })).toBe(MENSAGEM_CADASTRO_SENHA_OBRIGATORIA);
  });

  it('recusa menos de 8 e mais de 64 caracteres', () => {
    expect(mensagemDo('senha', { senha: 'Abc1234', confirmarSenha: 'Abc1234' })).toBe(
      mensagemCadastroSenhaTamanho(8, 64),
    );
    const longa = `${'a'.repeat(64)}1`;
    expect(mensagemDo('senha', { senha: longa, confirmarSenha: longa })).toBe(mensagemCadastroSenhaTamanho(8, 64));
  });

  it('recusa senha sem letra ou sem número', () => {
    expect(mensagemDo('senha', { senha: 'abcdefgh', confirmarSenha: 'abcdefgh' })).toBe(MENSAGEM_CADASTRO_SENHA_FRACA);
    expect(mensagemDo('senha', { senha: '12345678', confirmarSenha: '12345678' })).toBe(MENSAGEM_CADASTRO_SENHA_FRACA);
  });
});

describe('esquemaCadastro: confirmação de senha', () => {
  it('recusa senhas diferentes', () => {
    const resultado = resultadoCom({ confirmarSenha: 'Senha9999' });
    expect(resultado.success).toBe(false);
    if (resultado.success) return;
    const issue = resultado.error.issues.find((item) => item.path[0] === 'confirmarSenha');
    expect(issue?.message).toBe(MENSAGEM_CADASTRO_SENHAS_NAO_CONFEREM);
  });

  it('recusa confirmação vazia', () => {
    expect(mensagemDo('confirmarSenha', { confirmarSenha: '' })).toBe(MENSAGEM_CADASTRO_SENHAS_NAO_CONFEREM);
  });

  /**
   * Decide se a regra de confirmação roda junto com erros de outros campos. O resultado
   * real está documentado aqui: a confirmação é checada mesmo com nome vazio.
   */
  it('documenta: a confirmação é checada mesmo com outro campo inválido (nome vazio)', () => {
    const resultado = resultadoCom({ nome: '', confirmarSenha: 'outra' });
    expect(resultado.success).toBe(false);
    if (resultado.success) return;
    const issues = resultado.error.issues.map((item) => [item.path[0], item.message]);
    expect(issues).toContainEqual(['confirmarSenha', MENSAGEM_CADASTRO_SENHAS_NAO_CONFEREM]);
    expect(issues).toContainEqual(['nome', MENSAGEM_CADASTRO_NOME_OBRIGATORIO]);
  });
});

describe('esquemaCadastro: casos de borda da senha', () => {
  it('recusa senha só com espaços', () => {
    expect(mensagemDo('senha', { senha: '        ', confirmarSenha: '        ' })).toBe(
      MENSAGEM_CADASTRO_SENHA_OBRIGATORIA,
    );
  });

  it('aceita exatamente 8 e exatamente 64 caracteres', () => {
    expect(mensagemDo('senha', { senha: 'Abcdefg1', confirmarSenha: 'Abcdefg1' })).toBeUndefined();
    const com64 = `${'A'.repeat(63)}1`;
    expect(com64).toHaveLength(64);
    expect(mensagemDo('senha', { senha: com64, confirmarSenha: com64 })).toBeUndefined();
  });

  it('aceita letra acentuada junto com número (ex.: "Ação1234")', () => {
    expect(mensagemDo('senha', { senha: 'Ação1234', confirmarSenha: 'Ação1234' })).toBeUndefined();
  });
});

describe('esquemaCadastro: casos de borda do e-mail', () => {
  it('recusa e-mail sem ponto no domínio ("a@b")', () => {
    expect(mensagemDo('email', { email: 'a@b' })).toBe(MENSAGEM_CADASTRO_EMAIL_INVALIDO);
  });

  it('recusa e-mail com espaço no meio', () => {
    expect(mensagemDo('email', { email: 'mar iana@email.com' })).toBe(MENSAGEM_CADASTRO_EMAIL_INVALIDO);
  });
});

describe('esquemaCadastro: datas e CPF de borda', () => {
  /** Esquema com "hoje" informado (meio-dia local, para não cair na virada do dia). */
  function esquemaComHoje(ano: number, mes: number, dia: number) {
    return criarEsquemaCadastro(() => new Date(ano, mes, dia, 12));
  }

  it('nascido em 29/02: ainda não tem 18 em 28/02 do ano do aniversário, e tem em 01/03', () => {
    const dados = { ...VALIDO, dataNascimento: '2008-02-29', confirmarSenha: VALIDO.senha };
    const em28 = esquemaComHoje(2026, 1, 28).safeParse(dados);
    const em01 = esquemaComHoje(2026, 2, 1).safeParse(dados);

    expect(em28.success).toBe(false);
    if (!em28.success) {
      expect(em28.error.issues.find((item) => item.path[0] === 'dataNascimento')?.message).toBe(
        mensagemCadastroMenorDeIdade(18),
      );
    }
    expect(em01.success).toBe(true);
  });

  it('recusa data de nascimento inexistente ("2025-02-30")', () => {
    expect(mensagemDo('dataNascimento', { dataNascimento: '2025-02-30' })).toBe(
      MENSAGEM_CADASTRO_DATA_NASCIMENTO_OBRIGATORIA,
    );
  });

  it('recusa validade inexistente ("2025-02-30")', () => {
    expect(mensagemDo('cnhValidade', { cnhValidade: '2025-02-30' })).toBe(
      MENSAGEM_CADASTRO_CNH_VALIDADE_OBRIGATORIA,
    );
  });

  it('recusa CPF com letras', () => {
    expect(mensagemDo('cpf', { cpf: '529.982.2A5-25' })).toBe(MENSAGEM_CADASTRO_CPF_INVALIDO);
  });
});
