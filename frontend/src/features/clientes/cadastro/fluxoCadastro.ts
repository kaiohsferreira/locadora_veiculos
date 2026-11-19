/**
 * Lógica pura do envio do cadastro (P2-FE-02 e P2-FE-03): sem React, sem Axios. As dependências
 * (cadastrar, entrar e enviar documento) chegam por parâmetro, para o teste usar dublês.
 */
import { documentosPendentes, type ArquivosDocumento } from '@/lib/documentosCliente';
import { somenteDigitos } from '@/lib/formatters';
import { MENSAGEM_DOCUMENTO_SEM_ID_CLIENTE } from '@/lib/mensagensCadastro';
import type { RequisicaoLogin } from '@/types/autenticacao';
import type { RequisicaoCadastroCliente, RespostaCliente } from '@/types/clientes';
import type { DocumentoClienteGravado, TipoDocumentoCliente } from '@/types/documentosCliente';
import { converterFormularioParaRequisicao } from './conversaoCadastro';
import type { FormularioCadastro } from './esquemaCadastro';

/** Campos que a 409 (conflito) pode apontar. */
export type CampoConflito = 'cpf' | 'cnhNumero' | 'email';

/**
 * Qual campo o conflito 409 aponta, pela mensagem da API ("...CNH...", "...CPF..." ou "...e-mail...").
 * Null quando a mensagem não cita nenhum deles: vai para o alerta geral.
 */
export function escolherCampoDoConflito(mensagem: string): CampoConflito | null {
  if (mensagem.includes('CNH')) return 'cnhNumero';
  if (mensagem.includes('CPF')) return 'cpf';
  if (/e-?mail/i.test(mensagem)) return 'email';
  return null;
}

/** Ordem em que os campos aparecem na tela (seções, de cima para baixo). */
export const ORDEM_CAMPOS_TELA: readonly (keyof FormularioCadastro)[] = [
  'nome',
  'cpf',
  'dataNascimento',
  'email',
  'telefone',
  'cnhNumero',
  'cnhCategoria',
  'cnhValidade',
  'cep',
  'logradouro',
  'numero',
  'complemento',
  'bairro',
  'cidade',
  'uf',
  'senha',
  'confirmarSenha',
];

/** Primeiro campo da lista na ordem da tela (não na ordem em que o backend a enviou). */
export function primeiroCampoNaTela(campos: readonly string[]): keyof FormularioCadastro | undefined {
  return ORDEM_CAMPOS_TELA.find((campo) => campos.includes(campo));
}

/** Dependências do envio: o POST do cadastro, o login automático e o envio de cada documento. */
export interface DependenciasCadastro {
  cadastrar: (requisicao: RequisicaoCadastroCliente) => Promise<RespostaCliente>;
  entrar: (requisicao: RequisicaoLogin) => Promise<unknown>;
  enviarDocumento: (
    idCliente: number,
    tipo: TipoDocumentoCliente,
    arquivo: File,
  ) => Promise<DocumentoClienteGravado>;
}

/**
 * Entrada do envio. `valores` null quer dizer que o cadastro já existe nesta sessão (nova tentativa ou
 * cliente logado só para enviar): não há POST nem login. `idCliente` é o id do cliente criado (ou da sessão).
 * `arquivos` é o que a pessoa escolheu; `enviados` é o que já subiu (o mesmo arquivo não sobe de novo).
 */
export interface EntradaCadastro {
  valores: FormularioCadastro | null;
  idCliente: number | null;
  arquivos: ArquivosDocumento;
  enviados: ArquivosDocumento;
}

/**
 * Resultado do envio:
 * - `erro-cadastro`: o POST (ou a conversão) falhou; o login e os documentos não foram tentados;
 * - `sem-login`: cadastro criado, mas o login automático falhou (não se refaz o POST; nada foi enviado);
 * - `erro-documento`: um documento falhou; os seguintes não foram tentados. Traz o id e o que já subiu;
 * - `concluido`: cadastro (se houve), login e todos os documentos pendentes enviados.
 */
export type ResultadoCadastro =
  | { tipo: 'erro-cadastro'; erro: unknown }
  | { tipo: 'sem-login' }
  | {
      tipo: 'erro-documento';
      documento: TipoDocumentoCliente;
      erro: unknown;
      idCliente: number;
      enviados: ArquivosDocumento;
    }
  | { tipo: 'concluido'; idCliente: number; enviados: ArquivosDocumento };

/**
 * Sequência do envio: POST uma vez; se criou, login automático com perfil CLIENTE e CPF só com dígitos;
 * depois, a CNH e o comprovante, na ordem da tela, pulando o que já subiu. Para na primeira falha.
 * Nunca chama o POST duas vezes.
 */
export async function executarCadastro(
  dependencias: DependenciasCadastro,
  entrada: EntradaCadastro,
): Promise<ResultadoCadastro> {
  let idCliente = entrada.idCliente;

  if (entrada.valores !== null) {
    try {
      const criado = await dependencias.cadastrar(converterFormularioParaRequisicao(entrada.valores));
      idCliente = criado.idCliente;
    } catch (erro) {
      return { tipo: 'erro-cadastro', erro };
    }

    try {
      await dependencias.entrar({
        perfil: 'CLIENTE',
        cpf: somenteDigitos(entrada.valores.cpf),
        senha: entrada.valores.senha,
      });
    } catch {
      return { tipo: 'sem-login' };
    }
  }

  if (idCliente === null) {
    return { tipo: 'erro-cadastro', erro: new Error(MENSAGEM_DOCUMENTO_SEM_ID_CLIENTE) };
  }

  let enviados: ArquivosDocumento = { ...entrada.enviados };
  for (const tipo of documentosPendentes(entrada.arquivos, entrada.enviados)) {
    const arquivo = entrada.arquivos[tipo];
    if (arquivo === undefined) continue;
    try {
      await dependencias.enviarDocumento(idCliente, tipo, arquivo);
    } catch (erro) {
      return { tipo: 'erro-documento', documento: tipo, erro, idCliente, enviados };
    }
    enviados = { ...enviados, [tipo]: arquivo };
  }

  return { tipo: 'concluido', idCliente, enviados };
}

/**
 * O que a página faz com o resultado do envio. `cadastroNovo` é verdadeiro quando a sequência criou a conta
 * (botão final da etapa 2): só nesse caso o fim leva ao início. Nos demais casos a página fica na tela.
 */
export type AcaoAposEnvio = 'erro-cadastro' | 'ir-entrar' | 'ficar-com-falha' | 'ir-inicio' | 'avisar-concluido';

export function acaoAposEnvio(resultado: ResultadoCadastro, cadastroNovo: boolean): AcaoAposEnvio {
  switch (resultado.tipo) {
    case 'erro-cadastro':
      return 'erro-cadastro';
    case 'sem-login':
      return 'ir-entrar';
    case 'erro-documento':
      return 'ficar-com-falha';
    case 'concluido':
      return cadastroNovo ? 'ir-inicio' : 'avisar-concluido';
  }
}
