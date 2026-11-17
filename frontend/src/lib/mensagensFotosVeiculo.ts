/**
 * Mensagens do bloco "Fotos do Veículo" (tela 05, cadastro e edição). Reexportadas por `@/lib/mensagens`.
 * Textos do desenho 05 vêm literais; os estados sem desenho são neutros (aprovação do Kaio antes do commit).
 */

// Bloco e zona de envio ------------------------------------------------------

export const MENSAGEM_FOTOS_TITULO = 'Fotos do Veículo';
export const MENSAGEM_FOTOS_APOIO = 'Imagens de alta resolução aumentam a taxa de reserva no catálogo.';
export const MENSAGEM_FOTOS_ARRASTE = 'Arraste fotos aqui';
export const MENSAGEM_FOTOS_CLIQUE = 'ou clique para selecionar do computador';
export const MENSAGEM_FOTOS_FORMATO = 'JPG ou PNG até 10MB por arquivo';
export const MENSAGEM_FOTOS_ZONA_ROTULO = 'Carregar fotos do veículo';
export const MENSAGEM_FOTOS_SELECIONADAS = 'Fotos Selecionadas';
export const MENSAGEM_FOTOS_ADICIONAR_MAIS = '+ Adicionar Mais Fotos';

/** "4/15 Fotos" (o total vem do contador; o máximo vem da constante, nunca de número solto). */
export function mensagemContadorFotos(total: number, maximo: number): string {
  return `${total}/${maximo} Fotos`;
}

// Miniaturas -----------------------------------------------------------------

export const MENSAGEM_FOTOS_PRINCIPAL = 'Foto Principal';

/** Texto alternativo da foto: "Foto do veículo Jeep Renegade", ou só "Foto do veículo" sem descrição. */
export function mensagemFotoAlt(descricaoVeiculo?: string): string {
  const descricao = descricaoVeiculo?.trim() ?? '';
  return descricao ? `Foto do veículo ${descricao}` : 'Foto do veículo';
}

/** Rótulo acessível do botão × de cada miniatura: "Remover imagem 2". */
export function mensagemFotoRemoverNumero(posicao: number): string {
  return `Remover imagem ${posicao}`;
}

export const MENSAGEM_FOTOS_REMOVER_IMAGEM = 'Remover imagem';
export const MENSAGEM_FOTOS_AGUARDANDO = 'Aguardando envio';
export const MENSAGEM_FOTOS_ENVIADA = 'Foto enviada';
export const MENSAGEM_FOTOS_TENTAR_DE_NOVO = 'Tentar de novo';

/** "Enviando… 40%" (progresso de uma foto). */
export function mensagemFotoEnviando(percentual: number): string {
  return `Enviando… ${percentual}%`;
}

/** "Enviando foto 2 de 3" (progresso da fila). */
export function mensagemFotoProgressoFila(atual: number, total: number): string {
  return `Enviando foto ${atual} de ${total}`;
}

// Vazio, carregamento e erros ------------------------------------------------

export const MENSAGEM_FOTOS_VAZIO = 'Nenhuma foto adicionada.';
export const MENSAGEM_FOTOS_ERRO_CARREGAR = 'Não foi possível carregar as fotos do veículo.';
export const MENSAGEM_FOTOS_ERRO_ENVIO = 'Não foi possível enviar esta foto.';
export const MENSAGEM_FOTOS_ERRO_ARMAZENAMENTO = 'O armazenamento de fotos está indisponível. Tente de novo mais tarde.';

// Recusas antes do envio -----------------------------------------------------

export const MENSAGEM_FOTOS_ERRO_TIPO = 'Arquivo não aceito. Use JPG ou PNG.';
export const MENSAGEM_FOTOS_ERRO_TAMANHO = 'A foto passa de 10 MB.';
export const MENSAGEM_FOTOS_ERRO_DIMENSAO = 'A foto é grande demais. Use uma imagem de até 8.192 px de lado e 16 megapixels.';

/** "Limite de 15 fotos atingido. Remova uma foto para adicionar outra." */
export function mensagemFotoLimiteAtingido(maximo: number): string {
  return `Limite de ${maximo} fotos atingido. Remova uma foto para adicionar outra.`;
}

/** "Só cabe mais 1 foto." ou "Só cabem mais 3 fotos.", seguido de "As demais não foram adicionadas.". */
export function mensagemFotoSemVaga(vagas: number): string {
  const cabe = vagas === 1 ? 'Só cabe mais 1 foto.' : `Só cabem mais ${vagas} fotos.`;
  return `${cabe} As demais não foram adicionadas.`;
}

// Remoção --------------------------------------------------------------------

export const MENSAGEM_FOTOS_CONFIRMAR_REMOCAO_TITULO = 'Remover esta foto?';
export const MENSAGEM_FOTOS_CONFIRMAR_REMOCAO_DESCRICAO = 'Não é possível desfazer.';
export const MENSAGEM_FOTOS_CONFIRMAR_REMOVER = 'Remover';
export const MENSAGEM_FOTOS_CONFIRMAR_CANCELAR = 'Cancelar';
export const MENSAGEM_FOTOS_ERRO_REMOVER = 'Não foi possível remover a foto. Tente de novo.';
export const MENSAGEM_FOTOS_REMOVIDA = 'Foto removida.';
