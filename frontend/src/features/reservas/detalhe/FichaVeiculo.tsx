import { MENSAGENS_DETALHE_VEICULO as texto } from '@/lib/mensagensDetalheVeiculo';
import type { VeiculoLocacao } from '@/types/veiculos';

export function FichaVeiculo({ veiculo }: { veiculo: VeiculoLocacao }) {
  const cidadeUf = [veiculo.locadoraCidade, veiculo.locadoraUf].filter(Boolean).join(' – ');
  const campos = [
    [texto.marca, veiculo.marca],
    [texto.modelo, veiculo.modelo],
    [texto.anoFabricacao, veiculo.anoFabricacao],
    [texto.anoModelo, veiculo.anoModelo],
    [texto.cor, veiculo.cor],
    [texto.categoria, veiculo.categoriaNome],
    [texto.locadora, [veiculo.locadoraNomeFantasia, cidadeUf].filter(Boolean).join(' · ')],
  ];

  return (
    <section aria-labelledby="titulo-ficha" className="rounded-cartao border border-borda bg-branco p-4 shadow-cartao sm:p-6">
      <h2 id="titulo-ficha" className="text-marca text-navy">{texto.ficha}</h2>
      <dl className="mt-4 grid grid-cols-2 gap-2 sm:grid-cols-3">
        {campos.map(([rotulo, valor]) => valor && (
          <div key={rotulo} className="min-w-0 rounded-cartao bg-steel-fundo p-3">
            <dt className="text-apoio text-texto-apoio">{rotulo}</dt>
            <dd className="mt-1 break-words text-corpo-denso font-semibold text-navy">{valor}</dd>
          </div>
        ))}
      </dl>
    </section>
  );
}
