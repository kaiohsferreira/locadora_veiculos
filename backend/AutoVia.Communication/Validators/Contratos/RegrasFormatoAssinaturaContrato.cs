using AutoVia.Helpers;
using System;

namespace AutoVia.Communication.Validators.Contratos
{
    /// <summary>
    /// Regras de formato do nome de quem assina e da imagem da assinatura (contrato-locacao v5, RN-5). Puras: o validador do VO
    /// chama estas funcoes e o servico as chama de novo antes de gravar (defesa em profundidade). Cada uma devolve a mensagem da
    /// primeira falha, ou nulo quando o valor e aceito.
    /// </summary>
    public static class RegrasFormatoAssinaturaContrato
    {
        /// <summary>Nome vazio, acima de 100 caracteres ou com caractere de controle (U+0000 e demais controles, RN-5 da v2).</summary>
        public static string? ErroNomeAssinante(string? nome)
        {
            string? erro = null;
            if (string.IsNullOrWhiteSpace(nome))
            {
                erro = ConstantsMessage.ErroContratoNomeAssinante;
            }
            else if (nome.Length > ConstantsValues.ContratoNomeAssinanteTamanhoMaximo)
            {
                erro = ConstantsMessage.ErroContratoNomeAssinanteTamanho;
            }
            else if (ContemCaractereDeControle(nome))
            {
                erro = ConstantsMessage.ErroContratoNomeAssinanteInvalido;
            }

            return erro;
        }

        /// <summary>Assinatura vazia, base64 acima do teto, base64 invalido, acima de 65.536 bytes, ou que nao comeca com o cabecalho PNG.</summary>
        public static string? ErroAssinaturaPng(string? base64)
        {
            string? erro = null;
            if (string.IsNullOrWhiteSpace(base64))
            {
                erro = ConstantsMessage.ErroContratoAssinaturaObrigatoria;
            }
            else if (base64.Length > ConstantsValues.ContratoAssinaturaBase64TamanhoMaximo)
            {
                erro = ConstantsMessage.ErroContratoAssinaturaTamanho;
            }
            else
            {
                byte[]? bytes = DecodificarBase64(base64);
                if (bytes == null)
                {
                    erro = ConstantsMessage.ErroContratoAssinaturaInvalida;
                }
                else if (bytes.Length > ConstantsValues.ContratoAssinaturaTamanhoMaximoBytes)
                {
                    erro = ConstantsMessage.ErroContratoAssinaturaTamanho;
                }
                else if (!EhPng(bytes))
                {
                    erro = ConstantsMessage.ErroContratoAssinaturaInvalida;
                }
            }

            return erro;
        }

        private static bool ContemCaractereDeControle(string texto)
        {
            bool contem = false;
            foreach (char caractere in texto)
            {
                if (char.IsControl(caractere))
                {
                    contem = true;
                    break;
                }
            }

            return contem;
        }

        /// <summary>Decodifica o base64 sem lancar excecao; nulo quando o texto nao e base64 valido.</summary>
        private static byte[]? DecodificarBase64(string base64)
        {
            byte[] buffer = new byte[base64.Length];
            bool valido = Convert.TryFromBase64String(base64, buffer, out int escritos);
            byte[]? bytes = null;
            if (valido)
            {
                bytes = new byte[escritos];
                Array.Copy(buffer, bytes, escritos);
            }

            return bytes;
        }

        private static bool EhPng(byte[] bytes)
        {
            byte[] cabecalho = ConstantsValues.ContratoAssinaturaCabecalhoPng;
            bool png = bytes.Length >= cabecalho.Length;
            for (int indice = 0; png && indice < cabecalho.Length; indice++)
            {
                png = bytes[indice] == cabecalho[indice];
            }

            return png;
        }
    }
}
