import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { AreaPrevia, CartaoDocumento, type CartaoDocumentoProps } from '@/features/clientes/cadastro/CartaoDocumento';
import type { DocumentoClienteGravado } from '@/types/documentosCliente';

// Hora local: o backend manda dd/MM/yyyy HH:mm na hora de Brasília, sem fuso.
const AGORA = new Date(2026, 10, 19, 10, 0);

function propsBase(extra: Partial<CartaoDocumentoProps> = {}): CartaoDocumentoProps {
  return {
    tipo: 'CNH',
    titulo: 'Foto da CNH (frente e verso)',
    formatos: 'Formatos aceitos: PDF, JPG ou PNG (máx. 10MB)',
    arquivo: null,
    enviado: null,
    enviando: false,
    progresso: 0,
    falhou: false,
    recusa: null,
    agora: AGORA,
    aoEscolher: () => undefined,
    aoTentarNovamente: () => undefined,
    ...extra,
  };
}

/** PDF de 2,4 MB (2.516.582 bytes), só para o tamanho aparecer. */
const PDF_GRANDE = new File([new Uint8Array(2_516_582)], 'cnh.pdf', { type: 'application/pdf' });

const GRAVADO: DocumentoClienteGravado = {
  idClienteDocumento: 3,
  tipo: 'CNH',
  nomeArquivo: 'cnh-antiga.pdf',
  tipoConteudo: 'application/pdf',
  tamanhoBytes: 1_887_437,
  dataEnvio: '19/11/2026 09:58',
};

describe('CartaoDocumento em cada estado', () => {
  it('vazio: texto "Nenhum arquivo selecionado." e botão "Selecionar arquivo", sem selo', () => {
    const html = renderToStaticMarkup(<CartaoDocumento {...propsBase()} />);

    expect(html).toContain('data-estado="vazio"');
    expect(html).toContain('Nenhum arquivo selecionado.');
    expect(html).toContain('Selecionar arquivo');
    expect(html).not.toContain('Selecionado</span>');
    expect(html).not.toContain('<img');
  });

  it('selecionado (PDF, sem prévia): selo, nome, tamanho em MB e "Substituir arquivo"; sem "Enviado há"', () => {
    const html = renderToStaticMarkup(<CartaoDocumento {...propsBase({ arquivo: PDF_GRANDE })} />);

    expect(html).toContain('data-estado="selecionado"');
    expect(html).toContain('Selecionado</span>');
    expect(html).toContain('cnh.pdf');
    expect(html).toContain('2,4 MB');
    expect(html).toContain('picture_as_pdf');
    expect(html).toContain('Substituir arquivo');
    expect(html).not.toContain('<img');
    expect(html).not.toContain('Enviado há');
  });

  it('enviando: selo "Enviando", barra de progresso com o valor e sem o botão de substituir', () => {
    const html = renderToStaticMarkup(<CartaoDocumento {...propsBase({ arquivo: PDF_GRANDE, enviando: true, progresso: 40 })} />);

    expect(html).toContain('data-estado="enviando"');
    expect(html).toContain('Enviando</span>');
    expect(html).toContain('<progress');
    expect(html).toContain('value="40"');
    expect(html).not.toContain('Substituir arquivo');
  });

  it('enviado: selo "Enviado", "Enviado há 2 minutos" pela hora do servidor e o nome gravado', () => {
    const html = renderToStaticMarkup(<CartaoDocumento {...propsBase({ enviado: GRAVADO })} />);

    expect(html).toContain('data-estado="enviado"');
    expect(html).toContain('Enviado</span>');
    expect(html).toContain('Enviado há 2 minutos');
    expect(html).toContain('cnh-antiga.pdf');
    expect(html).toContain('1,8 MB');
    expect(html).toContain('Substituir arquivo');
    expect(html).not.toContain('Tentar de novo');
  });

  it('erro: selo "Erro no envio", mensagem e "Tentar de novo"', () => {
    const html = renderToStaticMarkup(<CartaoDocumento {...propsBase({ arquivo: PDF_GRANDE, falhou: true })} />);

    expect(html).toContain('data-estado="erro"');
    expect(html).toContain('Erro no envio</span>');
    expect(html).toContain('Não foi possível enviar este arquivo.');
    expect(html).toContain('Tentar de novo');
  });

  it('recusado: o motivo sai num alerta com o texto de tamanho', () => {
    const html = renderToStaticMarkup(<CartaoDocumento {...propsBase({ recusa: 'tamanho' })} />);

    expect(html).toContain('role="alert"');
    expect(html).toContain('O arquivo passa de 10 MB.');
  });

  it('recusado por formato e por arquivo vazio mostram o texto de cada motivo', () => {
    expect(renderToStaticMarkup(<CartaoDocumento {...propsBase({ recusa: 'formato' })} />)).toContain(
      'Use um arquivo PDF, JPG ou PNG.',
    );
    expect(renderToStaticMarkup(<CartaoDocumento {...propsBase({ recusa: 'vazio' })} />)).toContain(
      'O arquivo está vazio.',
    );
  });
});

describe('AreaPrevia (prévia de imagem ou ícone do PDF)', () => {
  it('imagem com prévia mostra o <img> com o link do navegador', () => {
    const html = renderToStaticMarkup(<AreaPrevia previa="blob:prévia-1" tipoConteudo="image/png" />);

    expect(html).toContain('<img');
    expect(html).toContain('src="blob:prévia-1"');
  });

  it('PDF sem prévia mostra o ícone de PDF e nenhuma imagem', () => {
    const html = renderToStaticMarkup(<AreaPrevia previa={null} tipoConteudo="application/pdf" />);

    expect(html).toContain('picture_as_pdf');
    expect(html).not.toContain('<img');
  });
});

describe('CartaoDocumento: desenho e toque', () => {
  it('o título usa o token do desenho (16/22, titulo-md), sem o token novo removido', () => {
    const html = renderToStaticMarkup(<CartaoDocumento {...propsBase()} />);

    expect(html).toContain('text-titulo-md');
    expect(html).not.toContain('text-cartao-titulo');
  });

  it('os ícones do cartão usam a cor de destaque do desenho (status pendente), não o azul', () => {
    const html = renderToStaticMarkup(<CartaoDocumento {...propsBase({ arquivo: PDF_GRANDE })} />);

    expect(html).toContain('text-status-pendente');
    expect(html).not.toMatch(/class="[^"]*\btext-steel\b[^-]/);
  });

  it('todo botão do cartão tem altura de toque de 44 px (h-11), inclusive no estado vazio e no erro', () => {
    const vazio = renderToStaticMarkup(<CartaoDocumento {...propsBase()} />);
    const erro = renderToStaticMarkup(<CartaoDocumento {...propsBase({ arquivo: PDF_GRANDE, falhou: true })} />);

    expect(vazio).toContain('!h-11');
    expect(erro).toContain('Tentar de novo');
    expect(erro.match(/<button[^>]*>/g)?.every((botao) => botao.includes('!h-11'))).toBe(true);
  });
});

describe('CartaoDocumento: seletor de arquivo', () => {
  it('aceita só PDF, JPG e PNG, sem captura pela câmera e sem arrastar e soltar', () => {
    const html = renderToStaticMarkup(<CartaoDocumento {...propsBase()} />);

    expect(html).toContain('accept="application/pdf,image/jpeg,image/png"');
    expect(html).not.toContain('capture=');
    expect(html).not.toContain('arrastar');
    expect(html).not.toContain('Arraste');
  });

  it('o seletor é desligado enquanto o cartão está enviando', () => {
    const html = renderToStaticMarkup(<CartaoDocumento {...propsBase({ arquivo: PDF_GRANDE, enviando: true })} />);

    expect(html).toMatch(/<input[^>]*disabled/);
  });
});
