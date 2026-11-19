import { describe, expect, it, vi } from 'vitest';
import {
  acaoAposEnvio,
  escolherCampoDoConflito,
  executarCadastro,
  primeiroCampoNaTela,
  type DependenciasCadastro,
  type EntradaCadastro,
  type ResultadoCadastro,
} from '@/features/clientes/cadastro/fluxoCadastro';
import type { FormularioCadastro } from '@/features/clientes/cadastro/esquemaCadastro';
import {
  MENSAGEM_CADASTRO_CNH_JA_CADASTRADA,
  MENSAGEM_CADASTRO_CPF_JA_CADASTRADO,
  MENSAGEM_CADASTRO_EMAIL_JA_CADASTRADO,
} from '@/lib/mensagens';
import type { RespostaCliente } from '@/types/clientes';
import type { RespostaDocumentoCliente, TipoDocumentoCliente } from '@/types/documentosCliente';

const FORMULARIO: FormularioCadastro = {
  nome: 'Mariana Ribeiro de Souza',
  cpf: '529.982.247-25',
  dataNascimento: '1992-08-14',
  email: 'mariana@email.com',
  telefone: '(11) 98765-4321',
  cnhNumero: '12345678901',
  cnhCategoria: 'B',
  cnhValidade: '2028-05-10',
  cep: '',
  logradouro: '',
  numero: '',
  complemento: '',
  bairro: '',
  cidade: '',
  uf: '',
  senha: 'Senha1234',
  confirmarSenha: 'Senha1234',
};

const RESPOSTA: RespostaCliente = {
  idCliente: 7,
  nome: 'Mariana Ribeiro de Souza',
  cpf: '52998224725',
  cpfStr: '529.982.247-25',
  dataNascimento: '1992-08-14',
  dataNascimentoStr: '14/08/1992',
  email: 'mariana@email.com',
  telefone: '11987654321',
  cnhNumero: '12345678901',
  cnhCategoria: 'B',
  cnhValidade: '2028-05-10',
  cnhValidadeStr: '10/05/2028',
  logradouro: null,
  numero: null,
  complemento: null,
  bairro: null,
  cidade: null,
  uf: null,
  cep: null,
  dataCadastro: '2026-11-12T10:00:00',
  dataCadastroStr: '12/11/2026 10:00',
  statusCadastro: 'PENDENTE',
};

const CNH = new File(['cnh'], 'cnh.pdf', { type: 'application/pdf' });
const COMPROVANTE = new File(['conta'], 'conta.png', { type: 'image/png' });
const CNH_NOVA = new File(['cnh nova'], 'cnh-nova.pdf', { type: 'application/pdf' });

function respostaDocumento(tipo: TipoDocumentoCliente): RespostaDocumentoCliente {
  return {
    idClienteDocumento: 1,
    tipo,
    nomeArquivo: 'arquivo',
    tipoConteudo: 'application/pdf',
    tamanhoBytes: 3,
    dataEnvio: '2026-11-19T10:00:00',
    url: null,
    expiraEm: null,
  };
}

/** Dublês: o POST, o login e o envio de documento, com falhas controladas por cada teste. */
function criarDublês(opcoes: { cadastroFalha?: unknown; loginFalha?: boolean; documentoFalha?: TipoDocumentoCliente }) {
  const cadastrar = vi.fn<DependenciasCadastro['cadastrar']>(async () => {
    if (opcoes.cadastroFalha !== undefined) throw opcoes.cadastroFalha;
    return RESPOSTA;
  });
  const entrar = vi.fn<DependenciasCadastro['entrar']>(async () => {
    if (opcoes.loginFalha) throw new Error('429');
    return {};
  });
  const enviarDocumento = vi.fn<DependenciasCadastro['enviarDocumento']>(async (_id, tipo) => {
    if (opcoes.documentoFalha === tipo) throw new Error('502');
    return respostaDocumento(tipo);
  });
  return { dependencias: { cadastrar, entrar, enviarDocumento }, cadastrar, entrar, enviarDocumento };
}

function entradaCompleta(extra: Partial<EntradaCadastro> = {}): EntradaCadastro {
  return {
    valores: FORMULARIO,
    idCliente: null,
    arquivos: { CNH, COMPROVANTE_RESIDENCIA: COMPROVANTE },
    enviados: {},
    ...extra,
  };
}

describe('escolherCampoDoConflito', () => {
  it('aponta CPF quando a mensagem cita CPF', () => {
    expect(escolherCampoDoConflito('Já existe um cliente ativo com este CPF.')).toBe('cpf');
  });

  it('aponta cnhNumero quando a mensagem cita CNH', () => {
    expect(escolherCampoDoConflito('Já existe um cliente ativo com este número de CNH.')).toBe('cnhNumero');
  });

  it('aponta email com a mensagem exata do backend', () => {
    expect(escolherCampoDoConflito('Já existe um cliente ativo com este e-mail.')).toBe('email');
  });

  it('reconhece e-mail com ou sem hífen e em qualquer caixa', () => {
    expect(escolherCampoDoConflito('E-MAIL já cadastrado.')).toBe('email');
    expect(escolherCampoDoConflito('Este email já existe.')).toBe('email');
  });

  it('devolve null para outra mensagem (vai para o alerta geral)', () => {
    expect(escolherCampoDoConflito('Não foi possível concluir a operação.')).toBeNull();
  });

  it('cada mensagem de duplicidade do front mapeia de volta ao campo certo', () => {
    expect(escolherCampoDoConflito(MENSAGEM_CADASTRO_CPF_JA_CADASTRADO)).toBe('cpf');
    expect(escolherCampoDoConflito(MENSAGEM_CADASTRO_EMAIL_JA_CADASTRADO)).toBe('email');
    expect(escolherCampoDoConflito(MENSAGEM_CADASTRO_CNH_JA_CADASTRADA)).toBe('cnhNumero');
  });

  it('as mensagens de duplicidade têm o texto exato do backend', () => {
    expect(MENSAGEM_CADASTRO_CPF_JA_CADASTRADO).toBe('Já existe um cliente ativo com este CPF.');
    expect(MENSAGEM_CADASTRO_EMAIL_JA_CADASTRADO).toBe('Já existe um cliente ativo com este e-mail.');
    expect(MENSAGEM_CADASTRO_CNH_JA_CADASTRADA).toBe('Já existe um cliente ativo com este número de CNH.');
  });
});

describe('primeiroCampoNaTela', () => {
  it('escolhe o campo mais alto na tela, mesmo que a lista venha em outra ordem', () => {
    expect(primeiroCampoNaTela(['senha', 'cep', 'nome'])).toBe('nome');
    expect(primeiroCampoNaTela(['uf', 'cpf'])).toBe('cpf');
  });

  it('devolve undefined sem campos', () => {
    expect(primeiroCampoNaTela([])).toBeUndefined();
  });
});

describe('acaoAposEnvio (o que a página faz com o resultado)', () => {
  const concluido: ResultadoCadastro = { tipo: 'concluido', idCliente: 7, enviados: {} };
  const falhaDocumento: ResultadoCadastro = {
    tipo: 'erro-documento',
    documento: 'COMPROVANTE_RESIDENCIA',
    erro: new Error('502'),
    idCliente: 7,
    enviados: { CNH },
  };

  it('cadastro novo que terminou vai para o início', () => {
    expect(acaoAposEnvio(concluido, true)).toBe('ir-inicio');
  });

  it('F2 e F4 (só enviar) que terminaram ficam na tela, com o aviso, e não vão para o início', () => {
    expect(acaoAposEnvio(concluido, false)).toBe('avisar-concluido');
  });

  it('falha de um documento fica na tela (F2), inclusive no cadastro novo: não manda para o início', () => {
    expect(acaoAposEnvio(falhaDocumento, true)).toBe('ficar-com-falha');
    expect(acaoAposEnvio(falhaDocumento, false)).toBe('ficar-com-falha');
  });

  it('login automático que falhou vai para entrar; erro do POST fica no formulário', () => {
    expect(acaoAposEnvio({ tipo: 'sem-login' }, true)).toBe('ir-entrar');
    expect(acaoAposEnvio({ tipo: 'erro-cadastro', erro: new Error('x') }, true)).toBe('erro-cadastro');
  });
});

describe('executarCadastro', () => {
  it('POST falha: login e documentos não são tentados e o erro volta para a página', async () => {
    const erro = new Error('409');
    const { dependencias, entrar, enviarDocumento } = criarDublês({ cadastroFalha: erro });

    const resultado = await executarCadastro(dependencias, entradaCompleta());

    expect(resultado).toEqual({ tipo: 'erro-cadastro', erro });
    expect(entrar).not.toHaveBeenCalled();
    expect(enviarDocumento).not.toHaveBeenCalled();
  });

  it('sequência completa: cadastro, login, CNH e comprovante, nesta ordem', async () => {
    const ordem: string[] = [];
    const { dependencias } = criarDublês({});
    dependencias.cadastrar.mockImplementation(async () => {
      ordem.push('cadastro');
      return RESPOSTA;
    });
    dependencias.entrar.mockImplementation(async () => {
      ordem.push('login');
      return {};
    });
    dependencias.enviarDocumento.mockImplementation(async (_id, tipo) => {
      ordem.push(tipo);
      return respostaDocumento(tipo);
    });

    const resultado = await executarCadastro(dependencias, entradaCompleta());

    expect(ordem).toEqual(['cadastro', 'login', 'CNH', 'COMPROVANTE_RESIDENCIA']);
    expect(resultado).toEqual({
      tipo: 'concluido',
      idCliente: 7,
      enviados: { CNH, COMPROVANTE_RESIDENCIA: COMPROVANTE },
    });
  });

  it('cada documento vai ao idCliente devolvido pelo POST, com o arquivo que a pessoa escolheu', async () => {
    const { dependencias, enviarDocumento } = criarDublês({});

    await executarCadastro(dependencias, entradaCompleta());

    expect(enviarDocumento).toHaveBeenNthCalledWith(1, 7, 'CNH', CNH);
    expect(enviarDocumento).toHaveBeenNthCalledWith(2, 7, 'COMPROVANTE_RESIDENCIA', COMPROVANTE);
  });

  it('POST ok e login falha: devolve sem-login, o POST foi chamado UMA vez e nada foi enviado', async () => {
    const { dependencias, cadastrar, entrar, enviarDocumento } = criarDublês({ loginFalha: true });

    const resultado = await executarCadastro(dependencias, entradaCompleta());

    expect(resultado).toEqual({ tipo: 'sem-login' });
    expect(cadastrar).toHaveBeenCalledTimes(1);
    expect(entrar).toHaveBeenCalledTimes(1);
    expect(enviarDocumento).not.toHaveBeenCalled();
  });

  it('login automático usa perfil CLIENTE e CPF só com dígitos', async () => {
    const { dependencias, entrar } = criarDublês({});

    await executarCadastro(dependencias, entradaCompleta());

    expect(entrar).toHaveBeenCalledWith({
      perfil: 'CLIENTE',
      cpf: '52998224725',
      senha: 'Senha1234',
    });
  });

  it('o POST recebe o payload sem a confirmação de senha', async () => {
    const { dependencias, cadastrar } = criarDublês({});

    await executarCadastro(dependencias, entradaCompleta());

    const [requisicao] = cadastrar.mock.calls[0];
    expect(Object.keys(requisicao)).not.toContain('confirmarSenha');
  });

  it('falha de conversão (categoria vazia) é tratada como erro do cadastro, sem POST', async () => {
    const { dependencias, cadastrar, entrar } = criarDublês({});

    const resultado = await executarCadastro(dependencias, entradaCompleta({ valores: { ...FORMULARIO, cnhCategoria: '' } }));

    expect(resultado.tipo).toBe('erro-cadastro');
    expect(cadastrar).not.toHaveBeenCalled();
    expect(entrar).not.toHaveBeenCalled();
  });

  it('falha na CNH para antes do comprovante e devolve o id e o que já subiu (nenhum)', async () => {
    const { dependencias, enviarDocumento } = criarDublês({ documentoFalha: 'CNH' });

    const resultado = await executarCadastro(dependencias, entradaCompleta());

    expect(resultado).toEqual({
      tipo: 'erro-documento',
      documento: 'CNH',
      erro: expect.any(Error),
      idCliente: 7,
      enviados: {},
    });
    expect(enviarDocumento).toHaveBeenCalledTimes(1);
  });

  it('falha no comprovante: a CNH já enviada fica registrada em enviados', async () => {
    const { dependencias } = criarDublês({ documentoFalha: 'COMPROVANTE_RESIDENCIA' });

    const resultado = await executarCadastro(dependencias, entradaCompleta());

    expect(resultado).toMatchObject({
      tipo: 'erro-documento',
      documento: 'COMPROVANTE_RESIDENCIA',
      enviados: { CNH },
    });
  });

  it('nova tentativa não refaz o cadastro nem o login e pula a CNH que já subiu', async () => {
    const primeira = criarDublês({ documentoFalha: 'COMPROVANTE_RESIDENCIA' });
    const falhou = await executarCadastro(primeira.dependencias, entradaCompleta());
    if (falhou.tipo !== 'erro-documento') throw new Error('esperava falha de documento');

    const segunda = criarDublês({});
    const resultado = await executarCadastro(segunda.dependencias, {
      valores: null,
      idCliente: falhou.idCliente,
      arquivos: { CNH, COMPROVANTE_RESIDENCIA: COMPROVANTE },
      enviados: falhou.enviados,
    });

    expect(segunda.cadastrar).not.toHaveBeenCalled();
    expect(segunda.entrar).not.toHaveBeenCalled();
    expect(segunda.enviarDocumento).toHaveBeenCalledTimes(1);
    expect(segunda.enviarDocumento).toHaveBeenCalledWith(7, 'COMPROVANTE_RESIDENCIA', COMPROVANTE);
    expect(resultado).toEqual({
      tipo: 'concluido',
      idCliente: 7,
      enviados: { CNH, COMPROVANTE_RESIDENCIA: COMPROVANTE },
    });
  });

  it('só enviar (cliente logado, sem dados do cadastro): envia apenas o que foi escolhido', async () => {
    const { dependencias, cadastrar, entrar, enviarDocumento } = criarDublês({});

    const resultado = await executarCadastro(dependencias, {
      valores: null,
      idCliente: 9,
      arquivos: { COMPROVANTE_RESIDENCIA: COMPROVANTE },
      enviados: {},
    });

    expect(cadastrar).not.toHaveBeenCalled();
    expect(entrar).not.toHaveBeenCalled();
    expect(enviarDocumento).toHaveBeenCalledWith(9, 'COMPROVANTE_RESIDENCIA', COMPROVANTE);
    expect(resultado).toEqual({ tipo: 'concluido', idCliente: 9, enviados: { COMPROVANTE_RESIDENCIA: COMPROVANTE } });
  });

  it('troca de arquivo: a CNH nova sobe e o comprovante já enviado não sobe de novo', async () => {
    const { dependencias, enviarDocumento } = criarDublês({});

    await executarCadastro(dependencias, {
      valores: null,
      idCliente: 9,
      arquivos: { CNH: CNH_NOVA, COMPROVANTE_RESIDENCIA: COMPROVANTE },
      enviados: { CNH, COMPROVANTE_RESIDENCIA: COMPROVANTE },
    });

    expect(enviarDocumento).toHaveBeenCalledTimes(1);
    expect(enviarDocumento).toHaveBeenCalledWith(9, 'CNH', CNH_NOVA);
  });

  it('sem id do cliente e sem POST, devolve erro do cadastro (nunca envia documento sem dono)', async () => {
    const { dependencias, enviarDocumento } = criarDublês({});

    const resultado = await executarCadastro(dependencias, {
      valores: null,
      idCliente: null,
      arquivos: { CNH },
      enviados: {},
    });

    expect(resultado.tipo).toBe('erro-cadastro');
    expect(enviarDocumento).not.toHaveBeenCalled();
  });
});
