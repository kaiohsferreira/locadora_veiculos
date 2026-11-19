import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { isValidElement, type ReactElement, type ReactNode } from 'react';
import type { RespostaDocumentoCliente } from '@/types/documentosCliente';
import type { ClienteFila, ClienteVerificacao } from '@/types/verificacaoCadastros';
import { CartaoPendentesVerificacao } from './CartaoPendentesVerificacao';
import { DadosVerificacao } from './DadosVerificacao';
import { DecisaoVerificacao } from './DecisaoVerificacao';
import { DocumentosVerificacao, type EstadoConsultaDocumentos } from './DocumentosVerificacao';
import { FiltrosFilaVerificacao } from './FiltrosFilaVerificacao';
import { TabelaFilaVerificacao } from './TabelaFilaVerificacao';

const CLIENTE_FILA: ClienteFila = {
  idCliente: 7,
  nome: 'Mariana Ribeiro de Souza',
  cpf: '32849180845',
  dataCadastro: '18/11/2026 09:00',
  status: 'PENDENTE',
  dataSolicitacao: '18/11/2026 09:30',
  dataVerificacao: null,
  justificativaRecusa: null,
};

const CLIENTE_DETALHE: ClienteVerificacao = {
  ...CLIENTE_FILA,
  dataNascimento: '14/08/1992',
  email: 'mariana.souza@email.com.br',
  telefone: '11987654321',
  cnhNumero: '06284918230',
  cnhCategoria: 'B',
  cnhValidade: '22/10/2028',
  logradouro: 'Rua Bela Cintra',
  numero: '1204',
  complemento: null,
  bairro: 'Consolação',
  cidade: 'São Paulo',
  uf: 'SP',
  cep: '01415000',
};

const CNH_IMAGEM: RespostaDocumentoCliente = {
  idClienteDocumento: 1,
  tipo: 'CNH',
  nomeArquivo: 'cnh-frente.jpg',
  tipoConteudo: 'image/jpeg',
  tamanhoBytes: 204800,
  dataEnvio: '18/11/2026 09:30',
  url: 'https://armazenamento.local/cnh-frente',
  expiraEm: '2026-11-19T10:00:00',
};

const COMPROVANTE_PDF: RespostaDocumentoCliente = {
  idClienteDocumento: 2,
  tipo: 'COMPROVANTE_RESIDENCIA',
  nomeArquivo: 'enel-nov.pdf',
  tipoConteudo: 'application/pdf',
  tamanhoBytes: 1048576,
  dataEnvio: '18/11/2026 09:31',
  url: 'https://armazenamento.local/enel-nov',
  expiraEm: '2026-11-19T10:00:00',
};

const COMPROVANTE_SEM_LINK: RespostaDocumentoCliente = {
  ...COMPROVANTE_PDF,
  url: null,
  expiraEm: null,
};

function tabela(parcial: Partial<Parameters<typeof TabelaFilaVerificacao>[0]> = {}): string {
  return renderToStaticMarkup(
    <TabelaFilaVerificacao
      itens={[CLIENTE_FILA]}
      carregando={false}
      vazio={<p>vazio da fila</p>}
      idSelecionado={null}
      aoAbrir={() => undefined}
      {...parcial}
    />,
  );
}

function documentos(estado: EstadoConsultaDocumentos): string {
  return renderToStaticMarkup(<DocumentosVerificacao estado={estado} aoTentarNovamente={() => undefined} />);
}

function decisao(parcial: Partial<Parameters<typeof DecisaoVerificacao>[0]> = {}): string {
  return renderToStaticMarkup(
    <DecisaoVerificacao
      status="PENDENTE"
      justificativa=""
      aoMudarJustificativa={() => undefined}
      enviando={false}
      decisaoEnviada={null}
      aoAprovar={() => undefined}
      aoRecusar={() => undefined}
      {...parcial}
    />,
  );
}

/** Recorta o botão pelo texto e devolve o trecho da abertura até ele (para ver o atributo disabled). */
function botaoComTexto(marcado: string, texto: string): string {
  const posicao = marcado.indexOf(texto);
  const inicio = marcado.lastIndexOf('<button', posicao);
  return marcado.slice(inicio, posicao);
}

/** Classes da tag que casa com o padrão (só a abertura conta). Sem casamento, a lista volta vazia e a asserção falha. */
function classesDe(marcado: string, padrao: RegExp): string[] {
  const achado = padrao.exec(marcado);
  if (achado === null) return [];
  const classe = /class="([^"]*)"/.exec(achado[0]);
  return classe ? classe[1].split(' ') : [];
}

type ElementoReact = ReactElement<Record<string, unknown>>;

/**
 * Sem DOM (não há jsdom neste projeto), o clique não aparece no HTML gerado. Esta caminhada percorre a árvore de elementos,
 * chama os componentes de função (sem hooks) e devolve os elementos que passam no predicado, com as props reais.
 */
function encontrar(no: ReactNode, predicado: (elemento: ElementoReact) => boolean): ElementoReact[] {
  if (no === null || no === undefined || typeof no === 'boolean' || typeof no === 'string' || typeof no === 'number') {
    return [];
  }
  if (Array.isArray(no)) {
    return no.flatMap((filho: ReactNode) => encontrar(filho, predicado));
  }
  if (!isValidElement<Record<string, unknown>>(no)) return [];

  const achados = predicado(no) ? [no] : [];
  const tipo = no.type;
  const expandido =
    typeof tipo === 'function' ? (tipo as (props: Record<string, unknown>) => ReactNode)(no.props) : null;
  return [
    ...achados,
    ...encontrar(expandido, predicado),
    ...encontrar(no.props.children as ReactNode, predicado),
  ];
}

describe('TabelaFilaVerificacao', () => {
  it('traz as colunas do contrato e nenhuma das removidas (documentos, protocolo, prioridade)', () => {
    const marcado = tabela();

    for (const titulo of ['Cliente', 'CPF', 'Data do cadastro', 'Status', 'Ação']) {
      expect(marcado).toContain(`>${titulo}<`);
    }
    expect(marcado).not.toContain('>Documentos<');
  });

  it('linha pendente: nome, CPF formatado, data do pedido, status e botão de abrir com seta', () => {
    const marcado = tabela();

    expect(marcado).toContain('Mariana Ribeiro de Souza');
    expect(marcado).toContain('328.491.808-45');
    expect(marcado).toContain('18/11/2026 09:00');
    expect(marcado).not.toContain('18/11/2026 09:30');
    expect(marcado).toContain('Pendente');
    expect(marcado).toContain('aria-label="Abrir cadastro de Mariana Ribeiro de Souza"');
    expect(marcado).toContain('>arrow_forward<');
    expect(marcado).toContain('aria-pressed="false"');
  });

  it('linha com painel aberto: destaque, olho e aria-pressed verdadeiro', () => {
    const marcado = tabela({ idSelecionado: 7 });

    expect(marcado).toContain('>visibility<');
    expect(marcado).toContain('aria-pressed="true"');
    expect(marcado).toContain('aria-selected="true"');
  });

  it('lista vazia mostra o conteúdo de vazio, sem linhas de dados', () => {
    const marcado = tabela({ itens: [] });

    expect(marcado).toContain('vazio da fila');
    expect(marcado).not.toContain('Mariana');
  });

  it('carregando mostra o esqueleto e não mostra dados', () => {
    const marcado = tabela({ carregando: true });

    expect(marcado).toContain('aria-busy="true"');
    expect(marcado).not.toContain('Mariana');
  });

  it('a linha abre o painel no clique, sem role nem tabindex; o botão segue com o nome acessível', () => {
    const abertos: number[] = [];
    const arvore = TabelaFilaVerificacao({
      itens: [CLIENTE_FILA],
      carregando: false,
      vazio: null,
      idSelecionado: null,
      aoAbrir: (id) => abertos.push(id),
    });

    const linhas = encontrar(arvore, (elemento) => elemento.type === 'tr' && typeof elemento.props.onClick === 'function');
    expect(linhas).toHaveLength(1);
    const linha = linhas[0];
    expect(linha.props.role).toBeUndefined();
    expect(linha.props.tabIndex).toBeUndefined();
    expect(String(linha.props.className)).toContain('cursor-pointer');
    (linha.props.onClick as () => void)();
    expect(abertos).toEqual([7]);

    const botoes = encontrar(arvore, (elemento) => elemento.type === 'button');
    expect(botoes).toHaveLength(1);
    expect(botoes[0].props['aria-label']).toBe('Abrir cadastro de Mariana Ribeiro de Souza');

    abertos.length = 0;
    let propagacaoParada = false;
    (botoes[0].props.onClick as (evento: { stopPropagation: () => void }) => void)({
      stopPropagation: () => {
        propagacaoParada = true;
      },
    });
    expect(propagacaoParada).toBe(true);
    expect(abertos).toEqual([7]);
  });

  it('com painel: nome longo corta com truncate e title, a célula do cliente absorve a largura e a data some abaixo de 1400px', () => {
    const nomeLongo = 'Maximiliano Augusto Figueiredo Cavalcanti de Albuquerque Neto';
    const marcado = tabela({ itens: [{ ...CLIENTE_FILA, nome: nomeLongo }], comPainel: true });
    const trecho = new RegExp(`<span(?=[^>]*\\btruncate\\b)(?=[^>]*title="${nomeLongo}")[^>]*>${nomeLongo}</span>`);

    expect(marcado).toMatch(trecho);
    expect(marcado).not.toContain('xl:w-20');
    expect(classesDe(marcado, /<th scope="row"[^>]*>/)).toEqual(expect.arrayContaining(['w-full', 'max-w-0']));
    expect(classesDe(marcado, /<th scope="col"[^>]*>Cliente<\/th>/)).toEqual(expect.arrayContaining(['w-full', 'max-w-0']));
    expect(classesDe(marcado, /<th scope="col"[^>]*>Data do cadastro<\/th>/)).toEqual(
      expect.arrayContaining(['hidden', 'min-[1400px]:table-cell']),
    );
    expect(classesDe(marcado, /<td[^>]*>18\/11\/2026 09:00<\/td>/)).toEqual(
      expect.arrayContaining(['hidden', 'min-[1400px]:table-cell']),
    );
    expect(tabela({ carregando: true, comPainel: true }).split('hidden min-[1400px]:table-cell').length - 1).toBe(6);
  });

  it('sem painel: a coluna de data aparece sempre (sem hidden) e o title do nome segue presente', () => {
    const marcado = tabela({ comPainel: false });

    expect(marcado).toContain('title="Mariana Ribeiro de Souza"');
    expect(classesDe(marcado, /<th scope="col"[^>]*>Data do cadastro<\/th>/)).not.toContain('hidden');
    expect(classesDe(marcado, /<td[^>]*>18\/11\/2026 09:00<\/td>/)).not.toContain('hidden');
  });
});

describe('CartaoPendentesVerificacao', () => {
  it('traz o círculo decorativo escondido de leitores de tela, com o cartão recortado', () => {
    const marcado = renderToStaticMarkup(<CartaoPendentesVerificacao total={14} carregando={false} />);

    expect(marcado).toContain('bg-orange-acao/10');
    expect(marcado).toContain('aria-hidden="true"');
    expect(marcado).toContain('overflow-hidden');
  });

  it('mostra o total de pendentes', () => {
    const marcado = renderToStaticMarkup(<CartaoPendentesVerificacao total={14} carregando={false} />);

    expect(marcado).toContain('Cadastros Pendentes');
    expect(marcado).toContain('14');
    expect(marcado).toContain('solicitações');
  });

  it('sem total mostra o traço, e carregando não mostra número', () => {
    expect(renderToStaticMarkup(<CartaoPendentesVerificacao total={null} carregando={false} />)).toContain('—');
    expect(renderToStaticMarkup(<CartaoPendentesVerificacao total={14} carregando />)).not.toContain('>14<');
  });
});

describe('FiltrosFilaVerificacao', () => {
  it('busca por nome, CPF ou e-mail, com 60 caracteres, e status com as opções do contrato', () => {
    const marcado = renderToStaticMarkup(
      <FiltrosFilaVerificacao busca="" aoMudarBusca={() => undefined} status="" aoMudarStatus={() => undefined} />,
    );

    expect(marcado).toContain('placeholder="Buscar por nome, CPF ou e-mail..."');
    expect(marcado).toContain('maxLength="60"');
    for (const opcao of ['Todos os Status', 'Pendente', 'Aprovado', 'Recusado']) {
      expect(marcado).toContain(`>${opcao}<`);
    }
    expect(marcado).not.toContain('Hoje (14/11)');
  });
});

describe('DadosVerificacao', () => {
  it('mostra CPF, nascimento, e-mail e telefone (decisão 9), CNH e endereço', () => {
    const marcado = renderToStaticMarkup(<DadosVerificacao cliente={CLIENTE_DETALHE} />);

    expect(marcado).toContain('328.491.808-45');
    expect(marcado).toContain('14/08/1992');
    expect(marcado).toContain('mariana.souza@email.com.br');
    expect(marcado).toContain('(11) 98765-4321');
    expect(marcado).toContain('06284918230');
    expect(marcado).toContain('22/10/2028');
    expect(marcado).toContain('Rua Bela Cintra, 1204, Consolação');
    expect(marcado).toContain('São Paulo / SP');
  });

  it('recusado com justificativa mostra o motivo; sem recusa não mostra o motivo', () => {
    const recusado = renderToStaticMarkup(
      <DadosVerificacao
        cliente={{ ...CLIENTE_DETALHE, status: 'RECUSADO', dataVerificacao: '18/11/2026 10:00', justificativaRecusa: 'Foto ilegível' }}
      />,
    );
    const pendente = renderToStaticMarkup(<DadosVerificacao cliente={CLIENTE_DETALHE} />);

    expect(recusado).toContain('Motivo da recusa: ');
    expect(recusado).toContain('Foto ilegível');
    expect(recusado).toContain('Última decisão em 18/11/2026 10:00');
    expect(pendente).not.toContain('Motivo da recusa');
  });

  it('sem endereço mostra o texto de endereço não informado', () => {
    const marcado = renderToStaticMarkup(
      <DadosVerificacao
        cliente={{ ...CLIENTE_DETALHE, logradouro: null, numero: null, bairro: null, cidade: null, uf: null, cep: null }}
      />,
    );

    expect(marcado).toContain('Endereço não informado.');
  });
});

describe('DocumentosVerificacao', () => {
  it('imagem JPG aparece pelo link, com rel seguro e texto alternativo', () => {
    const marcado = documentos({ situacao: 'ok', documentos: [CNH_IMAGEM] });

    expect(marcado).toContain('src="https://armazenamento.local/cnh-frente"');
    expect(marcado).toContain('href="https://armazenamento.local/cnh-frente"');
    expect(marcado).toContain('rel="noopener noreferrer"');
    expect(marcado).toContain('alt="Imagem enviada pelo cliente: CNH"');
    expect(marcado).toContain('Enviado em 18/11/2026 09:30');
    expect(marcado).toContain('cnh-frente.jpg');
  });

  it('PDF vira link para baixar, com rel seguro', () => {
    const marcado = documentos({ situacao: 'ok', documentos: [COMPROVANTE_PDF] });

    expect(marcado).toContain('Baixar PDF');
    expect(marcado).toContain('href="https://armazenamento.local/enel-nov"');
    expect(marcado).not.toContain('<img');
  });

  it('item sem link aparece sem imagem e sem link, com texto claro', () => {
    const marcado = documentos({ situacao: 'ok', documentos: [COMPROVANTE_SEM_LINK] });

    expect(marcado).toContain('Arquivo indisponível para visualização no momento.');
    expect(marcado).toContain('enel-nov.pdf');
    expect(marcado).not.toContain('href="null"');
    expect(marcado).not.toContain('Baixar PDF');
    expect(marcado).not.toContain('<img');
  });

  it('sem documento enviado mostra o texto de sem documentos para cada tipo que falta', () => {
    const marcado = documentos({ situacao: 'ok', documentos: [] });

    expect(marcado.split('Sem documentos enviados.').length - 1).toBe(2);
  });

  it('erro mostra a mensagem padrão e o botão de tentar de novo; carregando mostra o esqueleto', () => {
    const erro = documentos({ situacao: 'erro' });
    const carregando = documentos({ situacao: 'carregando' });

    expect(erro).toContain('Não foi possível carregar os documentos.');
    expect(erro).toContain('Tentar novamente');
    expect(carregando).not.toContain('Sem documentos enviados.');
  });
});

describe('DecisaoVerificacao', () => {
  it('pendente sem justificativa: Recusar desabilitado, Aprovar habilitado, e o texto de apoio', () => {
    const marcado = decisao();

    expect(botaoComTexto(marcado, 'Recusar')).toContain('disabled=""');
    expect(botaoComTexto(marcado, 'Aprovar Cadastro')).not.toContain('disabled=""');
    expect(marcado).toContain('A justificativa só é usada na recusa.');
  });

  it('recusado com justificativa: Recusar habilitado', () => {
    const marcado = decisao({ status: 'RECUSADO', justificativa: 'Documento vencido' });

    expect(botaoComTexto(marcado, 'Recusar')).not.toContain('disabled=""');
    expect(marcado).toContain('Aprovar Cadastro');
  });

  it('textarea com duas linhas e os dois botões com altura de 44px', () => {
    const marcado = decisao();

    expect(marcado).toContain('rows="2"');
    expect(marcado.split('h-11').length - 1).toBe(2);
  });

  it('aprovado: sem botões e sem campo, só o aviso', () => {
    const marcado = decisao({ status: 'APROVADO' });

    expect(marcado).toContain('Este cadastro já está aprovado.');
    expect(marcado).not.toContain('Aprovar Cadastro');
    expect(marcado).not.toContain('Recusar');
    expect(marcado).not.toContain('<textarea');
  });

  it('durante o envio os dois botões ficam bloqueados', () => {
    const marcado = decisao({ justificativa: 'Motivo', enviando: true, decisaoEnviada: 'RECUSADO' });

    expect(botaoComTexto(marcado, 'Aprovar Cadastro')).toContain('disabled=""');
    expect(botaoComTexto(marcado, 'Recusar')).toContain('disabled=""');
  });
});

describe('textos que não podem aparecer na tela 18', () => {
  it('nenhum termo fora do contrato no que a tela renderiza', () => {
    const tudo = [
      tabela(),
      renderToStaticMarkup(<DadosVerificacao cliente={CLIENTE_DETALHE} />),
      documentos({ situacao: 'ok', documentos: [CNH_IMAGEM, COMPROVANTE_SEM_LINK] }),
      decisao({ justificativa: 'x' }),
      renderToStaticMarkup(<CartaoPendentesVerificacao total={1} carregando={false} />),
      renderToStaticMarkup(
        <FiltrosFilaVerificacao busca="" aoMudarBusca={() => undefined} status="" aoMudarStatus={() => undefined} />,
      ),
    ].join('');

    for (const termo of ['Serasa', 'Score', 'OCR', 'Protocolo', 'Solicitar Reenvio', 'Em Revisão', 'Serpro']) {
      expect(tudo).not.toContain(termo);
    }
  });
});
