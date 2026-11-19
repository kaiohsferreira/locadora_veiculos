using AutoVia.Communication.Enums;
using AutoVia.Core.Regras;
using AutoVia.Helpers;

namespace AutoVia.Tests.Frota
{
    /// <summary>
    /// Regras puras da troca manual de status (P1-BE-04, RF-04), sem banco. Cobre os 36 pares da matriz do contrato, cada um
    /// com e sem reserva em aberto e com e sem locacao em aberto (144 casos); a ordem das checagens; a conversao do nome; e os
    /// conjuntos de valores de StatusVeiculo, StatusReserva e StatusLocacao fixados por teste. A matriz esperada aqui e a
    /// legenda do contrato escrita de novo, nao uma copia do codigo da regra.
    /// </summary>
    public class RegrasStatusVeiculoTests
    {
        private const string CodigoIgual = "=";
        private const string CodigoRecusaDestino = "D";
        private const string CodigoRecusaOrigem = "O";
        private const string CodigoPermitida = "OK*";

        /// <summary>Ordem das linhas e das colunas da matriz do contrato.</summary>
        private static readonly StatusVeiculo[] Ordem = new StatusVeiculo[]
        {
            StatusVeiculo.DISPONIVEL, StatusVeiculo.RESERVADO, StatusVeiculo.LOCADO, StatusVeiculo.MANUTENCAO, StatusVeiculo.VENDIDO, StatusVeiculo.BAIXADO
        };

        /// <summary>Linha = status atual; cada posicao segue a ordem de Ordem (status pedido). Codigos da legenda do contrato.</summary>
        private static readonly Dictionary<StatusVeiculo, string[]> Matriz = new Dictionary<StatusVeiculo, string[]>
        {
            [StatusVeiculo.DISPONIVEL] = new string[] { CodigoIgual, CodigoRecusaDestino, CodigoRecusaDestino, CodigoPermitida, CodigoRecusaDestino, CodigoPermitida },
            [StatusVeiculo.RESERVADO] = new string[] { CodigoRecusaOrigem, CodigoRecusaDestino, CodigoRecusaDestino, CodigoRecusaOrigem, CodigoRecusaDestino, CodigoRecusaOrigem },
            [StatusVeiculo.LOCADO] = new string[] { CodigoRecusaOrigem, CodigoRecusaDestino, CodigoRecusaDestino, CodigoRecusaOrigem, CodigoRecusaDestino, CodigoRecusaOrigem },
            [StatusVeiculo.MANUTENCAO] = new string[] { CodigoPermitida, CodigoRecusaDestino, CodigoRecusaDestino, CodigoIgual, CodigoRecusaDestino, CodigoPermitida },
            [StatusVeiculo.VENDIDO] = new string[] { CodigoRecusaOrigem, CodigoRecusaDestino, CodigoRecusaDestino, CodigoRecusaOrigem, CodigoRecusaDestino, CodigoRecusaOrigem },
            [StatusVeiculo.BAIXADO] = new string[] { CodigoRecusaOrigem, CodigoRecusaDestino, CodigoRecusaDestino, CodigoRecusaOrigem, CodigoRecusaDestino, CodigoIgual }
        };

        /// <summary>Casos de MemberData: cada par origem e destino, com e sem reserva em aberto e com e sem locacao em aberto.</summary>
        public static IEnumerable<object[]> Casos()
        {
            foreach (StatusVeiculo origem in Ordem)
            {
                foreach (StatusVeiculo destino in Ordem)
                {
                    foreach (bool reserva in new bool[] { false, true })
                    {
                        foreach (bool locacao in new bool[] { false, true })
                        {
                            yield return new object[] { origem, destino, reserva, locacao };
                        }
                    }
                }
            }
        }

        /// <summary>Resultado esperado pela legenda: igual (nada), D (so por fluxo), OK* (permitida) e O (origem), com a precedencia do contrato.</summary>
        private static (bool SemAlteracao, bool Permitida, string? Mensagem) Esperado(string codigo, StatusVeiculo origem, bool reserva, bool locacao)
        {
            if (codigo == CodigoIgual)
            {
                return (true, true, null);
            }

            if (codigo == CodigoRecusaDestino)
            {
                return (false, false, ConstantsMessage.ErroVeiculoStatusSomentePorFluxo);
            }

            if (locacao)
            {
                return (false, false, ConstantsMessage.ErroVeiculoStatusComLocacaoEmAberto);
            }

            if (reserva)
            {
                return (false, false, ConstantsMessage.ErroVeiculoStatusComReservaEmAberto);
            }

            if (codigo == CodigoPermitida)
            {
                return (false, true, null);
            }

            if (origem == StatusVeiculo.VENDIDO || origem == StatusVeiculo.BAIXADO)
            {
                return (false, false, ConstantsMessage.ErroVeiculoStatusVendidoOuBaixado);
            }

            return (false, false, ConstantsMessage.ErroVeiculoStatusOrigemPorFluxo);
        }

        [Fact]
        public void Matriz_TemSeisLinhasDeSeisColunas_eCentoEQuarentaEQuatroCasos()
        {
            Assert.Equal(6, Matriz.Count);
            Assert.All(Matriz.Values, linha => Assert.Equal(6, linha.Length));
            Assert.Equal(144, Casos().Count());
        }

        [Theory]
        [MemberData(nameof(Casos))]
        public void Avaliar_CadaParComAndSemReservaELocacao_ConfereComAMatrizDoContrato(StatusVeiculo origem, StatusVeiculo destino, bool reserva, bool locacao)
        {
            string codigo = Matriz[origem][Array.IndexOf(Ordem, destino)];
            (bool SemAlteracao, bool Permitida, string? Mensagem) esperado = Esperado(codigo, origem, reserva, locacao);

            RegrasStatusVeiculo.ResultadoTrocaStatus resultado = RegrasStatusVeiculo.Avaliar(origem, destino, reserva, locacao);

            Assert.Equal(esperado.SemAlteracao, resultado.SemAlteracao);
            Assert.Equal(esperado.Permitida, resultado.Permitida);
            Assert.Equal(esperado.Mensagem, resultado.MensagemRecusa);
        }

        [Fact]
        public void Ordem_LocacaoEmAbertoGanhaDaReservaEmAberto()
        {
            RegrasStatusVeiculo.ResultadoTrocaStatus resultado = RegrasStatusVeiculo.Avaliar(StatusVeiculo.DISPONIVEL, StatusVeiculo.MANUTENCAO, true, true);

            Assert.Equal(ConstantsMessage.ErroVeiculoStatusComLocacaoEmAberto, resultado.MensagemRecusa);
        }

        [Fact]
        public void Ordem_DestinoPorFluxoGanhaDeTudo_MesmoComLocacaoEReserva()
        {
            RegrasStatusVeiculo.ResultadoTrocaStatus resultado = RegrasStatusVeiculo.Avaliar(StatusVeiculo.RESERVADO, StatusVeiculo.RESERVADO, true, true);

            Assert.False(resultado.SemAlteracao);
            Assert.False(resultado.Permitida);
            Assert.Equal(ConstantsMessage.ErroVeiculoStatusSomentePorFluxo, resultado.MensagemRecusa);
        }

        [Fact]
        public void Ordem_MesmoStatusManualGanhaDeTudo_MesmoComLocacaoEReserva()
        {
            RegrasStatusVeiculo.ResultadoTrocaStatus resultado = RegrasStatusVeiculo.Avaliar(StatusVeiculo.DISPONIVEL, StatusVeiculo.DISPONIVEL, true, true);

            Assert.True(resultado.SemAlteracao);
            Assert.True(resultado.Permitida);
            Assert.Null(resultado.MensagemRecusa);
        }

        [Fact]
        public void Conjuntos_StatusVeiculo_SeisValoresFixados()
        {
            string[] nomes = new string[] { "DISPONIVEL", "RESERVADO", "LOCADO", "MANUTENCAO", "VENDIDO", "BAIXADO" };

            Assert.Equal(nomes, Enum.GetNames<StatusVeiculo>());
        }

        [Fact]
        public void Conjuntos_StatusReserva_QuatroValoresEmAbertoSoAtivaEConfirmada()
        {
            string[] nomes = new string[] { "ATIVA", "CONFIRMADA", "CANCELADA", "CONCLUIDA" };
            StatusReserva[] emAberto = new StatusReserva[] { StatusReserva.ATIVA, StatusReserva.CONFIRMADA };

            Assert.Equal(nomes, Enum.GetNames<StatusReserva>());
            Assert.Equal(emAberto, RegrasStatusVeiculo.StatusReservasEmAberto());
        }

        [Fact]
        public void Conjuntos_StatusLocacao_QuatroValoresEmAbertoSoAndamentoEAtrasada()
        {
            string[] nomes = new string[] { "EM_ANDAMENTO", "FINALIZADA", "ATRASADA", "CANCELADA" };
            StatusLocacao[] emAberto = new StatusLocacao[] { StatusLocacao.EM_ANDAMENTO, StatusLocacao.ATRASADA };

            Assert.Equal(nomes, Enum.GetNames<StatusLocacao>());
            Assert.Equal(emAberto, RegrasStatusVeiculo.StatusLocacoesEmAberto());
        }

        [Theory]
        [InlineData("DISPONIVEL", StatusVeiculo.DISPONIVEL)]
        [InlineData("RESERVADO", StatusVeiculo.RESERVADO)]
        [InlineData("LOCADO", StatusVeiculo.LOCADO)]
        [InlineData("MANUTENCAO", StatusVeiculo.MANUTENCAO)]
        [InlineData("VENDIDO", StatusVeiculo.VENDIDO)]
        [InlineData("BAIXADO", StatusVeiculo.BAIXADO)]
        [InlineData("  MANUTENCAO  ", StatusVeiculo.MANUTENCAO)]
        public void TentarLerStatusPorNome_NomeExatoDepoisDoTrim_Converte(string texto, StatusVeiculo esperado)
        {
            bool convertido = RegrasStatusVeiculo.TentarLerStatusPorNome(texto, out StatusVeiculo status);

            Assert.True(convertido);
            Assert.Equal(esperado, status);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("XYZ")]
        [InlineData("manutencao")]
        [InlineData("Manutencao")]
        [InlineData("3")]
        [InlineData("MANUTENCAO\0")]
        public void TentarLerStatusPorNome_NumeroMinusculasOuTextoEstranho_NaoConverte(string? texto)
        {
            bool convertido = RegrasStatusVeiculo.TentarLerStatusPorNome(texto, out StatusVeiculo status);

            Assert.False(convertido);
            Assert.Equal(StatusVeiculo.DISPONIVEL, status);
        }

        [Theory]
        [InlineData(StatusVeiculo.DISPONIVEL, true)]
        [InlineData(StatusVeiculo.MANUTENCAO, true)]
        [InlineData(StatusVeiculo.BAIXADO, true)]
        [InlineData(StatusVeiculo.RESERVADO, false)]
        [InlineData(StatusVeiculo.LOCADO, false)]
        [InlineData(StatusVeiculo.VENDIDO, false)]
        public void EhDestinoManual_SoTresDestinosSaoManuais(StatusVeiculo destino, bool esperado)
        {
            Assert.Equal(esperado, RegrasStatusVeiculo.EhDestinoManual(destino));
        }
    }
}
