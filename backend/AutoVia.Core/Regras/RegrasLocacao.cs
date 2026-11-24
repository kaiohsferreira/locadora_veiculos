using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Helpers;
using System;

namespace AutoVia.Core.Regras
{
    /// <summary>
    /// Regras puras da abertura de locacao a partir de reserva (RF-17). Sem banco, sem log e sem relogio: o servico e o
    /// repositorio usam as mesmas funcoes, e os testes as conferem direto.
    /// </summary>
    public static class RegrasLocacao
    {
        /// <summary>Verdadeiro quando a reserva pode dar origem a uma locacao: somente ATIVA ou CONFIRMADA.</summary>
        public static bool ReservaPermiteAbrirLocacao(StatusReserva status)
        {
            bool permite = status == StatusReserva.ATIVA || status == StatusReserva.CONFIRMADA;

            return permite;
        }

        /// <summary>Verdadeiro quando o veiculo pode ser entregue na abertura: somente RESERVADO.</summary>
        public static bool VeiculoPermiteAbrirLocacao(StatusVeiculo status)
        {
            bool permite = status == StatusVeiculo.RESERVADO;

            return permite;
        }

        /// <summary>Verdadeiro quando a CNH ja venceu (validade anterior a hoje). A validade de hoje ainda vale.</summary>
        public static bool CnhVencida(DateOnly validade, DateOnly hoje)
        {
            bool vencida = validade < hoje;

            return vencida;
        }

        /// <summary>Monta o endereco da locadora para a retirada, como texto pronto; parte vazia some junto com o separador.</summary>
        public static string MontarEnderecoLocadora(string logradouro, string numero, string bairro, string cidade, string uf)
        {
            string endereco = AnexarParteEndereco(string.Empty, logradouro, string.Empty);
            endereco = AnexarParteEndereco(endereco, numero, ConstantsValues.SeparadorNumeroEnderecoLocadora);
            endereco = AnexarParteEndereco(endereco, bairro, ConstantsValues.SeparadorBairroEnderecoLocadora);
            endereco = AnexarParteEndereco(endereco, cidade, ConstantsValues.SeparadorCidadeEnderecoLocadora);
            endereco = AnexarParteEndereco(endereco, uf, ConstantsValues.SeparadorUfEnderecoLocadora);

            return endereco;
        }

        /// <summary>Acrescenta uma parte aparada ao texto; parte vazia nao acrescenta nada, nem o separador dela.</summary>
        private static string AnexarParteEndereco(string atual, string parte, string separador)
        {
            string limpa = parte.Trim();
            if (limpa.Length == 0)
            {
                return atual;
            }

            if (atual.Length == 0)
            {
                return limpa;
            }

            return atual + separador + limpa;
        }

        /// <summary>Status da reserva depois da abertura da locacao.</summary>
        public static StatusReserva StatusReservaAposAbertura()
        {
            StatusReserva status = StatusReserva.CONCLUIDA;

            return status;
        }

        /// <summary>Status do veiculo depois da abertura da locacao.</summary>
        public static StatusVeiculo StatusVeiculoAposAbertura()
        {
            StatusVeiculo status = StatusVeiculo.LOCADO;

            return status;
        }

        /// <summary>
        /// Monta a locacao da abertura: quilometragem de saida = km atual do veiculo, diaria = diaria atual do veiculo,
        /// caucao 0, status EM_ANDAMENTO. A retirada vem do servico (IRelogio); CreatedAt e UpdatedAt vem do momento do registro.
        /// </summary>
        public static Locacao CriarLocacaoDeAbertura(Reserva reserva, Veiculo veiculo, int idFuncionario, DateTime dataRetirada, DateTime momentoRegistro)
        {
            Locacao locacao = new Locacao
            {
                IdReserva = reserva.IdReserva,
                IdCliente = reserva.IdCliente,
                IdVeiculo = veiculo.IdVeiculo,
                IdFuncionario = idFuncionario,
                DataRetirada = dataRetirada,
                DataDevolucaoPrevista = reserva.DataDevolucaoPrevista,
                KmSaida = veiculo.KmAtual,
                ValorDiaria = veiculo.ValorDiaria,
                ValorCaucao = 0m,
                Status = StatusLocacao.EM_ANDAMENTO,
                CreatedAt = momentoRegistro,
                UpdatedAt = momentoRegistro,
                DisabledAt = null
            };

            return locacao;
        }
    }
}
