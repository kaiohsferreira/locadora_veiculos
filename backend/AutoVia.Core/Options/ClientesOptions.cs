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
