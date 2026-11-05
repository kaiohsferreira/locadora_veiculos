namespace AutoVia.Communication.Enums
{
    /// <summary>
    /// Valores da coluna venda.forma_pagamento (restricao ck_venda_forma).
    /// Persistido como texto (HasConversion&lt;string&gt;): o NOME do membro e o valor
    /// gravado no banco, por isso os membros seguem exatamente a especificacao
    /// (MAIUSCULAS_COM_UNDERLINE) em vez de PascalCase.
    ///
    /// O primeiro membro (valor 0) e o DEFAULT da coluna, quando ela tem um.
    /// Nao reordene: o EF trata o valor 0 como "nao informado" no insert e deixa
    /// o banco aplicar o DEFAULT.
    /// </summary>
    public enum FormaPagamentoVenda
    {
        A_VISTA,
        FINANCIAMENTO,
        CONSORCIO
    }
}
