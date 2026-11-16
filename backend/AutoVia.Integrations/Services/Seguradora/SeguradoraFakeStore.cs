using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Seguradora;
using AutoVia.Integrations.Helpers.Seguradora;
using System.Collections.Concurrent;

namespace AutoVia.Integrations.Services.Seguradora
{
    /// <summary>
    /// Estado em memoria dos sinistros da seguradora FAKE (singleton). Usado so em SANDBOX e
    /// nos testes. AvancarStatus simula a seguradora mudando o andamento do sinistro.
    ///
    /// Imutabilidade: guarda registros imutaveis e cada leitura devolve uma copia nova de
    /// SinistroSeguradoraResultado; quem recebe a copia pode altera-la sem mexer no que esta guardado.
    /// Atomicidade: toda transicao le e grava sob o mesmo bloqueio.
    /// </summary>
    public class SeguradoraFakeStore
    {
        private readonly ConcurrentDictionary<string, SinistroArmazenado> _sinistros = new ConcurrentDictionary<string, SinistroArmazenado>(StringComparer.Ordinal);
        private readonly object _transicoes = new object();

        /// <summary>Devolve o sinistro do protocolo ou cria um novo, ABERTO, com o evento de abertura (idempotente).</summary>
        public SinistroSeguradoraResultado ObterOuCriar(string protocolo, string referenciaExterna, string provedor)
        {
            lock (_transicoes)
            {
                SinistroArmazenado armazenado = _sinistros.GetOrAdd(protocolo, (string chave) => CriarAberto(chave, referenciaExterna, provedor));

                return ParaResultado(armazenado);
            }
        }

        /// <summary>Copia do sinistro pelo protocolo, ou null se nao existir.</summary>
        public SinistroSeguradoraResultado? Obter(string protocolo)
        {
            lock (_transicoes)
            {
                SinistroArmazenado? armazenado;

                if (!_sinistros.TryGetValue(protocolo, out armazenado))
                {
                    return null;
                }

                return ParaResultado(armazenado);
            }
        }

        /// <summary>
        /// Simula a seguradora mudando o status. So aceita os caminhos de TransicoesSinistro;
        /// caminho invalido lanca BusinessException e protocolo inexistente lanca NotFoundException.
        /// </summary>
        public SinistroSeguradoraResultado AvancarStatus(string protocolo, StatusSinistro novoStatus, string? observacao)
        {
            lock (_transicoes)
            {
                SinistroArmazenado? armazenado;

                if (!_sinistros.TryGetValue(protocolo, out armazenado))
                {
                    throw new NotFoundException(ConstantsMessage.ErroSeguradoraSinistroNaoEncontrado);
                }

                if (!TransicoesSinistro.Permitida(armazenado.Status, novoStatus))
                {
                    throw new BusinessException(string.Format(ConstantsMessage.ErroSeguradoraTransicaoInvalida, armazenado.Status, novoStatus));
                }

                EventoArmazenado evento = new EventoArmazenado(armazenado.Status, novoStatus, DateTime.Now, observacao);
                List<EventoArmazenado> eventos = new List<EventoArmazenado>(armazenado.Eventos) { evento };
                SinistroArmazenado atualizado = armazenado with { Status = novoStatus, Eventos = eventos };
                _sinistros[protocolo] = atualizado;

                return ParaResultado(atualizado);
            }
        }

        private static SinistroArmazenado CriarAberto(string protocolo, string referenciaExterna, string provedor)
        {
            EventoArmazenado abertura = new EventoArmazenado(null, StatusSinistro.ABERTO, DateTime.Now, null);
            List<EventoArmazenado> eventos = new List<EventoArmazenado> { abertura };

            return new SinistroArmazenado(protocolo, referenciaExterna, StatusSinistro.ABERTO, eventos, provedor);
        }

        /// <summary>Copia nova para quem chama: alterar o resultado nao altera o que esta guardado.</summary>
        private static SinistroSeguradoraResultado ParaResultado(SinistroArmazenado armazenado)
        {
            List<SinistroEventoSeguradora> eventos = armazenado.Eventos
                .Select(x => new SinistroEventoSeguradora
                {
                    StatusAnterior = x.StatusAnterior,
                    StatusNovo = x.StatusNovo,
                    DataAlteracao = x.DataAlteracao,
                    Observacao = x.Observacao
                })
                .ToList();

            return new SinistroSeguradoraResultado
            {
                Protocolo = armazenado.Protocolo,
                ReferenciaExterna = armazenado.ReferenciaExterna,
                Status = armazenado.Status,
                Eventos = eventos,
                Provedor = armazenado.Provedor
            };
        }

        /// <summary>Registro imutavel guardado no store. A lista de eventos nunca e alterada: cada transicao cria outra.</summary>
        private sealed record SinistroArmazenado(
            string Protocolo,
            string ReferenciaExterna,
            StatusSinistro Status,
            IReadOnlyList<EventoArmazenado> Eventos,
            string Provedor);

        private sealed record EventoArmazenado(
            StatusSinistro? StatusAnterior,
            StatusSinistro StatusNovo,
            DateTime DataAlteracao,
            string? Observacao);
    }
}
