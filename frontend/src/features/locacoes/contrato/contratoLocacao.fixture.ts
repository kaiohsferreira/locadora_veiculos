import type { ContratoDocumento, ContratoLocacao } from '@/types/contratoLocacao';

/** Documento de exemplo (dados fictícios) com as oito cláusulas e configuração de cobranças da categoria. */
export const DOCUMENTO_CONTRATO: ContratoDocumento = {
  versaoDocumento: 1,
  locadora: {
    razaoSocial: 'AutoVia Locadora de Veículos S.A.',
    nomeFantasia: 'AutoVia',
    cnpj: '04928118000149',
    telefone: '3133334444',
    email: 'contato@autovia.test',
    cep: '30130000',
    logradouro: 'Rua Exemplo',
    numero: '100',
    complemento: null,
    bairro: 'Centro',
    cidade: 'Belo Horizonte',
    uf: 'MG',
  },
  locatario: {
    nome: 'João Pereira Silva',
    cpfMascarado: '***.912.838-**',
    cnhMascarada: '*****4910',
    cnhCategoria: 'B',
    cnhValidadeStr: '10/10/2030',
  },
  veiculo: {
    marca: 'Jeep',
    modelo: 'Renegade Longitude 1.3T',
    placa: 'BRA2E19',
    chassi: '9883569X7M483921',
    renavam: '01293847291',
    anoFabricacao: 2020,
    anoModelo: 2021,
    cor: null,
  },
  locacao: {
    numero: 15,
    dataRetiradaStr: '14/11/2026 10:00',
    dataDevolucaoPrevistaStr: '18/11/2026 10:00',
    valorDiariaStr: 'R$ 229,00',
    valorCaucaoStr: 'R$ 1.200,00',
    diariasPrevistas: 4,
    valorTotalPrevistoStr: 'R$ 916,00',
  },
  vistoriaEntrada: {
    dataHoraStr: '14/11/2026 10:02',
    kmRegistrado: 12000,
    nivelCombustivel: 1,
  },
  tabelaCobrancas: {
    versaoConfiguracao: 1,
    franquiaKm: 300,
    baseFranquia: 'TOTAL',
    valorKmExcedenteStr: 'R$ 1,50',
    valorTanqueCompletoStr: 'R$ 350,00',
    toleranciaAtrasoMinutos: 59,
    unidadeAtraso: 'HORA',
    fracionamentoAtraso: 'PROPORCIONAL',
    valorUnidadeAtrasoStr: 'R$ 40,00',
    baseAvarias: 'ESTIMATIVA',
  },
  clausulas: [
    { numero: 1, titulo: 'Objeto da locação', texto: 'O locador entrega ao locatário o veículo descrito neste contrato.' },
    { numero: 2, titulo: 'Prazos', texto: 'A locação começa na data de retirada.' },
    { numero: 3, titulo: 'Valores', texto: 'A diária é a indicada neste contrato.' },
    { numero: 4, titulo: 'Caução', texto: 'Quando há caução, o valor é pré-autorizado.' },
    { numero: 5, titulo: 'Política de combustível', texto: 'O veículo é retirado com o nível registrado.' },
    { numero: 6, titulo: 'Quilometragem', texto: 'A franquia de quilometragem é a indicada neste contrato.' },
    { numero: 7, titulo: 'Avarias', texto: 'Avarias registradas na devolução são cobradas.' },
    {
      numero: 8,
      titulo: 'Natureza do registro',
      texto:
        'Este documento é um registro gerado pelo sistema AutoVia. O texto do documento fica guardado com um código de conferência (SHA-256), que permite verificar se ele foi alterado depois da assinatura. Não tem certificação ICP-Brasil.',
    },
  ],
};

/** Documento sem configuração de cobranças da categoria: sem cláusula de combustível e sem aceite de combustível. */
export const DOCUMENTO_SEM_COBRANCAS: ContratoDocumento = {
  ...DOCUMENTO_CONTRATO,
  tabelaCobrancas: null,
  clausulas: DOCUMENTO_CONTRATO.clausulas.filter((clausula) => clausula.numero !== 5),
};

export const CONTRATO_PENDENTE: ContratoLocacao = {
  idLocacao: 15,
  situacao: 'PENDENTE_ASSINATURA',
  hashSha256: null,
  documento: DOCUMENTO_CONTRATO,
  assinatura: null,
};

export const HASH_EXEMPLO = 'a'.repeat(64);

export const CONTRATO_ASSINADO: ContratoLocacao = {
  idLocacao: 15,
  situacao: 'ASSINADO',
  hashSha256: HASH_EXEMPLO,
  documento: DOCUMENTO_CONTRATO,
  assinatura: {
    nomeAssinante: 'João Pereira Silva',
    aceiteCondicoesGerais: true,
    aceitePoliticaCombustivel: true,
    assinaturaPngBase64: 'iVBORw0KGgo=',
    assinadoEmStr: '14/11/2026 14:30',
    idFuncionarioColetor: 4,
    nomeFuncionarioColetor: 'Carlos Mendes',
  },
};
