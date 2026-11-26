import { describe, expect, it } from 'vitest';
import { MENSAGENS_CONTRATO_LOCACAO as M } from '@/lib/mensagensContratoLocacao';
import {
  TETO_ASSINATURA_BASE64,
  TETO_ASSINATURA_BYTES,
  aceiteCombustivelAssinado,
  bytesDeBase64,
  dadosDaClausula,
  dimensoesAssinatura,
  ehSemVistoriaEntrada,
  etapasDoContrato,
  exigeAceiteCombustivel,
  horaDaDataHora,
  imagemAssinaturaValida,
  percentualCombustivel,
  problemaAssinaturaPng,
  temCaractereDeControle,
  textoCartaoDiarias,
  textoFranquia,
  valoresPrevistos,
} from './regrasContrato';
import {
  CONTRATO_ASSINADO,
  CONTRATO_PENDENTE,
  DOCUMENTO_CONTRATO,
  DOCUMENTO_SEM_COBRANCAS,
} from './contratoLocacao.fixture';

describe('dimensoesAssinatura', () => {
  it('reduz dentro do teto de 600 por 200 mantendo a proporção', () => {
    expect(dimensoesAssinatura(1200, 400)).toEqual({ largura: 600, altura: 200 });
    expect(dimensoesAssinatura(900, 600)).toEqual({ largura: 300, altura: 200 });
  });

  it('nunca amplia uma imagem que já cabe, e devolve zero para quadro vazio', () => {
    expect(dimensoesAssinatura(300, 100)).toEqual({ largura: 300, altura: 100 });
    expect(dimensoesAssinatura(0, 200)).toEqual({ largura: 0, altura: 0 });
  });
});

describe('bytesDeBase64', () => {
  it('desconta o preenchimento e ignora quebras de linha', () => {
    expect(bytesDeBase64('AAAA')).toBe(3);
    expect(bytesDeBase64('AA==')).toBe(1);
    expect(bytesDeBase64('AAA=')).toBe(2);
    expect(bytesDeBase64('AA\nAA')).toBe(3);
    expect(bytesDeBase64('')).toBe(0);
  });
});

describe('problemaAssinaturaPng', () => {
  it('pede o desenho quando não há traço', () => {
    expect(problemaAssinaturaPng(null)).toBe(M.assinaturaParaAssinar);
    expect(problemaAssinaturaPng('')).toBe(M.assinaturaParaAssinar);
  });

  it('recusa acima do teto de 65.536 bytes e aceita no limite', () => {
    const acimaDoTeto = 'A'.repeat(87_385); // 65.538 bytes decodificados
    const noLimite = 'A'.repeat(87_380); // 65.535 bytes decodificados
    expect(bytesDeBase64(acimaDoTeto)).toBeGreaterThan(TETO_ASSINATURA_BYTES);
    expect(problemaAssinaturaPng(acimaDoTeto)).toBe(M.assinaturaTamanho);
    expect(problemaAssinaturaPng(noLimite)).toBeNull();
  });
});

describe('temCaractereDeControle', () => {
  it('acusa NUL, tabulação e DEL, e deixa passar nomes com acento', () => {
    expect(temCaractereDeControle('João Pereira')).toBe(false);
    expect(temCaractereDeControle('Jo\u0000ão')).toBe(true);
    expect(temCaractereDeControle('a\tb')).toBe(true);
    expect(temCaractereDeControle('x\u007f')).toBe(true);
  });
});

describe('exigeAceiteCombustivel', () => {
  it('só exige o aceite de combustível quando a cláusula de política de combustível consta do documento', () => {
    expect(exigeAceiteCombustivel(DOCUMENTO_CONTRATO)).toBe(true);
    expect(exigeAceiteCombustivel(DOCUMENTO_SEM_COBRANCAS)).toBe(false);
  });

  it('segue a configuração de cobranças (tabelaCobrancas), não o título da cláusula', () => {
    const semClausula = { ...DOCUMENTO_CONTRATO, clausulas: DOCUMENTO_CONTRATO.clausulas.filter((c) => c.numero !== 5) };
    expect(exigeAceiteCombustivel(semClausula)).toBe(true);
    const semClausulaESemTabela = { ...DOCUMENTO_SEM_COBRANCAS, clausulas: DOCUMENTO_CONTRATO.clausulas };
    expect(exigeAceiteCombustivel(semClausulaESemTabela)).toBe(false);
    expect(exigeAceiteCombustivel({ ...DOCUMENTO_CONTRATO, tabelaCobrancas: undefined })).toBe(false);
  });
});

describe('aceiteCombustivelAssinado', () => {
  const assinaturaBase = {
    nomeAssinante: 'João Pereira Silva',
    aceiteCondicoesGerais: true,
    assinaturaPngBase64: 'iVBORw0KGgo=',
    assinadoEmStr: '14/11/2026 14:30',
    idFuncionarioColetor: 7,
    nomeFuncionarioColetor: 'Atendente',
  };

  it('mostra o aceite só quando ele é true e a cláusula de combustível consta do documento', () => {
    expect(aceiteCombustivelAssinado({ ...assinaturaBase, aceitePoliticaCombustivel: true }, DOCUMENTO_CONTRATO)).toBe(true);
  });

  it('não mostra o aceite quando ele é false, mesmo com a cláusula no documento', () => {
    expect(aceiteCombustivelAssinado({ ...assinaturaBase, aceitePoliticaCombustivel: false }, DOCUMENTO_CONTRATO)).toBe(false);
  });

  it('não mostra o aceite quando a cláusula não consta do documento, mesmo com o aceite gravado', () => {
    expect(aceiteCombustivelAssinado({ ...assinaturaBase, aceitePoliticaCombustivel: true }, DOCUMENTO_SEM_COBRANCAS)).toBe(false);
    expect(aceiteCombustivelAssinado({ ...assinaturaBase, aceitePoliticaCombustivel: null }, DOCUMENTO_SEM_COBRANCAS)).toBe(false);
  });
});

describe('imagemAssinaturaValida', () => {
  it('aceita base64 com o alfabeto e o preenchimento, dentro do teto', () => {
    expect(imagemAssinaturaValida('iVBORw0KGgo=')).toBe(true);
    expect(imagemAssinaturaValida('A'.repeat(TETO_ASSINATURA_BASE64))).toBe(true);
  });

  it('recusa texto fora do alfabeto base64, preenchimento no meio e o vazio', () => {
    expect(imagemAssinaturaValida('')).toBe(false);
    expect(imagemAssinaturaValida('abc"onerror="x')).toBe(false);
    expect(imagemAssinaturaValida('AA==AA')).toBe(false);
    expect(imagemAssinaturaValida('AA AA')).toBe(false);
    expect(imagemAssinaturaValida('A'.repeat(TETO_ASSINATURA_BASE64 + 1))).toBe(false);
  });
});

describe('horaDaDataHora', () => {
  it('tira a hora de um texto dd/MM/yyyy HH:mm', () => {
    expect(horaDaDataHora('14/11/2026 10:02')).toBe('10:02');
    expect(horaDaDataHora('sem hora')).toBeNull();
    expect(horaDaDataHora(null)).toBeNull();
  });
});

describe('etapasDoContrato', () => {
  it('deixa a etapa do contrato ativa enquanto não há assinatura, sem linha de apoio (o selo fica na trilha)', () => {
    const [, vistoria, contrato, entrega] = etapasDoContrato(CONTRATO_PENDENTE);
    expect(vistoria).toEqual({ titulo: 'Vistoria', status: 'concluida', situacao: 'Concluída 10:02' });
    expect(contrato).toEqual({ titulo: 'Contrato', status: 'ativa', situacao: null });
    expect(entrega.status).toBe('pendente');
    expect(entrega.situacao).toBeNull();
  });

  it('conclui a etapa do contrato com a hora da assinatura', () => {
    const contrato = etapasDoContrato(CONTRATO_ASSINADO)[2];
    expect(contrato).toEqual({ titulo: 'Contrato', status: 'concluida', situacao: 'Concluída 14:30' });
  });
});

describe('textoFranquia e percentualCombustivel', () => {
  it('mostra a franquia pela base e "Não informada" sem configuração', () => {
    expect(textoFranquia(null)).toBe('Não informada');
    const tabela = DOCUMENTO_CONTRATO.tabelaCobrancas;
    expect(textoFranquia(tabela)).toBe('300 km (total)');
    expect(textoFranquia({ ...tabela, baseFranquia: 'POR_DIARIA' } as typeof tabela)).toBe('300 km por diária');
  });

  it('converte o nível de 0 a 1 em percentual e limita a faixa', () => {
    expect(percentualCombustivel(0.5)).toBe(50);
    expect(percentualCombustivel(0.33)).toBe(33);
    expect(percentualCombustivel(1)).toBe(100);
    expect(percentualCombustivel(1.7)).toBe(100);
    expect(percentualCombustivel(-1)).toBe(0);
  });
});

describe('dadosDaClausula', () => {
  it('indica os dados que cada cláusula diz estarem no contrato', () => {
    expect(dadosDaClausula('Prazos', DOCUMENTO_CONTRATO).map((linha) => linha.valor)).toEqual([
      '14/11/2026 10:00',
      '18/11/2026 10:00',
    ]);
    expect(dadosDaClausula('Valores', DOCUMENTO_CONTRATO).map((linha) => linha.valor)).toEqual([
      'R$ 229,00',
      'R$ 1.200,00',
    ]);
    expect(dadosDaClausula('Quilometragem', DOCUMENTO_CONTRATO).map((linha) => linha.valor)).toEqual([
      '300 km (total)',
      'R$ 1,50',
    ]);
  });

  it('não quebra quando o servidor omite o campo de caução ou a configuração (sem linha vazia)', () => {
    const semCaucaoOmitida = { ...DOCUMENTO_CONTRATO, locacao: { ...DOCUMENTO_CONTRATO.locacao, valorCaucaoStr: undefined } };
    expect(dadosDaClausula('Valores', semCaucaoOmitida)).toHaveLength(1);
    const semTabelaOmitida = { ...DOCUMENTO_CONTRATO, tabelaCobrancas: undefined };
    expect(dadosDaClausula('Quilometragem', semTabelaOmitida)).toEqual([]);
    expect(dadosDaClausula('Política de combustível', semTabelaOmitida)).toEqual([]);
    expect(textoFranquia(undefined)).toBe('Não informada');
  });

  it('omite a caução quando não há, e as cláusulas de cobrança quando não há configuração', () => {
    const semCaucao = { ...DOCUMENTO_CONTRATO, locacao: { ...DOCUMENTO_CONTRATO.locacao, valorCaucaoStr: null } };
    expect(dadosDaClausula('Valores', semCaucao)).toHaveLength(1);
    const semConfiguracao = { ...DOCUMENTO_CONTRATO, tabelaCobrancas: null };
    expect(dadosDaClausula('Quilometragem', semConfiguracao)).toEqual([]);
    expect(dadosDaClausula('Política de combustível', semConfiguracao)).toEqual([]);
  });

  it('não inventa dados para título desconhecido', () => {
    expect(dadosDaClausula('Natureza do registro', DOCUMENTO_CONTRATO)).toEqual([]);
  });
});

describe('textoCartaoDiarias', () => {
  it('mostra "{n} x R$ {valor da diária}" com as diárias previstas e o valor do servidor', () => {
    expect(textoCartaoDiarias(DOCUMENTO_CONTRATO.locacao)).toBe('4 x R$ 229,00');
  });

  it('segue o texto aprovado com uma diária', () => {
    expect(textoCartaoDiarias({ ...DOCUMENTO_CONTRATO.locacao, diariasPrevistas: 1 })).toBe('1 x R$ 229,00');
  });
});

describe('valoresPrevistos', () => {
  it('monta a frase, a linha de diárias e o total previsto com várias diárias', () => {
    expect(valoresPrevistos(DOCUMENTO_CONTRATO.locacao)).toEqual({
      frase: 'O valor previsto da locação é de R$ 916,00, assim discriminado:',
      linhaDiarias: '4 diárias × R$ 229,00',
      subtotal: 'R$ 916,00',
      total: 'R$ 916,00',
    });
  });

  it('usa o singular com uma diária, e o subtotal é o total (só diárias)', () => {
    const umaDiaria = valoresPrevistos({ ...DOCUMENTO_CONTRATO.locacao, diariasPrevistas: 1, valorTotalPrevistoStr: 'R$ 229,00' });
    expect(umaDiaria.linhaDiarias).toBe('1 diária × R$ 229,00');
    expect(umaDiaria.subtotal).toBe(umaDiaria.total);
    expect(umaDiaria.total).toBe('R$ 229,00');
  });

  it('não muda o total com ou sem caução: a caução não entra no total previsto', () => {
    const semCaucao = valoresPrevistos({ ...DOCUMENTO_CONTRATO.locacao, valorCaucaoStr: null });
    const comCaucao = valoresPrevistos(DOCUMENTO_CONTRATO.locacao);
    expect(semCaucao.total).toBe(comCaucao.total);
    expect(semCaucao.total).toBe('R$ 916,00');
  });
});

describe('ehSemVistoriaEntrada', () => {
  it('reconhece a recusa da RP-06 pela citação da regra', () => {
    expect(ehSemVistoriaEntrada('A locação não tem vistoria de entrada registrada. Registre a vistoria antes de emitir o contrato (RP-06).')).toBe(true);
    expect(ehSemVistoriaEntrada('Este contrato já foi assinado.')).toBe(false);
  });
});
