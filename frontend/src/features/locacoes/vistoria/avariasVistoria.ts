import type { AnguloAvaria, AvariaRegistrada, DadosNovaAvaria } from '@/types/avarias';
import { ANGULOS_AVARIA } from '@/types/avarias';
import { MENSAGEM_FOTOS_AGUARDANDO, MENSAGEM_FOTOS_ERRO_ENVIO, MENSAGEM_FOTOS_TENTAR_DE_NOVO } from '@/lib/mensagensFotosVeiculo';
import { MENSAGEM_AVARIA_ENVIANDO, MENSAGEM_AVARIA_FOTO_PENDENTE } from '@/lib/mensagensVistoriaAvarias';

/** Rótulo curto do ângulo: título do cartão da avaria e nome do quadrante em acessibilidade (N11.4 e TROCAR da tela 19). */
export const ROTULO_ANGULO: Readonly<Record<AnguloAvaria, string>> = {
  FRENTE: 'Frente',
  TRASEIRA: 'Traseira',
  LATERAL_ESQUERDA: 'Lateral Esquerda',
  LATERAL_DIREITA: 'Lateral Direita',
};

/** Título do quadrante, como no desenho 19. */
export const NOME_QUADRANTE: Readonly<Record<AnguloAvaria, string>> = {
  FRENTE: '1. Vista Frontal (Frente)',
  TRASEIRA: '2. Vista Traseira (Traseira)',
  LATERAL_ESQUERDA: '3. Lateral Esquerda (Motorista)',
  LATERAL_DIREITA: '4. Lateral Direita (Passageiro)',
};

/** Legenda embaixo de cada quadrante, como no desenho 19. */
export const LEGENDA_QUADRANTE: Readonly<Record<AnguloAvaria, string>> = {
  FRENTE: 'Eixo Frontal • Para-choque & Capô',
  TRASEIRA: 'Eixo Traseiro • Tampa & Lanternas',
  LATERAL_ESQUERDA: 'Portas, Caixas de Roda e Retrovisor Esquerdo',
  LATERAL_DIREITA: 'Porta Dianteira/Traseira e Estribo Direito',
};

/** Contagem de avarias por ângulo (quadrantes). Avaria sem ângulo não entra em nenhum quadrante. */
export function contarPorAngulo(avarias: readonly { angulo: AnguloAvaria | null }[]): Record<AnguloAvaria, number> {
  const contagem: Record<AnguloAvaria, number> = { FRENTE: 0, TRASEIRA: 0, LATERAL_ESQUERDA: 0, LATERAL_DIREITA: 0 };
  for (const avaria of avarias) {
    if (avaria.angulo !== null) contagem[avaria.angulo] += 1;
  }
  return contagem;
}

/** Soma dos custos estimados. Valor ausente não entra na soma. */
export function totalEstimado(valores: readonly (number | null)[]): number {
  return valores.reduce<number>((soma, valor) => (valor === null ? soma : soma + valor), 0);
}

/** Quilometragem acima da retirada, ou nulo quando a quilometragem informada é menor que a da retirada. */
export function kmAcimaDaRetirada(kmInformado: number, kmSaida: number): number | null {
  return kmInformado >= kmSaida ? kmInformado - kmSaida : null;
}

/**
 * Avaria ainda não confirmada pelo servidor. `enviando` existe só durante a chamada. `foto_pendente` quer dizer que a
 * avaria já foi gravada (idAvaria) e só falta a foto; `erro` quer dizer que nada foi gravado e o POST é repetido.
 */
export type SituacaoItemLocal = 'aguardando' | 'enviando' | 'foto_pendente' | 'erro';

export interface ItemAvariaLocal {
  /** Chave local e estável, para a lista e as ações. */
  chave: number;
  dados: DadosNovaAvaria;
  /** Prévia da foto (URL de objeto), liberada quando o item sai da lista. */
  previa: string;
  situacao: SituacaoItemLocal;
  /** Preenchido quando a avaria já foi gravada e só falta a foto (502 do POST). */
  idAvaria: number | null;
  /** Motivo da recusa do POST (400, 404 ou 409), texto do servidor já filtrado por obterMensagemErro. Some na próxima tentativa. */
  mensagemErro?: string;
  /** Recusa que repetir não resolve (400 ou 409): o item não oferece "Tentar de novo". */
  semRepeticao?: boolean;
}

/**
 * Avarias locais que ainda não chegaram ao servidor (aguardando) viram erro: aparecem o aviso de envio parcial e o
 * "Tentar de novo". Usado quando a entrada já existe e o registro foi recusado com 409: nada é enviado sem o clique do usuário.
 */
export function marcarAguardandoComoErro(locais: readonly ItemAvariaLocal[]): ItemAvariaLocal[] {
  return locais.map((item) => (item.situacao === 'aguardando' ? { ...item, situacao: 'erro' } : item));
}

/** Texto da situação de um item local na lista (só o que está aprovado ou já existe na tela 05). */
export function textoSituacaoItemLocal(situacao: SituacaoItemLocal): string {
  switch (situacao) {
    case 'aguardando':
      return MENSAGEM_FOTOS_AGUARDANDO;
    case 'enviando':
      return MENSAGEM_AVARIA_ENVIANDO;
    case 'foto_pendente':
      return MENSAGEM_AVARIA_FOTO_PENDENTE;
    case 'erro':
      return MENSAGEM_FOTOS_ERRO_ENVIO;
  }
}

/**
 * "Redefinir Marcadores": tira só o ângulo e a posição (tudo ou nada, N8). A avaria continua, sem posição. Vale para as
 * avarias ainda não gravadas; as já gravadas não mudam daqui.
 */
export function tirarMarcadores(dados: DadosNovaAvaria): DadosNovaAvaria {
  return { ...dados, angulo: null, posX: null, posY: null };
}

/** Há marcador a tirar: alguma avaria ainda não gravada com ângulo. Sem isso, o botão Redefinir Marcadores não aparece. */
export function temMarcadorParaRedefinir(locais: readonly ItemAvariaLocal[]): boolean {
  return locais.some((item) => item.idAvaria === null && item.dados.angulo !== null);
}

/**
 * Foto que não carrega (URL expirada ou arquivo sumido). A primeira falha da mesma avaria pede a lista de novo, porque a URL
 * dura pouco (15 min); se a imagem falha de novo, aparece o erro aprovado da foto.
 */
export function decidirFalhaDaImagem(jaRecarregouEstaAvaria: boolean): 'recarregar_lista' | 'mostrar_erro' {
  return jaRecarregouEstaAvaria ? 'mostrar_erro' : 'recarregar_lista';
}

/** Botão de repetir o envio: só nas situações em que há o que repetir. */
export function podeTentarDeNovo(situacao: SituacaoItemLocal): boolean {
  return situacao === 'foto_pendente' || situacao === 'erro';
}

export const MENSAGEM_BOTAO_TENTAR_DE_NOVO = MENSAGEM_FOTOS_TENTAR_DE_NOVO;

/** Uma linha da lista da tela: uma avaria do servidor OU uma avaria local ainda não confirmada, com o número da tela. */
export interface LinhaAvaria {
  numero: number;
  origem: 'servidor' | 'local';
  avaria: AvariaRegistrada | null;
  local: ItemAvariaLocal | null;
}

/**
 * Ordem de exibição e numeração: avarias do servidor (por id) e depois as locais, numeradas em sequência. Uma avaria
 * gravada sem foto (item local `foto_pendente`) aparece só como local, com o botão de repetir a foto; a versão do
 * servidor dela é escondida para não aparecer duas vezes.
 */
export function numerarAvarias(gravadas: readonly AvariaRegistrada[], locais: readonly ItemAvariaLocal[]): LinhaAvaria[] {
  const idsLocais = new Set(locais.map((item) => item.idAvaria).filter((id): id is number => id !== null));
  const servidor: Omit<LinhaAvaria, 'numero'>[] = gravadas
    .filter((avaria) => !idsLocais.has(avaria.idAvaria))
    .map((avaria) => ({ origem: 'servidor', avaria, local: null }));
  const local: Omit<LinhaAvaria, 'numero'>[] = locais.map((item) => ({ origem: 'local', avaria: null, local: item }));
  return [...servidor, ...local].map((linha, indice) => ({ numero: indice + 1, ...linha }));
}

/** Ângulos na ordem do desenho (frente, traseira, laterais). */
export const ORDEM_ANGULOS: readonly AnguloAvaria[] = ANGULOS_AVARIA;
