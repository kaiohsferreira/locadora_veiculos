import { formatarNumero } from '@/lib/formatters';
import { MENSAGENS_CONTRATO_LOCACAO as M } from '@/lib/mensagensContratoLocacao';
import { MENSAGENS_RETIRADA } from '@/lib/mensagensRetirada';
import { mensagemReservasDiariaVezes } from '@/lib/mensagensReservas';
import type {
  ContratoAssinatura,
  ContratoDadosLocacao,
  ContratoDocumento,
  ContratoLocacao,
  ContratoTabelaCobrancas,
} from '@/types/contratoLocacao';

/** Teto da assinatura no navegador (contrato-locacao.contract.json, RN-5 e VO): 600 por 200 pixels no máximo. */
export const LARGURA_MAXIMA_ASSINATURA = 600;
export const ALTURA_MAXIMA_ASSINATURA = 200;
/** Teto dos bytes decodificados do PNG (ConstantsValues.ContratoAssinaturaTamanhoMaximoBytes). */
export const TETO_ASSINATURA_BYTES = 65_536;

export interface DimensoesImagem {
  largura: number;
  altura: number;
}

/**
 * Tamanho da assinatura dentro do teto, mantendo a proporção. Nunca amplia: uma imagem menor que o teto volta igual.
 * Tamanho zero (quadro vazio) devolve zero.
 */
export function dimensoesAssinatura(largura: number, altura: number): DimensoesImagem {
  if (largura <= 0 || altura <= 0) return { largura: 0, altura: 0 };
  const escala = Math.min(1, LARGURA_MAXIMA_ASSINATURA / largura, ALTURA_MAXIMA_ASSINATURA / altura);
  return {
    largura: Math.max(1, Math.round(largura * escala)),
    altura: Math.max(1, Math.round(altura * escala)),
  };
}

/** Quantidade de bytes de um texto em base64 depois de decodificado (com o preenchimento '=' descontado). */
export function bytesDeBase64(base64: string): number {
  const limpo = base64.replace(/\s/g, '');
  if (limpo.length === 0) return 0;
  const preenchimento = limpo.endsWith('==') ? 2 : limpo.endsWith('=') ? 1 : 0;
  return Math.floor((limpo.length * 3) / 4) - preenchimento;
}

/** Verdadeiro quando o texto tem caractere de controle (código menor que 32 ou o DEL), que o servidor recusa. */
export function temCaractereDeControle(texto: string): boolean {
  for (let indice = 0; indice < texto.length; indice += 1) {
    const codigo = texto.charCodeAt(indice);
    if (codigo < 32 || codigo === 127) return true;
  }
  return false;
}

/** Teto de caracteres do PNG em base64 (65.536 bytes decodificados, em 4 caracteres a cada 3 bytes). */
export const TETO_ASSINATURA_BASE64 = 87_384;

/**
 * Verdadeiro quando o PNG guardado pode ir para o src de uma imagem: só o alfabeto base64, com preenchimento '=' no fim,
 * dentro do teto. Evita montar o src com texto que não seja base64.
 */
export function imagemAssinaturaValida(assinaturaBase64: string): boolean {
  if (assinaturaBase64.length === 0 || assinaturaBase64.length > TETO_ASSINATURA_BASE64) return false;
  return /^[A-Za-z0-9+/]+={0,2}$/.test(assinaturaBase64);
}

/**
 * O aceite de política de combustível é exigido quando o documento tem a configuração de cobranças da categoria
 * (tabelaCobrancas, RN-12, O-6). É o mesmo critério do servidor, e a cláusula de combustível só existe nesse caso.
 */
export function exigeAceiteCombustivel(documento: ContratoDocumento): boolean {
  return documento.tabelaCobrancas != null;
}

/**
 * Verdadeiro só quando o contrato assinado tem o aceite de combustível gravado como true e a cláusula consta do
 * documento. O servidor grava o aceite sem nulo, então "não nulo" não basta: um false não pode virar "Aceito".
 */
export function aceiteCombustivelAssinado(assinatura: ContratoAssinatura, documento: ContratoDocumento): boolean {
  return assinatura.aceitePoliticaCombustivel === true && exigeAceiteCombustivel(documento);
}

/** Problema do quadro de assinatura, ou null quando o PNG pode ser enviado. */
export function problemaAssinaturaPng(assinaturaBase64: string | null): string | null {
  if (assinaturaBase64 === null || assinaturaBase64.length === 0) return M.assinaturaParaAssinar;
  if (bytesDeBase64(assinaturaBase64) > TETO_ASSINATURA_BYTES) return M.assinaturaTamanho;
  return null;
}

/** Hora (HH:mm) de um texto dd/MM/yyyy HH:mm, ou null quando não há hora. */
export function horaDaDataHora(texto: string | null | undefined): string | null {
  if (!texto) return null;
  const encontrado = /(\d{2}:\d{2})/.exec(texto);
  return encontrado ? encontrado[1] : null;
}

export type StatusEtapa = 'concluida' | 'ativa' | 'pendente';

export interface EtapaContrato {
  titulo: string;
  status: StatusEtapa;
  /** Linha de apoio sob o título; nula quando a etapa não tem linha. */
  situacao: string | null;
}

/**
 * Trilha de quatro etapas (nomes e textos da tela 16). A conferência e a vistoria já estão concluídas quando o contrato
 * existe; o contrato fica ativo enquanto não há assinatura (o selo "Em andamento" é mostrado pela trilha, ao lado do
 * rótulo da etapa, sem linha de apoio) e concluído, com a hora, depois dela.
 */
export function etapasDoContrato(contrato: ContratoLocacao): EtapaContrato[] {
  const horaVistoria = horaDaDataHora(contrato.documento.vistoriaEntrada.dataHoraStr);
  const assinado = contrato.situacao === 'ASSINADO';
  const horaAssinatura = horaDaDataHora(contrato.assinatura?.assinadoEmStr);
  const [conferencia, vistoria, contratoEtapa, entrega] = MENSAGENS_RETIRADA.passos;
  return [
    { titulo: conferencia, status: 'concluida', situacao: M.etapaConcluida },
    {
      titulo: vistoria,
      status: 'concluida',
      situacao: horaVistoria ? M.etapaConcluidaComHora(horaVistoria) : M.etapaConcluida,
    },
    assinado
      ? {
          titulo: contratoEtapa,
          status: 'concluida',
          situacao: horaAssinatura ? M.etapaConcluidaComHora(horaAssinatura) : M.etapaConcluida,
        }
      : { titulo: contratoEtapa, status: 'ativa', situacao: null },
    { titulo: entrega, status: 'pendente', situacao: null },
  ];
}

/**
 * Texto da franquia de km do cartão do resumo, ou "Não informada" sem configuração da categoria. Nulo ou ausente
 * (o contrato diz nulo; a tela aceita a omissão do campo sem quebrar a página).
 */
export function textoFranquia(tabela: ContratoTabelaCobrancas | null | undefined): string {
  if (tabela == null) return M.franquiaNaoInformada;
  const km = formatarNumero(tabela.franquiaKm);
  return tabela.baseFranquia === 'TOTAL' ? M.franquiaTotal(km) : M.franquiaPorDiaria(km);
}

/** Percentual de combustível (0 a 100) do nível gravado de 0 a 1 (N1). Valores fora da faixa são limitados. */
export function percentualCombustivel(nivel: number): number {
  return Math.min(100, Math.max(0, Math.round(nivel * 100)));
}

/** Títulos das cláusulas como o servidor os manda (contrato-locacao, clausulas_candidatas). Identificam a cláusula, porque o número muda com a renumeração (RN-6, RN-12). */
export const TITULO_CLAUSULA_PRAZOS = 'Prazos';
export const TITULO_CLAUSULA_VALORES = 'Valores';
export const TITULO_CLAUSULA_CAUCAO = 'Caução';
export const TITULO_CLAUSULA_COMBUSTIVEL = 'Política de combustível';
export const TITULO_CLAUSULA_QUILOMETRAGEM = 'Quilometragem';

export interface LinhaDadoClausula {
  rotulo: string;
  valor: string;
}

/** Dados que a cláusula diz estarem "indicados neste contrato", vindos do próprio documento (sem cálculo). */
export function dadosDaClausula(titulo: string, documento: ContratoDocumento): LinhaDadoClausula[] {
  const { locacao, tabelaCobrancas: tabela } = documento;
  const linhas: LinhaDadoClausula[] = [];
  if (titulo === TITULO_CLAUSULA_PRAZOS) {
    linhas.push({ rotulo: M.dadosClausulas.retirada, valor: locacao.dataRetiradaStr });
    linhas.push({ rotulo: M.dadosClausulas.devolucaoPrevista, valor: locacao.dataDevolucaoPrevistaStr });
  }
  if (titulo === TITULO_CLAUSULA_VALORES) {
    linhas.push({ rotulo: M.dadosClausulas.diaria, valor: locacao.valorDiariaStr });
    if (locacao.valorCaucaoStr != null) linhas.push({ rotulo: M.dadosClausulas.caucao, valor: locacao.valorCaucaoStr });
  }
  if (titulo === TITULO_CLAUSULA_COMBUSTIVEL && tabela != null) {
    linhas.push({ rotulo: M.dadosClausulas.tanqueCompleto, valor: tabela.valorTanqueCompletoStr });
  }
  if (titulo === TITULO_CLAUSULA_QUILOMETRAGEM && tabela != null) {
    linhas.push({ rotulo: M.dadosClausulas.franquia, valor: textoFranquia(tabela) });
    linhas.push({ rotulo: M.dadosClausulas.kmExcedente, valor: tabela.valorKmExcedenteStr });
  }
  return linhas;
}

/** Texto do cartão "Diárias" do resumo (anexo tela 08, N12): "{n} x R$ {valor da diária}". Os valores vêm prontos do servidor. */
export function textoCartaoDiarias(locacao: ContratoDadosLocacao): string {
  return M.diariasCartao(locacao.diariasPrevistas, locacao.valorDiariaStr);
}

/** Linhas do quadro de valores previstos da cláusula de valores (anexo tela 08). Só diárias. */
export interface ValoresPrevistos {
  frase: string;
  /** "4 diárias × R$ 229,00", a mesma forma das reservas. */
  linhaDiarias: string;
  subtotal: string;
  total: string;
}

/**
 * Quadro de valores previstos. Como o total previsto é só de diárias (extras, caução e taxas ficam para o encerramento),
 * o subtotal da linha é o próprio total. Os valores chegam formatados do servidor; a tela não recalcula.
 */
export function valoresPrevistos(locacao: ContratoDadosLocacao): ValoresPrevistos {
  return {
    frase: M.valoresPrevistosFrase(locacao.valorTotalPrevistoStr),
    linhaDiarias: mensagemReservasDiariaVezes(locacao.diariasPrevistas, locacao.valorDiariaStr),
    subtotal: locacao.valorTotalPrevistoStr,
    total: locacao.valorTotalPrevistoStr,
  };
}

/** Verdadeiro quando a falha do servidor é a da vistoria de entrada ausente (RP-06), a que leva ao registro da vistoria. */
export function ehSemVistoriaEntrada(mensagem: string): boolean {
  return mensagem.includes(M.citacaoSemVistoria);
}
