import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter } from 'react-router-dom';
import {
  PainelHistoricoLocacoes,
  type PainelHistoricoLocacoesProps,
} from '@/features/area-cliente/locacoes/componentes/PainelHistoricoLocacoes';
import { LOCACAO_CANCELADA, LOCACAO_FINALIZADA, respostaComItens } from '@/features/area-cliente/locacoes/fixturasLocacoes';
import { ErroApi } from '@/types/api';
import type { LocacaoCliente } from '@/types/locacoesCliente';

const PROIBIDOS = [
  'prorrogar',
  'recibo',
  'nota fiscal',
  'whatsapp',
  'sac',
  'gold',
  'vaga',
  'loc-',
  '0800',
  'franquia',
  'ilimitada',
  'ver contrato',
];

function renderizar(parcial: Partial<PainelHistoricoLocacoesProps['estado']>, pagina = 1): string {
  const props: PainelHistoricoLocacoesProps = {
    estado: {
      dados: undefined,
      carregando: false,
      erro: null,
      atualizando: false,
      aoTentarNovamente: () => undefined,
      ...parcial,
    },
    pagina,
    aoMudarPagina: () => undefined,
    aoFalharFotoCapa: () => undefined,
  };
  return renderToStaticMarkup(
    <MemoryRouter>
      <PainelHistoricoLocacoes {...props} />
    </MemoryRouter>,
  );
}

describe('PainelHistoricoLocacoes (aba Histórico)', () => {
  it('lista as locações finalizadas com o número, a situação e o valor total', () => {
    const html = renderizar({ dados: respostaComItens([LOCACAO_FINALIZADA], 1) });
    expect(html).toContain('Histórico</h2>');
    expect(html).toContain('Locação nº 9');
    expect(html).toContain('Finalizada');
    expect(html).toContain('R$ 780,00');
    expect(html).not.toContain('line-through');
  });

  it('cancelada: valor riscado, 85% de opacidade e o chip vermelho com ícone', () => {
    const html = renderizar({ dados: respostaComItens([LOCACAO_CANCELADA], 1) });
    expect(html).toContain('line-through');
    expect(html).toContain('opacity-85');
    expect(html).toContain('bg-manutencao-alerta-trilho text-manutencao-alerta-escuro');
    expect(html).toContain('>cancel</span>');
  });

  it('sem histórico: o título e a descrição aprovados de vazio, sem lista', () => {
    const html = renderizar({ dados: respostaComItens([], 0) });
    expect(html).toContain('Nenhuma locação no histórico.');
    expect(html).toContain('Locações encerradas e canceladas aparecem aqui.');
    expect(html).not.toContain('Locação nº');
  });

  it('erro sem dado: título de erro aprovado e a tentativa de novo', () => {
    const html = renderizar({ erro: new ErroApi({ status: 500, mensagem: 'Falha.' }) });
    expect(html).toContain('Não foi possível carregar suas locações.');
    expect(html).toContain('Tentar novamente');
  });

  it('carregando: o esqueleto fica dentro de uma área ocupada, sem dado', () => {
    const html = renderizar({ carregando: true });
    expect(html).toContain('aria-busy="true"');
    expect(html).not.toContain('Locação nº');
  });

  it('falha de foto de capa: o aviso aparece uma vez, com a mensagem do servidor', () => {
    const MENSAGEM = 'Não foi possível carregar as fotos agora. Tente novamente em instantes.';
    const comFalha: LocacaoCliente[] = [
      { ...LOCACAO_FINALIZADA, fotoCapa: null, erroFotoCapa: MENSAGEM },
      { ...LOCACAO_CANCELADA, fotoCapa: null, erroFotoCapa: MENSAGEM },
    ];
    const html = renderizar({ dados: respostaComItens(comFalha, 2) });
    expect(html.split('<output').length - 1).toBe(1);
    expect(html.split(MENSAGEM).length - 1).toBe(1);
  });

  it('nenhum termo sem lastro aparece no histórico', () => {
    const html = renderizar({ dados: respostaComItens([LOCACAO_FINALIZADA, LOCACAO_CANCELADA], 2) }).toLowerCase();
    for (const termo of PROIBIDOS) {
      expect(html, `termo proibido: ${termo}`).not.toContain(termo);
    }
  });
});
