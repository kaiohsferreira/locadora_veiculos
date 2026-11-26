import type { GravidadeAvaria } from './enums';

/** Ângulos do desenho 19 (contrato avaria, v6). Gravados como texto no servidor. */
export const ANGULOS_AVARIA = ['FRENTE', 'TRASEIRA', 'LATERAL_ESQUERDA', 'LATERAL_DIREITA'] as const;
export type AnguloAvaria = (typeof ANGULOS_AVARIA)[number];

/** Foto da avaria: URL temporária de leitura (até 900 s). Não é guardada fora da tela. */
export interface FotoAvaria {
  url: string;
  expiraEm: string;
}

/** Avaria como o servidor devolve (ReturnAvariaVO, contrato avaria v6). */
export interface AvariaRegistrada {
  idAvaria: number;
  idVistoria: number;
  descricao: string;
  gravidade: GravidadeAvaria;
  valorEstimado: number | null;
  angulo: AnguloAvaria | null;
  /** Percentual de 0 a 100 na largura do quadrante, nulo quando não informado. */
  posX: number | null;
  /** Percentual de 0 a 100 na altura do quadrante, nulo quando não informado. */
  posY: number | null;
  /** Nulo quando o arquivo não existe no armazenamento. */
  foto: FotoAvaria | null;
  /** Só na lista, em avaria de entrada sem arquivo. O texto exibido vem do front. */
  erroFoto: string | null;
}

/** Campos do formulário já convertidos para o envio (texto no formato do servidor, foto reduzida ou original). */
export interface DadosNovaAvaria {
  descricao: string;
  gravidade: GravidadeAvaria;
  /** "150.50" (ponto decimal, até 2 casas) ou nulo quando vazio. */
  valorEstimado: string | null;
  angulo: AnguloAvaria | null;
  posX: string | null;
  posY: string | null;
  foto: File;
}
