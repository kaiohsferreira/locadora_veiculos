/**
 * Sequência do envio do cadastro da locadora (tela 37): um POST e, se criou, o login automático do responsável.
 * Sem React e sem Axios: as dependências (cadastrar e entrar) chegam por parâmetro, para o teste usar dublês.
 *
 * O POST nunca é refeito: se a conta foi criada e o login falhou, o resultado diz `loginAutomatico: false` e a
 * tela oferece "Fazer login". A senha só passa por esta função dentro da chamada de login; nada é registrado.
 */
import type { RequisicaoLogin } from '@/types/autenticacao';
import type { RequisicaoCadastroLocadora, RespostaEmpresa } from '@/types/empresas';

/** Dependências do envio: o POST do cadastro e o login automático (o mesmo `useEntrar` do cliente). */
export interface DependenciasEnvioLocadora {
  cadastrar: (requisicao: RequisicaoCadastroLocadora) => Promise<RespostaEmpresa>;
  entrar: (requisicao: RequisicaoLogin) => Promise<unknown>;
}

/**
 * Resultado do envio:
 * - `erro`: o POST falhou (a conta não foi criada; o login não foi tentado);
 * - `criado`: a conta existe; `loginAutomatico` diz se a sessão foi aberta.
 */
export type ResultadoEnvioLocadora =
  | { tipo: 'erro'; erro: unknown }
  | { tipo: 'criado'; empresa: RespostaEmpresa; loginAutomatico: boolean };

/**
 * POST uma vez. Se a conta foi criada, login automático com perfil FUNCIONARIO, o CPF do responsável (já só com
 * dígitos) e a senha digitada. Falha do login não desfaz a conta: vira `loginAutomatico: false`.
 */
export async function executarCadastroLocadora(
  dependencias: DependenciasEnvioLocadora,
  requisicao: RequisicaoCadastroLocadora,
): Promise<ResultadoEnvioLocadora> {
  let empresa: RespostaEmpresa;
  try {
    empresa = await dependencias.cadastrar(requisicao);
  } catch (erro) {
    return { tipo: 'erro', erro };
  }

  try {
    await dependencias.entrar({
      perfil: 'FUNCIONARIO',
      cpf: requisicao.responsavelCpf,
      senha: requisicao.senha,
    });
    return { tipo: 'criado', empresa, loginAutomatico: true };
  } catch {
    return { tipo: 'criado', empresa, loginAutomatico: false };
  }
}
