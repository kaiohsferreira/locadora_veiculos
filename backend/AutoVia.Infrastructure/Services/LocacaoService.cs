using AutoMapper;
using AutoVia.Communication.Enums;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Locacoes;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Regras;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Services
{
    /// <summary>Regras de negocio para iniciar locacao a partir de reserva.</summary>
    public class LocacaoService : ILocacaoService
    {
        private readonly ILocacaoRepository _locacaoRepository;
        private readonly IRelogio _relogio;
        private readonly IMapper _mapper;
        private readonly ILogService _logService;
        private readonly IContextoEmpresa _contextoEmpresa;
        private readonly IFotoCapaVeiculoService _fotoCapaVeiculoService;
        private readonly IClienteDocumentoRepository _clienteDocumentoRepository;
        private readonly ICaucaoRetiradaService _caucaoRetiradaService;

        public LocacaoService(ILocacaoRepository locacaoRepository, IRelogio relogio, IMapper mapper, ILogService logService, IContextoEmpresa contextoEmpresa, IFotoCapaVeiculoService fotoCapaVeiculoService, IClienteDocumentoRepository clienteDocumentoRepository, ICaucaoRetiradaService caucaoRetiradaService)
        {
            _locacaoRepository = locacaoRepository;
            _relogio = relogio;
            _mapper = mapper;
            _logService = logService;
            _contextoEmpresa = contextoEmpresa;
            _fotoCapaVeiculoService = fotoCapaVeiculoService;
            _clienteDocumentoRepository = clienteDocumentoRepository;
            _caucaoRetiradaService = caucaoRetiradaService;
        }

        /// <summary>
        /// Inicia a locacao de uma reserva valida para um funcionario ativo. Sem caucao exigida, o fluxo do RF-17. Com caucao
        /// exigida (contrato caucao-retirada v1.1): todas as recusas de hoje primeiro, depois a pre-autorizacao FORA da transacao
        /// e das travas; a retirada e lida depois da confirmacao; a locacao e a linha CAUCAO nascem no mesmo SaveChanges. Se a
        /// gravacao falhar, compensa so o id novo (nunca o que outra abertura ja gravou).
        /// </summary>
        public async Task<ReturnLocacaoVO> StartFromReservaAsync(LocacaoVO locacaoVO, int idFuncionario)
        {
            try
            {
                Reserva? reserva = await _locacaoRepository.FindReservaByIdAsync(locacaoVO.IdReserva);

                if (reserva == null)
                {
                    throw new NotFoundException(ConstantsMessage.ErroReservaNaoEncontrada);
                }

                if (!RegrasLocacao.ReservaPermiteAbrirLocacao(reserva.Status))
                {
                    throw new BusinessException(ConstantsMessage.ErroLocacaoReservaEstadoInvalido);
                }

                if (reserva.Veiculo == null || reserva.Veiculo.DisabledAt != null || !RegrasLocacao.VeiculoPermiteAbrirLocacao(reserva.Veiculo.Status))
                {
                    throw new BusinessException(ConstantsMessage.ErroLocacaoVeiculoIndisponivel);
                }

                bool existeLocacao = await _locacaoRepository.ExistsActiveByReservaAsync(locacaoVO.IdReserva);

                if (existeLocacao)
                {
                    throw new BusinessException(ConstantsMessage.ErroLocacaoReservaDuplicada);
                }

                Funcionario? funcionario = await _locacaoRepository.FindActiveFuncionarioByIdAsync(idFuncionario);

                if (funcionario == null)
                {
                    throw new BusinessException(ConstantsMessage.ErroLocacaoFuncionarioNaoEncontrado);
                }

                CaucaoAberturaRegistro? caucao = null;

                if (_caucaoRetiradaService.CaucaoExigida)
                {
                    caucao = await _caucaoRetiradaService.PreAutorizarAsync(reserva, locacaoVO);
                }

                // Lida depois da confirmacao da caucao: e o instante em que a locacao pode nascer.
                DateTime dataRetirada = _relogio.Agora;

                Locacao criada = await AbrirLocacaoAsync(locacaoVO.IdReserva, idFuncionario, dataRetirada, caucao);

                if (caucao != null)
                {
                    string registro = string.Format(CultureInfo.InvariantCulture, ConstantsMessage.InfoCaucaoRetiradaGravada, locacaoVO.IdReserva, criada.IdLocacao, caucao.ReferenciaExterna, caucao.IdTransacao);
                    _logService.WriteInfo(registro, GetType().ToString());
                }

                ReturnLocacaoVO retorno = _mapper.Map<ReturnLocacaoVO>(criada);

                return retorno;
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (BusinessException ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSalvarLocacao, GetType().ToString());
                throw;
            }
            catch (NotFoundException ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSalvarLocacao, GetType().ToString());
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSalvarLocacao, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Chama a abertura do repositorio. Sem caucao, exatamente como no RF-17. Com caucao, se a gravacao falhar (erro do banco,
        /// reconferencia 404/409 depois da trava, perdedor de corrida), compensa: o mesmo id ja gravado responde 409 de locacao
        /// duplicada sem cancelar; senao o id novo e cancelado no provedor e a excecao ORIGINAL sobe.
        /// </summary>
        private async Task<Locacao> AbrirLocacaoAsync(int idReserva, int idFuncionario, DateTime dataRetirada, CaucaoAberturaRegistro? caucao)
        {
            if (caucao == null)
            {
                Locacao semCaucao = await _locacaoRepository.StartFromReservaAsync(idReserva, idFuncionario, dataRetirada, null);

                return semCaucao;
            }

            try
            {
                Locacao comCaucao = await _locacaoRepository.StartFromReservaAsync(idReserva, idFuncionario, dataRetirada, caucao);

                return comCaucao;
            }
            catch (Exception)
            {
                bool jaGravada = await _caucaoRetiradaService.CompensarAsync(idReserva, caucao);

                if (jaGravada)
                {
                    throw new BusinessException(ConstantsMessage.ErroLocacaoReservaDuplicada);
                }

                throw;
            }
        }

        /// <summary>
        /// Prepara a retirada com cadastro local e CNH conforme cargo conferido no servidor. Por ultimo, depois das conferencias
        /// do 404 (reserva de outra locadora nunca chega ao gateway), os dados da caucao: o gateway so e consultado com a caucao
        /// exigida e sem locacao ja aberta, e a falha dele nao derruba a leitura.
        /// </summary>
        public async Task<ReturnRetiradaVO> GetRetiradaAsync(int idReserva)
        {
            try
            {
                Reserva? reserva = await _locacaoRepository.FindReservaParaRetiradaAsync(idReserva);
                if (reserva == null || reserva.DisabledAt != null || reserva.Cliente == null ||
                    reserva.Cliente.DisabledAt != null || reserva.Veiculo == null || reserva.Veiculo.DisabledAt != null ||
                    _contextoEmpresa.IdEmpresa <= 0 || reserva.Veiculo.IdEmpresa != _contextoEmpresa.IdEmpresa)
                {
                    throw new NotFoundException(ConstantsMessage.ErroReservaNaoEncontrada);
                }
                ReturnRetiradaVO retorno = _mapper.Map<ReturnRetiradaVO>(reserva);
                CargoFuncionario? cargo = _contextoEmpresa.Cargo;
                if (cargo != CargoFuncionario.ATENDENTE && cargo != CargoFuncionario.ADMINISTRADOR)
                {
                    retorno.CnhNumero = StaticMethods.MascararCnh(retorno.CnhNumero);
                }
                retorno.CnhVencida = RegrasLocacao.CnhVencida(reserva.Cliente.CnhValidade, DateOnly.FromDateTime(_relogio.Agora));
                await PreencherDocumentosEnviadosAsync(reserva.IdCliente, retorno);
                await _fotoCapaVeiculoService.PreencherAsync(new List<Veiculo> { reserva.Veiculo }, new List<ReturnRetiradaVO> { retorno });
                await _caucaoRetiradaService.PreencherRetiradaAsync(retorno);
                return retorno;
            }
            catch (BusinessException ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarLocacao, GetType().ToString());
                throw;
            }
            catch (NotFoundException ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarLocacao, GetType().ToString());
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarLocacao, GetType().ToString());
                throw;
            }
        }

        /// <summary>Preenche a data do envio da CNH e do comprovante de residencia (so a data; nada de link nem de arquivo).</summary>
        private async Task PreencherDocumentosEnviadosAsync(int idCliente, ReturnRetiradaVO retorno)
        {
            List<ClienteDocumento> ativos = await _clienteDocumentoRepository.FindAtivosDoClienteAsync(idCliente);
            ClienteDocumento? cnh = RegrasClienteDocumento.EscolherAtivoPorTipo(ativos, TipoDocumentoCliente.CNH);
            ClienteDocumento? comprovante = RegrasClienteDocumento.EscolherAtivoPorTipo(ativos, TipoDocumentoCliente.COMPROVANTE_RESIDENCIA);

            retorno.CnhEnviadaEm = cnh?.CreatedAt;
            retorno.CnhEnviadaEmStr = cnh == null ? string.Empty : StaticMethods.FormateDateTimeToPTBR(cnh.CreatedAt);
            retorno.ComprovanteResidenciaEnviadoEm = comprovante?.CreatedAt;
            retorno.ComprovanteResidenciaEnviadoEmStr = comprovante == null ? string.Empty : StaticMethods.FormateDateTimeToPTBR(comprovante.CreatedAt);
        }

        /// <summary>Consulta uma locacao ativa; registros desativados contam como inexistentes.</summary>
        public async Task<ReturnLocacaoVO> GetByIdAsync(int idLocacao)
        {
            try
            {
                Locacao? locacao = await _locacaoRepository.FindByIdAsync(idLocacao);

                if (locacao == null)
                {
                    throw new NotFoundException(ConstantsMessage.ErroLocacaoNaoEncontrada);
                }

                ReturnLocacaoVO retorno = _mapper.Map<ReturnLocacaoVO>(locacao);

                return retorno;
            }
            catch (BusinessException ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarLocacao, GetType().ToString());
                throw;
            }
            catch (NotFoundException ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarLocacao, GetType().ToString());
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarLocacao, GetType().ToString());
                throw;
            }
        }
    }
}
