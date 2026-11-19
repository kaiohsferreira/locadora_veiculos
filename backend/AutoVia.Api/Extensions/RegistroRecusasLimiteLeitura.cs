using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using System;
using System.Globalization;

namespace AutoVia.Api.Extensions
{
    /// <summary>
    /// Registro AGREGADO das recusas 429 da rota publica de leitura de arquivo. Um registro por minuto, no maximo, por motivo
    /// (limite por IP ou teto da rota), com a contagem das recusas desde o ultimo registro. A primeira recusa grava na hora.
    /// Nao leva IP, chave nem URL, e nao guarda um contador por IP (cresceria com IP forjado).
    /// </summary>
    public sealed class RegistroRecusasLimiteLeitura
    {
        private readonly ILogService _logService;
        private readonly TimeProvider _relogio;
        private readonly object _trava = new object();
        private readonly ContadorDeRecusas _porIp = new ContadorDeRecusas();
        private readonly ContadorDeRecusas _teto = new ContadorDeRecusas();

        public RegistroRecusasLimiteLeitura(ILogService logService, TimeProvider relogio)
        {
            _logService = logService;
            _relogio = relogio;
        }

        /// <summary>Conta uma recusa do limite por IP e registra (agregado) quando passou o intervalo do ultimo registro.</summary>
        public void RegistrarPorIp()
        {
            Registrar(_porIp, ConstantsMessage.LogArmazenamentoLeituraLimitePorIp);
        }

        /// <summary>Conta uma recusa do teto da rota e registra (agregado) quando passou o intervalo do ultimo registro.</summary>
        public void RegistrarTeto()
        {
            Registrar(_teto, ConstantsMessage.LogArmazenamentoLeituraTetoDaRota);
        }

        /// <summary>Soma a recusa no motivo e grava o registro agregado quando o intervalo ja passou. Seguro para chamadas simultaneas.</summary>
        private void Registrar(ContadorDeRecusas contador, string modeloMensagem)
        {
            try
            {
                int contagem = 0;
                bool gravar = false;

                lock (_trava)
                {
                    contador.RecusasDesdeUltimoRegistro++;
                    DateTimeOffset agora = _relogio.GetLocalNow();

                    if (contador.UltimoRegistro == null
                        || agora - contador.UltimoRegistro.Value >= TimeSpan.FromSeconds(ConstantsValues.IntervaloRegistroRecusaLimiteSegundos))
                    {
                        gravar = true;
                        contagem = contador.RecusasDesdeUltimoRegistro;
                        contador.RecusasDesdeUltimoRegistro = 0;
                        contador.UltimoRegistro = agora;
                    }
                }

                if (gravar)
                {
                    string mensagem = string.Format(CultureInfo.InvariantCulture, modeloMensagem, contagem);
                    _logService.Write(mensagem, ConstantsMessage.ErroLimiteRequisicoes, GetType().ToString());
                }
            }
            catch (Exception ex)
            {
                _logService.Write(ex.GetType().Name, ConstantsMessage.ErroInterno, GetType().ToString());
            }
        }

        /// <summary>Contagem de um motivo e o instante do ultimo registro gravado (nulo antes do primeiro).</summary>
        private sealed class ContadorDeRecusas
        {
            public int RecusasDesdeUltimoRegistro { get; set; }

            public DateTimeOffset? UltimoRegistro { get; set; }
        }
    }
}
