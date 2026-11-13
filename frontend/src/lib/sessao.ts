/**
 * Sessão do usuário no navegador (token + dados mínimos do usuário).
 *
 * Guarda em localStorage, sempre com try/catch: em janela anônima ou com o
 * armazenamento bloqueado a sessão continua valendo enquanto a página estiver
 * aberta (cópia em memória). Outros módulos acompanham as mudanças com
 * `inscreverMudancasSessao` (usado pelo contexto de autenticação).
 *
 * Sessão vencida (`expiraEm` no passado) conta como inexistente: é checada ao
 * carregar o módulo, em `obterToken()` e por um temporizador que encerra a
 * sessão no instante do vencimento.
 */
import { PERFIS_USUARIO, type PerfilUsuario, type UsuarioSessao } from '@/types/autenticacao';
import {
  CARGO_FUNCIONARIO,
  STATUS_CADASTRO_CLIENTE,
  type CargoFuncionario,
  type StatusCadastroCliente,
} from '@/types/enums';

const CHAVE_TOKEN = 'autovia:token';
const CHAVE_USUARIO = 'autovia:usuario';

/** Maior atraso aceito pelo setTimeout (ms). Sessões que vencem depois disso são reagendadas. */
const LIMITE_TEMPORIZADOR_MS = 2_147_483_647;

interface SessaoArmazenada {
  token: string;
  usuario: UsuarioSessao;
}

type OuvinteSessao = () => void;

const ouvintes = new Set<OuvinteSessao>();
let temporizadorVencimento: ReturnType<typeof setTimeout> | null = null;

function lerItem(chave: string): string | null {
  try {
    return window.localStorage.getItem(chave);
  } catch {
    return null;
  }
}

function gravarItem(chave: string, valor: string): void {
  try {
    window.localStorage.setItem(chave, valor);
  } catch {
    // Armazenamento indisponível: a sessão fica só em memória.
  }
}

function removerItem(chave: string): void {
  try {
    window.localStorage.removeItem(chave);
  } catch {
    // Armazenamento indisponível: nada a remover.
  }
}

function ehPerfilUsuario(valor: unknown): valor is PerfilUsuario {
  return typeof valor === 'string' && (PERFIS_USUARIO as readonly string[]).includes(valor);
}

function ehCargoFuncionario(valor: unknown): valor is CargoFuncionario {
  return typeof valor === 'string' && (CARGO_FUNCIONARIO as readonly string[]).includes(valor);
}

function ehStatusCadastroCliente(valor: unknown): valor is StatusCadastroCliente {
  return (
    typeof valor === 'string' && (STATUS_CADASTRO_CLIENTE as readonly string[]).includes(valor)
  );
}

function ehDataValida(valor: unknown): valor is string {
  return typeof valor === 'string' && !Number.isNaN(Date.parse(valor));
}

/**
 * Lê o usuário guardado. Só `nome` e `perfil` são obrigatórios: campo opcional
 * com tipo ou valor desconhecido é descartado, sem invalidar a sessão.
 */
function lerUsuarioArmazenado(valor: unknown): UsuarioSessao | null {
  if (typeof valor !== 'object' || valor === null) return null;
  const { nome, perfil, idUsuario, expiraEm, cargo, statusCadastro } = valor as Record<
    string,
    unknown
  >;
  if (typeof nome !== 'string' || !ehPerfilUsuario(perfil)) return null;

  const usuario: UsuarioSessao = { nome, perfil };
  if (typeof idUsuario === 'number' && Number.isInteger(idUsuario)) usuario.idUsuario = idUsuario;
  if (ehDataValida(expiraEm)) usuario.expiraEm = expiraEm;
  if (ehCargoFuncionario(cargo)) usuario.cargo = cargo;
  if (ehStatusCadastroCliente(statusCadastro)) usuario.statusCadastro = statusCadastro;
  return usuario;
}

function lerSessaoArmazenada(): SessaoArmazenada | null {
  const token = lerItem(CHAVE_TOKEN);
  const usuarioJson = lerItem(CHAVE_USUARIO);
  if (!token || !usuarioJson) return null;
  try {
    const usuario: unknown = JSON.parse(usuarioJson);
    const usuarioValido = lerUsuarioArmazenado(usuario);
    return usuarioValido ? { token, usuario: usuarioValido } : null;
  } catch {
    return null;
  }
}

/** Sessão guardada, descartando (e apagando do armazenamento) a que já venceu. */
function carregarSessaoGuardada(): SessaoArmazenada | null {
  const sessao = lerSessaoArmazenada();
  if (sessao === null) return null;
  if (estaVencida(sessao.usuario)) {
    removerItem(CHAVE_TOKEN);
    removerItem(CHAVE_USUARIO);
    return null;
  }
  return sessao;
}

/** Vencida = o token já não vale. Sem `expiraEm` a sessão não vence por tempo. */
function estaVencida(usuario: UsuarioSessao): boolean {
  if (usuario.expiraEm === undefined) return false;
  return Date.parse(usuario.expiraEm) <= Date.now();
}

/** Cópia que leva só os campos definidos, para não guardar chaves vazias. */
function copiarUsuario(usuario: UsuarioSessao): UsuarioSessao {
  const copia: UsuarioSessao = { nome: usuario.nome, perfil: usuario.perfil };
  if (usuario.idUsuario !== undefined) copia.idUsuario = usuario.idUsuario;
  // Vencimento que não é data válida é descartado: sem ele a sessão não vence por tempo.
  if (usuario.expiraEm !== undefined && ehDataValida(usuario.expiraEm)) copia.expiraEm = usuario.expiraEm;
  if (usuario.cargo !== undefined) copia.cargo = usuario.cargo;
  if (usuario.statusCadastro !== undefined) copia.statusCadastro = usuario.statusCadastro;
  return copia;
}

let sessaoAtual: SessaoArmazenada | null = carregarSessaoGuardada();

function notificar(): void {
  ouvintes.forEach((ouvinte) => ouvinte());
}

function cancelarVencimento(): void {
  if (temporizadorVencimento !== null) {
    clearTimeout(temporizadorVencimento);
    temporizadorVencimento = null;
  }
}

/** Agenda o encerramento automático da sessão no instante em que ela vence. */
function agendarVencimento(): void {
  cancelarVencimento();
  const expiraEm = sessaoAtual?.usuario.expiraEm;
  if (expiraEm === undefined) return;

  const restante = Date.parse(expiraEm) - Date.now();
  // Data não parseável (NaN) não agenda nada: setTimeout(fn, NaN) dispararia sem parar.
  if (!Number.isFinite(restante)) return;
  const atraso = Math.min(Math.max(restante, 0), LIMITE_TEMPORIZADOR_MS);
  temporizadorVencimento = setTimeout(() => {
    temporizadorVencimento = null;
    if (sessaoAtual === null) return;
    if (estaVencida(sessaoAtual.usuario)) {
      limparSessao();
    } else {
      // Vencimento além do limite do setTimeout: aguarda mais um ciclo.
      agendarVencimento();
    }
  }, atraso);
}

/** Token salvo, ou null quando não há sessão ou ela venceu. */
export function obterToken(): string | null {
  if (sessaoAtual !== null && estaVencida(sessaoAtual.usuario)) {
    limparSessao();
    return null;
  }
  return sessaoAtual?.token ?? null;
}

/**
 * Usuário da sessão, ou null quando não há sessão ou ela venceu. Devolve sempre
 * a mesma referência enquanto a sessão não muda. Não altera a sessão nem avisa
 * os ouvintes: é usada por `useSyncExternalStore`.
 */
export function obterUsuarioSessao(): UsuarioSessao | null {
  if (sessaoAtual === null || estaVencida(sessaoAtual.usuario)) return null;
  return sessaoAtual.usuario;
}

/** Abre a sessão após o login. */
export function salvarSessao(token: string, usuario: UsuarioSessao): void {
  sessaoAtual = { token, usuario: copiarUsuario(usuario) };
  gravarItem(CHAVE_TOKEN, token);
  gravarItem(CHAVE_USUARIO, JSON.stringify(sessaoAtual.usuario));
  agendarVencimento();
  notificar();
}

/** Encerra a sessão (logout ou token expirado). */
export function limparSessao(): void {
  cancelarVencimento();
  if (sessaoAtual === null && lerItem(CHAVE_TOKEN) === null) return;
  sessaoAtual = null;
  removerItem(CHAVE_TOKEN);
  removerItem(CHAVE_USUARIO);
  notificar();
}

/** Registra um ouvinte de mudanças da sessão. Devolve a função que cancela a inscrição. */
export function inscreverMudancasSessao(ouvinte: OuvinteSessao): () => void {
  ouvintes.add(ouvinte);
  return () => {
    ouvintes.delete(ouvinte);
  };
}

agendarVencimento();

// Mantém abas abertas sincronizadas (login ou logout feito em outra aba).
window.addEventListener('storage', (evento: StorageEvent) => {
  if (evento.key === null || evento.key === CHAVE_TOKEN || evento.key === CHAVE_USUARIO) {
    sessaoAtual = carregarSessaoGuardada();
    agendarVencimento();
    notificar();
  }
});
