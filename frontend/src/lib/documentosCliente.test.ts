import { describe, expect, it } from 'vitest';
import {
  TAMANHO_MAXIMO_DOCUMENTO_BYTES,
  conferirArquivoDocumento,
  detectarTipoDocumento,
  documentosFaltando,
  documentosPendentes,
  estadoCartaoDocumento,
  mascararCpf,
  mensagemRecusaDocumento,
  percentualProgresso,
  semTipo,
  situacaoModoSoEnviar,
  tiposNoServidor,
  validarArquivoDocumento,
} from '@/lib/documentosCliente';
import { formatarTamanhoArquivo, formatarTempoDecorrido, interpretarDataHoraServidor } from '@/lib/formatters';
import {
  MENSAGEM_DOCUMENTO_JA_ENVIADOS,
  MENSAGEM_DOCUMENTO_MODO_SO_ENVIAR,
  MENSAGEM_DOCUMENTO_RECUSA_FORMATO,
  MENSAGEM_DOCUMENTO_RECUSA_TAMANHO,
  MENSAGEM_DOCUMENTO_RECUSA_VAZIO,
} from '@/lib/mensagensCadastro';
import type { DocumentoClienteGravado } from '@/types/documentosCliente';

const PDF = new Uint8Array([0x25, 0x50, 0x44, 0x46, 0x2d, 0x31, 0x2e, 0x34]);
const JPEG = new Uint8Array([0xff, 0xd8, 0xff, 0xe0, 0x00, 0x10]);
const PNG = new Uint8Array([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a, 0x00]);
const TEXTO = new Uint8Array([0x48, 0x65, 0x6c, 0x6f, 0x21, 0x21]);

describe('detectarTipoDocumento', () => {
  it('reconhece PDF, JPEG e PNG pelos primeiros bytes', () => {
    expect(detectarTipoDocumento(PDF)).toBe('application/pdf');
    expect(detectarTipoDocumento(JPEG)).toBe('image/jpeg');
    expect(detectarTipoDocumento(PNG)).toBe('image/png');
  });

  it('devolve null para outro conteúdo (texto, GIF) e para cabeçalho curto demais', () => {
    expect(detectarTipoDocumento(TEXTO)).toBeNull();
    expect(detectarTipoDocumento(new Uint8Array([0x47, 0x49, 0x46, 0x38, 0x39, 0x61]))).toBeNull();
    expect(detectarTipoDocumento(new Uint8Array([0x25, 0x50]))).toBeNull();
  });
});

describe('validarArquivoDocumento', () => {
  it('aceita PDF, JPG e PNG válidos', () => {
    expect(validarArquivoDocumento({ type: 'application/pdf', size: 1000 }, PDF)).toBeNull();
    expect(validarArquivoDocumento({ type: 'image/jpeg', size: 1000 }, JPEG)).toBeNull();
    expect(validarArquivoDocumento({ type: 'image/png', size: 1000 }, PNG)).toBeNull();
  });

  it('aceita exatamente 10.485.760 bytes e recusa 10.485.761', () => {
    expect(TAMANHO_MAXIMO_DOCUMENTO_BYTES).toBe(10_485_760);
    expect(validarArquivoDocumento({ type: 'application/pdf', size: 10_485_760 }, PDF)).toBeNull();
    expect(validarArquivoDocumento({ type: 'application/pdf', size: 10_485_761 }, PDF)).toBe('tamanho');
  });

  it('recusa arquivo vazio', () => {
    expect(validarArquivoDocumento({ type: 'application/pdf', size: 0 }, new Uint8Array())).toBe('vazio');
  });

  it('recusa tipo declarado fora de PDF, JPG e PNG (ex.: GIF)', () => {
    expect(validarArquivoDocumento({ type: 'image/gif', size: 1000 }, PDF)).toBe('formato');
    expect(validarArquivoDocumento({ type: '', size: 1000 }, PDF)).toBe('formato');
  });

  it('recusa conteúdo que não é PDF, JPG nem PNG, mesmo com tipo declarado aceito', () => {
    expect(validarArquivoDocumento({ type: 'application/pdf', size: 1000 }, TEXTO)).toBe('formato');
  });

  it('recusa tipo declarado diferente do tipo do conteúdo', () => {
    expect(validarArquivoDocumento({ type: 'application/pdf', size: 1000 }, JPEG)).toBe('formato');
    expect(validarArquivoDocumento({ type: 'image/png', size: 1000 }, JPEG)).toBe('formato');
  });
});

describe('conferirArquivoDocumento (arquivo real)', () => {
  it('lê os primeiros bytes do arquivo e aceita PDF válido', async () => {
    const arquivo = new File([PDF], 'cnh.pdf', { type: 'application/pdf' });
    expect(await conferirArquivoDocumento(arquivo)).toBeNull();
  });

  it('recusa arquivo vazio e conteúdo errado', async () => {
    expect(await conferirArquivoDocumento(new File([], 'vazio.pdf', { type: 'application/pdf' }))).toBe('vazio');
    expect(await conferirArquivoDocumento(new File([TEXTO], 'falso.pdf', { type: 'application/pdf' }))).toBe('formato');
  });
});

describe('mensagemRecusaDocumento', () => {
  it('devolve o texto de cada motivo', () => {
    expect(mensagemRecusaDocumento('formato')).toBe(MENSAGEM_DOCUMENTO_RECUSA_FORMATO);
    expect(mensagemRecusaDocumento('tamanho')).toBe(MENSAGEM_DOCUMENTO_RECUSA_TAMANHO);
    expect(mensagemRecusaDocumento('vazio')).toBe(MENSAGEM_DOCUMENTO_RECUSA_VAZIO);
    expect(MENSAGEM_DOCUMENTO_RECUSA_TAMANHO).toBe('O arquivo passa de 10 MB.');
  });
});

describe('formatarTamanhoArquivo', () => {
  it('usa MB com uma casa e vírgula acima de 1 MB', () => {
    expect(formatarTamanhoArquivo(2_516_582)).toBe('2,4 MB');
    expect(formatarTamanhoArquivo(1_887_437)).toBe('1,8 MB');
  });

  it('usa KB inteiros abaixo de 1 MB', () => {
    expect(formatarTamanhoArquivo(512_000)).toBe('500 KB');
  });
});

describe('formatarTempoDecorrido ("Enviado há X")', () => {
  // Hora local, para o teste valer em qualquer fuso (o backend manda hora de Brasília, sem fuso).
  const agora = new Date(2026, 10, 19, 10, 0);

  it('lê o formato real do backend, dd/MM/yyyy HH:mm, e diz em minutos, horas e dias', () => {
    expect(formatarTempoDecorrido('19/11/2026 09:59', agora)).toBe('1 minuto');
    expect(formatarTempoDecorrido('19/11/2026 09:58', agora)).toBe('2 minutos');
    expect(formatarTempoDecorrido('19/11/2026 09:59:40', agora)).toBe('menos de 1 minuto');
    expect(formatarTempoDecorrido('19/11/2026 09:00', agora)).toBe('1 hora');
    expect(formatarTempoDecorrido('19/11/2026 05:00', agora)).toBe('5 horas');
    expect(formatarTempoDecorrido('18/11/2026 10:00', agora)).toBe('1 dia');
    expect(formatarTempoDecorrido('16/11/2026 10:00', agora)).toBe('3 dias');
  });

  it('dia até 12 não vira mês: 05/11 é 5 de novembro (14 dias antes de 19/11)', () => {
    expect(formatarTempoDecorrido('05/11/2026 10:00', agora)).toBe('14 dias');
  });

  it('dia maior que 12 não vira data inválida (antes sempre dizia "menos de 1 minuto")', () => {
    expect(formatarTempoDecorrido('19/11/2026 09:58', agora)).not.toBe('menos de 1 minuto');
    expect(formatarTempoDecorrido('25/11/2026 10:00', agora)).toBe('menos de 1 minuto');
  });

  it('ISO continua aceito (data de fuso explícito)', () => {
    const agoraUtc = new Date('2026-11-19T10:00:00Z');
    expect(formatarTempoDecorrido('2026-11-19T09:58:00Z', agoraUtc)).toBe('2 minutos');
  });

  it('data inválida ou de dia que não existe vira o tempo mínimo, sem quebrar', () => {
    expect(formatarTempoDecorrido('nao-e-data', agora)).toBe('menos de 1 minuto');
    expect(formatarTempoDecorrido('31/02/2026 10:00', agora)).toBe('menos de 1 minuto');
  });
});

describe('interpretarDataHoraServidor', () => {
  it('lê dd/MM/yyyy HH:mm como hora local, sem trocar dia e mês', () => {
    expect(interpretarDataHoraServidor('12/11/2026 10:00')).toEqual(new Date(2026, 10, 12, 10, 0));
    expect(interpretarDataHoraServidor('05/11/2026 09:30:15')).toEqual(new Date(2026, 10, 5, 9, 30, 15));
  });

  it('devolve null para texto que não é data', () => {
    expect(interpretarDataHoraServidor('ontem')).toBeNull();
    expect(interpretarDataHoraServidor('29/02/2026 10:00')).toBeNull();
  });
});

describe('semTipo (mapa por tipo sem a chave)', () => {
  const CNH = new File(['a'], 'cnh.pdf', { type: 'application/pdf' });
  const COMPROVANTE = new File(['c'], 'conta.png', { type: 'image/png' });

  it('tira só o tipo pedido, sem deixar undefined para trás, e não muda o mapa original', () => {
    const original = { CNH, COMPROVANTE_RESIDENCIA: COMPROVANTE };
    const copia = semTipo(original, 'CNH');
    expect(copia).toEqual({ COMPROVANTE_RESIDENCIA: COMPROVANTE });
    expect(Object.keys(copia)).not.toContain('CNH');
    expect(original.CNH).toBe(CNH);
  });
});

describe('mascararCpf', () => {
  it('mostra só os três grupos do meio, com o resto mascarado', () => {
    expect(mascararCpf('529.982.247-25')).toBe('***.982.247-**');
    expect(mascararCpf('52998224725')).toBe('***.982.247-**');
  });

  it('devolve vazio quando não há 11 dígitos', () => {
    expect(mascararCpf('123')).toBe('');
    expect(mascararCpf('')).toBe('');
  });
});

describe('documentos pendentes, faltando e no servidor', () => {
  const CNH = new File(['a'], 'cnh.pdf', { type: 'application/pdf' });
  const CNH_NOVA = new File(['b'], 'cnh2.pdf', { type: 'application/pdf' });
  const COMPROVANTE = new File(['c'], 'conta.png', { type: 'image/png' });

  it('pendente é o arquivo escolhido que ainda não subiu; troca conta como pendente', () => {
    expect(documentosPendentes({ CNH, COMPROVANTE_RESIDENCIA: COMPROVANTE }, {})).toEqual(['CNH', 'COMPROVANTE_RESIDENCIA']);
    expect(documentosPendentes({ CNH, COMPROVANTE_RESIDENCIA: COMPROVANTE }, { CNH })).toEqual(['COMPROVANTE_RESIDENCIA']);
    expect(documentosPendentes({ CNH: CNH_NOVA }, { CNH })).toEqual(['CNH']);
    expect(documentosPendentes({}, {})).toEqual([]);
  });

  it('faltando é o tipo sem arquivo escolhido e sem documento no servidor, na ordem da tela', () => {
    expect(documentosFaltando({}, [])).toEqual(['CNH', 'COMPROVANTE_RESIDENCIA']);
    expect(documentosFaltando({ CNH }, [])).toEqual(['COMPROVANTE_RESIDENCIA']);
    expect(documentosFaltando({}, ['COMPROVANTE_RESIDENCIA'])).toEqual(['CNH']);
    expect(documentosFaltando({ CNH }, ['COMPROVANTE_RESIDENCIA'])).toEqual([]);
  });

  it('tiposNoServidor lista só os tipos com documento gravado', () => {
    const doc: DocumentoClienteGravado = {
      idClienteDocumento: 1,
      tipo: 'CNH',
      nomeArquivo: 'cnh.pdf',
      tipoConteudo: 'application/pdf',
      tamanhoBytes: 3,
      dataEnvio: '19/11/2026 10:00',
    };
    expect(tiposNoServidor({ CNH: doc })).toEqual(['CNH']);
    expect(tiposNoServidor({})).toEqual([]);
  });
});

describe('situacaoModoSoEnviar (aviso e botão do modo só enviar)', () => {
  it('com os dois documentos gravados e nada escolhido: aviso de já enviados, sem botão', () => {
    expect(situacaoModoSoEnviar(['CNH', 'COMPROVANTE_RESIDENCIA'], [])).toEqual({
      aviso: 'Seus documentos já foram enviados. Se precisar, substitua um arquivo.',
      mostrarBotao: false,
    });
    expect(situacaoModoSoEnviar(['CNH', 'COMPROVANTE_RESIDENCIA'], []).aviso).toBe(MENSAGEM_DOCUMENTO_JA_ENVIADOS);
  });

  it('com os dois gravados e um arquivo novo escolhido (substituição): botão aparece', () => {
    const situacao = situacaoModoSoEnviar(['CNH', 'COMPROVANTE_RESIDENCIA'], ['CNH']);
    expect(situacao.mostrarBotao).toBe(true);
    expect(situacao.aviso).toBe(MENSAGEM_DOCUMENTO_JA_ENVIADOS);
  });

  it('com algum documento faltando: aviso e botão como antes, mesmo sem arquivo escolhido', () => {
    expect(situacaoModoSoEnviar([], [])).toEqual({ aviso: MENSAGEM_DOCUMENTO_MODO_SO_ENVIAR, mostrarBotao: true });
    expect(situacaoModoSoEnviar(['CNH'], [])).toEqual({ aviso: MENSAGEM_DOCUMENTO_MODO_SO_ENVIAR, mostrarBotao: true });
    expect(situacaoModoSoEnviar(['COMPROVANTE_RESIDENCIA'], ['COMPROVANTE_RESIDENCIA'])).toEqual({
      aviso: MENSAGEM_DOCUMENTO_MODO_SO_ENVIAR,
      mostrarBotao: true,
    });
  });
});

describe('estadoCartaoDocumento', () => {
  const base = { selecionado: false, enviado: false, enviando: false, falhou: false };

  it('cada estado, com a prioridade enviando > erro > selecionado > enviado > vazio', () => {
    expect(estadoCartaoDocumento(base)).toBe('vazio');
    expect(estadoCartaoDocumento({ ...base, enviado: true })).toBe('enviado');
    expect(estadoCartaoDocumento({ ...base, selecionado: true, enviado: true })).toBe('selecionado');
    expect(estadoCartaoDocumento({ ...base, selecionado: true, falhou: true })).toBe('erro');
    expect(estadoCartaoDocumento({ ...base, selecionado: true, enviando: true, falhou: true })).toBe('enviando');
  });
});

describe('percentualProgresso', () => {
  it('fica entre 0 e 100 e aceita arredondamento', () => {
    expect(percentualProgresso(-5)).toBe(0);
    expect(percentualProgresso(42.6)).toBe(43);
    expect(percentualProgresso(250)).toBe(100);
    expect(percentualProgresso(Number.NaN)).toBe(0);
  });
});
