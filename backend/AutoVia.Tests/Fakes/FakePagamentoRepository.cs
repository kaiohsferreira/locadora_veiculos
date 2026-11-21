using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.Regras;
using AutoVia.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace AutoVia.Tests.Fakes
{
    /// <summary>
    /// Repositorio de cobrancas em memoria, para os testes de servico e HTTP sem PostgreSQL. As listas de locacoes e de vendas
    /// sao as que existem para a locadora do teste (quem nao esta nelas e de outra locadora ou inexistente). Aplica as mesmas
    /// regras de origem, de status, de posse e de PIX do repositorio real; as consultas SQL sao provadas por PagamentoConsultasSqlTests.
    /// Os ganchos (AntesDaTrava, AntesDaGravacao e FalharGravacao) simulam a corrida entre a leitura e a trava.
    /// </summary>
    public class FakePagamentoRepository : IPagamentoRepository
    {
        private int _proximoId = 1;

        /// <summary>Cobrancas semeadas e lancadas (a locadora do teste).</summary>
        public List<Pagamento> Pagamentos { get; } = new List<Pagamento>();

        /// <summary>Locacoes da locadora do teste.</summary>
        public List<Locacao> Locacoes { get; } = new List<Locacao>();

        /// <summary>Vendas da locadora do teste.</summary>
        public List<Venda> Vendas { get; } = new List<Venda>();

        /// <summary>Quantidade de baixas que chegaram ao repositorio (inclusive as recusadas).</summary>
        public int Baixas { get; private set; }

        /// <summary>Quantidade de gravacoes de PIX que de fato sobrescreveram a cobranca.</summary>
        public int GravacoesDePix { get; private set; }

        /// <summary>Quando verdadeiro, a baixa falha depois de passar pela consulta (excecao generica, como um erro de banco na trava).</summary>
        public bool FalharBaixa { get; set; }

        /// <summary>Gancho chamado dentro da trava de reabertura, antes de ler a cobranca (simula outra requisicao).</summary>
        public Action<Pagamento>? AntesDaTrava { get; set; }

        /// <summary>Gancho chamado dentro da trava da gravacao, antes de conferir a cobranca (simula outra requisicao).</summary>
        public Action<Pagamento>? AntesDaGravacao { get; set; }

        /// <summary>Quando verdadeiro, a gravacao do PIX falha depois da chamada ao provedor (excecao generica).</summary>
        public bool FalharGravacao { get; set; }

        /// <summary>Grava a cobranca PENDENTE, se a origem existir na locadora do teste; senao recusa com 404.</summary>
        public Task<Pagamento> LancarAtomicamenteAsync(Pagamento pagamento, DateTime agora)
        {
            if (pagamento.IdLocacao.HasValue && !Locacoes.Any(x => x.IdLocacao == pagamento.IdLocacao.Value && x.DisabledAt == null))
            {
                throw new NotFoundException(ConstantsMessage.ErroLocacaoNaoEncontrada);
            }

            if (pagamento.IdVenda.HasValue && !Vendas.Any(x => x.IdVenda == pagamento.IdVenda.Value && x.DisabledAt == null))
            {
                throw new NotFoundException(ConstantsMessage.ErroPagamentoVendaNaoEncontrada);
            }

            pagamento.IdPagamento = _proximoId;
            _proximoId++;
            pagamento.Status = StatusPagamento.PENDENTE;
            pagamento.DataPagamento = null;
            pagamento.Forma = null;
            pagamento.IdTransacao = null;
            pagamento.CreatedAt = agora;
            pagamento.UpdatedAt = agora;
            pagamento.DisabledAt = null;

            Pagamentos.Add(pagamento);

            return Task.FromResult(pagamento);
        }

        /// <summary>Da baixa como o repositorio real: inexistente ou de origem desativada (404), depois as recusas de status (409) e a regra P6 do PIX gerado.</summary>
        public Task<Pagamento> BaixarAtomicamenteAsync(int idPagamento, string forma, DateOnly dataPagamento, DateTime agora, bool confirmarBaixaComPixGerado, string? idTransacaoCancelado)
        {
            Baixas++;

            if (FalharBaixa)
            {
                throw new InvalidOperationException("Falha simulada da baixa.");
            }

            Pagamento? pagamento = ConsultaBaixa(idPagamento).FirstOrDefault();

            if (pagamento == null)
            {
                throw new NotFoundException(ConstantsMessage.ErroPagamentoNaoEncontrado);
            }

            string? erroDaBaixa = RegrasPagamento.ErroDaBaixa(pagamento.Tipo, pagamento.Status);

            if (erroDaBaixa != null)
            {
                throw new BusinessException(erroDaBaixa);
            }

            if (RegrasPagamento.ExigeConfirmacaoBaixaComPix(pagamento.IdTransacao, forma, confirmarBaixaComPixGerado))
            {
                throw new BusinessException(ConstantsMessage.ErroPagamentoBaixaComPixGerado);
            }

            if (RegrasPagamento.PixValidoSemCancelamento(pagamento.IdTransacao, pagamento.PixExpiraEm, forma, idTransacaoCancelado, agora))
            {
                throw new BusinessException(ConstantsMessage.ErroPagamentoBaixaComPixGerado);
            }

            pagamento.Status = StatusPagamento.PAGO;
            pagamento.DataPagamento = dataPagamento;
            pagamento.Forma = forma;
            pagamento.UpdatedAt = agora;

            return Task.FromResult(pagamento);
        }

        /// <summary>Quando verdadeiro, a atualizacao de atrasos falha depois de passar pela consulta (excecao generica, como um erro de banco).</summary>
        public bool FalharAtrasos { get; set; }

        /// <summary>Consulta da rotina de atraso: nao desativada, PENDENTE, sem caucao e com vencimento antes de hoje (a regra pura).</summary>
        public IQueryable<Pagamento> ConsultaAtrasosVencidos(DateOnly hoje)
        {
            return Pagamentos
                .Where(x => x.DisabledAt == null && RegrasPagamento.DeveVirarAtrasado(x.Tipo, x.Status, x.DataVencimento, hoje))
                .AsQueryable();
        }

        /// <summary>Marca como ATRASADO os pagamentos da rotina; a lista do teste e de uma so locadora, entao e a mesma conta.</summary>
        public Task<int> AtualizarAtrasosTodasLocadorasAsync(DateOnly hoje, DateTime agora)
        {
            return Task.FromResult(MarcarAtrasos(hoje, agora));
        }

        /// <summary>Marca como ATRASADO os pagamentos vencidos da locadora do teste.</summary>
        public Task<int> AtualizarAtrasosDaLocadoraAsync(DateOnly hoje, DateTime agora)
        {
            return Task.FromResult(MarcarAtrasos(hoje, agora));
        }

        /// <summary>Atualiza status e horario das cobrancas que a regra marca e devolve a quantidade (falha simulada se FalharAtrasos).</summary>
        private int MarcarAtrasos(DateOnly hoje, DateTime agora)
        {
            if (FalharAtrasos)
            {
                throw new InvalidOperationException("Falha simulada da atualizacao de atrasos.");
            }

            List<Pagamento> alvos = ConsultaAtrasosVencidos(hoje).ToList();

            foreach (Pagamento cobranca in alvos)
            {
                cobranca.Status = StatusPagamento.ATRASADO;
                cobranca.UpdatedAt = agora;
            }

            return alvos.Count;
        }

        /// <summary>Lista da locadora: predicado, ordem de vencimento e id, com pagina e total. Devolve a projecao sem o QR.</summary>
        public Task<(List<PagamentoListagemRegistro> Itens, int Total)> ListarPagamentosDaLocadoraAsync(Expression<Func<Pagamento, bool>> predicado, int deslocamento, int tamanho)
        {
            List<Pagamento> filtrados = Pagamentos
                .Where(x => x.DisabledAt == null && OrigemAtiva(x))
                .AsQueryable()
                .Where(predicado)
                .OrderBy(x => x.DataVencimento)
                .ThenBy(x => x.IdPagamento)
                .ToList();

            List<PagamentoListagemRegistro> pagina = filtrados.Skip(deslocamento).Take(tamanho).Select(ParaRegistro).ToList();

            return Task.FromResult((pagina, filtrados.Count));
        }

        /// <summary>Lista do cliente: predicado sobre a consulta base do cliente, ordem decrescente de vencimento e id. Devolve a projecao sem o QR.</summary>
        public Task<(List<PagamentoListagemRegistro> Itens, int Total)> ListarMeusPagamentosAsync(int idCliente, Expression<Func<Pagamento, bool>> predicado, int deslocamento, int tamanho)
        {
            List<Pagamento> filtrados = ConsultaMeusPagamentos(idCliente)
                .Where(predicado)
                .OrderByDescending(x => x.DataVencimento)
                .ThenByDescending(x => x.IdPagamento)
                .ToList();

            List<PagamentoListagemRegistro> pagina = filtrados.Skip(deslocamento).Take(tamanho).Select(ParaRegistro).ToList();

            return Task.FromResult((pagina, filtrados.Count));
        }

        /// <summary>Resumo do cliente: conta e soma a consulta base em cada um dos tres predicados.</summary>
        public Task<ResumoPagamentosClienteRegistro> ResumirMeusPagamentosAsync(int idCliente, Expression<Func<Pagamento, bool>> pendentes, Expression<Func<Pagamento, bool>> pagosNoPeriodo, Expression<Func<Pagamento, bool>> quitadosNoAno)
        {
            List<Pagamento> consulta = ConsultaMeusPagamentos(idCliente).ToList();

            List<Pagamento> listaPendentes = consulta.AsQueryable().Where(pendentes).ToList();
            List<Pagamento> listaPagos = consulta.AsQueryable().Where(pagosNoPeriodo).ToList();
            List<Pagamento> listaQuitadas = consulta.AsQueryable().Where(quitadosNoAno).ToList();

            ResumoPagamentosClienteRegistro registro = new ResumoPagamentosClienteRegistro(
                listaPendentes.Count,
                listaPendentes.Sum(x => x.Valor),
                listaQuitadas.Count,
                listaQuitadas.Sum(x => x.Valor),
                listaPagos.Count);

            return Task.FromResult(registro);
        }

        /// <summary>Leitura do PIX pelo cliente, sem trava: cobranca dele, com origem ativa. 404 se nao achou.</summary>
        public Task<Pagamento> ConsultarCobrancaDoClienteParaPixAsync(int idCliente, int idPagamento)
        {
            Pagamento? cobranca = ConsultaMeusPagamentos(idCliente).FirstOrDefault(x => x.IdPagamento == idPagamento);

            return Task.FromResult(Copia(cobranca) ?? throw new NotFoundException(ConstantsMessage.ErroPagamentoNaoEncontrado));
        }

        /// <summary>Leitura do PIX pela locadora do contexto, sem trava: so cobrancas da locadora do teste. 404 se nao achou.</summary>
        public Task<Pagamento> ConsultarCobrancaDaLocadoraParaPixAsync(int idPagamento)
        {
            Pagamento? cobranca = Pagamentos.FirstOrDefault(x => x.IdPagamento == idPagamento && x.DisabledAt == null && OrigemAtiva(x));

            return Task.FromResult(Copia(cobranca) ?? throw new NotFoundException(ConstantsMessage.ErroPagamentoNaoEncontrado));
        }

        /// <summary>Trava curta da reabertura: gancho antes da leitura; cliente (posse) ou locadora. 404 se nao achou.</summary>
        public Task<Pagamento> TravarCobrancaParaPixAsync(int? idCliente, int idPagamento)
        {
            Pagamento? original = Pagamentos.FirstOrDefault(x => x.IdPagamento == idPagamento);

            if (original != null)
            {
                AntesDaTrava?.Invoke(original);
            }

            Pagamento? cobranca = Pagamentos.FirstOrDefault(x => x.IdPagamento == idPagamento && x.DisabledAt == null && OrigemAtiva(x) && PertenceAoEscopo(x, idCliente));

            return Task.FromResult(Copia(cobranca) ?? throw new NotFoundException(ConstantsMessage.ErroPagamentoNaoEncontrado));
        }

        /// <summary>Gravacao do PIX sob trava: repete a regra de geracao (409), nao sobrescreve PIX valido e falha quando pedido.</summary>
        public Task<(Pagamento Pagamento, bool Gravou)> GravarPixAtomicamenteAsync(int? idCliente, int idPagamento, PixGravacaoRegistro dados, DateTime agora)
        {
            if (FalharGravacao)
            {
                throw new InvalidOperationException("Falha simulada da gravacao do PIX.");
            }

            Pagamento? original = Pagamentos.FirstOrDefault(x => x.IdPagamento == idPagamento);

            if (original != null)
            {
                AntesDaGravacao?.Invoke(original);
            }

            Pagamento? cobranca = Pagamentos.FirstOrDefault(x => x.IdPagamento == idPagamento && x.DisabledAt == null && OrigemAtiva(x) && PertenceAoEscopo(x, idCliente));

            if (cobranca == null)
            {
                throw new NotFoundException(ConstantsMessage.ErroPagamentoNaoEncontrado);
            }

            string? erroDaGeracao = RegrasPagamento.ErroDaGeracaoPix(cobranca.Tipo, cobranca.Status);

            if (erroDaGeracao != null)
            {
                throw new BusinessException(erroDaGeracao);
            }

            if (RegrasPagamento.PixAindaValido(cobranca.IdTransacao, cobranca.PixExpiraEm, agora))
            {
                return Task.FromResult((Copia(cobranca)!, false));
            }

            cobranca.IdTransacao = dados.IdentificadorProvedor;
            cobranca.Forma = AutoVia.Helpers.ConstantsValues.FormaPix;
            cobranca.QrCodeBase64 = dados.QrCodeBase64;
            cobranca.PixCopiaECola = dados.CopiaECola;
            cobranca.PixExpiraEm = dados.ExpiraEm;
            cobranca.UpdatedAt = agora;
            GravacoesDePix++;

            return Task.FromResult((Copia(cobranca)!, true));
        }

        /// <summary>Zera o id do PIX cancelado so se a cobranca ainda o guarda (como o repositorio real, sob trava); a validade fica.</summary>
        public Task LimparPixCanceladoAsync(int idPagamento, string idTransacaoCancelado, DateTime agora)
        {
            Pagamento? cobranca = Pagamentos.FirstOrDefault(x => x.IdPagamento == idPagamento && x.DisabledAt == null && OrigemAtiva(x));

            if (cobranca != null && cobranca.IdTransacao == idTransacaoCancelado)
            {
                cobranca.IdTransacao = null;
                cobranca.UpdatedAt = agora;
            }

            return Task.CompletedTask;
        }

        /// <summary>Consulta base do cliente, como o repositorio real: nao desativada, origem do cliente e nao desativada.</summary>
        public IQueryable<Pagamento> ConsultaMeusPagamentos(int idCliente)
        {
            return Pagamentos
                .Where(x => x.DisabledAt == null && OrigemAtiva(x) && OrigemDoCliente(x, idCliente))
                .AsQueryable();
        }

        /// <summary>Consulta da baixa: a cobranca nao desativada e com origem ativa.</summary>
        private List<Pagamento> ConsultaBaixa(int idPagamento)
        {
            return Pagamentos
                .Where(x => x.IdPagamento == idPagamento && x.DisabledAt == null && OrigemAtiva(x))
                .ToList();
        }

        /// <summary>Posse da trava: sem cliente (locadora do teste) ou com a origem do cliente informado.</summary>
        private static bool PertenceAoEscopo(Pagamento pagamento, int? idCliente)
        {
            return !idCliente.HasValue || OrigemDoCliente(pagamento, idCliente.Value);
        }

        /// <summary>Copia da cobranca, como o AsNoTracking do repositorio real: alterar a copia nao altera a guardada.</summary>
        private static Pagamento? Copia(Pagamento? cobranca)
        {
            if (cobranca == null)
            {
                return null;
            }

            return new Pagamento
            {
                IdPagamento = cobranca.IdPagamento,
                IdLocacao = cobranca.IdLocacao,
                IdVenda = cobranca.IdVenda,
                Tipo = cobranca.Tipo,
                Valor = cobranca.Valor,
                DataVencimento = cobranca.DataVencimento,
                DataPagamento = cobranca.DataPagamento,
                Forma = cobranca.Forma,
                Status = cobranca.Status,
                IdTransacao = cobranca.IdTransacao,
                QrCodeBase64 = cobranca.QrCodeBase64,
                PixCopiaECola = cobranca.PixCopiaECola,
                PixExpiraEm = cobranca.PixExpiraEm,
                CreatedAt = cobranca.CreatedAt,
                UpdatedAt = cobranca.UpdatedAt,
                DisabledAt = cobranca.DisabledAt,
                Locacao = cobranca.Locacao,
                Venda = cobranca.Venda
            };
        }

        /// <summary>Projecao da listagem: o mesmo que o repositorio real le (sem o QR e sem o id da transacao).</summary>
        private static PagamentoListagemRegistro ParaRegistro(Pagamento pagamento)
        {
            Veiculo? veiculo = pagamento.Locacao?.Veiculo ?? pagamento.Venda?.Veiculo;

            return new PagamentoListagemRegistro
            {
                IdPagamento = pagamento.IdPagamento,
                IdLocacao = pagamento.IdLocacao,
                IdVenda = pagamento.IdVenda,
                Tipo = pagamento.Tipo,
                Valor = pagamento.Valor,
                DataVencimento = pagamento.DataVencimento,
                DataPagamento = pagamento.DataPagamento,
                Forma = pagamento.Forma,
                Status = pagamento.Status,
                VeiculoDescricao = veiculo == null ? null : veiculo.Marca + " " + veiculo.Modelo
            };
        }

        /// <summary>Verdadeiro quando a locacao ou a venda de origem existe e nao esta desativada.</summary>
        private static bool OrigemAtiva(Pagamento pagamento)
        {
            bool locacaoAtiva = pagamento.Locacao != null && pagamento.Locacao.DisabledAt == null;
            bool vendaAtiva = pagamento.Venda != null && pagamento.Venda.DisabledAt == null;

            return locacaoAtiva || vendaAtiva;
        }

        /// <summary>Verdadeiro quando a locacao ou a venda de origem e do cliente.</summary>
        private static bool OrigemDoCliente(Pagamento pagamento, int idCliente)
        {
            bool locacaoDoCliente = pagamento.Locacao != null && pagamento.Locacao.DisabledAt == null && pagamento.Locacao.IdCliente == idCliente;
            bool vendaDoCliente = pagamento.Venda != null && pagamento.Venda.DisabledAt == null && pagamento.Venda.IdCliente == idCliente;

            return locacaoDoCliente || vendaDoCliente;
        }
    }
}
