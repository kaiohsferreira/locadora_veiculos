import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import type { AvariaRegistrada, DadosNovaAvaria } from '@/types/avarias';
import { MENSAGEM_FOTOS_ERRO_ENVIO, MENSAGEM_FOTOS_TENTAR_DE_NOVO } from '@/lib/mensagensFotosVeiculo';
import { marcarAguardandoComoErro, numerarAvarias, type ItemAvariaLocal } from './avariasVistoria';
import { modeloLinhaAvaria } from './modeloLinhaAvaria';
import { ListaAvarias } from './ListaAvarias';
import { STATUS_SEM_REPETICAO_DO_POST, avariaEhDoEnvio, localizarAvariaDoEnvio } from './sequenciaEnvioAvaria';

const DADOS: DadosNovaAvaria = {
  descricao: 'Amassado', gravidade: 'MEDIA', valorEstimado: '150.50', angulo: 'FRENTE', posX: '42.50', posY: '17.00',
  foto: new File(['x'], 'foto.jpg', { type: 'image/jpeg' }),
};

function gravada(id: number, parcial: Partial<AvariaRegistrada> = {}): AvariaRegistrada {
  return {
    idAvaria: id, idVistoria: 2, descricao: 'Amassado', gravidade: 'MEDIA', valorEstimado: 150.5, angulo: 'FRENTE',
    posX: 42.5, posY: 17, foto: null, erroFoto: null, ...parcial,
  };
}

function local(parcial: Partial<ItemAvariaLocal>): ItemAvariaLocal {
  return { chave: 1, dados: DADOS, previa: 'blob:1', situacao: 'aguardando', idAvaria: null, ...parcial };
}

describe('avariaEhDoEnvio: compara o que o servidor devolveu com o que foi enviado', () => {
  it('mesmos dados, com o valor e a posição convertidos de texto para número', () => {
    expect(avariaEhDoEnvio(gravada(5), DADOS)).toBe(true);
  });

  it('qualquer dado diferente quebra a igualdade', () => {
    expect(avariaEhDoEnvio(gravada(5, { valorEstimado: 99 }), DADOS)).toBe(false);
    expect(avariaEhDoEnvio(gravada(5, { angulo: 'TRASEIRA' }), DADOS)).toBe(false);
    expect(avariaEhDoEnvio(gravada(5, { posX: 1 }), DADOS)).toBe(false);
  });

  it('sem valor e sem posição no envio, o servidor tem que devolver nulo', () => {
    const semExtras: DadosNovaAvaria = { ...DADOS, valorEstimado: null, angulo: null, posX: null, posY: null };
    expect(avariaEhDoEnvio(gravada(5, { valorEstimado: null, angulo: null, posX: null, posY: null }), semExtras)).toBe(true);
    expect(avariaEhDoEnvio(gravada(5), semExtras)).toBe(false);
  });
});

describe('localizarAvariaDoEnvio: o POST que perdeu a resposta só conta se a avaria é nova na lista', () => {
  it('avaria nova com os mesmos dados é a gravada pelo envio', () => {
    const achada = localizarAvariaDoEnvio([1, 2], [gravada(1), gravada(2), gravada(7)], DADOS);
    expect(achada?.idAvaria).toBe(7);
  });

  it('avaria igual que já existia antes do POST NÃO conta (evita engolir uma avaria digitada de novo)', () => {
    expect(localizarAvariaDoEnvio([1, 2], [gravada(1), gravada(2)], DADOS)).toBeNull();
  });

  it('com mais de uma nova igual, pega a de maior id', () => {
    expect(localizarAvariaDoEnvio([1], [gravada(1), gravada(6), gravada(9)], DADOS)?.idAvaria).toBe(9);
  });

  it('nada parecido na lista: nulo, e o item pode repetir o POST', () => {
    expect(localizarAvariaDoEnvio([1], [gravada(1), gravada(3, { descricao: 'Outra' })], DADOS)).toBeNull();
  });
});

describe('marcarAguardandoComoErro: entrada já existe (409), avarias não enviadas', () => {
  it('só as aguardando viram erro; as que já estão em outra situação ficam como estão', () => {
    const resultado = marcarAguardandoComoErro([
      local({ chave: 1, situacao: 'aguardando' }),
      local({ chave: 2, situacao: 'foto_pendente', idAvaria: 9 }),
      local({ chave: 3, situacao: 'enviando' }),
    ]);
    expect(resultado.map((item) => item.situacao)).toEqual(['erro', 'foto_pendente', 'enviando']);
  });
});

describe('modeloLinhaAvaria: motivo da recusa e repetição', () => {
  it('recusa do POST com motivo: o texto do servidor entra no lugar do genérico', () => {
    const [linha] = numerarAvarias([], [local({ situacao: 'erro', mensagemErro: 'A locação deve estar em andamento ou atrasada para lançar avarias.' })]);
    const modelo = modeloLinhaAvaria(linha);
    expect(modelo.mensagemErro).toBe('A locação deve estar em andamento ou atrasada para lançar avarias.');
    expect(modelo.podeRepetir).toBe(true);
  });

  it('recusa 409 (semRepeticao): não oferece repetir', () => {
    const [linha] = numerarAvarias([], [local({ situacao: 'erro', mensagemErro: 'Conflito.', semRepeticao: true })]);
    expect(modeloLinhaAvaria(linha).podeRepetir).toBe(false);
  });
});

describe('ListaAvarias: o texto e o botão da recusa', () => {
  it('mostra o motivo do servidor e não mostra "Tentar de novo" no 409', () => {
    const linhas = numerarAvarias([], [local({ situacao: 'erro', mensagemErro: 'Conflito de regra.', semRepeticao: true })]);
    const html = renderToStaticMarkup(<ListaAvarias linhas={linhas} somenteLeitura={false} enviando={false} aoTentarDeNovo={() => undefined} />);
    expect(html).toContain('Conflito de regra.');
    expect(html).not.toContain(MENSAGEM_FOTOS_ERRO_ENVIO);
    expect(html).not.toContain(MENSAGEM_FOTOS_TENTAR_DE_NOVO);
  });

  it('erro sem motivo continua com o texto genérico e o botão de tentar de novo', () => {
    const linhas = numerarAvarias([], [local({ situacao: 'erro' })]);
    const html = renderToStaticMarkup(<ListaAvarias linhas={linhas} somenteLeitura={false} enviando={false} aoTentarDeNovo={() => undefined} />);
    expect(html).toContain(MENSAGEM_FOTOS_ERRO_ENVIO);
    expect(html).toContain(MENSAGEM_FOTOS_TENTAR_DE_NOVO);
  });
});

describe('recusa 400 do POST da avaria: igual ao 409, sem repetir e com o motivo do servidor', () => {
  it('400 e 409 não se repetem; 404 continua repetível', () => {
    expect(STATUS_SEM_REPETICAO_DO_POST).toContain(400);
    expect(STATUS_SEM_REPETICAO_DO_POST).toContain(409);
    expect(STATUS_SEM_REPETICAO_DO_POST).not.toContain(404);
  });

  it('erro 400 com motivo: mostra o texto do servidor e não mostra Tentar de novo', () => {
    const linhas = numerarAvarias([], [local({ situacao: 'erro', mensagemErro: 'O valor estimado deve ter até 2 casas decimais.', semRepeticao: true })]);
    expect(modeloLinhaAvaria(linhas[0]).podeRepetir).toBe(false);
    const html = renderToStaticMarkup(<ListaAvarias linhas={linhas} somenteLeitura={false} enviando={false} aoTentarDeNovo={() => undefined} />);
    expect(html).toContain('O valor estimado deve ter até 2 casas decimais.');
    expect(html).not.toContain(MENSAGEM_FOTOS_TENTAR_DE_NOVO);
  });
});
