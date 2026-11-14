import { describe, expect, it } from 'vitest';
import {
  etapaEmAndamento,
  montarPassos,
  prepararPrimeiroAcesso,
  progressoAtivacao,
} from './primeirosPassos';

describe('montarPassos', () => {
  it('sem veículo (tela 40): conta concluída e o cadastro do veículo como próximo passo', () => {
    const passos = montarPassos(true);

    expect(passos.map((passo) => passo.id)).toEqual(['conta', 'veiculo']);
    expect(passos[0]).toMatchObject({ numero: 1, situacao: 'concluido', titulo: 'Conta criada' });
    expect(passos[1]).toMatchObject({ numero: 2, situacao: 'proximo', titulo: 'Cadastre o primeiro veículo' });
  });

  it('com veículo: os dois passos concluídos', () => {
    const passos = montarPassos(false);

    expect(passos.every((passo) => passo.situacao === 'concluido')).toBe(true);
  });

  it('não traz os passos fora desta fatia (dados da locadora, publicação no catálogo, verificação)', () => {
    const titulos = montarPassos(true).map((passo) => passo.titulo.toLowerCase());

    expect(titulos.join(' | ')).not.toMatch(/dados da locadora|publique|catálogo|verificação/);
  });
});

describe('progressoAtivacao', () => {
  it('sem veículo: 1 de 2 concluído, 1 restante e 50%', () => {
    expect(progressoAtivacao(montarPassos(true))).toEqual({
      concluidos: 1,
      total: 2,
      restantes: 1,
      percentual: 50,
    });
  });

  it('com veículo: 2 de 2, nada restante e 100%', () => {
    expect(progressoAtivacao(montarPassos(false))).toEqual({
      concluidos: 2,
      total: 2,
      restantes: 0,
      percentual: 100,
    });
  });

  it('lista vazia não divide por zero', () => {
    expect(progressoAtivacao([])).toEqual({ concluidos: 0, total: 0, restantes: 0, percentual: 0 });
  });
});

describe('etapaEmAndamento', () => {
  it('sem veículo: a etapa 2 está em andamento', () => {
    expect(etapaEmAndamento(montarPassos(true))).toBe(2);
  });

  it('com tudo concluído: devolve o último passo', () => {
    expect(etapaEmAndamento(montarPassos(false))).toBe(2);
  });
});

describe('prepararPrimeiroAcesso', () => {
  it('junta passos e progresso a partir da flag', () => {
    const preparado = prepararPrimeiroAcesso(true);

    expect(preparado.passos).toHaveLength(2);
    expect(preparado.progresso.concluidos).toBe(1);
    expect(preparado.progresso.total).toBe(2);
  });
});
