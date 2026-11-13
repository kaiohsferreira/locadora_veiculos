import { describe, expect, it } from 'vitest';
import { calcularCamposValidos } from '@/features/clientes/cadastro/etapasCadastro';
import {
  CAMPO_API_POR_CAMPO_FORMULARIO,
  combinarValidosComVerificacao,
  deveBloquearContinuar,
  estadoVigente,
  mensagemDeDuplicidade,
  interpretarRespostaVerificacao,
  normalizarValorVerificacao,
  OPCOES_CONSULTA_VERIFICACAO,
  precisaVerificarAgora,
  resultadoVigente,
  valorVerificavel,
  verificarComFalhaAberta,
  type EstadoVerificacao,
  type RegistroVerificacao,
} from '@/features/clientes/cadastro/verificacaoCadastro';
import {
  MENSAGEM_CADASTRO_CNH_JA_CADASTRADA,
  MENSAGEM_CADASTRO_CPF_JA_CADASTRADO,
  MENSAGEM_CADASTRO_EMAIL_JA_CADASTRADO,
} from '@/lib/mensagens';

const ETAPA_1_VALIDA = {
  nome: 'Mariana Ribeiro de Souza',
  cpf: '529.982.247-25',
  dataNascimento: '1992-08-14',
  email: 'mariana@email.com',
  telefone: '(11) 98765-4321',
};

describe('normalizarValorVerificacao', () => {
  it('CPF e CNH ficam só com dígitos', () => {
    expect(normalizarValorVerificacao('cpf', '529.982.247-25')).toBe('52998224725');
    expect(normalizarValorVerificacao('cnhNumero', ' 123 456 789-01 ')).toBe('12345678901');
  });

  it('e-mail perde os espaços das pontas e vai para minúsculas', () => {
    expect(normalizarValorVerificacao('email', '  Mariana@Email.COM ')).toBe('mariana@email.com');
  });
});

describe('valorVerificavel', () => {
  it('devolve o valor normalizado quando o campo passa no Zod', () => {
    expect(valorVerificavel('cpf', '529.982.247-25')).toBe('52998224725');
    expect(valorVerificavel('email', ' Mariana@Email.com ')).toBe('mariana@email.com');
    expect(valorVerificavel('cnhNumero', '12345678901')).toBe('12345678901');
  });

  it('devolve null para CPF inválido, incompleto ou vazio', () => {
    expect(valorVerificavel('cpf', '111.111.111-11')).toBeNull();
    expect(valorVerificavel('cpf', '529.982.247-2')).toBeNull();
    expect(valorVerificavel('cpf', '')).toBeNull();
  });

  it('devolve null para CNH curta ou vazia', () => {
    expect(valorVerificavel('cnhNumero', '1234567890')).toBeNull();
    expect(valorVerificavel('cnhNumero', '')).toBeNull();
  });

  it('devolve null para e-mail sem formato ou vazio', () => {
    expect(valorVerificavel('email', 'mariana')).toBeNull();
    expect(valorVerificavel('email', 'mariana@')).toBeNull();
    expect(valorVerificavel('email', '   ')).toBeNull();
    expect(valorVerificavel('email', '')).toBeNull();
  });
});

describe('mapa de campos e mensagens', () => {
  it('mapeia o campo do formulário para o campo do contrato', () => {
    expect(CAMPO_API_POR_CAMPO_FORMULARIO).toEqual({ cpf: 'CPF', email: 'EMAIL', cnhNumero: 'CNH' });
  });

  it('mensagemDeDuplicidade devolve o texto do campo', () => {
    expect(mensagemDeDuplicidade('cpf')).toBe(MENSAGEM_CADASTRO_CPF_JA_CADASTRADO);
    expect(mensagemDeDuplicidade('email')).toBe(MENSAGEM_CADASTRO_EMAIL_JA_CADASTRADO);
    expect(mensagemDeDuplicidade('cnhNumero')).toBe(MENSAGEM_CADASTRO_CNH_JA_CADASTRADA);
  });
});

describe('resultadoVigente e estadoVigente', () => {
  const registro: RegistroVerificacao = { valor: '52998224725', estado: 'existe' };

  it('vale para o mesmo valor, mesmo digitado com máscara', () => {
    expect(resultadoVigente(registro, 'cpf', '529.982.247-25')).toBe('existe');
    expect(estadoVigente(registro, 'cpf', '52998224725')).toBe('existe');
  });

  it('descarta a resposta de um valor antigo', () => {
    expect(resultadoVigente(registro, 'cpf', '111.444.777-35')).toBeNull();
    expect(estadoVigente(registro, 'cpf', '111.444.777-35')).toBe('nao-verificado');
  });

  it('sem registro não há resultado nem estado verificado', () => {
    expect(resultadoVigente(undefined, 'cpf', '529.982.247-25')).toBeNull();
    expect(estadoVigente(undefined, 'cpf', '529.982.247-25')).toBe('nao-verificado');
  });

  it('verificação em andamento ainda não é resultado, mas é o estado "verificando"', () => {
    const andamento: RegistroVerificacao = { valor: '52998224725', estado: 'verificando' };
    expect(resultadoVigente(andamento, 'cpf', '529.982.247-25')).toBeNull();
    expect(estadoVigente(andamento, 'cpf', '529.982.247-25')).toBe('verificando');
  });

  it('compara e-mail sem diferenciar caixa nem espaços das pontas', () => {
    const doEmail: RegistroVerificacao = { valor: 'mariana@email.com', estado: 'livre' };
    expect(resultadoVigente(doEmail, 'email', ' Mariana@Email.com ')).toBe('livre');
    expect(resultadoVigente(doEmail, 'email', 'outra@email.com')).toBeNull();
  });

  it('indisponivel também é um resultado conhecido (não se repete a chamada)', () => {
    const falhou: RegistroVerificacao = { valor: '12345678901', estado: 'indisponivel' };
    expect(resultadoVigente(falhou, 'cnhNumero', '123.456.789-01')).toBe('indisponivel');
  });
});

describe('interpretarRespostaVerificacao', () => {
  it('existe true vira existe e existe false vira livre', () => {
    expect(interpretarRespostaVerificacao({ existe: true })).toBe('existe');
    expect(interpretarRespostaVerificacao({ existe: false })).toBe('livre');
  });

  it('qualquer outra coisa vira indisponivel', () => {
    for (const resposta of [null, undefined, {}, { existe: 'true' }, { existe: 1 }, { existe: null }, 'texto', 42, []]) {
      expect(interpretarRespostaVerificacao(resposta)).toBe('indisponivel');
    }
  });
});

describe('verificarComFalhaAberta', () => {
  it('resposta verdadeira: existe', async () => {
    const r = await verificarComFalhaAberta(async () => ({ existe: true }));
    expect(r).toEqual({ resultado: 'existe', respostaInvalida: false });
  });

  it('resposta falsa: livre', async () => {
    const r = await verificarComFalhaAberta(async () => ({ existe: false }));
    expect(r).toEqual({ resultado: 'livre', respostaInvalida: false });
  });

  it('resposta inválida: indisponivel e marcada como inválida (para sair do cache)', async () => {
    const r = await verificarComFalhaAberta(async () => ({ existe: 'sim' }));
    expect(r).toEqual({ resultado: 'indisponivel', respostaInvalida: true });
  });

  it('chamada que lança vira indisponivel, sem lançar', async () => {
    const r = await verificarComFalhaAberta(async () => {
      throw new Error('falha');
    });
    expect(r).toEqual({ resultado: 'indisponivel', respostaInvalida: false });
  });

  it('erro lançado de forma síncrona dentro de buscar também vira indisponivel', async () => {
    const r = await verificarComFalhaAberta(() => {
      throw new Error('falha');
    });
    expect(r.resultado).toBe('indisponivel');
  });
});

describe('OPCOES_CONSULTA_VERIFICACAO', () => {
  it('sem nova tentativa automática e com 5 minutos de memória', () => {
    expect(OPCOES_CONSULTA_VERIFICACAO.retry).toBe(false);
    expect(OPCOES_CONSULTA_VERIFICACAO.staleTime).toBe(300_000);
    expect(OPCOES_CONSULTA_VERIFICACAO.gcTime).toBe(300_000);
  });
});

describe('precisaVerificarAgora', () => {
  const cpf = '529.982.247-25';
  const doValorAtual = (estado: RegistroVerificacao['estado']): RegistroVerificacao => ({
    valor: '52998224725',
    estado,
  });

  it('sem registro: verifica', () => {
    expect(precisaVerificarAgora(undefined, 'cpf', cpf)).toBe(true);
  });

  it('indisponivel do valor atual: verifica de novo', () => {
    expect(precisaVerificarAgora(doValorAtual('indisponivel'), 'cpf', cpf)).toBe(true);
  });

  it('livre ou existe do valor atual: não verifica', () => {
    expect(precisaVerificarAgora(doValorAtual('livre'), 'cpf', cpf)).toBe(false);
    expect(precisaVerificarAgora(doValorAtual('existe'), 'cpf', cpf)).toBe(false);
  });

  it('em andamento: não inicia outra (aproveita a chamada)', () => {
    expect(precisaVerificarAgora(doValorAtual('verificando'), 'cpf', cpf)).toBe(false);
  });

  it('registro de valor antigo: verifica', () => {
    expect(precisaVerificarAgora(doValorAtual('livre'), 'cpf', '111.444.777-35')).toBe(true);
    expect(precisaVerificarAgora(doValorAtual('existe'), 'cpf', '111.444.777-35')).toBe(true);
  });
});

describe('combinarValidosComVerificacao', () => {
  const validos = calcularCamposValidos(ETAPA_1_VALIDA);

  function combinar(cpf: EstadoVerificacao, email: EstadoVerificacao) {
    return combinarValidosComVerificacao(validos, { cpf, email });
  }

  it('sem ícone enquanto verifica', () => {
    const resultado = combinar('verificando', 'verificando');
    expect(resultado.cpf).toBe(false);
    expect(resultado.email).toBe(false);
  });

  it('com ícone quando livre', () => {
    const resultado = combinar('livre', 'livre');
    expect(resultado.cpf).toBe(true);
    expect(resultado.email).toBe(true);
  });

  it('com ícone quando a verificação falhou (indisponivel)', () => {
    const resultado = combinar('indisponivel', 'indisponivel');
    expect(resultado.cpf).toBe(true);
    expect(resultado.email).toBe(true);
  });

  it('sem ícone quando já existe', () => {
    const resultado = combinar('existe', 'existe');
    expect(resultado.cpf).toBe(false);
    expect(resultado.email).toBe(false);
  });

  it('sem ícone enquanto o valor aprovado pelo esquema ainda não foi verificado', () => {
    const resultado = combinar('nao-verificado', 'nao-verificado');
    expect(resultado.cpf).toBe(false);
    expect(resultado.email).toBe(false);
  });

  it('sem ícone quando o esquema rejeita, seja qual for o estado', () => {
    const invalidos = calcularCamposValidos({ ...ETAPA_1_VALIDA, cpf: '123', email: 'x' });
    for (const estado of ['nao-verificado', 'verificando', 'livre', 'indisponivel', 'existe'] as const) {
      const resultado = combinarValidosComVerificacao(invalidos, { cpf: estado, email: estado });
      expect(resultado.cpf).toBe(false);
      expect(resultado.email).toBe(false);
    }
  });

  it('cada campo segue o seu estado e os outros campos não mudam', () => {
    const resultado = combinar('existe', 'livre');
    expect(resultado.cpf).toBe(false);
    expect(resultado.email).toBe(true);
    expect(resultado.nome).toBe(validos.nome);
    expect(resultado.dataNascimento).toBe(validos.dataNascimento);
    expect(resultado.telefone).toBe(validos.telefone);
  });
});

describe('deveBloquearContinuar', () => {
  it('bloqueia só diante de existe', () => {
    expect(deveBloquearContinuar('existe')).toBe(true);
    expect(deveBloquearContinuar('livre')).toBe(false);
    expect(deveBloquearContinuar('indisponivel')).toBe(false);
    expect(deveBloquearContinuar(null)).toBe(false);
  });
});
