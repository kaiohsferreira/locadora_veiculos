import { useState } from 'react';
import { aoFalharFotoCapa, criarControleNovaConsultaFoto } from '@/lib/fotosVeiculo';

/**
 * Devolve o aviso de imagem de capa que falhou. A decisão (chamar `recarregar` só na primeira falha da visita) fica em
 * `aoFalharFotoCapa`, coberta por teste; aqui só se guarda o controle no estado da tela. O controle dura enquanto a tela
 * estiver montada: uma consulta por visita, mesmo com várias imagens falhando juntas.
 */
export function useAoFalharFotoCapa(recarregar: () => void): () => void {
  const [controle] = useState(criarControleNovaConsultaFoto);
  return () => aoFalharFotoCapa(controle, recarregar);
}
