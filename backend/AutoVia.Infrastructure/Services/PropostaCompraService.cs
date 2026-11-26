using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Vendas;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Regras;
using AutoVia.Core.RepositoriesInterface;
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
    /// Regras da proposta de compra (P6-BE-02, RF-27). O valor de venda vem sempre do banco (veiculo do catalogo publico) e
    /// o parcelamento sai de RegrasPropostaCompra, o mesmo calculo na simulacao e no envio. Ordem do POST: veiculo a venda (404),
    /// entrada menor que o valor e parcela minima (400), cadastro APROVADO na locadora do veiculo (404, 409, 429, como na
    /// reserva) e so entao a gravacao.
    /// </summary>
    public class PropostaCompraService : IPropostaCompraService
    {
        private readonly IVeiculoRepository _veiculoRepository;
        private readonly IPropostaCompraRepository _propostaCompraRepository;
        private readonly IClienteLocadoraService _clienteLocadoraService;
        private readonly IMapper _mapper;
        private readonly ILogService _logService;

        public PropostaCompraService(IVeiculoRepository veiculoRepository, IPropostaCompraRepository propostaCompraRepository, IClienteLocadoraService clienteLocadoraService, IMapper mapper, ILogService logService)
        {
            _veiculoRepository = veiculoRepository;
            _propostaCompraRepository = propostaCompraRepository;
            _clienteLocadoraService = clienteLocadoraService;
            _mapper = mapper;
            _logService = logService;
        }

        public async Task<ReturnSimulacaoParcelamentoVO> SimularAsync(int idVeiculo, SimulacaoParcelamentoVO simulacao)
        {
            try
            {
                Veiculo veiculo = await BuscarVeiculoAVendaAsync(idVeiculo);
                ParcelamentoCalculado parcelamento = CalcularValidando(veiculo, simulacao.ValorEntrada, simulacao.NumeroParcelas);

                ReturnSimulacaoParcelamentoVO retorno = _mapper.Map<ReturnSimulacaoParcelamentoVO>(parcelamento);
                retorno.IdVeiculo = veiculo.IdVeiculo;
                return retorno;
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
                _logService.Write(ex, ConstantsMessage.ErroSimularParcelamento, GetType().ToString());
                throw;
            }
        }

        public async Task<ReturnPropostaCompraVO> CriarAsync(int idCliente, PropostaCompraVO propostaVO)
        {
            try
            {
                Veiculo veiculo = await BuscarVeiculoAVendaAsync(propostaVO.IdVeiculo);
                ParcelamentoCalculado parcelamento = CalcularValidando(veiculo, propostaVO.ValorEntrada, propostaVO.NumeroParcelas);

                try
                {
                    await _clienteLocadoraService.ExigirCadastroAprovadoAsync(idCliente, veiculo.IdEmpresa, ConstantsMessage.ErroPropostaCadastroEmAnalise);
                }
                catch (NotFoundException)
                {
                    throw new NotFoundException(ConstantsMessage.ErroPropostaVeiculoNaoDisponivel);
                }

                PropostaCompra proposta = new PropostaCompra
                {
                    IdVeiculo = veiculo.IdVeiculo,
                    IdCliente = idCliente,
                    ValorVenda = parcelamento.ValorVenda,
                    ValorEntrada = parcelamento.ValorEntrada,
                    NumeroParcelas = parcelamento.NumeroParcelas,
                    ValorParcela = parcelamento.ValorParcela,
                    ValorUltimaParcela = parcelamento.ValorUltimaParcela,
                    Status = StatusPropostaCompra.ENVIADA
                };

                PropostaCompra criada = await _propostaCompraRepository.InsertAsync(proposta);

                // O veiculo veio da leitura sem rastreamento: entra so para montar o retorno, depois da gravacao.
                criada.Veiculo = veiculo;

                ReturnPropostaCompraVO retorno = _mapper.Map<ReturnPropostaCompraVO>(criada);
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
            catch (LimiteExcedidoException)
            {
                throw;
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroRegistrarProposta, GetType().ToString());
                throw;
            }
        }

        public async Task<ReturnPropostaCompraVO> GetByIdAsync(int idPropostaCompra, int idCliente)
        {
            try
            {
                PropostaCompra? proposta = await _propostaCompraRepository.FindDoClienteByIdAsync(idPropostaCompra, idCliente);

                if (proposta == null)
                {
                    throw new NotFoundException(ConstantsMessage.ErroPropostaNaoEncontrada);
                }

                ReturnPropostaCompraVO retorno = _mapper.Map<ReturnPropostaCompraVO>(proposta);
                return retorno;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarProposta, GetType().ToString());
                throw;
            }
        }

        /// <summary>Veiculo do catalogo publico (ativo, DISPONIVEL, com valor de venda, locadora ativa); 404 quando nao esta a venda.</summary>
        private async Task<Veiculo> BuscarVeiculoAVendaAsync(int idVeiculo)
        {
            try
            {
                Veiculo? veiculo = await _veiculoRepository.FindAVendaByIdAsync(idVeiculo);

                if (veiculo == null || !veiculo.ValorVenda.HasValue)
                {
                    throw new NotFoundException(ConstantsMessage.ErroPropostaVeiculoNaoDisponivel);
                }

                return veiculo;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroPropostaBuscarVeiculo, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Calcula o parcelamento com o valor de venda do banco. Entrada maior ou igual ao valor (campo valorEntrada) e parcela
        /// abaixo de um centavo (campo numeroParcelas) viram 400 com o nome do campo, como as regras do validador.
        /// </summary>
        private ParcelamentoCalculado CalcularValidando(Veiculo veiculo, decimal valorEntrada, int numeroParcelas)
        {
            try
            {
                decimal valorVenda = veiculo.ValorVenda ?? 0m;

                if (!RegrasPropostaCompra.EntradaMenorQueValor(valorVenda, valorEntrada))
                {
                    throw new ValidationException(new List<ValidationFailure>
                    {
                        new ValidationFailure(nameof(PropostaCompraVO.ValorEntrada), ConstantsMessage.ErroPropostaEntradaMaiorQueValor)
                    });
                }

                ParcelamentoCalculado parcelamento = RegrasPropostaCompra.CalcularParcelamento(valorVenda, valorEntrada, numeroParcelas);

                if (!RegrasPropostaCompra.ParcelaAtingeOMinimo(parcelamento))
                {
                    throw new ValidationException(new List<ValidationFailure>
                    {
                        new ValidationFailure(nameof(PropostaCompraVO.NumeroParcelas), ConstantsMessage.ErroPropostaParcelaMinima)
                    });
                }

                return parcelamento;
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSimularParcelamento, GetType().ToString());
                throw;
            }
        }
    }
}
