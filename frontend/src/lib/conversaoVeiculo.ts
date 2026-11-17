/**
 * Conversão entre o formulário de veículo e os tipos da API (sem React). Só valores que já passaram pelo esquema
 * `criarEsquemaVeiculo` chegam em `montarRequisicaoVeiculo`.
 *
 * - Pedido: sem status e sem empresa (o servidor decide). Texto aparado; placa sem hífen e em maiúsculas; número a
 *   partir do texto em reais; cor vazia vai como texto vazio (o servidor grava nulo); venda e data vazias vão como nulo.
 * - Edição: `valoresIniciaisEdicao` leva os valores do veículo para o formulário, com o valor em reais no formato da
 *   máscara (229,00). Cadastro: tudo vazio, sem categoria marcada e sem cor marcada.
 */
import { formatarNumero, converterMoedaPtBrParaNumero } from '@/lib/formatters';
import { normalizarPlacaParaServidor } from '@/lib/normalizacaoVeiculo';
import type { ValoresFormularioVeiculo } from '@/lib/esquemaVeiculo';
import type { VeiculoRequisicao, VeiculoRetorno } from '@/types/veiculos';

/** Casas decimais do valor em reais na tela (a máscara sempre mostra centavos). */
const CASAS_DECIMAIS_MOEDA = 2;

/** Formulário de cadastro: nada preenchido. */
export function valoresIniciaisCadastro(): ValoresFormularioVeiculo {
  return {
    idCategoria: '',
    placa: '',
    chassi: '',
    renavam: '',
    marca: '',
    modelo: '',
    anoFabricacao: '',
    anoModelo: '',
    cor: '',
    kmAtual: '',
    valorDiaria: '',
    valorVenda: '',
    dataAquisicao: '',
    fornecedor: '',
    numeroNotaFiscalCompra: '',
  };
}

/** Formulário de edição, com os valores do veículo lido da API. Valores em reais vão no formato da máscara. */
export function valoresIniciaisEdicao(veiculo: VeiculoRetorno): ValoresFormularioVeiculo {
  return {
    idCategoria: String(veiculo.idCategoria),
    placa: veiculo.placa,
    chassi: veiculo.chassi,
    renavam: veiculo.renavam,
    marca: veiculo.marca,
    modelo: veiculo.modelo,
    anoFabricacao: String(veiculo.anoFabricacao),
    anoModelo: String(veiculo.anoModelo),
    cor: veiculo.cor ?? '',
    kmAtual: String(veiculo.kmAtual),
    valorDiaria: formatarNumero(veiculo.valorDiaria, CASAS_DECIMAIS_MOEDA),
    valorVenda: veiculo.valorVenda === null ? '' : formatarNumero(veiculo.valorVenda, CASAS_DECIMAIS_MOEDA),
    dataAquisicao: veiculo.dataAquisicao ?? '',
    fornecedor: veiculo.fornecedor ?? '',
    numeroNotaFiscalCompra: veiculo.numeroNotaFiscalCompra ?? '',
  };
}

/** Inteiro digitado; vazio vale zero (o servidor trata a quilometragem omitida como zero). */
function inteiroDoFormulario(texto: string): number {
  const aparado = texto.trim();
  return aparado.length === 0 ? 0 : Number(aparado);
}

/**
 * Corpo do POST e do PUT. Chamado só com valores validados: o zero de `?? 0` nunca sai na prática, e existe só
 * para o tipo do número.
 */
export function montarRequisicaoVeiculo(valores: ValoresFormularioVeiculo): VeiculoRequisicao {
  const valorVendaAparado = valores.valorVenda.trim();
  const dataAparada = valores.dataAquisicao.trim();
  return {
    idCategoria: inteiroDoFormulario(valores.idCategoria),
    placa: normalizarPlacaParaServidor(valores.placa),
    chassi: valores.chassi.trim().toUpperCase(),
    renavam: valores.renavam.trim(),
    marca: valores.marca.trim(),
    modelo: valores.modelo.trim(),
    anoFabricacao: inteiroDoFormulario(valores.anoFabricacao),
    anoModelo: inteiroDoFormulario(valores.anoModelo),
    cor: valores.cor.trim(),
    kmAtual: inteiroDoFormulario(valores.kmAtual),
    valorDiaria: converterMoedaPtBrParaNumero(valores.valorDiaria) ?? 0,
    valorVenda: valorVendaAparado.length === 0 ? null : converterMoedaPtBrParaNumero(valorVendaAparado),
    dataAquisicao: dataAparada.length === 0 ? null : dataAparada,
    fornecedor: valores.fornecedor.trim(),
    numeroNotaFiscalCompra: valores.numeroNotaFiscalCompra.trim(),
  };
}
