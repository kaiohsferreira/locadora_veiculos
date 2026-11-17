namespace AutoVia.Communication.ViewObjects.Frota
{
    /// <summary>
    /// VO de saida de lista que mostra a foto de capa do veiculo. O IFotoCapaVeiculoService preenche os dois campos depois do
    /// mapeamento: fotoCapa (a capa, quando ha foto assinada) e erroFotoCapa (texto do servidor, quando o armazenamento falhou
    /// para um item que tem foto cadastrada). Nulo nos dois NAO significa sem foto quando o VO vem de GET por id ou do POST.
    /// </summary>
    public interface IComFotoCapa
    {
        /// <summary>Identificador do veiculo do item; e a chave com que a capa e casada.</summary>
        int IdVeiculo { get; }

        /// <summary>Foto de capa do veiculo, ou nulo quando nao ha foto, quando falhou o armazenamento ou quando o campo nao foi preenchido.</summary>
        FotoCapaVeiculoVO? FotoCapa { get; set; }

        /// <summary>Mensagem para o usuario quando a capa de um item com foto cadastrada nao pode ser montada agora; nulo quando nao ha erro.</summary>
        string? ErroFotoCapa { get; set; }
    }
}
