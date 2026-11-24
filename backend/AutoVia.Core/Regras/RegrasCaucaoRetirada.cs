using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Helpers;
using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace AutoVia.Core.Regras
{
    /// <summary>
    /// Regras puras da caucao na retirada (P4-BE-04, RF-20; contrato caucao-retirada v1.1). Sem banco, sem log, sem relogio e
    /// sem tipo da integracao: o Core nao enxerga AutoVia.Integrations, entao a decisao sobre o estado devolvido pelo provedor
    /// fica em RegrasDecisaoCaucaoRetirada (AutoVia.Integrations/Helpers/Caucao). A conferencia do retorno (valor e referencia)
    /// reaproveita RegrasPagamento.MotivoDivergenciaRetornoPix. Uma caucao por locacao e garantida por construcao: a abertura
    /// monta no maximo uma linha CAUCAO (RegrasLocacao.CriarLocacaoDeAbertura), sem indice unico no banco.
    /// </summary>
    public static class RegrasCaucaoRetirada
    {
        /// <summary>
        /// Le o valor da caucao da configuracao (Pagamentos:ValorCaucao). Ausente, vazio ou so com espacos: valido, valor 0 (sem
        /// caucao). Senao, o texto aparado precisa casar ConstantsValues.PadraoValorCaucao (so ponto decimal, ate 2 casas, ate 8
        /// digitos inteiros) e e convertido em cultura invariante, na faixa de 0 a ConstantsValues.ValorCaucaoMaximo. Devolve falso,
        /// com valor 0, quando o texto nao vale: quem chama decide (na subida, a API nao sobe; o valor nunca cai em 0 calado).
        /// </summary>
        public static bool TentarLerValorCaucao(string? texto, out decimal valor)
        {
            valor = 0m;

            if (string.IsNullOrWhiteSpace(texto))
            {
                return true;
            }

            string aparado = texto.Trim();

            if (!Regex.IsMatch(aparado, ConstantsValues.PadraoValorCaucao))
            {
                return false;
            }

            bool convertido = decimal.TryParse(aparado, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal lido);

            if (!convertido || lido > ConstantsValues.ValorCaucaoMaximo)
            {
                return false;
            }

            valor = lido;

            return true;
        }

        /// <summary>
        /// Referencia da caucao no provedor (external_reference): CAUCAO-RES-{idReserva}, com o numero em cultura invariante.
        /// Deterministica por reserva (a locacao ainda nao tem id na pre-autorizacao). Ate 21 caracteres.
        /// </summary>
        public static string ReferenciaDaCaucao(int idReserva)
        {
            string referencia = ConstantsValues.CaucaoPrefixoReferencia + idReserva.ToString(CultureInfo.InvariantCulture);

            return referencia;
        }

        /// <summary>
        /// Politica da abertura da locacao (decisao 4 de 24/11/2026): sem caucao exigida, qualquer funcionario abre (RF-17 como
        /// hoje), inclusive sem cargo no contexto; com caucao exigida, so ATENDENTE ou ADMINISTRADOR de uma locadora do contexto
        /// (idEmpresa maior que 0). O cargo e a empresa vem do banco (IContextoEmpresa), nunca da claim.
        /// </summary>
        public static bool CargoPodeAbrirComCaucao(bool caucaoExigida, int idEmpresa, CargoFuncionario? cargo)
        {
            if (!caucaoExigida)
            {
                return true;
            }

            bool cargoPermitido = cargo == CargoFuncionario.ATENDENTE || cargo == CargoFuncionario.ADMINISTRADOR;
            bool permitido = idEmpresa > 0 && cargoPermitido;

            return permitido;
        }

        /// <summary>
        /// Leitura da retirada (GET; correcao f e CE-5): o gateway so e consultado com a caucao exigida e sem locacao ja aberta
        /// para a reserva. Sem caucao (valor 0), ou com a locacao ja aberta, nada vai ao provedor e nada e registrado.
        /// </summary>
        public static bool ConsultaConfiguracaoNaRetirada(bool caucaoExigida, int? idLocacaoExistente)
        {
            bool consulta = caucaoExigida && idLocacaoExistente == null;

            return consulta;
        }

        /// <summary>
        /// Leitura da retirada (GET): a abertura com caucao pode ser tentada pela tela quando a caucao e exigida e o provedor de
        /// simulacao esta liberado (so a simulacao existe na tela; com o Mercado Pago configurado, a caucao aparece indisponivel).
        /// </summary>
        public static bool CaucaoDisponivelNaRetirada(bool caucaoExigida, bool modoSimulacao)
        {
            bool disponivel = caucaoExigida && modoSimulacao;

            return disponivel;
        }

        /// <summary>
        /// Linha CAUCAO gravada junto com a locacao. O id_locacao vem pela navegacao Locacao.Pagamentos, na gravacao; id_venda
        /// fica nulo. Valor da caucao, forma CARTAO, status PENDENTE (RESERVADA no provedor), id_transacao do provedor, vencimento
        /// na data de devolucao prevista, sem data de pagamento e sem as colunas do PIX; created_at e updated_at no momento do
        /// registro.
        /// </summary>
        public static Pagamento CriarPagamentoCaucao(decimal valor, string idTransacao, DateTime dataDevolucaoPrevista, DateTime momentoRegistro)
        {
            Pagamento pagamento = new Pagamento
            {
                IdVenda = null,
                Tipo = TipoPagamento.CAUCAO,
                Valor = valor,
                DataVencimento = DateOnly.FromDateTime(dataDevolucaoPrevista),
                DataPagamento = null,
                Forma = ConstantsValues.FormaCartao,
                Status = StatusPagamento.PENDENTE,
                IdTransacao = idTransacao,
                QrCodeBase64 = null,
                PixCopiaECola = null,
                PixExpiraEm = null,
                CreatedAt = momentoRegistro,
                UpdatedAt = momentoRegistro,
                DisabledAt = null
            };

            return pagamento;
        }
    }
}
