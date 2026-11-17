/**
 * Conferência dos arquivos escolhidos antes da fila: tipo, tamanho e dimensão (lidas da prévia do navegador).
 * É só conveniência: o servidor decide em cada envio. Se a imagem não puder ser lida, o arquivo segue para o servidor.
 */
import {
  mensagemRecusaFoto,
  validarArquivoFoto,
  validarDimensoesFoto,
  type DimensoesFoto,
} from '@/lib/fotosVeiculo';
import type { ArquivoParaFila } from '@/lib/filaFotosVeiculo';

/** Arquivo recusado: o nome (texto puro) e a mensagem do motivo. */
export interface RecusaArquivoFoto {
  nome: string;
  mensagem: string;
}

/** Resultado da conferência: os arquivos que seguem para a fila e as recusas. */
export interface ResultadoAnaliseArquivos {
  validos: ArquivoParaFila[];
  recusas: RecusaArquivoFoto[];
}

/** Dependências da conferência: o navegador cria e libera as prévias e lê as dimensões. */
export interface DependenciasAnaliseArquivos {
  criarPrevia(arquivo: File): string;
  liberarPrevia(previa: string): void;
  lerDimensoes(previa: string): Promise<DimensoesFoto | null>;
}

/** Forma mínima da imagem usada para ler as dimensões (o elemento de imagem do navegador). */
type ImagemParaLeitura = Pick<HTMLImageElement, 'naturalWidth' | 'naturalHeight' | 'src' | 'onload' | 'onerror'>;

/**
 * Lê largura e altura pela prévia. Devolve nulo quando o navegador não consegue decodificar a imagem.
 * A fábrica da imagem é injetável para os testes.
 */
export function lerDimensoesFoto(
  previa: string,
  criarImagem: () => ImagemParaLeitura = () => new Image(),
): Promise<DimensoesFoto | null> {
  return new Promise((resolver) => {
    const imagem = criarImagem();
    imagem.onload = () => resolver({ largura: imagem.naturalWidth, altura: imagem.naturalHeight });
    imagem.onerror = () => resolver(null);
    imagem.src = previa;
  });
}

/**
 * Confere cada arquivo em ordem. Recusado por tipo, tamanho ou dimensão: a prévia é liberada e o motivo
 * vai para a lista de recusas. Os demais seguem para a fila, com a prévia.
 */
export async function analisarArquivosFoto(
  arquivos: readonly File[],
  dependencias: DependenciasAnaliseArquivos,
): Promise<ResultadoAnaliseArquivos> {
  const validos: ArquivoParaFila[] = [];
  const recusas: RecusaArquivoFoto[] = [];

  for (const arquivo of arquivos) {
    const motivoArquivo = validarArquivoFoto(arquivo);
    if (motivoArquivo !== null) {
      recusas.push({ nome: arquivo.name, mensagem: mensagemRecusaFoto(motivoArquivo) });
      continue;
    }

    const previa = dependencias.criarPrevia(arquivo);
    const dimensoes = await dependencias.lerDimensoes(previa);
    const motivoDimensao = dimensoes === null ? null : validarDimensoesFoto(dimensoes.largura, dimensoes.altura);
    if (motivoDimensao !== null) {
      dependencias.liberarPrevia(previa);
      recusas.push({ nome: arquivo.name, mensagem: mensagemRecusaFoto(motivoDimensao) });
      continue;
    }

    validos.push({ arquivo, previa });
  }

  return { validos, recusas };
}
