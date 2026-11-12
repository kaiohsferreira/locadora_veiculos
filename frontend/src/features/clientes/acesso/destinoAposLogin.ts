/**
 * Decide para onde a pessoa vai depois do login (ou quando já está autenticada e abre /entrar).
 * Funções puras: não lêem sessão nem navegam; quem chama decide o que fazer com o caminho.
 */
import { CAMINHOS, obterCaminhoRetorno } from '@/routes/caminhos';
import type { PerfilApi, PerfilUsuario } from '@/types/autenticacao';

/**
 * Perfil marcado ao abrir a tela: Equipe quando a página pedida é do back office,
 * Cliente nos demais casos.
 */
export function perfilInicialPara(estado: unknown): PerfilApi {
  const retorno = obterCaminhoRetorno(estado, '');
  return retorno.startsWith(CAMINHOS.backOffice) ? 'FUNCIONARIO' : 'CLIENTE';
}

/**
 * Caminho interno para onde seguir após o login. Usa o retorno de `location.state`
 * (só caminhos internos). Cliente nunca vai para o back office: cai no início,
 * em vez de "acesso negado".
 */
export function resolverDestinoAposLogin(perfil: PerfilUsuario, estado: unknown): string {
  const padrao = perfil === 'cliente' ? CAMINHOS.inicio : CAMINHOS.backOffice;
  const destino = obterCaminhoRetorno(estado, padrao);
  if (perfil === 'cliente' && destino.startsWith(CAMINHOS.backOffice)) {
    return CAMINHOS.inicio;
  }
  return destino;
}
