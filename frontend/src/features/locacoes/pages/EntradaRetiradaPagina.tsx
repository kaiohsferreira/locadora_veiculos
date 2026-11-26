import { generatePath, useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Botao, BotaoLink, Campo, Cartao, EstadoErro, EstadoVazio, Etiqueta, Tabela, type ColunaTabela } from '@/components/ui';
import { useTituloDocumento } from '@/hooks/useTituloDocumento';
import { obterMensagemErro } from '@/lib/erros';
import { formatarPlaca } from '@/lib/formatters';
import { MENSAGENS_RETIRADA as M, mensagemAcaoLinhaRetirada } from '@/lib/mensagensRetirada';
import { CAMINHOS } from '@/routes/caminhos';
import type { ItemPainelOperacional } from '@/types/painelOperacional';
import { ListaDevolucoesPendentes } from '@/features/devolucao/componentes/ListaDevolucoesPendentes';
import { useRetiradasDeHoje } from '../hooks/useRetiradasDeHoje';
import { lerCodigoReserva } from '../retirada/codigoReserva';
import { acaoDaLinha, ehStatusConhecido, textoColunaReserva } from '../retirada/regrasEntradaRetirada';

type Consulta = ReturnType<typeof useRetiradasDeHoje>;

interface CamposCodigo { codigo: string }

/** Só o formato é checado aqui. Se a reserva existe e é desta locadora, quem diz é a tela 16 (consulta da retirada). */
const esquemaCodigo = z.object({
  codigo: z.string().refine((valor) => lerCodigoReserva(valor) !== null, M.entradaCampoErro),
});

/** Campo "Código da reserva": valida o formato e leva à tela 16 daquela reserva. Não consulta a reserva. */
function CampoCodigoReserva() {
  const navegar = useNavigate();
  const { register, handleSubmit, formState: { errors } } = useForm<CamposCodigo>({
    resolver: zodResolver(esquemaCodigo),
    defaultValues: { codigo: '' },
  });

  function abrir(campos: CamposCodigo) {
    const idReserva = lerCodigoReserva(campos.codigo);
    if (idReserva === null) return;
    navegar(generatePath(CAMINHOS.retiradaVeiculo, { reservaId: String(idReserva) }));
  }

  return (
    <form noValidate onSubmit={handleSubmit(abrir)} className="flex flex-col gap-4 sm:flex-row sm:items-end">
      <div className="min-w-0 flex-1">
        <Campo
          rotulo={M.entradaCampoRotulo}
          obrigatorio
          required
          dica={M.entradaCampoDica}
          erro={errors.codigo?.message}
          {...register('codigo')}
        />
      </div>
      <Botao type="submit" icone="arrow_forward">{M.entradaCampoBotao}</Botao>
    </form>
  );
}

/**
 * Ação da linha: origem RESERVA leva à tela 16; origem LOCACAO já aberta leva à vistoria. Sem o id, sem ação. O nome para o
 * leitor de tela leva a referência da linha (ex.: "Iniciar retirada AV-00042"), porque todas as linhas têm o mesmo texto.
 */
function AcaoLinha({ item }: { item: ItemPainelOperacional }) {
  const acao = acaoDaLinha(item);
  if (acao === null) return null;
  const referencia = textoColunaReserva(item);
  if (acao.tipo === 'retirada') {
    return (
      <BotaoLink
        to={generatePath(CAMINHOS.retiradaVeiculo, { reservaId: String(acao.id) })}
        tamanho="pequeno"
        aria-label={mensagemAcaoLinhaRetirada(M.entradaAcaoReserva, referencia)}
      >
        {M.entradaAcaoReserva}
      </BotaoLink>
    );
  }
  return (
    <BotaoLink
      to={generatePath(CAMINHOS.vistoriaLocacao, { locacaoId: String(acao.id) })}
      variante="contorno"
      tamanho="pequeno"
      aria-label={mensagemAcaoLinhaRetirada(M.continuarVistoria, referencia)}
    >
      {M.continuarVistoria}
    </BotaoLink>
  );
}

const COLUNAS: readonly ColunaTabela<ItemPainelOperacional>[] = [
  { chave: 'reserva', titulo: M.entradaColunaReserva, renderizar: (item) => textoColunaReserva(item) },
  { chave: 'cliente', titulo: M.entradaColunaCliente, renderizar: (item) => item.nomeCliente },
  { chave: 'veiculo', titulo: M.entradaColunaVeiculo, renderizar: (item) => `${item.marca} ${item.modelo}` },
  { chave: 'placa', titulo: M.entradaColunaPlaca, renderizar: (item) => formatarPlaca(item.placa) },
  {
    chave: 'situacao',
    titulo: M.entradaColunaSituacao,
    renderizar: (item) => (ehStatusConhecido(item.status) ? <Etiqueta status={item.status} /> : <Etiqueta rotulo={item.status} tom="neutro" />),
  },
  { chave: 'acao', titulo: M.entradaColunaAcao, renderizar: (item) => <AcaoLinha item={item} /> },
];

function chaveDaLinha(item: ItemPainelOperacional): string {
  return `${item.origem}-${item.idReserva ?? item.idLocacao}`;
}

/** Lista das retiradas previstas para hoje, com o estado de carregando, vazio e erro da consulta. */
function ListaRetiradasDeHoje({ consulta }: { consulta: Consulta }) {
  if (consulta.isError) {
    return (
      <EstadoErro
        mensagem={obterMensagemErro(consulta.error)}
        aoTentarNovamente={() => { void consulta.refetch(); }}
        tentando={consulta.isFetching}
      />
    );
  }
  const dados = consulta.data;
  return (
    <Cartao titulo={dados ? M.entradaListaTitulo(dados.dataStr) : undefined} semEspacamento>
      <Tabela
        colunas={COLUNAS}
        dados={dados?.retiradas ?? []}
        obterChave={chaveDaLinha}
        legenda={M.entradaTitulo}
        carregando={consulta.isPending}
        vazio={<EstadoVazio titulo={M.entradaListaVazia} />}
      />
    </Cartao>
  );
}

/**
 * Página de entrada da retirada (locacao.contract.json, pagina_de_entrada_da_retirada; sem desenho). Entra pelo item
 * Locações do menu. Acesso: qualquer funcionário, como a tela 16 e a rota da API.
 */
export default function EntradaRetiradaPagina() {
  useTituloDocumento(M.entradaTitulo);
  const consulta = useRetiradasDeHoje();
  return (
    <div className="mx-auto w-full max-w-5xl space-y-6 px-4 py-8 sm:px-6">
      <header className="space-y-2">
        <h1 className="text-2xl font-bold text-navy">{M.entradaTitulo}</h1>
        <p className="text-texto-suave">{M.entradaSubtitulo}</p>
      </header>
      <Cartao><CampoCodigoReserva /></Cartao>
      <ListaRetiradasDeHoje consulta={consulta} />
      <ListaDevolucoesPendentes />
    </div>
  );
}
