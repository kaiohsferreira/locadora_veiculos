import { describe, expect, it } from 'vitest';
import { MENSAGENS_CONTRATO_LOCACAO as M } from '@/lib/mensagensContratoLocacao';
import { criarEsquemaAssinatura, LIMITE_NOME_ASSINANTE } from './esquemaAssinatura';

const VALIDO = { nomeAssinante: 'João Pereira Silva', aceiteCondicoesGerais: true, aceitePoliticaCombustivel: false };

/** Mensagens de erro por campo, como o formulário mostra. */
function errosDe(entrada: Record<string, unknown>, exigeCombustivel: boolean): Record<string, string[]> {
  const resultado = criarEsquemaAssinatura(exigeCombustivel).safeParse(entrada);
  if (resultado.success) return {};
  const erros: Record<string, string[]> = {};
  for (const problema of resultado.error.issues) {
    const campo = String(problema.path[0]);
    erros[campo] = [...(erros[campo] ?? []), problema.message];
  }
  return erros;
}

describe('esquema da assinatura', () => {
  it('aceita o nome e os aceites obrigatórios quando a cláusula de combustível não consta', () => {
    expect(criarEsquemaAssinatura(false).safeParse(VALIDO).success).toBe(true);
  });

  it('recusa nome vazio, nome longo e nome com caractere de controle, com o texto aprovado', () => {
    expect(errosDe({ ...VALIDO, nomeAssinante: '   ' }, false).nomeAssinante).toEqual([M.nomeVazio]);
    expect(errosDe({ ...VALIDO, nomeAssinante: 'a'.repeat(LIMITE_NOME_ASSINANTE + 1) }, false).nomeAssinante).toEqual([
      M.nomeTamanho,
    ]);
    expect(errosDe({ ...VALIDO, nomeAssinante: 'Ana\u0001' }, false).nomeAssinante).toEqual([M.nomeInvalido]);
  });

  it('aceita o nome no limite de 100 caracteres', () => {
    expect(errosDe({ ...VALIDO, nomeAssinante: 'a'.repeat(LIMITE_NOME_ASSINANTE) }, false)).toEqual({});
  });

  it('exige o aceite de condições gerais', () => {
    expect(errosDe({ ...VALIDO, aceiteCondicoesGerais: false }, false).aceiteCondicoesGerais).toEqual([M.aceitesParaAssinar]);
  });

  it('exige o aceite de combustível só quando a cláusula consta do documento', () => {
    expect(errosDe({ ...VALIDO, aceitePoliticaCombustivel: false }, true).aceitePoliticaCombustivel).toEqual([
      M.aceitesParaAssinar,
    ]);
    expect(errosDe({ ...VALIDO, aceitePoliticaCombustivel: true }, true)).toEqual({});
    expect(errosDe({ ...VALIDO, aceitePoliticaCombustivel: false }, false)).toEqual({});
  });
});
