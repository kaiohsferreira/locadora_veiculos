/**
 * Conferência dos arquivos escolhidos antes da fila: tipo, tamanho e dimensão (lidas do cabeçalho do arquivo, sem
 * decodificar a imagem). É só conveniência: o servidor decide em cada envio. Se o cabeçalho não puder ser lido, o
 * arquivo segue para o servidor. A prévia só é criada para o que passa.
 */
import type { CabecalhoFoto } from '@/lib/cabecalhoFoto';
import type { ArquivoParaFila } from '@/lib/filaFotosVeiculo';
import { mensagemRecusaFoto, validarArquivoFoto, validarDimensoesFoto } from '@/lib/fotosVeiculo';

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

/** Dependências da conferência: o navegador cria a prévia e lê o cabeçalho do arquivo. */
export interface DependenciasAnaliseArquivos {
  criarPrevia(arquivo: File): string;
  lerCabecalho(arquivo: File): Promise<CabecalhoFoto | null>;
}

/**
 * Confere cada arquivo em ordem. Recusado por tipo, tamanho ou dimensão (lida do cabeçalho): não cria prévia e o
 * motivo vai para a lista de recusas. Os demais seguem para a fila, com a prévia e o cabeçalho.
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

    const cabecalho = await dependencias.lerCabecalho(arquivo);
    const motivoDimensao = cabecalho === null ? null : validarDimensoesFoto(cabecalho.largura, cabecalho.altura);
    if (motivoDimensao !== null) {
      recusas.push({ nome: arquivo.name, mensagem: mensagemRecusaFoto(motivoDimensao) });
      continue;
    }

    validos.push({ arquivo, previa: dependencias.criarPrevia(arquivo), cabecalho });
  }

  return { validos, recusas };
}
