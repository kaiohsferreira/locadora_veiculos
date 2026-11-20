import type { CalculoCobrancasExtras } from '@/types/cobrancasExtras';
import { formatarMoeda, formatarNumero, formatarQuilometragem } from '@/lib/formatters';

export interface ResultadoSimulacaoProps { resultado: CalculoCobrancasExtras }
export function ResultadoSimulacao({ resultado: r }: ResultadoSimulacaoProps) {
  return <section aria-label={r.simulacao ? 'Resultado provisório da simulação' : 'Apuração registrada'} aria-live="polite" className="min-w-0 space-y-4 rounded-lg border border-borda p-4">
    <h3 className="font-semibold text-navy">{r.simulacao ? 'Memória de cálculo provisória' : 'Memória de cálculo registrada'}</h3>
    <p>Política {r.configuracao.idConfiguracao} · {r.configuracao.categoriaNome} · Versão {r.configuracao.versao}. Franquia {r.configuracao.franquiaKm} km {r.configuracao.baseFranquia === 'TOTAL' ? 'no total' : 'por diária prevista'}; {r.diariasPrevistas} diária(s) prevista(s). Base de avarias: estimativas da vistoria.</p>
    <dl className="space-y-4">
      <div><dt className="font-medium">Quilometragem: {formatarMoeda(r.quilometragem.valor)}</dt>
        <dd>Entrada {formatarQuilometragem(r.quilometragem.kmEntrada)}; saída {formatarQuilometragem(r.quilometragem.kmSaida)}; pacote total {formatarQuilometragem(r.quilometragem.pacoteKmTotal)}. Excedente {formatarQuilometragem(r.quilometragem.kmExcedentes)} × {formatarMoeda(r.quilometragem.valorKmExcedente)}.</dd></div>
      <div><dt className="font-medium">Combustível: {formatarMoeda(r.combustivel.valor)}</dt>
        <dd>Nível de entrada {formatarNumero(r.combustivel.nivelEntrada, 2)}; saída {formatarNumero(r.combustivel.nivelSaida, 2)}. Fração faltante {formatarNumero(r.combustivel.fracaoFaltante, 2)} × tanque completo {formatarMoeda(r.combustivel.valorTanqueCompleto)}.</dd></div>
      <div><dt className="font-medium">Atraso: {formatarMoeda(r.atraso.valor)}</dt>
        <dd>Previsão {r.atraso.dataPrevistaStr}; saída registrada {r.atraso.dataSaidaStr}. Tolerância descontada: {formatarNumero(r.atraso.toleranciaAtrasoMinutos)} minutos. {r.atraso.unidadeAtraso === 'HORA' ? 'Horas' : 'Diárias de 24 horas'}, {r.atraso.fracionamentoAtraso === 'PROPORCIONAL' ? 'proporcionais' : 'por unidade iniciada'}. Após tolerância: {formatarNumero(r.atraso.unidadesAposTolerancia, 6)}; unidades cobradas: {formatarNumero(r.atraso.unidadesCobradas, 6)} × {formatarMoeda(r.atraso.valorUnidadeAtraso)}.</dd></div>
      <div><dt className="font-medium">Novas avarias (estimativas provisórias): {formatarMoeda(r.avarias.valor)}</dt>
        <dd>{r.avarias.itens.length === 0 ? 'Nenhuma nova avaria estimada.' : <ul className="list-inside list-disc">{r.avarias.itens.map(a => <li key={a.idAvaria} className="break-words">{a.descricao}: {formatarMoeda(a.valorEstimado)}</li>)}</ul>}</dd></div>
      <div><dt className="font-semibold">{r.simulacao ? 'Total provisório' : 'Total registrado'}</dt><dd className="text-xl font-bold text-navy">{formatarMoeda(r.total)}</dd></div>
    </dl>
    <p className="text-sm text-texto-suave">Cada categoria é arredondada a centavos antes da soma. {r.simulacao ? 'Não há lançamento financeiro nesta prévia.' : 'Este registro preserva a política e os dados utilizados na apuração.'} {r.aviso}</p>
  </section>;
}
