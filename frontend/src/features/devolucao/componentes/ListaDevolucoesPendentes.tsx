import { useState } from 'react';
import { generatePath } from 'react-router-dom';
import { Botao, BotaoLink, Cartao, EstadoErro, EstadoVazio, Etiqueta, Paginacao, Tabela, type ColunaTabela } from '@/components/ui';
import { obterMensagemErro } from '@/lib/erros';
import { formatarPlaca } from '@/lib/formatters';
import { MENSAGENS_DEVOLUCAO as M } from '@/lib/mensagensDevolucao';
import { MENSAGENS_RETIRADA, mensagemAcaoLinhaRetirada } from '@/lib/mensagensRetirada';
import { usePermissoes } from '@/routes/autenticacao/usePermissoes';
import { CAMINHOS } from '@/routes/caminhos';
import type { DevolucaoPendente } from '@/types/devolucao';
import { useDevolucoesPendentes } from '../hooks/useDevolucoesPendentes';
import { TAMANHO_PAGINA_DEVOLUCOES, podeEncerrarLocacao } from '../regras/regrasDevolucao';

interface AcaoDevolucaoProps {
  item: DevolucaoPendente;
  podeRegistrar: boolean;
}

/**
 * Botão "Registrar devolução" da linha (leva à tela 06 da locação). Para os cargos que não encerram, fica desligado (N7).
 * O nome para o leitor de tela leva a referência da linha, como nas retiradas.
 */
function AcaoDevolucao({ item, podeRegistrar }: AcaoDevolucaoProps) {
  if (!podeRegistrar) {
    return <Botao variante="contorno" tamanho="pequeno" disabled>{M.pendentes.botao}</Botao>;
  }
  return (
    <BotaoLink
      to={generatePath(CAMINHOS.devolucaoLocacao, { locacaoId: String(item.idLocacao) })}
      variante="contorno"
      tamanho="pequeno"
      aria-label={mensagemAcaoLinhaRetirada(M.pendentes.botao, MENSAGENS_RETIRADA.locacaoNumero(item.idLocacao))}
    >
      {M.pendentes.botao}
    </BotaoLink>
  );
}

/**
 * Bloco "Devoluções pendentes" da página de entrada da retirada (locacao.contract.json v7, decisão D7; textos aprovados pelo
 * Kaio em 26/11/2026). Lista as locações em andamento e as atrasadas da locadora do funcionário.
 */
export function ListaDevolucoesPendentes() {
  const [pagina, setPagina] = useState(1);
  const podeRegistrar = podeEncerrarLocacao(usePermissoes().cargo);
  const consulta = useDevolucoesPendentes({ status: 'EM_ANDAMENTO', pagina, tamanhoPagina: TAMANHO_PAGINA_DEVOLUCOES });

  const colunas: readonly ColunaTabela<DevolucaoPendente>[] = [
    { chave: 'locacao', titulo: M.pendentes.colunas.locacao, renderizar: item => MENSAGENS_RETIRADA.locacaoNumero(item.idLocacao) },
    { chave: 'cliente', titulo: M.pendentes.colunas.cliente, renderizar: item => item.nomeCliente },
    { chave: 'veiculo', titulo: M.pendentes.colunas.veiculo, renderizar: item => `${item.marca} ${item.modelo}` },
    { chave: 'placa', titulo: M.pendentes.colunas.placa, renderizar: item => formatarPlaca(item.placa) },
    { chave: 'prevista', titulo: M.pendentes.colunas.devolucaoPrevista, renderizar: item => item.devolucaoPrevistaEfetivaStr },
    {
      chave: 'situacao',
      titulo: M.pendentes.colunas.situacao,
      renderizar: item => (item.atrasada ? <Etiqueta rotulo={M.pendentes.atrasada} tom="perigo" /> : <Etiqueta status="EM_ANDAMENTO" />),
    },
    { chave: 'acao', titulo: M.pendentes.colunas.acao, renderizar: item => <AcaoDevolucao item={item} podeRegistrar={podeRegistrar} /> },
  ];

  if (consulta.isError) {
    return (
      <Cartao titulo={M.pendentes.titulo} semEspacamento>
        <EstadoErro mensagem={obterMensagemErro(consulta.error)} aoTentarNovamente={() => { void consulta.refetch(); }} tentando={consulta.isFetching} />
      </Cartao>
    );
  }
  const dados = consulta.data;
  return (
    <Cartao titulo={M.pendentes.titulo} semEspacamento>
      <Tabela
        colunas={colunas}
        dados={dados?.itens ?? []}
        obterChave={item => String(item.idLocacao)}
        legenda={M.pendentes.titulo}
        carregando={consulta.isPending}
        vazio={<EstadoVazio titulo={M.pendentes.vazio} />}
      />
      {dados && dados.total > 0 && (
        <Paginacao
          pagina={dados.pagina}
          tamanhoPagina={dados.tamanhoPagina}
          total={dados.total}
          aoMudarPagina={setPagina}
          desabilitada={consulta.isFetching}
          rotuloItens="devoluções"
        />
      )}
    </Cartao>
  );
}
