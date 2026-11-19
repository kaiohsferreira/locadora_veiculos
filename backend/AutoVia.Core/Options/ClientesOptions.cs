using AutoVia.Helpers;

namespace AutoVia.Core.Options
{
    /// <summary>
    /// Secao Clientes do appsettings, no que toca ao teto de pedidos de aprovacao por cliente. Um valor menor ou igual a
    /// zero cai no padrao (mesma regra dos outros limites). A regra fica no proprio getter, para o servico nunca ler
    /// IConfiguration direto.
    /// </summary>
    public class ClientesOptions
    {
        private int _limitePedidosAprovacaoPorDia = ConstantsValues.LimitePedidosAprovacaoPorDiaPadrao;
        private int _limiteEnviosDocumentoPorDia = ConstantsValues.LimiteEnviosDocumentoPorDiaPadrao;

        /// <summary>Quantos envios de documento do cliente (CNH e comprovante, ativos ou substituidos) podem ser feitos na janela de 24 horas. Padrao 20 (Clientes:LimiteEnviosDocumentoPorDia).</summary>
        public int LimiteEnviosDocumentoPorDia
        {
            get
            {
                if (_limiteEnviosDocumentoPorDia <= 0)
                {
                    return ConstantsValues.LimiteEnviosDocumentoPorDiaPadrao;
                }

                return _limiteEnviosDocumentoPorDia;
            }
            set
            {
                _limiteEnviosDocumentoPorDia = value;
            }
        }

        /// <summary>Quantas locadoras novas (distintas) o cliente pode pedir aprovacao na janela. Padrao 5 (Clientes:LimitePedidosAprovacaoPorDia).</summary>
        public int LimitePedidosAprovacaoPorDia
        {
            get
            {
                if (_limitePedidosAprovacaoPorDia <= 0)
                {
                    return ConstantsValues.LimitePedidosAprovacaoPorDiaPadrao;
                }

                return _limitePedidosAprovacaoPorDia;
            }
            set
            {
                _limitePedidosAprovacaoPorDia = value;
            }
        }
    }
}
