using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Clientes;
using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Options;
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
    /// Regras da aprovacao do cadastro do cliente por locadora (T-BE-06). Exigir antes da reserva: APROVADO segue; sem
    /// vinculo e chamado pelo proprio cliente, grava PENDENTE e responde 409; PENDENTE e RECUSADO respondem 409. Decidir
    /// (ATENDENTE ou ADMINISTRADOR) grava em todas as linhas ativas do par; APROVADO e final. Corrida sem indice unico:
    /// vale a linha ativa de menor id do par e quem nao for a menor se desativa logo apos o INSERT. Teto (v2): o cliente
    /// nao pede aprovacao em mais de N locadoras novas na janela de 24 horas (429, nada gravado).
    /// </summary>
    public class ClienteLocadoraService : IClienteLocadoraService
    {
        private readonly IClienteLocadoraRepository _clienteLocadoraRepository;
        private readonly IContextoEmpresa _contextoEmpresa;
        private readonly IRelogio _relogio;
        private readonly IMapper _mapper;
        private readonly ILogService _logService;
        private readonly ClientesOptions _clientesOptions;

        public ClienteLocadoraService(
            IClienteLocadoraRepository clienteLocadoraRepository,
            IContextoEmpresa contextoEmpresa,
            IRelogio relogio,
            IMapper mapper,
            ILogService logService,
            ClientesOptions clientesOptions)
        {
            _clienteLocadoraRepository = clienteLocadoraRepository;
            _contextoEmpresa = contextoEmpresa;
            _relogio = relogio;
            _mapper = mapper;
            _logService = logService;
            _clientesOptions = clientesOptions;
        }

        /// <summary>
        /// Exige o cadastro APROVADO do cliente na locadora. Ordem: locadora ativa (404); vinculo ativo do par (o teto NAO
        /// vale para quem ja tem vinculo); sem vinculo e nao sendo o proprio cliente, 409 sem contar; sem vinculo e proprio
        /// cliente, teto de pedidos na janela (429, nada gravado); depois cria o PENDENTE (gravado antes do 409). Status
        /// PENDENTE ou RECUSADO geram 409 com a mensagem da locadora; APROVADO segue sem excecao.
        /// </summary>
        public async Task ExigirCadastroAprovadoAsync(int idCliente, int idEmpresa)
        {
            try
            {
                Empresa? empresa = await _clienteLocadoraRepository.FindEmpresaAtivaAsync(idEmpresa);

                if (empresa == null)
                {
                    throw new NotFoundException(ConstantsMessage.ErroClienteLocadoraEmpresaNaoEncontrada);
                }

                ClienteLocadora? vinculo = await _clienteLocadoraRepository.FindAtivoMaisAntigoAsync(idCliente, idEmpresa);

                if (vinculo == null)
                {
                    bool proprioCliente = _contextoEmpresa.IdCliente > 0 && _contextoEmpresa.IdCliente == idCliente;

                    if (!proprioCliente)
                    {
                        throw new BusinessException(ConstantsMessage.ErroReservaCadastroNaoAprovado);
                    }

                    await ValidarTetoPedidosAsync(idCliente);

                    vinculo = await CriarPendenteAsync(idCliente, idEmpresa);
                }

                AvaliarStatusDaReserva(vinculo.Status, empresa.NomeFantasia);
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
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroExigirCadastroAprovado, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Registra a decisao da locadora do contexto. Ordem: contexto do funcionario; cliente sem vinculo na locadora (404 com
        /// a mesma mensagem de inexistente, sem alterar nada); APROVADO ja registrado (409); grava em todas as linhas ativas do
        /// par com o verificador do contexto e a data do relogio. Aprovacao limpa a justificativa; recusa a guarda.
        /// </summary>
        public async Task DecidirAsync(int idCliente, VerificacaoCadastroVO verificacaoVO)
        {
            try
            {
                int idEmpresa = _contextoEmpresa.IdEmpresa;
                int idFuncionario = _contextoEmpresa.IdFuncionario;

                if (idEmpresa <= 0 || idFuncionario <= 0)
                {
                    throw new InvalidOperationException(ConstantsMessage.ErroDefinirContextoEmpresa);
                }

                StatusCadastroCliente? decisaoInformada = verificacaoVO.Decisao;

                if (!decisaoInformada.HasValue || decisaoInformada.Value == StatusCadastroCliente.PENDENTE)
                {
                    throw new InvalidOperationException(ConstantsMessage.ErroDecidirCadastroCliente);
                }

                StatusCadastroCliente decisao = decisaoInformada.Value;
                string? justificativaInformada = verificacaoVO.Justificativa?.Trim();

                // Recusa exige justificativa com texto. Barrado aqui tambem (nao so no validador HTTP): 400 tipado.
                if (decisao == StatusCadastroCliente.RECUSADO && string.IsNullOrWhiteSpace(justificativaInformada))
                {
                    throw new ValidationException(new List<ValidationFailure>
                    {
                        new ValidationFailure(nameof(VerificacaoCadastroVO.Justificativa), ConstantsMessage.ErroVerificacaoJustificativaObrigatoria)
                    });
                }

                // Cliente desativado (ou inexistente) nao e decidido: mesma 404 de inexistente, nada gravado.
                bool clienteAtivo = await _clienteLocadoraRepository.ClienteAtivoAsync(idCliente);

                if (!clienteAtivo)
                {
                    throw new NotFoundException(ConstantsMessage.ErroClienteNaoEncontrada);
                }

                ClienteLocadora? atual = await _clienteLocadoraRepository.FindAtivoMaisAntigoAsync(idCliente, idEmpresa);

                if (atual == null)
                {
                    throw new NotFoundException(ConstantsMessage.ErroClienteNaoEncontrada);
                }

                if (atual.Status == StatusCadastroCliente.APROVADO)
                {
                    throw new BusinessException(ConstantsMessage.ErroVerificacaoJaAprovado);
                }

                string? justificativa = null;

                if (decisao == StatusCadastroCliente.RECUSADO)
                {
                    justificativa = justificativaInformada;
                }

                bool gravado = await _clienteLocadoraRepository.DecidirParAsync(idCliente, idEmpresa, decisao, justificativa, idFuncionario, _relogio.Agora);

                if (!gravado)
                {
                    throw new NotFoundException(ConstantsMessage.ErroClienteNaoEncontrada);
                }
            }
            catch (ValidationException)
            {
                throw;
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
                _logService.Write(ex, ConstantsMessage.ErroDecidirCadastroCliente, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Lista as locadoras do cliente do contexto (um vinculo por locadora, o de menor id). Sem cliente no contexto a
        /// pagina volta vazia, sem consultar. A justificativa da recusa nao sai no VO.
        /// </summary>
        public async Task<PaginaVO<ReturnClienteLocadoraVO>> GetMinhasAsync(FiltroPaginacaoVO filtro)
        {
            try
            {
                int idCliente = _contextoEmpresa.IdCliente;

                if (idCliente <= 0)
                {
                    return new PaginaVO<ReturnClienteLocadoraVO>(new List<ReturnClienteLocadoraVO>(), 0, filtro.Pagina, filtro.TamanhoPagina);
                }

                int total = await _clienteLocadoraRepository.CountMinhasAsync(idCliente);
                List<ClienteLocadora> vinculos = await _clienteLocadoraRepository.FindMinhasAsync(idCliente, filtro.Deslocamento, filtro.TamanhoPagina);
                List<ReturnClienteLocadoraVO> itens = _mapper.Map<List<ReturnClienteLocadoraVO>>(vinculos);

                return new PaginaVO<ReturnClienteLocadoraVO>(itens, total, filtro.Pagina, filtro.TamanhoPagina);
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
                _logService.Write(ex, ConstantsMessage.ErroListarMinhasLocadoras, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Teto de pedidos de aprovacao (v2): conta as locadoras distintas com pedido do cliente na janela de 24 horas (pelo
        /// IRelogio), ativas ou desativadas. Chegando ao limite, lanca 429 antes de qualquer gravacao.
        /// </summary>
        private async Task ValidarTetoPedidosAsync(int idCliente)
        {
            DateTime desde = _relogio.Agora.AddHours(-ConstantsValues.JanelaPedidosAprovacaoHoras);
            int locadorasPedidas = await _clienteLocadoraRepository.CountEmpresasSolicitadasDesdeAsync(idCliente, desde);

            if (locadorasPedidas >= _clientesOptions.LimitePedidosAprovacaoPorDia)
            {
                throw new LimiteExcedidoException(ConstantsMessage.ErroLimitePedidosAprovacao);
            }
        }

        /// <summary>
        /// Cria o pedido PENDENTE do proprio cliente e aplica a regra da corrida: se a linha gravada nao for a de menor id ativa
        /// do par, ela se desativa e vale a linha mais antiga. A gravacao ja foi confirmada no repositorio antes do 409.
        /// </summary>
        private async Task<ClienteLocadora> CriarPendenteAsync(int idCliente, int idEmpresa)
        {
            ClienteLocadora novo = new ClienteLocadora
            {
                IdCliente = idCliente,
                IdEmpresa = idEmpresa,
                Status = StatusCadastroCliente.PENDENTE,
                DataSolicitacao = _relogio.Agora
            };

            ClienteLocadora gravado = await _clienteLocadoraRepository.InsertAsync(novo);
            ClienteLocadora? maisAntigo = await _clienteLocadoraRepository.FindAtivoMaisAntigoAsync(idCliente, idEmpresa);

            if (maisAntigo == null)
            {
                throw new InvalidOperationException(ConstantsMessage.ErroSalvarVinculoClienteLocadora);
            }

            if (maisAntigo.IdClienteLocadora != gravado.IdClienteLocadora)
            {
                await _clienteLocadoraRepository.DesativarAsync(gravado.IdClienteLocadora);
            }

            return maisAntigo;
        }

        /// <summary>Decide a reserva pelo status da linha que vale: APROVADO segue; RECUSADO e PENDENTE respondem 409 com o nome da locadora.</summary>
        private static void AvaliarStatusDaReserva(StatusCadastroCliente status, string nomeFantasia)
        {
            if (status == StatusCadastroCliente.APROVADO)
            {
                return;
            }

            if (status == StatusCadastroCliente.RECUSADO)
            {
                throw new BusinessException(string.Format(ConstantsMessage.ErroReservaCadastroRecusado, nomeFantasia));
            }

            throw new BusinessException(string.Format(ConstantsMessage.ErroReservaCadastroEmAnalise, nomeFantasia));
        }
    }
}
