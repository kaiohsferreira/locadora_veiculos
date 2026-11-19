import { useRef, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Botao, Campo, Select, useAviso } from '@/components/ui';
import { useCategorias } from '@/features/frota/hooks/useCategorias';
import { obterMensagemErro } from '@/lib/erros';
import { MENSAGENS_COBRANCAS_EXTRAS as M } from '@/lib/mensagensCobrancasExtras';
import { ErroApi } from '@/types/api';
import type { ConfiguracaoCobrancasExtras } from '@/types/configuracoesCobrancasExtras';
import { camposConfiguracaoCobrancas, converterConfiguracaoCobrancas, esquemaConfiguracaoCobrancas, type CamposConfiguracaoCobrancas } from './validacaoConfiguracao';
import { useSalvarConfiguracaoCobrancas } from './useConfiguracoesCobrancas';

export interface FormularioConfiguracaoCobrancasProps {
  inicial?: ConfiguracaoCobrancasExtras;
  aoConcluir: () => void;
  aoCancelar: () => void;
  recarregar?: () => Promise<ConfiguracaoCobrancasExtras>;
}
export function FormularioConfiguracaoCobrancas({ inicial, aoConcluir, aoCancelar, recarregar }: FormularioConfiguracaoCobrancasProps) {
  const categorias = useCategorias();
  const salvar = useSalvarConfiguracaoCobrancas();
  const aviso = useAviso();
  const [versao, setVersao] = useState(inicial?.versao);
  const [conflito, setConflito] = useState(false);
  const [recarregando, setRecarregando] = useState(false);
  const [erro, setErro] = useState<string | null>(null);
  const enviando = useRef(false);
  const { register, reset, handleSubmit, formState: { errors, isSubmitting } } = useForm<CamposConfiguracaoCobrancas>({ resolver: zodResolver(esquemaConfiguracaoCobrancas), defaultValues: camposConfiguracaoCobrancas(inicial) });
  const ocupado = salvar.isPending || isSubmitting || recarregando;
  async function enviar(campos: CamposConfiguracaoCobrancas) {
    if (enviando.current || ocupado || conflito) return;
    enviando.current = true; setErro(null);
    try {
      await salvar.mutateAsync({ id: inicial?.idConfiguracao ?? null, dados: converterConfiguracaoCobrancas(campos), versaoEsperada: versao });
      aviso.sucesso(M.salvo); aoConcluir();
    } catch (falha: unknown) {
      const mensagem = falha instanceof ErroApi && falha.status === 409 ? M.configConflito : obterMensagemErro(falha);
      setErro(mensagem); setConflito(Boolean(inicial && falha instanceof ErroApi && falha.status === 409)); aviso.erro(mensagem);
    } finally { enviando.current = false; }
  }
  async function recarregarDados() {
    if (!recarregar || ocupado) return;
    setRecarregando(true);
    try { const atual = await recarregar(); reset(camposConfiguracaoCobrancas(atual)); setVersao(atual.versao); setConflito(false); setErro(null); }
    catch (falha: unknown) { setErro(obterMensagemErro(falha)); }
    finally { setRecarregando(false); }
  }
  return <form className="min-w-0 w-full space-y-6" aria-label="Política de cobranças extras" aria-busy={ocupado} noValidate onSubmit={event => { void handleSubmit(enviar, () => aviso.erro(M.invalido))(event); }}>
    {inicial && <p>Política {inicial.idConfiguracao} · Versão carregada {versao}</p>}
    <p>Tarifas definidas pela locadora por categoria. A franquia diária usa as diárias previstas da locação, de 24 horas arredondadas para cima, sem incluir atraso. O combustível usa o preço de um tanque completo. A tolerância é descontada antes das unidades de atraso.</p>
    {categorias.isError && <div role="alert"><p>Não foi possível carregar as categorias.</p><Botao variante="contorno" onClick={() => { void categorias.refetch(); }}>Recarregar categorias</Botao></div>}
    <fieldset disabled={ocupado} className="min-w-0 w-full space-y-6"><legend className="sr-only">Parâmetros da política</legend>
      <div className="grid min-w-0 grid-cols-1 gap-6 sm:grid-cols-2">
        <Select rotulo="Categoria" obrigatorio required disabled={categorias.isPending || categorias.isError} opcoes={(categorias.data?.itens ?? []).map(c => ({ valor: String(c.idCategoria), rotulo: c.nome }))} {...register('idCategoria')} erro={errors.idCategoria?.message} />
        <Select rotulo="Base da franquia" obrigatorio required opcoes={[{ valor: 'TOTAL', rotulo: 'Total da locação' }, { valor: 'POR_DIARIA', rotulo: 'Por diária prevista (24 horas)' }]} {...register('baseFranquia')} erro={errors.baseFranquia?.message} />
        <Campo rotulo="Franquia de km" obrigatorio required inputMode="numeric" {...register('franquiaKm')} erro={errors.franquiaKm?.message} />
        <Campo rotulo="Valor por km excedente (R$)" obrigatorio required inputMode="decimal" {...register('valorKmExcedente')} erro={errors.valorKmExcedente?.message} />
        <Campo rotulo="Preço de um tanque completo (R$)" obrigatorio required inputMode="decimal" {...register('valorTanqueCompleto')} erro={errors.valorTanqueCompleto?.message} />
        <Campo rotulo="Tolerância de atraso (minutos)" obrigatorio required inputMode="numeric" {...register('toleranciaAtrasoMinutos')} erro={errors.toleranciaAtrasoMinutos?.message} />
        <Select rotulo="Unidade de atraso" obrigatorio required opcoes={[{ valor: 'HORA', rotulo: 'Hora' }, { valor: 'DIARIA', rotulo: 'Diária de 24 horas' }]} {...register('unidadeAtraso')} erro={errors.unidadeAtraso?.message} />
        <Select rotulo="Fracionamento de atraso" obrigatorio required opcoes={[{ valor: 'PROPORCIONAL', rotulo: 'Proporcional' }, { valor: 'INICIADA', rotulo: 'Unidade iniciada' }]} {...register('fracionamentoAtraso')} erro={errors.fracionamentoAtraso?.message} />
        <Campo rotulo="Valor por unidade de atraso (R$)" obrigatorio required inputMode="decimal" dica="Aceita vírgula e até duas casas decimais." {...register('valorUnidadeAtraso')} erro={errors.valorUnidadeAtraso?.message} />
        <Select rotulo="Base de cálculo das avarias" obrigatorio required opcoes={[{ valor: 'ESTIMATIVA_VISTORIA', rotulo: 'Estimativas das novas avarias da vistoria' }]} {...register('baseAvarias')} erro={errors.baseAvarias?.message} />
      </div>
      <p className="text-sm text-texto-suave">Ao selecionar estimativas da vistoria, a locadora autoriza essa base para as novas avarias. Os valores permanecem identificados como estimados.</p>
      <div className="flex flex-wrap gap-4"><Botao type="submit" disabled={ocupado || conflito || categorias.isPending || categorias.isError} carregando={salvar.isPending}>Salvar política</Botao><Botao variante="contorno" onClick={aoCancelar}>Cancelar edição</Botao></div>
    </fieldset>
    {erro && <p role="alert" className="text-status-perigo">{erro}</p>}
    {conflito && recarregar && <Botao variante="contorno" onClick={() => { void recarregarDados(); }} disabled={ocupado}>Recarregar e descartar alterações</Botao>}
  </form>;
}
