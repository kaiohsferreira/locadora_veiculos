using AutoVia.Communication.Enums;

namespace AutoVia.Communication.ViewObjects.Avarias
{
    /// <summary>Dados publicos de uma avaria e a URL temporaria da foto.</summary>
    public sealed class ReturnAvariaVO
    {
        public int IdAvaria { get; set; }
        public int IdVistoria { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public GravidadeAvaria Gravidade { get; set; }
        public decimal? ValorEstimado { get; set; }
        public FotoAvariaVO? Foto { get; set; }

        /// <summary>Lado do carro do desenho; nulo em avaria anterior a v4 e em avaria nova da saida.</summary>
        public AnguloAvaria? Angulo { get; set; }

        /// <summary>Posicao X do marcador (percentual de 0 a 100 na largura do quadrante); nula quando nao informada.</summary>
        public decimal? PosX { get; set; }

        /// <summary>Posicao Y do marcador (percentual de 0 a 100 na altura do quadrante); nula quando nao informada.</summary>
        public decimal? PosY { get; set; }

        /// <summary>Texto do erro da foto: so preenchido na lista, em avaria de entrada cujo arquivo nao existe no armazenamento.</summary>
        public string? ErroFoto { get; set; }
    }
}
