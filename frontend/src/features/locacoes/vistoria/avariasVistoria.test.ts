import { describe, expect, it } from 'vitest';
import type { AvariaRegistrada, DadosNovaAvaria } from '@/types/avarias';
import {
  contarPorAngulo,
  decidirFalhaDaImagem,
  kmAcimaDaRetirada,
  numerarAvarias,
  podeTentarDeNovo,
  temMarcadorParaRedefinir,
  textoSituacaoItemLocal,
  tirarMarcadores,
  totalEstimado,
  type ItemAvariaLocal,
} from './avariasVistoria';
import {
  MENSAGEM_AVARIA_FOTO_PENDENTE,
  mensagemAvariasMapeadas,
  mensagemContagemQuadrante,
} from '@/lib/mensagensVistoriaAvarias';
import { MENSAGEM_FOTOS_AGUARDANDO, MENSAGEM_FOTOS_ERRO_ENVIO } from '@/lib/mensagensFotosVeiculo';

function avaria(id: number, angulo: AvariaRegistrada['angulo'], valor: number | null = null): AvariaRegistrada {
  return {
    idAvaria: id, idVistoria: 1, descricao: `Avaria ${id}`, gravidade: 'LEVE', valorEstimado: valor,
    angulo, posX: 10, posY: 20, foto: null, erroFoto: null,
  };
}

function dados(descricao: string): DadosNovaAvaria {
  return {
    descricao, gravidade: 'MEDIA', valorEstimado: null, angulo: 'FRENTE', posX: '10.00', posY: '20.00',
    foto: new File(['x'], 'foto.jpg', { type: 'image/jpeg' }),
  };
}

function local(chave: number, situacao: ItemAvariaLocal['situacao'], idAvaria: number | null = null): ItemAvariaLocal {
  return { chave, dados: dados(`Local ${chave}`), previa: `blob:${chave}`, situacao, idAvaria };
}

describe('contagens e totais', () => {
  it('conta avarias por quadrante e ignora as sem ângulo', () => {
    expect(contarPorAngulo([avaria(1, 'FRENTE'), avaria(2, 'FRENTE'), avaria(3, null), avaria(4, 'LATERAL_DIREITA')])).toEqual({
      FRENTE: 2, TRASEIRA: 0, LATERAL_ESQUERDA: 0, LATERAL_DIREITA: 1,
    });
  });

  it('soma os custos e ignora o que não foi informado', () => {
    expect(totalEstimado([180, null, 350.5])).toBe(530.5);
    expect(totalEstimado([])).toBe(0);
  });

  it('textos de contagem: desenho para zero e um, e número para mais', () => {
    expect(mensagemContagemQuadrante(0)).toBe('Sem Avarias');
    expect(mensagemContagemQuadrante(1)).toBe('1 Avaria');
    expect(mensagemContagemQuadrante(3)).toBe('3 Avarias');
    expect(mensagemAvariasMapeadas(3)).toBe('3 Avarias Mapeadas');
    expect(mensagemAvariasMapeadas(1)).toBe('1 Avaria Mapeada');
  });
});

describe('quilometragem da vistoria', () => {
  it('mostra a diferença quando a quilometragem é igual ou maior que a da retirada', () => {
    expect(kmAcimaDaRetirada(12018, 12000)).toBe(18);
    expect(kmAcimaDaRetirada(12000, 12000)).toBe(0);
  });

  it('abaixo da retirada não há diferença a mostrar', () => {
    expect(kmAcimaDaRetirada(11999, 12000)).toBeNull();
  });
});

describe('situação dos itens locais', () => {
  it('textos e botão de repetir: foto pendente e erro podem ser repetidos', () => {
    expect(textoSituacaoItemLocal('aguardando')).toBe(MENSAGEM_FOTOS_AGUARDANDO);
    expect(textoSituacaoItemLocal('erro')).toBe(MENSAGEM_FOTOS_ERRO_ENVIO);
    expect(textoSituacaoItemLocal('foto_pendente')).toBe(MENSAGEM_AVARIA_FOTO_PENDENTE);
    expect(podeTentarDeNovo('foto_pendente')).toBe(true);
    expect(podeTentarDeNovo('erro')).toBe(true);
    expect(podeTentarDeNovo('aguardando')).toBe(false);
    expect(podeTentarDeNovo('enviando')).toBe(false);
  });
});

describe('numerarAvarias', () => {
  it('numera as do servidor primeiro e depois as locais, em sequência', () => {
    const linhas = numerarAvarias([avaria(7, 'FRENTE'), avaria(9, 'TRASEIRA')], [local(1, 'aguardando')]);
    expect(linhas.map((linha) => [linha.numero, linha.origem])).toEqual([[1, 'servidor'], [2, 'servidor'], [3, 'local']]);
  });

  it('uma avaria gravada sem foto aparece só como local, para o botão de repetir a foto', () => {
    const linhas = numerarAvarias([avaria(7, 'FRENTE'), avaria(8, 'TRASEIRA')], [local(1, 'foto_pendente', 8)]);
    expect(linhas.map((linha) => [linha.origem, linha.avaria?.idAvaria ?? null, linha.local?.idAvaria ?? null])).toEqual([
      ['servidor', 7, null],
      ['local', null, 8],
    ]);
  });

  it('lista vazia gera lista vazia', () => {
    expect(numerarAvarias([], [])).toEqual([]);
  });
});

describe('Redefinir Marcadores e falha da imagem', () => {
  const DADOS_COM_MARCA: DadosNovaAvaria = {
    descricao: 'Risco', gravidade: 'LEVE', valorEstimado: '90.00', angulo: 'FRENTE', posX: '10.00', posY: '20.00',
    foto: new File(['x'], 'foto.jpg', { type: 'image/jpeg' }),
  };

  function itemLocal(chave: number, situacao: ItemAvariaLocal['situacao'], idAvaria: number | null, dados: DadosNovaAvaria): ItemAvariaLocal {
    return { chave, dados, previa: `blob:${chave}`, situacao, idAvaria };
  }

  it('tirarMarcadores zera ângulo e posição juntos e mantém o resto da avaria', () => {
    const semMarca = tirarMarcadores(DADOS_COM_MARCA);
    expect(semMarca.angulo).toBeNull();
    expect(semMarca.posX).toBeNull();
    expect(semMarca.posY).toBeNull();
    expect(semMarca.descricao).toBe('Risco');
    expect(semMarca.valorEstimado).toBe('90.00');
    expect(DADOS_COM_MARCA.angulo).toBe('FRENTE');
  });

  it('o botão Redefinir Marcadores só aparece com avaria ainda não gravada que tem ângulo', () => {
    expect(temMarcadorParaRedefinir([itemLocal(1, 'aguardando', null, DADOS_COM_MARCA)])).toBe(true);
    expect(temMarcadorParaRedefinir([itemLocal(1, 'aguardando', null, tirarMarcadores(DADOS_COM_MARCA))])).toBe(false);
    expect(temMarcadorParaRedefinir([itemLocal(1, 'foto_pendente', 9, DADOS_COM_MARCA)])).toBe(false);
    expect(temMarcadorParaRedefinir([])).toBe(false);
  });

  it('a primeira falha da imagem pede a lista de novo; a segunda vira o erro aprovado', () => {
    expect(decidirFalhaDaImagem(false)).toBe('recarregar_lista');
    expect(decidirFalhaDaImagem(true)).toBe('mostrar_erro');
  });
});
