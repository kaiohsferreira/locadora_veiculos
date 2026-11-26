using AutoVia.Communication.CustomExceptions;
using AutoVia.Core.Regras;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Email;
using AutoVia.Integrations.Interfaces;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Services
{
    public class NotificacaoTransacionalService : INotificacaoTransacionalService
    {
        private readonly IReservaRepository _reservaRepository;
        private readonly IEmailFila _emailFila;
        private readonly ILogService _logService;

        public NotificacaoTransacionalService(IReservaRepository reservaRepository, IEmailFila emailFila, ILogService logService)
        {
            _reservaRepository = reservaRepository;
            _emailFila = emailFila;
            _logService = logService;
        }

        public async Task ConfirmarReservaAsync(ReservaCriadaEmail evento)
        {
            try
            {
                string? destinatario = await _reservaRepository.FindEmailTitularAsync(evento.IdReserva, evento.IdCliente, evento.IdEmpresa);
                if (string.IsNullOrWhiteSpace(destinatario))
                {
                    return;
                }

                string codigo = RegrasReserva.FormateCodigoReserva(evento.IdReserva);
                string retirada = StaticMethods.FormateDateToPTBR(evento.RetiradaPrevista);
                string devolucao = StaticMethods.FormateDateToPTBR(evento.DevolucaoPrevista);
                string corpo = string.Format(CultureInfo.InvariantCulture, ConstantsMessage.EmailReservaCriadaCorpo, codigo, retirada, devolucao);
                if (evento.ValorEstimado.HasValue)
                {
                    string valor = StaticMethods.FormateToMonetary(evento.ValorEstimado.Value);
                    corpo += string.Format(CultureInfo.InvariantCulture, ConstantsMessage.EmailReservaCriadaValorEstimado, valor);
                }

                Enfileirar(new EmailMensagem
                {
                    DestinatarioEmail = destinatario,
                    Assunto = ConstantsMessage.EmailReservaCriadaAssunto,
                    CorpoTexto = corpo,
                    Etiqueta = ConstantsMessage.EmailReservaCriadaEtiqueta
                });
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroEmailConfirmarReserva, GetType().ToString());
                throw;
            }
        }

        public Task EmitirReciboDevolucaoAsync(LocacaoFinalizadaEmail evento)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(evento.DestinatarioEmail))
                {
                    throw new BusinessException(ConstantsMessage.ErroEmailMensagemInvalida);
                }

                string devolucao = StaticMethods.FormateDateTimeToPTBR(evento.DevolucaoEfetiva);
                string valor = StaticMethods.FormateToMonetary(evento.ValorFinal);
                string corpo = string.Format(CultureInfo.InvariantCulture, ConstantsMessage.EmailLocacaoFinalizadaCorpo, evento.IdLocacao, devolucao, valor);
                Enfileirar(new EmailMensagem
                {
                    DestinatarioEmail = evento.DestinatarioEmail,
                    Assunto = ConstantsMessage.EmailLocacaoFinalizadaAssunto,
                    CorpoTexto = corpo,
                    Etiqueta = ConstantsMessage.EmailLocacaoFinalizadaEtiqueta
                });

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroEmailEmitirReciboDevolucao, GetType().ToString());
                throw;
            }
        }

        public Task AvisarCobrancaAsync(CobrancaLancadaEmail evento)
        {
            try
            {
                bool origemLocacao = evento.IdLocacao is > 0 && evento.IdVenda == null;
                bool origemVenda = evento.IdVenda is > 0 && evento.IdLocacao == null;
                if (!origemLocacao && !origemVenda)
                {
                    throw new BusinessException(ConstantsMessage.ErroEmailCobrancaOrigemInvalida);
                }

                if (string.IsNullOrWhiteSpace(evento.DestinatarioEmail))
                {
                    throw new BusinessException(ConstantsMessage.ErroEmailMensagemInvalida);
                }

                string origem = origemLocacao
                    ? string.Format(CultureInfo.InvariantCulture, ConstantsMessage.EmailCobrancaLancadaOrigemLocacao, evento.IdLocacao)
                    : string.Format(CultureInfo.InvariantCulture, ConstantsMessage.EmailCobrancaLancadaOrigemVenda, evento.IdVenda);
                string valor = StaticMethods.FormateToMonetary(evento.Valor);
                string vencimento = StaticMethods.FormateDateToPTBR(evento.DataVencimento);
                string corpo = string.Format(CultureInfo.InvariantCulture, ConstantsMessage.EmailCobrancaLancadaCorpo, evento.IdPagamento, origem, valor, vencimento);
                Enfileirar(new EmailMensagem
                {
                    DestinatarioEmail = evento.DestinatarioEmail,
                    Assunto = ConstantsMessage.EmailCobrancaLancadaAssunto,
                    CorpoTexto = corpo,
                    Etiqueta = ConstantsMessage.EmailCobrancaLancadaEtiqueta
                });

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroEmailAvisarCobranca, GetType().ToString());
                throw;
            }
        }

        private void Enfileirar(EmailMensagem mensagem)
        {
            try
            {
                bool enfileirada = _emailFila.Enfileirar(mensagem);
                if (!enfileirada)
                {
                    _logService.Write(ConstantsMessage.ErroEmailTransacionalEnfileirar, ConstantsMessage.ErroEmailTransacionalEnfileirar, GetType().ToString());
                }
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroEmailTransacionalEnfileirar, GetType().ToString());
                throw;
            }
        }
    }
}
