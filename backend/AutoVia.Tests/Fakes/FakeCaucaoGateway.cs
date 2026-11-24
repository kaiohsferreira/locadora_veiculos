using AutoVia.Integrations.Communication.Caucao;
using AutoVia.Integrations.Helpers.Caucao;
using AutoVia.Integrations.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AutoVia.Tests.Fakes
{
    /// <summary>
    /// Gateway da caucao de teste (sem rede e sem provedor): registra cada chamada, na ordem, com o CancellationToken recebido, e
    /// devolve o que o teste programa. Padrao: pre-autorizacao RESERVADA com o identificador fixo, o valor e a referencia pedidos e
    /// o final do cartao '7391' (para o teste de log); consulta RESERVADA do mesmo identificador; cancelamento CANCELADA.
    /// Os testes HTTP usam este gateway porque rodam em ambiente Testing, onde a trava do FAKE real recusaria.
    /// </summary>
    public class FakeCaucaoGateway : ICaucaoGateway
    {
        /// <summary>Identificador devolvido pela pre-autorizacao padrao (formato do FAKE: FAKE-CAU- + 12 hex).</summary>
        public const string IdentificadorPadrao = "FAKE-CAU-0123456789ab";

        /// <summary>Final do cartao devolvido pelas respostas padrao: nunca pode aparecer no log.</summary>
        public const string CartaoFinalDeTeste = "7391";

        public FakeCaucaoGateway()
        {
            ResponderPreAutorizacao = RetornoReservado;
            ResponderConsulta = (string identificador) => RetornoNoEstado(identificador, CaucaoStatus.RESERVADA, 0m, string.Empty);
        }

        /// <summary>Chamadas na ordem: PreAutorizar, Consultar, Cancelar, Capturar e Configuracao.</summary>
        public List<string> Chamadas { get; } = new List<string>();

        /// <summary>Solicitacoes de pre-autorizacao recebidas, na ordem.</summary>
        public List<CaucaoSolicitacao> Solicitacoes { get; } = new List<CaucaoSolicitacao>();

        /// <summary>Identificadores consultados, na ordem.</summary>
        public List<string> Consultas { get; } = new List<string>();

        /// <summary>Identificadores cancelados, na ordem.</summary>
        public List<string> Cancelamentos { get; } = new List<string>();

        /// <summary>CancellationToken recebido em cada chamada, na ordem das chamadas.</summary>
        public List<CancellationToken> TokensRecebidos { get; } = new List<CancellationToken>();

        /// <summary>Quantas vezes a configuracao do cartao foi pedida.</summary>
        public int ChamadasConfiguracao { get; private set; }

        /// <summary>Quantas chamadas o gateway recebeu, de qualquer tipo.</summary>
        public int TotalDeChamadas => Chamadas.Count;

        /// <summary>Quem decide o retorno da pre-autorizacao (sem ExcecaoPreAutorizacao).</summary>
        public Func<CaucaoSolicitacao, CaucaoResultado> ResponderPreAutorizacao { get; set; }

        /// <summary>Excecao que a pre-autorizacao lanca (ex.: BusinessException de cartao recusado ou IntegrationException de rede).</summary>
        public Exception? ExcecaoPreAutorizacao { get; set; }

        /// <summary>Quem decide o retorno da consulta (sem ExcecaoConsulta).</summary>
        public Func<string, CaucaoResultado> ResponderConsulta { get; set; }

        /// <summary>Excecao que a consulta lanca.</summary>
        public Exception? ExcecaoConsulta { get; set; }

        /// <summary>Gancho chamado no inicio da consulta (ex.: mudar o relogio ou conferir que o repositorio ainda nao foi chamado).</summary>
        public Action<string>? AoConsultar { get; set; }

        /// <summary>Excecao que o cancelamento lanca.</summary>
        public Exception? ExcecaoCancelamento { get; set; }

        /// <summary>Gancho chamado no inicio do cancelamento.</summary>
        public Action<string>? AoCancelar { get; set; }

        /// <summary>Configuracao do cartao devolvida (padrao: FAKE em simulacao).</summary>
        public CaucaoConfiguracaoCartao Configuracao { get; set; } = new CaucaoConfiguracaoCartao
        {
            Provedor = ConstantsCaucao.ProvedorFake,
            ChavePublica = ConstantsCaucao.ChavePublicaFake,
            ModoSimulacao = true
        };

        /// <summary>Excecao que a configuracao do cartao lanca.</summary>
        public Exception? ExcecaoConfiguracao { get; set; }

        /// <summary>Retorno padrao: RESERVADA, identificador fixo, valor e referencia pedidos, final do cartao de teste.</summary>
        public CaucaoResultado RetornoReservado(CaucaoSolicitacao solicitacao)
        {
            CaucaoResultado resultado = RetornoNoEstado(IdentificadorPadrao, CaucaoStatus.RESERVADA, solicitacao.Valor, solicitacao.ReferenciaExterna);

            return resultado;
        }

        /// <summary>Retorno com o estado informado, para o identificador, o valor e a referencia dados.</summary>
        public static CaucaoResultado RetornoNoEstado(string identificador, CaucaoStatus status, decimal valor, string referencia)
        {
            CaucaoResultado resultado = new CaucaoResultado
            {
                IdentificadorProvedor = identificador,
                ReferenciaExterna = referencia,
                Status = status,
                Valor = valor,
                CartaoFinal = CartaoFinalDeTeste,
                CriadoEm = new DateTime(2026, 11, 24, 9, 0, 0),
                Provedor = ConstantsCaucao.ProvedorFake
            };

            return resultado;
        }

        public Task<CaucaoResultado> PreAutorizarAsync(CaucaoSolicitacao solicitacao, CancellationToken cancellationToken = default)
        {
            Chamadas.Add(nameof(PreAutorizarAsync));
            TokensRecebidos.Add(cancellationToken);
            Solicitacoes.Add(solicitacao);

            if (ExcecaoPreAutorizacao != null)
            {
                throw ExcecaoPreAutorizacao;
            }

            CaucaoResultado resultado = ResponderPreAutorizacao(solicitacao);

            return Task.FromResult(resultado);
        }

        public Task<CaucaoResultado> ConsultarAsync(string identificadorProvedor, CancellationToken cancellationToken = default)
        {
            Chamadas.Add(nameof(ConsultarAsync));
            TokensRecebidos.Add(cancellationToken);
            Consultas.Add(identificadorProvedor);
            AoConsultar?.Invoke(identificadorProvedor);

            if (ExcecaoConsulta != null)
            {
                throw ExcecaoConsulta;
            }

            CaucaoResultado resultado = ResponderConsulta(identificadorProvedor);

            return Task.FromResult(resultado);
        }

        public Task<CaucaoResultado> CapturarAsync(string identificadorProvedor, decimal? valor = null, CancellationToken cancellationToken = default)
        {
            Chamadas.Add(nameof(CapturarAsync));
            TokensRecebidos.Add(cancellationToken);

            throw new NotSupportedException();
        }

        public Task<CaucaoResultado> CancelarAsync(string identificadorProvedor, CancellationToken cancellationToken = default)
        {
            Chamadas.Add(nameof(CancelarAsync));
            TokensRecebidos.Add(cancellationToken);
            Cancelamentos.Add(identificadorProvedor);
            AoCancelar?.Invoke(identificadorProvedor);

            if (ExcecaoCancelamento != null)
            {
                throw ExcecaoCancelamento;
            }

            CaucaoResultado resultado = RetornoNoEstado(identificadorProvedor, CaucaoStatus.CANCELADA, 0m, string.Empty);

            return Task.FromResult(resultado);
        }

        public Task<CaucaoConfiguracaoCartao> ObterConfiguracaoCartaoAsync(CancellationToken cancellationToken = default)
        {
            Chamadas.Add(nameof(ObterConfiguracaoCartaoAsync));
            TokensRecebidos.Add(cancellationToken);
            ChamadasConfiguracao++;

            if (ExcecaoConfiguracao != null)
            {
                throw ExcecaoConfiguracao;
            }

            return Task.FromResult(Configuracao);
        }
    }
}
