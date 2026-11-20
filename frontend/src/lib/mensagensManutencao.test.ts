import { describe, expect, it } from 'vitest';
import {
  MENSAGEM_MANUTENCAO_TITULO,
  mensagemManutencaoCicloAtingido,
  mensagemManutencaoCicloLimite,
  mensagemManutencaoEncerrarTitulo,
  mensagemManutencaoFaltam,
  mensagemManutencaoFaltamApenas,
  mensagemManutencaoIntervalo,
  mensagemManutencaoOrdensAtivas,
  mensagemManutencaoOs,
  mensagemManutencaoPassouLimite,
  mensagemManutencaoSeloAlertas,
} from './mensagensManutencao';
import * as mensagens from './mensagens';

describe('textos da manutenção', () => {
  it('o título da página é o do desenho', () => {
    expect(MENSAGEM_MANUTENCAO_TITULO).toBe('Manutenção da Frota');
  });

  it('são reexportados por @/lib/mensagens', () => {
    expect(mensagens.MENSAGEM_MANUTENCAO_TITULO).toBe(MENSAGEM_MANUTENCAO_TITULO);
    expect(mensagens.mensagemManutencaoOs).toBe(mensagemManutencaoOs);
  });
});

describe('selo de alertas: singular e plural', () => {
  it('um alerta: "1 Requer Atenção"', () => {
    expect(mensagemManutencaoSeloAlertas(1, '1')).toBe('1 Requer Atenção');
  });

  it('mais de um: "{n} Requerem Atenção" com o número já formatado', () => {
    expect(mensagemManutencaoSeloAlertas(4, '4')).toBe('4 Requerem Atenção');
    expect(mensagemManutencaoSeloAlertas(1200, '1.200')).toBe('1.200 Requerem Atenção');
  });
});

describe('métricas: "1 ordem" ou "{n} ordens"', () => {
  it('só exatamente uma ordem usa o singular', () => {
    expect(mensagemManutencaoOrdensAtivas(1, '1')).toBe('1 ordem');
    expect(mensagemManutencaoOrdensAtivas(0, '0')).toBe('0 ordens');
    expect(mensagemManutencaoOrdensAtivas(18, '18')).toBe('18 ordens');
  });
});

describe('textos com número', () => {
  it('intervalo, faltam e ciclo', () => {
    expect(mensagemManutencaoIntervalo('10.000')).toBe('Intervalo Padrão: 10.000 km');
    expect(mensagemManutencaoFaltam('1.060')).toBe('Faltam 1.060 km');
    expect(mensagemManutencaoFaltamApenas('180')).toBe('Faltam apenas 180 km!');
    expect(mensagemManutencaoPassouLimite('320')).toBe('Passou 320 km do limite');
    expect(mensagemManutencaoCicloAtingido('89')).toBe('89% do ciclo atingido');
    expect(mensagemManutencaoCicloLimite('98')).toBe('98% do ciclo limite');
  });

  it('código da OS e título do modal de encerrar', () => {
    expect(mensagemManutencaoOs(8841)).toBe('OS #8841');
    expect(mensagemManutencaoEncerrarTitulo(8841)).toBe('Encerrar ordem #8841');
  });
});
