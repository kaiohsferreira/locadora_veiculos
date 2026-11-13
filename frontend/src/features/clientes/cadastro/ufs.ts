/**
 * As 27 unidades federativas do Brasil, para o campo UF do endereço do cadastro.
 * O backend aceita duas letras; a tela só oferece as siglas desta lista.
 */

export interface UnidadeFederativa {
  sigla: string;
  nome: string;
}

export const UNIDADES_FEDERATIVAS: readonly UnidadeFederativa[] = [
  { sigla: 'AC', nome: 'Acre' },
  { sigla: 'AL', nome: 'Alagoas' },
  { sigla: 'AP', nome: 'Amapá' },
  { sigla: 'AM', nome: 'Amazonas' },
  { sigla: 'BA', nome: 'Bahia' },
  { sigla: 'CE', nome: 'Ceará' },
  { sigla: 'DF', nome: 'Distrito Federal' },
  { sigla: 'ES', nome: 'Espírito Santo' },
  { sigla: 'GO', nome: 'Goiás' },
  { sigla: 'MA', nome: 'Maranhão' },
  { sigla: 'MT', nome: 'Mato Grosso' },
  { sigla: 'MS', nome: 'Mato Grosso do Sul' },
  { sigla: 'MG', nome: 'Minas Gerais' },
  { sigla: 'PA', nome: 'Pará' },
  { sigla: 'PB', nome: 'Paraíba' },
  { sigla: 'PR', nome: 'Paraná' },
  { sigla: 'PE', nome: 'Pernambuco' },
  { sigla: 'PI', nome: 'Piauí' },
  { sigla: 'RJ', nome: 'Rio de Janeiro' },
  { sigla: 'RN', nome: 'Rio Grande do Norte' },
  { sigla: 'RS', nome: 'Rio Grande do Sul' },
  { sigla: 'RO', nome: 'Rondônia' },
  { sigla: 'RR', nome: 'Roraima' },
  { sigla: 'SC', nome: 'Santa Catarina' },
  { sigla: 'SP', nome: 'São Paulo' },
  { sigla: 'SE', nome: 'Sergipe' },
  { sigla: 'TO', nome: 'Tocantins' },
];

const SIGLAS = new Set<string>(UNIDADES_FEDERATIVAS.map((uf) => uf.sigla));

/** Opções do Select de UF: valor é a sigla, rótulo é "SP - São Paulo". */
export const OPCOES_UF: readonly { valor: string; rotulo: string }[] = UNIDADES_FEDERATIVAS.map(
  (uf) => ({ valor: uf.sigla, rotulo: `${uf.sigla} - ${uf.nome}` }),
);

/** A sigla existe na lista (exige maiúsculas). */
export function ehSiglaUf(valor: string): boolean {
  return SIGLAS.has(valor);
}
