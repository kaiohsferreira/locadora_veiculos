namespace AutoVia.Core.Options
{
    /// <summary>
    /// Valor da caucao na retirada (Pagamentos:ValorCaucao; contrato caucao-retirada v1.1). Criado uma vez, na subida da API, pela
    /// fabrica de AddCaucaoRetiradaStartup com a regra pura RegrasCaucaoRetirada.TentarLerValorCaucao: valor invalido impede a
    /// subida e nunca vira 0. Trocar o valor exige reiniciar a API. Quem precisa do valor le daqui, nunca de IConfiguration.
    /// </summary>
    public class CaucaoRetiradaOptions
    {
        public CaucaoRetiradaOptions(decimal valorCaucao)
        {
            ValorCaucao = valorCaucao;
        }

        /// <summary>Valor da caucao configurado no servidor (0 = locadora sem caucao). Nunca vem do corpo da requisicao.</summary>
        public decimal ValorCaucao { get; }

        /// <summary>Verdadeiro quando a retirada exige caucao (valor maior que 0). Com 0, a retirada segue o RF-17 como hoje.</summary>
        public bool CaucaoExigida
        {
            get
            {
                bool exigida = ValorCaucao > 0m;

                return exigida;
            }
        }
    }
}
