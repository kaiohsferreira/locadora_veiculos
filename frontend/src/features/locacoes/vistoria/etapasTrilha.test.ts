import { describe, expect, it } from 'vitest';
import { etapasDaTrilha, rotuloDaEtapa } from './etapasTrilha';
import { modeloLinhaAvaria, tituloDoAngulo } from './modeloLinhaAvaria';
import { numerarAvarias } from './avariasVistoria';
import type { AvariaRegistrada, DadosNovaAvaria } from '@/types/avarias';
import { MENSAGEM_AVARIA_SEM_ANGULO } from '@/lib/mensagensVistoriaAvarias';

describe('trilha da tela 19', () => {
  it('sem vistoria: etapa 2 em andamento e etapa 1 concluída', () => {
    const etapas = etapasDaTrilha(false);
    expect(etapas.map((etapa) => etapa.situacao)).toEqual(['concluida', 'atual', 'futura', 'futura']);
    expect(rotuloDaEtapa(etapas[0])).toBe('Etapa 1 • Concluído');
    expect(rotuloDaEtapa(etapas[1])).toBe('Etapa 2 • Em Andamento');
    expect(rotuloDaEtapa(etapas[2])).toBe('Etapa 3');
  });

  it('com vistoria: etapa 2 concluída e etapa 3 atual, para avançar ao contrato', () => {
    const etapas = etapasDaTrilha(true);
    expect(etapas.map((etapa) => etapa.situacao)).toEqual(['concluida', 'concluida', 'atual', 'futura']);
    expect(rotuloDaEtapa(etapas[1])).toBe('Etapa 2 • Concluído');
  });
});

const DADOS: DadosNovaAvaria = {
  descricao: 'Amassado', gravidade: 'GRAVE', valorEstimado: '350.50', angulo: 'TRASEIRA', posX: '10.00', posY: '20.00',
  foto: new File(['x'], 'foto.jpg', { type: 'image/jpeg' }),
};

const DO_SERVIDOR: AvariaRegistrada = {
  idAvaria: 3, idVistoria: 1, descricao: 'Risco', gravidade: 'LEVE', valorEstimado: null, angulo: null,
  posX: null, posY: null, foto: null, erroFoto: 'x',
};

describe('modelo da linha de avaria', () => {
  it('título é o rótulo do ângulo; sem ângulo, o texto aprovado "Sem ângulo"', () => {
    expect(tituloDoAngulo('LATERAL_DIREITA')).toBe('Lateral Direita');
    expect(tituloDoAngulo(null)).toBe(MENSAGEM_AVARIA_SEM_ANGULO);
  });

  it('linha local: valor vira número, situação e botão de repetir', () => {
    const [linha] = numerarAvarias([], [{ chave: 4, dados: DADOS, previa: 'blob:4', situacao: 'erro', idAvaria: null }]);
    expect(modeloLinhaAvaria(linha)).toMatchObject({
      numero: 1, titulo: 'Traseira', valorEstimado: 350.5, situacao: 'erro', podeRepetir: true, chaveLocal: 4, semArquivo: false,
    });
  });

  it('linha do servidor sem foto: marca a falta de arquivo, sem botão de repetir', () => {
    const [linha] = numerarAvarias([DO_SERVIDOR], []);
    expect(modeloLinhaAvaria(linha)).toMatchObject({
      titulo: MENSAGEM_AVARIA_SEM_ANGULO, semArquivo: true, urlFoto: null, podeRepetir: false, idAvaria: 3, valorEstimado: null,
    });
  });
});
