import { describe, expect, it } from 'vitest';
import { MENSAGEM_FINANCEIRO_TEXTO_SERVIDOR_PIX_GERADO } from '@/lib/mensagensPainelFinanceiro';
// Código-fonte do back-end lido como texto (Vite, ?raw): se a frase mudar lá, este teste falha.
import codigoServidor from '../../../backend/AutoVia.Helpers/ConstantsMessage.Pagamento.cs?raw';

/** Valor literal de uma constante C# do arquivo (ex.: `public const string Nome = "texto";`). */
function valorConstanteServidor(nome: string): string | undefined {
  const casamento = new RegExp(`const string ${nome} = "([^"]*)";`).exec(codigoServidor);
  return casamento?.[1];
}

/**
 * O 409 da baixa com PIX gerado é reconhecido pelo texto exato que o servidor manda. Se a frase mudar no back-end e não
 * aqui, a caixa "Confirmo a baixa mesmo com o PIX gerado." deixa de aparecer sem erro: este teste avisa antes.
 */
describe('textos do servidor espelhados (origem: ConstantsMessage.Pagamento.cs)', () => {
  it('o texto do PIX gerado é igual ao da constante ErroPagamentoBaixaComPixGerado', () => {
    expect(valorConstanteServidor('ErroPagamentoBaixaComPixGerado')).toBe(MENSAGEM_FINANCEIRO_TEXTO_SERVIDOR_PIX_GERADO);
  });
});
