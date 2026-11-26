namespace AutoVia.Communication.Enums
{
    /// <summary>
    /// Lados do carro do desenho da vistoria (tela 19) onde a avaria aparece: valores da coluna avaria.angulo
    /// (restricao ck_avaria_angulo). Persistido como texto (HasConversion&lt;string&gt;): o NOME do membro e o valor
    /// gravado no banco, por isso os membros seguem a especificacao (MAIUSCULAS_COM_UNDERLINE).
    /// </summary>
    public enum AnguloAvaria
    {
        FRENTE,
        TRASEIRA,
        LATERAL_ESQUERDA,
        LATERAL_DIREITA
    }
}
