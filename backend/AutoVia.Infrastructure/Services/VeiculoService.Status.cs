using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Core.Regras;
using AutoVia.Helpers;
using System;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Services
{
    /// <summary>
    /// Troca manual de status do veiculo (P1-BE-04, RF-04). Arquivo parcial do VeiculoService: converte o nome validado em
    /// StatusVeiculo (nunca numero), chama o repositorio (trava e regra dentro da transacao) e registra log informativo so
    /// quando o status mudou. O log leva ids e nomes de status, sem placa, chassi, renavam nem valores.
    /// </summary>
    public partial class VeiculoService
    {
        /// <summary>
        /// Troca o status do veiculo pelo nome pedido. Lanca NotFoundException (404) ou BusinessException (409); falha
        /// inesperada vai para o log e sobe. Mesmo status manual nao grava e nao registra log.
        /// </summary>
        public async Task AlterarStatusAsync(int idVeiculo, AlterarStatusVeiculoVO alterarStatusVeiculoVO)
        {
            try
            {
                if (!RegrasStatusVeiculo.TentarLerStatusPorNome(alterarStatusVeiculoVO.Status, out StatusVeiculo destino))
                {
                    throw new InvalidOperationException(ConstantsMessage.ErroAlterarStatusVeiculo);
                }

                (bool Alterou, StatusVeiculo StatusAnterior) resultado = await _veiculoRepository.AlterarStatusAtomicamenteAsync(idVeiculo, destino);

                if (resultado.Alterou)
                {
                    int idFuncionario = _contextoEmpresa.IdFuncionario;
                    string mensagem = string.Format(ConstantsMessage.InfoVeiculoStatusAlterado, idVeiculo, resultado.StatusAnterior, destino, idFuncionario);

                    _logService.WriteInfo(mensagem, GetType().ToString());
                }
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroAlterarStatusVeiculo, GetType().ToString());
                throw;
            }
        }
    }
}
