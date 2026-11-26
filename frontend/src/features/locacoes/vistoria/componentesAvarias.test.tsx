import { describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import type { AvariaRegistrada, DadosNovaAvaria } from '@/types/avarias';
import { DiagramaAvarias, type MarcadorDiagrama } from './DiagramaAvarias';
import { FormularioAvaria } from './FormularioAvaria';
import { GradeFotosAvarias, ListaAvarias } from './ListaAvarias';
import { numerarAvarias, type ItemAvariaLocal } from './avariasVistoria';

const DADOS: DadosNovaAvaria = {
  descricao: 'Amassado no para-choque', gravidade: 'MEDIA', valorEstimado: '350.00', angulo: 'FRENTE', posX: '25.00', posY: '60.00',
  foto: new File(['x'], 'foto.jpg', { type: 'image/jpeg' }),
};

const SERVIDOR_SEM_FOTO: AvariaRegistrada = {
  idAvaria: 5, idVistoria: 2, descricao: 'Risco superficial', gravidade: 'LEVE', valorEstimado: 180, angulo: 'LATERAL_DIREITA',
  posX: 80, posY: 40, foto: null, erroFoto: 'texto do servidor',
};

const SERVIDOR_COM_FOTO: AvariaRegistrada = {
  ...SERVIDOR_SEM_FOTO, idAvaria: 6, angulo: 'TRASEIRA', foto: { url: 'https://armazenamento.exemplo/foto.jpg', expiraEm: '2026-11-26T10:00:00+00:00' }, erroFoto: null,
};

function local(chave: number, situacao: ItemAvariaLocal['situacao']): ItemAvariaLocal {
  return { chave, dados: DADOS, previa: `blob:previa-${chave}`, situacao, idAvaria: situacao === 'foto_pendente' ? 9 : null };
}

const MARCADOR: MarcadorDiagrama = { numero: 1, angulo: 'FRENTE', posX: 25, posY: 60, descricao: 'Amassado', gravidade: 'MEDIA' };

describe('DiagramaAvarias', () => {
  it('modo de edição: quadrantes com botão de marcar, contagem e Redefinir Marcadores', () => {
    const html = renderToStaticMarkup(
      <DiagramaAvarias avarias={[MARCADOR]} aoMarcar={vi.fn()} aoRedefinir={vi.fn()} podeRedefinir />,
    );
    expect(html).toContain('Marcar avaria na Frente');
    expect(html).toContain('Marcar avaria na Lateral Direita');
    expect(html).toContain('1 Avaria');
    expect(html).toContain('Sem Avarias');
    expect(html).toContain('Redefinir Marcadores');
    expect(html).toContain('Clique sobre o diagrama para posicionar um marcador no ponto da avaria.');
    expect(html).toContain('left:25%;top:60%');
    // Marcador com nome acessível e balão do desenho 19 (duas linhas), nos dois modos.
    expect(html).toContain('<span class="sr-only">Avaria 1, Amassado, Média</span>');
    expect(html).toContain('Avaria 1 • Frente');
    expect(html).toContain('Amassado · Média');
    expect(html).not.toContain('title="Avaria 1');
    // Contagem zero usa o cinza do desenho; com avaria, o laranja suave.
    expect(html).toContain('rounded px-2 py-0.5 text-micro bg-steel-suave font-medium text-texto-apoio">Sem Avarias');
    expect(html).toContain('rounded px-2 py-0.5 text-micro bg-orange-acao/10 font-semibold text-frota-oficina">1 Avaria');
  });

  it('edição sem marcador para tirar: Redefinir Marcadores aparece desabilitado', () => {
    const html = renderToStaticMarkup(<DiagramaAvarias avarias={[]} aoMarcar={vi.fn()} aoRedefinir={vi.fn()} podeRedefinir={false} />);
    expect(html).toMatch(/<button[^>]*disabled=""[^>]*>(?:(?!<\/button>).)*Redefinir Marcadores<\/button>/s);
  });

  it('edição com marcador para tirar: Redefinir Marcadores aparece habilitado', () => {
    const html = renderToStaticMarkup(<DiagramaAvarias avarias={[MARCADOR]} aoMarcar={vi.fn()} aoRedefinir={vi.fn()} podeRedefinir />);
    expect(html).toContain('Redefinir Marcadores');
    expect(html).not.toContain('disabled=""');
  });

  it('leitura: sem botões de marcar nem de redefinir, marcadores com nome acessível', () => {
    const html = renderToStaticMarkup(<DiagramaAvarias avarias={[MARCADOR]} somenteLeitura />);
    expect(html).not.toContain('Marcar avaria na');
    expect(html).not.toContain('Redefinir Marcadores');
    expect(html).toContain('<span class="sr-only">Avaria 1, Amassado, Média</span>');
  });

  it('avaria sem posição fica fora do mapa (só na lista)', () => {
    const html = renderToStaticMarkup(<DiagramaAvarias avarias={[{ ...MARCADOR, posX: null, posY: null }]} somenteLeitura />);
    expect(html).not.toContain('Avaria 1, Amassado');
    expect(html).toContain('1 Avaria');
  });
});

describe('ListaAvarias', () => {
  it('vazia: texto da edição ou da leitura conforme o modo', () => {
    expect(renderToStaticMarkup(<ListaAvarias linhas={[]} somenteLeitura={false} enviando={false} aoTentarDeNovo={vi.fn()} />))
      .toContain('Nenhuma avaria mapeada ainda. Clique no diagrama ou use Adicionar Avaria.');
    expect(renderToStaticMarkup(<ListaAvarias linhas={[]} somenteLeitura enviando={false} aoTentarDeNovo={vi.fn()} />))
      .toContain('Nenhuma avaria registrada nesta vistoria.');
  });

  it('avaria do servidor: título pelo ângulo, selo de gravidade, custo e aviso de que não se remove', () => {
    const html = renderToStaticMarkup(
      <ListaAvarias linhas={numerarAvarias([SERVIDOR_SEM_FOTO], [])} somenteLeitura enviando={false} aoTentarDeNovo={vi.fn()} />,
    );
    expect(html).toContain('Lateral Direita');
    expect(html).toContain('Leve');
    expect(html).toContain('180,00');
    expect(html).toContain('Custo Estimado Reparo:');
    expect(html).toContain('Avarias já registradas não podem ser removidas.');
    expect(html).not.toContain('Tentar de novo');
  });

  it('avaria local com erro: situação e botão Tentar de novo, também com a vistoria registrada (leitura)', () => {
    const linhas = numerarAvarias([], [local(1, 'erro')]);
    const edicao = renderToStaticMarkup(<ListaAvarias linhas={linhas} somenteLeitura={false} enviando={false} aoTentarDeNovo={vi.fn()} />);
    expect(edicao).toContain('Não foi possível enviar esta foto.');
    expect(edicao).toContain('Tentar de novo');
    const leitura = renderToStaticMarkup(<ListaAvarias linhas={linhas} somenteLeitura enviando={false} aoTentarDeNovo={vi.fn()} />);
    expect(leitura).toContain('Não foi possível enviar esta foto.');
    expect(leitura).toContain('Tentar de novo');
  });

  it('avaria local já gravada (foto pendente) tem Tentar de novo na leitura', () => {
    const html = renderToStaticMarkup(
      <ListaAvarias linhas={numerarAvarias([], [local(3, 'foto_pendente')])} somenteLeitura enviando={false} aoTentarDeNovo={vi.fn()} />,
    );
    expect(html).toContain('Tentar de novo');
  });

  it('avaria local aguardando envio mostra a situação e não tem botão de repetir', () => {
    const html = renderToStaticMarkup(
      <ListaAvarias linhas={numerarAvarias([], [local(2, 'aguardando')])} somenteLeitura={false} enviando={false} aoTentarDeNovo={vi.fn()} />,
    );
    expect(html).toContain('Aguardando envio');
    expect(html).not.toContain('Tentar de novo');
  });
});

describe('GradeFotosAvarias', () => {
  it('foto do servidor: legenda Avaria n e selo Marcador n; sem arquivo, o erro aprovado e o campo de nova foto', () => {
    const linhas = numerarAvarias([SERVIDOR_COM_FOTO, SERVIDOR_SEM_FOTO], []);
    const html = renderToStaticMarkup(
      <GradeFotosAvarias linhas={linhas} enviando={false} aoEnviarFotoGravada={vi.fn()} />,
    );
    expect(html).toContain('src="https://armazenamento.exemplo/foto.jpg"');
    expect(html).toContain('Avaria 1');
    expect(html).toContain('Marcador 2');
    expect(html).toContain('Não foi possível carregar esta foto. Se o problema continuar, remova a foto e envie de novo.');
    // Só a avaria sem arquivo ganha o campo de foto; a que tem foto não.
    expect(html.match(/type="file"/g)).toHaveLength(1);
  });

  it('a avaria gravada sem foto ganha o envio de nova foto também com a vistoria registrada (sem modo de edição)', () => {
    const html = renderToStaticMarkup(
      <GradeFotosAvarias linhas={numerarAvarias([SERVIDOR_SEM_FOTO], [])} enviando={false} aoEnviarFotoGravada={vi.fn()} />,
    );
    expect(html).toContain('type="file"');
    expect(html).toContain('accept="image/jpeg,image/png"');
  });

  it('sem fotos, mostra o vazio aprovado', () => {
    const html = renderToStaticMarkup(<GradeFotosAvarias linhas={[]} enviando={false} aoEnviarFotoGravada={vi.fn()} />);
    expect(html).toContain('Nenhuma foto adicionada.');
  });
});

describe('FormularioAvaria', () => {
  it('campos, rótulos e botões do formulário da avaria, com a foto obrigatória', () => {
    const html = renderToStaticMarkup(<FormularioAvaria marcacao={null} aoAdicionar={vi.fn()} aoCancelar={vi.fn()} />);
    expect(html).toContain('Adicionar Avaria');
    expect(html).toContain('Sem ângulo');
    expect(html).toContain('Gravidade');
    expect(html).toContain('Descrição');
    expect(html).toContain('Custo Estimado Reparo (R$)');
    expect(html).toContain('Posição X (%)');
    expect(html).toContain('Posição Y (%)');
    expect(html).toContain('Foto da avaria');
    expect(html).toContain('type="file"');
    expect(html).toContain('>Adicionar avaria<');
    expect(html).toContain('>Cancelar<');
  });
});
