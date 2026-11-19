import { describe, expect, it } from 'vitest';
import { MENSAGEM_ERRO_API_PADRAO, MENSAGEM_VERIFICACAO_SUCESSO_APROVADO, MENSAGEM_VERIFICACAO_SUCESSO_RECUSADO } from '@/lib/mensagens';
import { ErroApi } from '@/types/api';
import type { RespostaDocumentoCliente } from '@/types/documentosCliente';
import type { ClienteVerificacao } from '@/types/verificacaoCadastros';
import {
  alterarListagemFila,
  dataExibicao,
  dataHoraExibicao,
  documentoDoTipo,
  ESTADO_LISTAGEM_FILA_INICIAL,
  filtroPendentes,
  iniciaisCliente,
  linhasEndereco,
  mensagemErroVerificacao,
  mensagemSucessoDecisao,
  mostrarDecisao,
  montarFiltroFila,
  OPCOES_STATUS_FILA,
  recusaHabilitada,
  requisicaoDecisao,
  situacaoDoSeletor,
  situacaoVisualDocumento,
  TAMANHO_MAXIMO_BUSCA_FILA,
  temFiltroFila,
  termoBuscaFila,
  rotuloTipoDocumentoVerificacao,
  type EstadoListagemFila,
} from './regras';

const DOCUMENTO: RespostaDocumentoCliente = {
  idClienteDocumento: 1,
  tipo: 'CNH',
  nomeArquivo: 'cnh.jpg',
  tipoConteudo: 'image/jpeg',
  tamanhoBytes: 2048,
  dataEnvio: '18/11/2026 09:30',
  url: 'https://armazenamento.local/cnh',
  expiraEm: '2026-11-19T10:00:00',
};

const CLIENTE: ClienteVerificacao = {
  idCliente: 7,
  nome: 'Mariana Ribeiro de Souza',
  cpf: '32849180845',
  dataCadastro: '18/11/2026 09:00',
  status: 'PENDENTE',
  dataSolicitacao: '18/11/2026 09:30',
  dataVerificacao: null,
  justificativaRecusa: null,
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

describe('termoBuscaFila', () => {
  it('apara o texto e devolve vazio para espaços', () => {
    expect(termoBuscaFila('  Ana Lima  ')).toBe('Ana Lima');
    expect(termoBuscaFila('   ')).toBe('');
  });

  it('limita ao teto do contrato, depois de aparar', () => {
    expect(termoBuscaFila('a'.repeat(TAMANHO_MAXIMO_BUSCA_FILA + 20))).toHaveLength(TAMANHO_MAXIMO_BUSCA_FILA);
  });
});

describe('alterarListagemFila', () => {
  const comPagina3: EstadoListagemFila = { pagina: 3, status: '', busca: '' };

  it('trocar o status volta para a página 1', () => {
    expect(alterarListagemFila(comPagina3, { tipo: 'status', status: 'PENDENTE' })).toEqual({
      pagina: 1,
      status: 'PENDENTE',
      busca: '',
    });
  });

  it('escolher o status que já está marcado não muda nada (a página fica)', () => {
    expect(alterarListagemFila(comPagina3, { tipo: 'status', status: '' })).toBe(comPagina3);
  });

  it('busca nova volta para a página 1; espaços a mais não contam como mudança', () => {
    const comBusca = alterarListagemFila(comPagina3, { tipo: 'busca', busca: 'ana' });
    expect(comBusca).toEqual({ pagina: 1, status: '', busca: 'ana' });

    const paginaDois = { ...comBusca, pagina: 2 };
    expect(alterarListagemFila(paginaDois, { tipo: 'busca', busca: '  ana  ' })).toBe(paginaDois);
  });

  it('troca de página só muda a página e não altera o estado recebido', () => {
    const proximo = alterarListagemFila(comPagina3, { tipo: 'pagina', pagina: 4 });

    expect(proximo.pagina).toBe(4);
    expect(comPagina3.pagina).toBe(3);
  });
});

describe('montarFiltroFila', () => {
  it('sem filtro, leva só página e tamanho', () => {
    expect(montarFiltroFila(ESTADO_LISTAGEM_FILA_INICIAL)).toEqual({ pagina: 1, tamanhoPagina: 10 });
  });

  it('leva status e busca quando escolhidos', () => {
    expect(montarFiltroFila({ pagina: 2, status: 'RECUSADO', busca: 'lima' })).toEqual({
      pagina: 2,
      tamanhoPagina: 10,
      status: 'RECUSADO',
      busca: 'lima',
    });
  });

  it('temFiltroFila diz se há busca ou status', () => {
    expect(temFiltroFila(ESTADO_LISTAGEM_FILA_INICIAL)).toBe(false);
    expect(temFiltroFila({ pagina: 1, status: 'APROVADO', busca: '' })).toBe(true);
    expect(temFiltroFila({ pagina: 1, status: '', busca: 'x' })).toBe(true);
  });
});

describe('filtroPendentes (cartão de pendentes)', () => {
  it('é só o status PENDENTE, na primeira página, sem busca', () => {
    expect(filtroPendentes()).toEqual({ pagina: 1, tamanhoPagina: 1, status: 'PENDENTE' });
  });
});

describe('situacaoDoSeletor e opções de status', () => {
  it('aceita só os status conhecidos; o resto vira "todos"', () => {
    expect(situacaoDoSeletor('APROVADO')).toBe('APROVADO');
    expect(situacaoDoSeletor('EM_REVISAO')).toBe('');
    expect(situacaoDoSeletor('')).toBe('');
  });

  it('oferece Pendente, Aprovado e Recusado, com os rótulos do domínio', () => {
    expect(OPCOES_STATUS_FILA.map((opcao) => opcao.rotulo)).toEqual(['Pendente', 'Aprovado', 'Recusado']);
  });
});

describe('decisão', () => {
  it('os botões aparecem para pendente e recusado, e não para aprovado', () => {
    expect(mostrarDecisao('PENDENTE')).toBe(true);
    expect(mostrarDecisao('RECUSADO')).toBe(true);
    expect(mostrarDecisao('APROVADO')).toBe(false);
  });

  it('recusar só habilita com justificativa (aparada) e fora do envio', () => {
    expect(recusaHabilitada('', false)).toBe(false);
    expect(recusaHabilitada('   ', false)).toBe(false);
    expect(recusaHabilitada('Foto ilegível', false)).toBe(true);
    expect(recusaHabilitada('Foto ilegível', true)).toBe(false);
  });

  it('o corpo da recusa leva a justificativa aparada; aprovar não leva justificativa', () => {
    expect(requisicaoDecisao('RECUSADO', '  Documento vencido  ')).toEqual({
      decisao: 'RECUSADO',
      justificativa: 'Documento vencido',
    });
    expect(requisicaoDecisao('APROVADO', 'nota qualquer')).toEqual({ decisao: 'APROVADO' });
  });

  it('avisos de sucesso por decisão', () => {
    expect(mensagemSucessoDecisao('APROVADO')).toBe(MENSAGEM_VERIFICACAO_SUCESSO_APROVADO);
    expect(mensagemSucessoDecisao('RECUSADO')).toBe(MENSAGEM_VERIFICACAO_SUCESSO_RECUSADO);
  });

  it('409 e 404 mostram a frase do servidor; os outros erros usam o texto padrão', () => {
    expect(mensagemErroVerificacao(new ErroApi({ status: 409, mensagem: 'Este cadastro já foi aprovado.' }))).toBe(
      'Este cadastro já foi aprovado.',
    );
    expect(mensagemErroVerificacao(new ErroApi({ status: 404, mensagem: 'Cliente sem pedido de cadastro nesta locadora.' }))).toBe(
      'Cliente sem pedido de cadastro nesta locadora.',
    );
    expect(mensagemErroVerificacao(new ErroApi({ status: 403, mensagem: 'Frase do servidor' }))).toBe(MENSAGEM_ERRO_API_PADRAO);
    expect(mensagemErroVerificacao(new ErroApi({ status: 500, mensagem: 'Erro' }))).toBe(MENSAGEM_ERRO_API_PADRAO);
    expect(mensagemErroVerificacao(new Error('falha'))).toBe(MENSAGEM_ERRO_API_PADRAO);
  });
});

describe('apresentação', () => {
  it('iniciais: as duas primeiras palavras do nome', () => {
    expect(iniciaisCliente('Mariana Ribeiro de Souza')).toBe('MR');
    expect(iniciaisCliente('Rodrigo')).toBe('R');
    expect(iniciaisCliente('   ')).toBe('');
  });

  it('data e hora: formata ISO; texto que não é data volta como veio', () => {
    expect(dataHoraExibicao('2026-11-18T09:30:00')).toBe('18/11/2026 09:30');
    expect(dataHoraExibicao(null)).toBe('—');
    expect(dataHoraExibicao('18/11/2026 09:30')).toBe('18/11/2026 09:30');
    expect(dataHoraExibicao('texto solto')).toBe('texto solto');
  });

  it('data e hora do servidor com dia de 1 a 12 não troca dia e mês', () => {
    expect(dataHoraExibicao('05/11/2026 09:30')).toBe('05/11/2026 09:30');
    expect(dataHoraExibicao('12/01/2026 23:05')).toBe('12/01/2026 23:05');
  });

  it('data de nascimento e validade no formato do servidor (dd/MM/yyyy) aparecem como vieram', () => {
    expect(dataExibicao('14/08/1992')).toBe('14/08/1992');
    expect(dataExibicao('05/08/1992')).toBe('05/08/1992');
    expect(dataExibicao('22/10/2028')).toBe('22/10/2028');
  });

  it('data em ISO continua formatada (fallback)', () => {
    expect(dataExibicao('1992-08-14')).toBe('14/08/1992');
    expect(dataExibicao(null)).toBe('—');
  });

  it('endereço: uma linha para a rua e outra para cidade, UF e CEP; campos vazios saem', () => {
    expect(linhasEndereco(CLIENTE)).toEqual([
      'Rua Bela Cintra, 1204, Consolação',
      'São Paulo / SP · 01415-000',
    ]);
    expect(
      linhasEndereco({ ...CLIENTE, logradouro: null, numero: null, bairro: null, cidade: null, uf: null, cep: null }),
    ).toEqual([]);
  });

  it('rótulo dos tipos de documento', () => {
    expect(rotuloTipoDocumentoVerificacao('CNH')).toBe('CNH');
    expect(rotuloTipoDocumentoVerificacao('COMPROVANTE_RESIDENCIA')).toBe('Comprovante de residência');
  });
});

describe('documentos no painel', () => {
  it('documentoDoTipo acha o do tipo ou devolve undefined', () => {
    expect(documentoDoTipo([DOCUMENTO], 'CNH')).toBe(DOCUMENTO);
    expect(documentoDoTipo([DOCUMENTO], 'COMPROVANTE_RESIDENCIA')).toBeUndefined();
  });

  it('imagem com link aparece pelo link; PDF com link vira download; sem link não tem nada', () => {
    expect(situacaoVisualDocumento(DOCUMENTO)).toBe('imagem');
    expect(situacaoVisualDocumento({ ...DOCUMENTO, tipoConteudo: 'image/png' })).toBe('imagem');
    expect(situacaoVisualDocumento({ ...DOCUMENTO, tipoConteudo: 'application/pdf' })).toBe('pdf');
    expect(situacaoVisualDocumento({ ...DOCUMENTO, url: null, expiraEm: null })).toBe('sem-link');
    expect(situacaoVisualDocumento(undefined)).toBe('ausente');
  });
});
