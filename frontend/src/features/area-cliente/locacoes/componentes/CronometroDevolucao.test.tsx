import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { CronometroDevolucao, type CronometroDevolucaoProps } from '@/features/area-cliente/locacoes/componentes/CronometroDevolucao';

/** Relógio local da marcação: 16/11/2026 10:00, faltando dois dias para a devolução de 18/11/2026 10:00. */
const AGORA = new Date(2026, 10, 16, 10, 0, 0);

function renderizar(parcial: Partial<CronometroDevolucaoProps> = {}): string {
  return renderToStaticMarkup(
    <CronometroDevolucao
      devolucaoPrevistaEfetiva="2026-11-18T10:00:00"
      toleranciaAtrasoMinutos={59}
      atrasadaServidor={false}
      progresso={50}
      agora={AGORA}
      {...parcial}
    />,
  );
}

describe('CronometroDevolucao (marcação)', () => {
  it('no prazo: mostra os quatro blocos com dois dígitos e o selo com a tolerância', () => {
    const html = renderizar();
    expect(html).toContain('>02</span>');
    expect(html).toContain('>00</span>');
    expect(html).toContain('Dentro do prazo • Tolerância: 59 min');
    expect(html).not.toContain('Prazo de devolução vencido');
  });

  it('rótulos: o nome completo para o desktop e o curto (Hrs, Min, Seg) para o celular', () => {
    const html = renderizar();
    expect(html).toContain('<span class="hidden sm:inline">Horas</span>');
    expect(html).toContain('<span class="sm:hidden">Hrs</span>');
    expect(html).toContain('<span class="sm:hidden">Seg</span>');
  });

  it('o texto para leitor de tela diz o tempo restante em palavras, sem aria-live', () => {
    const html = renderizar();
    expect(html).toContain('Faltam 2 dia(s), 0 hora(s) e 0 minuto(s) para a devolução.');
    expect(html).not.toContain('aria-live');
  });

  it('o progresso vira texto para leitor de tela e a barra fica escondida dele', () => {
    const html = renderizar({ progresso: 50 });
    expect(html).toContain('Progresso da locação: 50% percorrido');
    expect(html).toContain('aria-hidden="true" class="h-1.5 w-full');
    expect(html).not.toContain('role="progressbar"');
  });

  it('sem tolerância da categoria, o selo mostra só "Dentro do prazo"', () => {
    const html = renderizar({ toleranciaAtrasoMinutos: null });
    expect(html).toContain('Dentro do prazo</span>');
    expect(html).not.toContain('Tolerância');
  });

  it('prazo vencido pela data: blocos em zero, aviso de cobrança e sem texto de "faltam"', () => {
    const html = renderizar({ devolucaoPrevistaEfetiva: '2026-11-10T10:00:00' });
    expect(html).toContain('Prazo de devolução vencido. O atraso pode gerar cobrança extra.');
    expect(html).not.toContain('Faltam');
    expect(html).not.toContain('Dentro do prazo');
  });

  it('marcada como atrasada pelo servidor, mesmo com a data ainda no futuro', () => {
    const html = renderizar({ atrasadaServidor: true });
    expect(html).toContain('Prazo de devolução vencido. O atraso pode gerar cobrança extra.');
    expect(html).not.toContain('Dentro do prazo');
  });

  it('data que não pode ser lida: travessão nos blocos e nenhum texto de "faltam"', () => {
    const html = renderizar({ devolucaoPrevistaEfetiva: 'nao-e-data' });
    expect(html).toContain('>—</span>');
    expect(html).not.toContain('Faltam');
  });

  it('sem progresso calculável, a barra não aparece', () => {
    const html = renderizar({ progresso: null });
    expect(html).not.toContain('Progresso da locação');
  });
});
