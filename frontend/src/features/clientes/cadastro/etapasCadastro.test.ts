import { describe, expect, it } from 'vitest';
import { converterFormularioParaRequisicao } from '@/features/clientes/cadastro/conversaoCadastro';
import {
  criarEsquemaCadastro,
  type DadosPessoaisCadastro,
} from '@/features/clientes/cadastro/esquemaCadastro';
import {
  calcularCamposValidos,
  CAMPOS_ETAPA_1,
  CAMPOS_PAYLOAD_API,
  errosParaAplicarNaEtapa,
  CAMPOS_ETAPA_2,
  etapaDeDestino,
  etapaDoCampo,
  extrairDocumentos,
  juntarFormulario,
  separarErrosPorEtapa,
  validarCadastroCompleto,
  type DadosEtapa2,
} from '@/features/clientes/cadastro/etapasCadastro';
import { ORDEM_CAMPOS_TELA, primeiroCampoNaTela } from '@/features/clientes/cadastro/fluxoCadastro';
import { MENSAGEM_CADASTRO_CPF_INVALIDO, MENSAGEM_CADASTRO_SENHAS_NAO_CONFEREM } from '@/lib/mensagens';

// Referência fixa: 12/11/2026 (hora local).
const HOJE = new Date(2026, 10, 12, 10, 0);
const esquema = criarEsquemaCadastro(() => HOJE);

const ETAPA_1: DadosPessoaisCadastro = {
  nome: 'Mariana Ribeiro de Souza',
  cpf: '529.982.247-25',
  dataNascimento: '1992-08-14',
  email: 'Mariana@Email.com',
  telefone: '(11) 98765-4321',
};

const ETAPA_2: DadosEtapa2 = {
  cnhNumero: '12345678901',
  cnhCategoria: 'B',
  cnhValidade: '2028-05-10',
  cep: '01310-100',
  logradouro: 'Avenida Paulista',
  numero: '1000',
  complemento: '',
  bairro: 'Bela Vista',
  cidade: 'São Paulo',
  uf: 'SP',
  senha: 'Senha1234',
  confirmarSenha: 'Senha1234',
};

describe('etapaDoCampo: de qual etapa é cada campo', () => {
  it('nome, CPF, nascimento, e-mail e telefone são da etapa 1', () => {
    for (const campo of ['nome', 'cpf', 'dataNascimento', 'email', 'telefone']) {
      expect(etapaDoCampo(campo)).toBe(1);
    }
  });

  it('CNH, endereço e senha são da etapa 2', () => {
    for (const campo of [
      'cnhNumero',
      'cnhCategoria',
      'cnhValidade',
      'cep',
      'logradouro',
      'numero',
      'complemento',
      'bairro',
      'cidade',
      'uf',
      'senha',
      'confirmarSenha',
    ]) {
      expect(etapaDoCampo(campo)).toBe(2);
    }
  });

  it('campo desconhecido não é de nenhuma etapa', () => {
    expect(etapaDoCampo('perfil')).toBeNull();
    expect(etapaDoCampo('')).toBeNull();
  });

  it('as duas listas cobrem todos os campos da ordem da tela, sem repetir', () => {
    expect([...CAMPOS_ETAPA_1, ...CAMPOS_ETAPA_2]).toEqual([...ORDEM_CAMPOS_TELA]);
  });
});

describe('separarErrosPorEtapa e etapaDeDestino: roteamento dos erros da API', () => {
  it('conflito de CPF (409) cai na etapa 1; conflito de CNH, na etapa 2', () => {
    const cpf = separarErrosPorEtapa([{ campo: 'cpf', mensagem: 'Já existe um cliente ativo com este CPF.' }]);
    expect(cpf.etapa1).toHaveLength(1);
    expect(etapaDeDestino(cpf)).toBe(1);

    const cnh = separarErrosPorEtapa([{ campo: 'cnhNumero', mensagem: 'Já existe um cliente ativo com este número de CNH.' }]);
    expect(cnh.etapa2).toHaveLength(1);
    expect(etapaDeDestino(cnh)).toBe(2);
  });

  it('erro em campo das duas etapas volta para a etapa 1 e separa os da etapa 2', () => {
    const separados = separarErrosPorEtapa([
      { campo: 'senha', mensagem: 'A senha deve ter ao menos uma letra e um número.' },
      { campo: 'telefone', mensagem: 'O telefone deve ter 10 ou 11 dígitos (DDD e número).' },
    ]);
    expect(separados.etapa1.map((erro) => erro.campo)).toEqual(['telefone']);
    expect(separados.etapa2.map((erro) => erro.campo)).toEqual(['senha']);
    expect(etapaDeDestino(separados)).toBe(1);
  });

  it('mantém a ordem de chegada dentro de cada etapa; o foco usa a ordem da tela', () => {
    const separados = separarErrosPorEtapa([
      { campo: 'email', mensagem: 'a' },
      { campo: 'nome', mensagem: 'b' },
    ]);
    expect(separados.etapa1.map((erro) => erro.campo)).toEqual(['email', 'nome']);
    expect(primeiroCampoNaTela(separados.etapa1.map((erro) => erro.campo))).toBe('nome');
  });

  it('campo desconhecido fica em semEtapa e, sozinho, não leva a nenhuma etapa (vira alerta geral)', () => {
    const separados = separarErrosPorEtapa([{ campo: 'perfil', mensagem: 'x' }]);
    expect(separados.semEtapa).toHaveLength(1);
    expect(etapaDeDestino(separados)).toBeNull();
  });

  it('sem erros de campo não há destino', () => {
    expect(etapaDeDestino(separarErrosPorEtapa([]))).toBeNull();
  });
});

describe('juntarFormulario: dados das duas etapas na requisição final', () => {
  it('junta as duas etapas no formulário completo, com a senha e a confirmação da etapa 2', () => {
    const formulario = juntarFormulario(ETAPA_1, ETAPA_2);
    expect(formulario.nome).toBe(ETAPA_1.nome);
    expect(formulario.cpf).toBe(ETAPA_1.cpf);
    expect(formulario.cnhNumero).toBe(ETAPA_2.cnhNumero);
    expect(formulario.senha).toBe('Senha1234');
    expect(formulario.confirmarSenha).toBe('Senha1234');
  });

  it('o resultado passa pelo esquema completo e vira o corpo do POST normalizado', () => {
    const validacao = validarCadastroCompleto(juntarFormulario(ETAPA_1, ETAPA_2), esquema);
    expect(validacao.ok).toBe(true);
    if (!validacao.ok) return;

    expect(converterFormularioParaRequisicao(validacao.formulario)).toEqual({
      nome: 'Mariana Ribeiro de Souza',
      cpf: '52998224725',
      dataNascimento: '1992-08-14',
      email: 'mariana@email.com',
      telefone: '11987654321',
      cnhNumero: '12345678901',
      cnhCategoria: 'B',
      cnhValidade: '2028-05-10',
      logradouro: 'Avenida Paulista',
      numero: '1000',
      complemento: null,
      bairro: 'Bela Vista',
      cidade: 'São Paulo',
      uf: 'SP',
      cep: '01310100',
      senha: 'Senha1234',
    });
  });

  it('a confirmação de senha nunca vai para o corpo do POST', () => {
    const requisicao = converterFormularioParaRequisicao(juntarFormulario(ETAPA_1, ETAPA_2));
    expect(Object.keys(requisicao)).not.toContain('confirmarSenha');
  });
});

describe('validarCadastroCompleto: o envio final confere as duas etapas de novo', () => {
  it('devolve um erro por campo, com a mensagem do esquema', () => {
    const resultado = validarCadastroCompleto(
      juntarFormulario({ ...ETAPA_1, cpf: '529.982.247-26' }, { ...ETAPA_2, confirmarSenha: 'Outra1234' }),
      esquema,
    );
    expect(resultado.ok).toBe(false);
    if (resultado.ok) return;
    expect(resultado.erros).toEqual([
      { campo: 'cpf', mensagem: MENSAGEM_CADASTRO_CPF_INVALIDO },
      { campo: 'confirmarSenha', mensagem: MENSAGEM_CADASTRO_SENHAS_NAO_CONFEREM },
    ]);
    // Um dado da etapa 1 que ficou inválido manda a pessoa de volta para a etapa 1.
    expect(etapaDeDestino(separarErrosPorEtapa(resultado.erros))).toBe(1);
  });
});

describe('extrairDocumentos: o que fica em memória entre as telas', () => {
  it('guarda todos os campos de CNH e endereço e deixa a senha de fora', () => {
    expect(extrairDocumentos(ETAPA_2)).toEqual({
      cnhNumero: '12345678901',
      cnhCategoria: 'B',
      cnhValidade: '2028-05-10',
      cep: '01310-100',
      logradouro: 'Avenida Paulista',
      numero: '1000',
      complemento: '',
      bairro: 'Bela Vista',
      cidade: 'São Paulo',
      uf: 'SP',
    });
  });

  it('nunca leva senha nem confirmação', () => {
    const guardado = extrairDocumentos(ETAPA_2);
    expect(Object.keys(guardado)).not.toContain('senha');
    expect(Object.keys(guardado)).not.toContain('confirmarSenha');
    expect(JSON.stringify(guardado)).not.toContain('Senha1234');
  });
});

describe('errosParaAplicarNaEtapa: erros pendentes entre as telas', () => {
  const cpf = { campo: 'cpf', mensagem: 'x' };
  const cnh = { campo: 'cnhNumero', mensagem: 'y' };

  it('só erros da etapa 2: a etapa 2 aplica agora', () => {
    expect(errosParaAplicarNaEtapa(2, [cnh])).toEqual([cnh]);
    expect(errosParaAplicarNaEtapa(1, [cnh])).toEqual([]);
  });

  it('erros das duas etapas: a etapa 2 não consome até a etapa 1 resolver os seus', () => {
    expect(errosParaAplicarNaEtapa(2, [cpf, cnh])).toEqual([]);
    expect(errosParaAplicarNaEtapa(1, [cpf, cnh])).toEqual([cpf]);
    // Resolvida a etapa 1 (erros removidos), sobra só o da etapa 2.
    expect(errosParaAplicarNaEtapa(2, [cnh])).toEqual([cnh]);
  });

  it('nada pendente: nada a fazer', () => {
    expect(errosParaAplicarNaEtapa(1, [])).toEqual([]);
    expect(errosParaAplicarNaEtapa(2, [])).toEqual([]);
  });
});

describe('CAMPOS_PAYLOAD_API', () => {
  it('são os campos das duas etapas, sem a confirmação de senha (só no front)', () => {
    expect(CAMPOS_PAYLOAD_API).toHaveLength(16);
    expect(CAMPOS_PAYLOAD_API).toContain('senha');
    expect(CAMPOS_PAYLOAD_API).not.toContain('confirmarSenha');
  });
});

describe('calcularCamposValidos: ícone de conferido', () => {
  it('marca só os campos válidos', () => {
    const validos = calcularCamposValidos({ ...ETAPA_1, cpf: '123', email: '' });
    expect(validos).toEqual({ nome: true, cpf: false, dataNascimento: true, email: false, telefone: true });
  });

  it('campo ausente ou vazio nunca é válido', () => {
    expect(calcularCamposValidos({})).toEqual({
      nome: false,
      cpf: false,
      dataNascimento: false,
      email: false,
      telefone: false,
    });
  });
});
