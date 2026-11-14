using AutoVia.Communication.Enums;

namespace AutoVia.Communication.ViewObjects.Painel
{
    /// <summary>
    /// Um item de frotaPorSituacao da visao geral: quantos veiculos da locadora estao em uma situacao (StatusVeiculo, como
    /// texto). A lista traz TODOS os status, na ordem do enum, inclusive os de quantidade zero.
    /// </summary>
    public class FrotaSituacaoVO
    {
        /// <summary>Situacao do veiculo (DISPONIVEL, RESERVADO, LOCADO, MANUTENCAO, VENDIDO ou BAIXADO).</summary>
        public StatusVeiculo Situacao { get; set; }

        /// <summary>Quantidade de veiculos com DisabledAt nulo nesta situacao (zero quando nao ha nenhum).</summary>
        public int Quantidade { get; set; }
    }
}
