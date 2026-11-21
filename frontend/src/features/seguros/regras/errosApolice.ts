import { obterErroDeCampo, obterMensagemErro } from '@/lib/erros';
import { ErroApi } from '@/types/api';
import type { CampoApolice } from './formularioApolice';

const STATUS_DADOS_INVALIDOS = 400;
const STATUS_NAO_ENCONTRADO = 404;
const STATUS_CONFLITO = 409;

/** Campo da API (SeguroVO) → campo do formulário. */
const CAMPOS_DA_API: readonly (readonly [string, CampoApolice])[] = [
  ['idVeiculo', 'veiculo'],
  ['seguradora', 'seguradora'],
  ['numeroApolice', 'numeroApolice'],
  ['dataInicio', 'dataInicio'],
  ['dataFim', 'dataFim'],
  ['valorPremio', 'valorPremio'],
  ['cobertura', 'cobertura'],
];

/** Onde mostrar a falha ao salvar: nos campos, num aviso geral no topo da gaveta, ou nos dois. */
export interface ResultadoErroApolice {
  campos: Partial<Record<CampoApolice, string>>;
  aviso: string | null;
}

/**
 * Leva cada erro do servidor para o lugar certo, sempre com a mensagem em português que a API mandou:
 * - 400 com erros por campo (ex.: vigência invertida) → no campo correspondente;
 * - 409 (número já usado por outra apólice ativa) → no campo "Número da apólice";
 * - 404 do veículo ("O veículo informado não foi encontrado.") → no campo "Veículo";
 * - qualquer outro (404 da apólice, 429, rede, 500) → aviso geral.
 */
export function interpretarErroApolice(erro: unknown): ResultadoErroApolice {
  const mensagem = obterMensagemErro(erro);
  if (!(erro instanceof ErroApi)) return { campos: {}, aviso: mensagem };

  if (erro.status === STATUS_DADOS_INVALIDOS) {
    const campos: Partial<Record<CampoApolice, string>> = {};
    for (const [campoApi, campoFormulario] of CAMPOS_DA_API) {
      const mensagemCampo = obterErroDeCampo(erro, campoApi);
      if (mensagemCampo !== undefined) campos[campoFormulario] = mensagemCampo;
    }
    return Object.keys(campos).length > 0 ? { campos, aviso: null } : { campos: {}, aviso: mensagem };
  }

  if (erro.status === STATUS_CONFLITO) return { campos: { numeroApolice: mensagem }, aviso: null };

  if (erro.status === STATUS_NAO_ENCONTRADO && /ve[ií]culo/i.test(erro.mensagem)) {
    return { campos: { veiculo: mensagem }, aviso: null };
  }

  return { campos: {}, aviso: mensagem };
}
