import { describe, expect, it } from 'vitest';
import {
  CAMPO_API_POR_CAMPO_LOCADORA,
  camposBloqueadosAoAvancar,
  cnpjConferidoPelaVerificacao,
  CHAVE_VERIFICACAO_LOCADORA,
  ehCampoVerificavelLocadora,
  estadoVigenteLocadora,
  interpretarRespostaVerificacaoEmpresa,
  mensagemDeDuplicidadeLocadora,
  normalizarValorVerificacaoLocadora,
  precisaConsultarLocadora,
  valorVerificavelLocadora,
  verificarComFalhaAbertaLocadora,
  type RegistroVerificacaoLocadora,
} from './verificacaoLocadora';
import { MENSAGEM_LOCADORA_CNPJ_JA_CADASTRADO, MENSAGEM_LOCADORA_EMAIL_JA_CADASTRADO } from '@/lib/mensagens';

describe('normalização e valor verificável', () => {
  it('CNPJ vai só com dígitos; e-mail sem espaços e em minúsculas', () => {
    expect(normalizarValorVerificacaoLocadora('cnpj', '11.222.333/0001-81')).toBe('11222333000181');
    expect(normalizarValorVerificacaoLocadora('email', '  Contato@RotaSul.com.br ')).toBe('contato@rotasul.com.br');
  });

  it('valor malformado não vai para a API (CNPJ com dígito errado, e-mail sem formato, vazio)', () => {
    expect(valorVerificavelLocadora('cnpj', '11.222.333/0001-82')).toBeNull();
    expect(valorVerificavelLocadora('cnpj', '')).toBeNull();
    expect(valorVerificavelLocadora('email', 'contato.rotasul.com.br')).toBeNull();
    expect(valorVerificavelLocadora('email', '')).toBeNull();
  });

  it('valor bom devolve o valor normalizado', () => {
    expect(valorVerificavelLocadora('cnpj', '11.222.333/0001-81')).toBe('11222333000181');
    expect(valorVerificavelLocadora('email', 'Contato@RotaSul.com.br')).toBe('contato@rotasul.com.br');
  });

  it('o campo do contrato é CNPJ ou EMAIL, e o nome do campo do formulário é checado por tipo', () => {
    expect(CAMPO_API_POR_CAMPO_LOCADORA).toEqual({ cnpj: 'CNPJ', email: 'EMAIL' });
    expect(ehCampoVerificavelLocadora('cnpj')).toBe(true);
    expect(ehCampoVerificavelLocadora('email')).toBe(true);
    expect(ehCampoVerificavelLocadora('razaoSocial')).toBe(false);
  });

  it('a chave de consulta é a família que o provedor apaga ao sair do cadastro', () => {
    expect(CHAVE_VERIFICACAO_LOCADORA).toBe('verificar-empresa');
  });
});

describe('interpretação da resposta (contrato: { existe })', () => {
  it('existe true é existe; existe false é livre', () => {
    expect(interpretarRespostaVerificacaoEmpresa({ existe: true })).toBe('existe');
    expect(interpretarRespostaVerificacaoEmpresa({ existe: false })).toBe('livre');
  });

  it('resposta fora do contrato é indisponível (nunca bloqueia)', () => {
    expect(interpretarRespostaVerificacaoEmpresa(undefined)).toBe('indisponivel');
    expect(interpretarRespostaVerificacaoEmpresa(null)).toBe('indisponivel');
    expect(interpretarRespostaVerificacaoEmpresa({ existe: 'sim' })).toBe('indisponivel');
    expect(interpretarRespostaVerificacaoEmpresa({})).toBe('indisponivel');
  });
});

describe('falha aberta', () => {
  it('resposta com existe false devolve livre', async () => {
    const resultado = await verificarComFalhaAbertaLocadora(async () => ({ existe: false }));

    expect(resultado).toEqual({ resultado: 'livre', respostaInvalida: false });
  });

  it('resposta com existe true devolve existe', async () => {
    const resultado = await verificarComFalhaAbertaLocadora(async () => ({ existe: true }));

    expect(resultado.resultado).toBe('existe');
  });

  it('erro de rede vira indisponível, sem lançar', async () => {
    const resultado = await verificarComFalhaAbertaLocadora(async () => {
      throw new Error('Network Error');
    });

    expect(resultado).toEqual({ resultado: 'indisponivel', respostaInvalida: false });
  });

  it('limite de 429 vira indisponível, sem lançar', async () => {
    const resultado = await verificarComFalhaAbertaLocadora(async () => {
      throw { status: 429, mensagem: 'Muitas requisições.' };
    });

    expect(resultado.resultado).toBe('indisponivel');
  });

  it('cancelamento (AbortError) vira indisponível, sem lançar', async () => {
    const resultado = await verificarComFalhaAbertaLocadora(async () => {
      throw new DOMException('cancelado', 'AbortError');
    });

    expect(resultado.resultado).toBe('indisponivel');
  });

  it('resposta fora do contrato vira indisponível e marca para não guardar em cache', async () => {
    const resultado = await verificarComFalhaAbertaLocadora(async () => ({ outra: 1 }));

    expect(resultado).toEqual({ resultado: 'indisponivel', respostaInvalida: true });
  });
});

describe('precisaConsultarLocadora: valor repetido não consulta de novo', () => {
  const livre: RegistroVerificacaoLocadora = { valor: '11222333000181', estado: 'livre' };
  const existe: RegistroVerificacaoLocadora = { valor: '11222333000181', estado: 'existe' };
  const verificando: RegistroVerificacaoLocadora = { valor: '11222333000181', estado: 'verificando' };
  const indisponivel: RegistroVerificacaoLocadora = { valor: '11222333000181', estado: 'indisponivel' };

  it('sem registro, consulta', () => {
    expect(precisaConsultarLocadora(undefined, '11222333000181')).toBe(true);
  });

  it('registro de outro valor consulta de novo', () => {
    expect(precisaConsultarLocadora(livre, '11444777000161')).toBe(true);
  });

  it('o mesmo valor já livre ou existente não consulta de novo', () => {
    expect(precisaConsultarLocadora(livre, '11222333000181')).toBe(false);
    expect(precisaConsultarLocadora(existe, '11222333000181')).toBe(false);
  });

  it('o mesmo valor com verificação em andamento aproveita a chamada e não consulta de novo', () => {
    expect(precisaConsultarLocadora(verificando, '11222333000181', true)).toBe(false);
  });

  it('indisponível não repete ao sair do campo, mas repete ao avançar', () => {
    expect(precisaConsultarLocadora(indisponivel, '11222333000181')).toBe(false);
    expect(precisaConsultarLocadora(indisponivel, '11222333000181', true)).toBe(true);
  });
});

describe('estado vigente e ícone de conferido', () => {
  it('registro de outro valor conta como não verificado', () => {
    const registro: RegistroVerificacaoLocadora = { valor: 'contato@rotasul.com.br', estado: 'livre' };

    expect(estadoVigenteLocadora(registro, 'email', 'outro@rotasul.com.br')).toBe('nao-verificado');
    expect(estadoVigenteLocadora(registro, 'email', 'Contato@RotaSul.com.br')).toBe('livre');
  });

  it('CNPJ com formato certo e verificação livre ou indisponível mostra o ícone', () => {
    expect(cnpjConferidoPelaVerificacao(true, 'livre')).toBe(true);
    expect(cnpjConferidoPelaVerificacao(true, 'indisponivel')).toBe(true);
  });

  it('CNPJ sem o ícone quando o formato não confere, enquanto verifica, sem verificação ou quando já existe', () => {
    expect(cnpjConferidoPelaVerificacao(false, 'livre')).toBe(false);
    expect(cnpjConferidoPelaVerificacao(true, 'verificando')).toBe(false);
    expect(cnpjConferidoPelaVerificacao(true, 'nao-verificado')).toBe(false);
    expect(cnpjConferidoPelaVerificacao(true, 'existe')).toBe(false);
  });
});

describe('avanço da etapa 1: só existe bloqueia', () => {
  it('existe bloqueia; falha, sem resultado e livre seguem', () => {
    expect(camposBloqueadosAoAvancar('existe', 'livre')).toEqual(['cnpj']);
    expect(camposBloqueadosAoAvancar('livre', 'existe')).toEqual(['email']);
    expect(camposBloqueadosAoAvancar('indisponivel', null)).toEqual([]);
    expect(camposBloqueadosAoAvancar(null, 'livre')).toEqual([]);
  });

  it('com os dois existentes, a lista vem na ordem da tela (CNPJ antes do e-mail)', () => {
    expect(camposBloqueadosAoAvancar('existe', 'existe')).toEqual(['cnpj', 'email']);
  });
});

describe('mensagens de duplicidade (texto do desenho 36)', () => {
  it('CNPJ e e-mail têm a mensagem "já está em uso" do desenho', () => {
    expect(mensagemDeDuplicidadeLocadora('cnpj')).toBe(MENSAGEM_LOCADORA_CNPJ_JA_CADASTRADO);
    expect(mensagemDeDuplicidadeLocadora('email')).toBe(MENSAGEM_LOCADORA_EMAIL_JA_CADASTRADO);
    expect(MENSAGEM_LOCADORA_EMAIL_JA_CADASTRADO).toBe('Este e-mail já está em uso.');
  });
});
