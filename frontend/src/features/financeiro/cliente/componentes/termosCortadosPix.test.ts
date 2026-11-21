import { describe, expect, it } from 'vitest';
import { termosCortadosPresentes } from './termosCortadosPix';

describe('termosCortadosPresentes (detector dos cortes da P4)', () => {
  it('acha o corte com acento', () => {
    expect(termosCortadosPresentes('<span>PIX Instantâneo</span>')).toContain('PIX Instantâneo');
  });

  it('acha o mesmo corte sem acento e em outra caixa (a versão que o teste antigo deixava passar)', () => {
    expect(termosCortadosPresentes('<span>PIX Instantaneo</span>')).toContain('PIX Instantâneo');
    expect(termosCortadosPresentes('<span>pix instantaneo</span>')).toContain('PIX Instantâneo');
  });

  it('acha cartão, Mastercard e recibo no texto visível', () => {
    const html = '<p>Cartão Padrão</p><p>Mastercard •••• 9842</p><p>Recibo enviado ao e-mail</p>';
    const achados = termosCortadosPresentes(html);
    expect(achados).toEqual(expect.arrayContaining(['Cartão', 'Mastercard', 'Recibo enviado']));
  });

  it('não confunde o atributo class (rounded-cartao) com o texto Cartão', () => {
    expect(termosCortadosPresentes('<div class="rounded-cartao bg-steel-fundo"><p>Locação nº 1</p></div>')).toEqual([]);
  });

  it('ignora o src (imagem base64 do QR), mesmo que o blob contenha um termo', () => {
    expect(termosCortadosPresentes('<img src="data:image/png;base64,Mastercard9842" alt="QR">')).toEqual([]);
  });

  it('marcação limpa não tem termos cortados', () => {
    expect(termosCortadosPresentes('<h2>Pagamento Seguro AutoVia</h2><p>Escaneie com o app do seu Banco</p>')).toEqual([]);
  });
});
