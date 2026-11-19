using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Manutencao;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.Regras;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using FluentValidation;
using FluentValidation.Results;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Services
{
    /// <summary>
    /// Regras de negocio da manutencao (P8-BE-01, P8-BE-02 e P8-BE-03). Confere, com o relogio do servidor, a data de
    /// entrada e a de saida (nao podem ser futuras), converte o tipo pelo nome, normaliza os textos e deixa a transacao, a
    /// trava e as recusas de data e de km para o repositorio. Calcula os alertas de revisao pela RegrasRevisao. Registra no
    /// log uma linha informativa por ordem aberta e por ordem encerrada (ids e funcionario, sem placa, chassi ou renavam).
    /// </summary>
    public class ManutencaoService : IManutencaoService
    {
        private readonly IManutencaoRepository _manutencaoRepository;
        private readonly IMapper _mapper;
        private readonly IRelogio _relogio;
        private readonly IContextoEmpresa _contextoEmpresa;
        private readonly ILogService _logService;

        public ManutencaoService(IManutencaoRepository manutencaoRepository, IMapper mapper, IRelogio relogio, IContextoEmpresa contextoEmpresa, ILogService logService)
        {
            _manutencaoRepository = manutencaoRepository;
            _mapper = mapper;
            _relogio = relogio;
            _contextoEmpresa = contextoEmpresa;
            _logService = logService;
        }

        /// <summary>
        /// Abre a ordem: data de entrada ausente vale hoje e data futura e recusada (400); o tipo vai para o enum pelo nome; o
        /// repositorio faz a trava, a regra de troca para MANUTENCAO e a recusa de ordem aberta (404 e 409).
        /// </summary>
        public async Task<ReturnManutencaoVO> AbrirAsync(AbrirManutencaoVO abrirManutencaoVO)
        {
            try
            {
                DateOnly hoje = DateOnly.FromDateTime(_relogio.Agora);
                DateOnly dataEntrada = abrirManutencaoVO.DataEntrada ?? hoje;

                if (dataEntrada > hoje)
                {
                    throw CriarRecusaDeCampo(nameof(AbrirManutencaoVO.DataEntrada), ConstantsMessage.ErroManutencaoDataEntradaFutura);
                }

                if (!RegrasManutencao.TentarLerTipoPorNome(abrirManutencaoVO.Tipo, out TipoManutencao tipo))
                {
                    throw CriarRecusaDeCampo(nameof(AbrirManutencaoVO.Tipo), ConstantsMessage.ErroManutencaoTipoInvalido);
                }

                Manutencao manutencao = new Manutencao
                {
                    IdVeiculo = abrirManutencaoVO.IdVeiculo,
                    Tipo = tipo,
                    DataEntrada = dataEntrada,
                    Oficina = NormalizarTextoOpcional(abrirManutencaoVO.Oficina),
                    Descricao = NormalizarTextoOpcional(abrirManutencaoVO.Descricao)
                };

                Manutencao gravada = await _manutencaoRepository.AbrirAtomicamenteAsync(manutencao);
                ReturnManutencaoVO retorno = _mapper.Map<ReturnManutencaoVO>(gravada);

                string mensagem = string.Format(ConstantsMessage.InfoManutencaoAberta, gravada.IdManutencao, gravada.IdVeiculo, _contextoEmpresa.IdFuncionario);
                _logService.WriteInfo(mensagem, GetType().ToString());

                return retorno;
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroAbrirManutencao, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Encerra a ordem: quilometragem obrigatoria; data de saida ausente vale hoje e data futura e recusada (400). A
        /// comparacao com a entrada, o km do veiculo e a ordem ja encerrada ficam no repositorio, na transacao.
        /// </summary>
        public async Task<ReturnManutencaoVO> EncerrarAsync(int idManutencao, EncerrarManutencaoVO encerrarManutencaoVO)
        {
            try
            {
                if (!encerrarManutencaoVO.KmAtual.HasValue)
                {
                    throw CriarRecusaDeCampo(nameof(EncerrarManutencaoVO.KmAtual), ConstantsMessage.ErroManutencaoKmObrigatorio);
                }

                DateOnly hoje = DateOnly.FromDateTime(_relogio.Agora);
                DateOnly dataSaida = encerrarManutencaoVO.DataSaida ?? hoje;

                if (dataSaida > hoje)
                {
                    throw CriarRecusaDeCampo(nameof(EncerrarManutencaoVO.DataSaida), ConstantsMessage.ErroManutencaoDataSaidaFutura);
                }

                Manutencao encerrada = await _manutencaoRepository.EncerrarAtomicamenteAsync(idManutencao, dataSaida, encerrarManutencaoVO.KmAtual.Value, encerrarManutencaoVO.Custo);
                ReturnManutencaoVO retorno = _mapper.Map<ReturnManutencaoVO>(encerrada);

                string mensagem = string.Format(ConstantsMessage.InfoManutencaoEncerrada, encerrada.IdManutencao, encerrada.IdVeiculo, _contextoEmpresa.IdFuncionario);
                _logService.WriteInfo(mensagem, GetType().ToString());

                return retorno;
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroEncerrarManutencao, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Lista os alertas de revisao: o repositorio devolve os veiculos em VENCIDA ou PROXIMA e o total; aqui cada item
        /// recebe a base, a proxima revisao, quanto falta, o percentual do ciclo e a situacao (RegrasRevisao).
        /// </summary>
        public async Task<ReturnAlertasRevisaoVO> ListarAlertasRevisaoAsync()
        {
            try
            {
                (List<VeiculoRevisaoRegistro> Itens, int Total) consulta = await _manutencaoRepository.ListarAlertasRevisaoAsync();

                ReturnAlertasRevisaoVO retorno = new ReturnAlertasRevisaoVO { Total = consulta.Total };

                foreach (VeiculoRevisaoRegistro registro in consulta.Itens)
                {
                    retorno.Itens.Add(MontarAlerta(registro));
                }

                return retorno;
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
                _logService.Write(ex, ConstantsMessage.ErroListarAlertasRevisao, GetType().ToString());
                throw;
            }
        }

        /// <summary>Monta um item do alerta com a regra pura de revisao. Funcao pura, sem acesso a dados.</summary>
        private static ReturnAlertaRevisaoVO MontarAlerta(VeiculoRevisaoRegistro registro)
        {
            int kmBase = RegrasRevisao.KmBase(registro.KmUltimaPreventiva);
            int kmProximaRevisao = RegrasRevisao.KmProximaRevisao(kmBase);
            int faltamKm = RegrasRevisao.FaltamKm(kmProximaRevisao, registro.KmAtual);

            return new ReturnAlertaRevisaoVO
            {
                IdVeiculo = registro.IdVeiculo,
                Placa = registro.Placa,
                Marca = registro.Marca,
                Modelo = registro.Modelo,
                CategoriaNome = registro.CategoriaNome,
                KmAtual = registro.KmAtual,
                KmUltimaPreventiva = kmBase,
                KmProximaRevisao = kmProximaRevisao,
                FaltamKm = faltamKm,
                PercentualCiclo = RegrasRevisao.PercentualCiclo(registro.KmAtual, kmBase),
                Situacao = RegrasRevisao.Situacao(faltamKm)
            };
        }

        /// <summary>Remove os espacos das pontas; nulo ou vazio vira nulo. Funcao pura, sem acesso a dados.</summary>
        private static string? NormalizarTextoOpcional(string? texto)
        {
            if (texto == null)
            {
                return null;
            }

            string textoSemEspacos = texto.Trim();

            if (textoSemEspacos.Length == 0)
            {
                return null;
            }

            return textoSemEspacos;
        }

        /// <summary>Monta a excecao de validacao de um campo (400), para regras que dependem do relogio ou da ordem.</summary>
        private static ValidationException CriarRecusaDeCampo(string campo, string mensagem)
        {
            List<ValidationFailure> falhas = new List<ValidationFailure> { new ValidationFailure(campo, mensagem) };

            return new ValidationException(mensagem, falhas);
        }
    }
}
