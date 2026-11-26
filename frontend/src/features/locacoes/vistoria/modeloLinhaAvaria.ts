import type { AnguloAvaria } from '@/types/avarias';
import type { GravidadeAvaria } from '@/types/enums';
import { MENSAGEM_AVARIA_SEM_ANGULO } from '@/lib/mensagensVistoriaAvarias';
import { ROTULO_ANGULO, podeTentarDeNovo, type LinhaAvaria, type SituacaoItemLocal } from './avariasVistoria';

/** O que a lista mostra para uma linha, sem regra de negócio: só a leitura da linha. */
export interface ModeloLinhaAvaria {
  numero: number;
  titulo: string;
  gravidade: GravidadeAvaria;
  descricao: string;
  valorEstimado: number | null;
  angulo: AnguloAvaria | null;
  /** Só na linha local: a situação e se cabe repetir o envio. */
  situacao: SituacaoItemLocal | null;
  /** Motivo da recusa do servidor no lugar do texto genérico de erro (400, 404 ou 409). */
  mensagemErro?: string;
  podeRepetir: boolean;
  chaveLocal: number | null;
  /** Id da avaria no servidor (linha do servidor ou local já gravada sem foto). */
  idAvaria: number | null;
  /** Linha do servidor sem arquivo: a foto mostra o erro e pode ser enviada de novo. */
  semArquivo: boolean;
  urlFoto: string | null;
}

/** Título do cartão: o rótulo do ângulo; sem ângulo (avaria antiga), o texto aprovado "Sem ângulo". */
export function tituloDoAngulo(angulo: AnguloAvaria | null): string {
  return angulo === null ? MENSAGEM_AVARIA_SEM_ANGULO : ROTULO_ANGULO[angulo];
}

/** Converte a linha (servidor ou local) no modelo da tela. O valor local vem como texto e vira número. */
export function modeloLinhaAvaria(linha: LinhaAvaria): ModeloLinhaAvaria {
  if (linha.local !== null) {
    const { dados, situacao, chave, mensagemErro, semRepeticao } = linha.local;
    return {
      numero: linha.numero,
      titulo: tituloDoAngulo(dados.angulo),
      gravidade: dados.gravidade,
      descricao: dados.descricao,
      valorEstimado: dados.valorEstimado === null ? null : Number(dados.valorEstimado),
      angulo: dados.angulo,
      situacao,
      mensagemErro,
      podeRepetir: podeTentarDeNovo(situacao) && semRepeticao !== true,
      chaveLocal: chave,
      idAvaria: linha.local.idAvaria,
      semArquivo: false,
      urlFoto: linha.local.previa,
    };
  }
  const avaria = linha.avaria;
  if (avaria === null) throw new Error('Linha sem avaria nem local.');
  return {
    numero: linha.numero,
    titulo: tituloDoAngulo(avaria.angulo),
    gravidade: avaria.gravidade,
    descricao: avaria.descricao,
    valorEstimado: avaria.valorEstimado,
    angulo: avaria.angulo,
    situacao: null,
    podeRepetir: false,
    chaveLocal: null,
    idAvaria: avaria.idAvaria,
    semArquivo: avaria.foto === null,
    urlFoto: avaria.foto?.url ?? null,
  };
}
