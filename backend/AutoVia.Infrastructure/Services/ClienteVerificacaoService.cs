using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Clientes;
using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Services
{
    /// <summary>
    /// Leituras da equipe que verifica o cadastro (P2-BE-05, RF-10). A locadora vem sempre do contexto do funcionario; a fila
    /// e o detalhe usam o vinculo ativo de menor id do par (a mesma linha que o servico de decisao usa). Cliente sem vinculo
    /// ativo com a locadora responde NotFoundException, igual ao inexistente, e nada e lido nem registrado. Os documentos so
    /// saem depois dessa conferencia, e cada acesso registra o id do funcionario, o id do cliente e a quantidade.
    /// </summary>
    public class ClienteVerificacaoService : IClienteVerificacaoService
    {
        private readonly IClienteLocadoraRepository _clienteLocadoraRepository;
        private readonly IClienteDocumentoService _clienteDocumentoService;
        private readonly IContextoEmpresa _contextoEmpresa;
        private readonly IMapper _mapper;
        private readonly ILogService _logService;

        public ClienteVerificacaoService(
            IClienteLocadoraRepository clienteLocadoraRepository,
            IClienteDocumentoService clienteDocumentoService,
            IContextoEmpresa contextoEmpresa,
            IMapper mapper,
            ILogService logService)
        {
            _clienteLocadoraRepository = clienteLocadoraRepository;
            _clienteDocumentoService = clienteDocumentoService;
            _contextoEmpresa = contextoEmpresa;
            _mapper = mapper;
            _logService = logService;
        }

        /// <summary>
        /// Fila paginada da locadora do contexto. O status filtra o status do vinculo (nunca o geral do cliente). A busca vem
        /// sem espacos nas bordas: vazia vira sem busca; o CPF so entra quando a busca tem digitos (os digitos do termo).
        /// </summary>
        public async Task<PaginaVO<ReturnClienteFilaVO>> ListarFilaAsync(FiltroClienteVerificacaoVO filtro)
        {
            try
            {
                StatusCadastroCliente? status = null;

                if (filtro.Status != null)
                {
                    status = Enum.Parse<StatusCadastroCliente>(filtro.Status);
                }

                string? termoBusca = string.IsNullOrWhiteSpace(filtro.Busca) ? null : filtro.Busca.Trim();
                string? digitosCpf = termoBusca == null ? null : StaticMethods.OnlyDigits(termoBusca);

                if (string.IsNullOrEmpty(digitosCpf))
                {
                    digitosCpf = null;
                }

                int total = await _clienteLocadoraRepository.CountFilaAsync(status, termoBusca, digitosCpf);
                List<ClienteLocadora> vinculos = await _clienteLocadoraRepository.FindFilaAsync(status, termoBusca, digitosCpf, filtro.Deslocamento, filtro.TamanhoPagina);
                List<ReturnClienteFilaVO> itens = _mapper.Map<List<ReturnClienteFilaVO>>(vinculos);

                return new PaginaVO<ReturnClienteFilaVO>(itens, total, filtro.Pagina, filtro.TamanhoPagina);
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarFilaVerificacao, GetType().ToString());
                throw;
            }
        }

        /// <summary>Detalhe do cadastro do cliente pela locadora do contexto. Sem vinculo ativo: 404 de cliente inexistente.</summary>
        public async Task<ReturnClienteVerificacaoVO> ObterVerificacaoAsync(int idCliente)
        {
            try
            {
                ClienteLocadora vinculo = await VinculoDoContextoOuRecusarAsync(idCliente);

                return _mapper.Map<ReturnClienteVerificacaoVO>(vinculo);
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroObterVerificacaoCliente, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Documentos ativos do cliente para a equipe da locadora do contexto. Confere o vinculo antes de qualquer leitura do
        /// armazenamento; registra uma linha informativa com o id do funcionario, o id do cliente e a quantidade.
        /// </summary>
        public async Task<List<ReturnClienteDocumentoVO>> ListarDocumentosAsync(int idCliente, CancellationToken cancellationToken = default)
        {
            try
            {
                int idFuncionario = _contextoEmpresa.IdFuncionario;

                await VinculoDoContextoOuRecusarAsync(idCliente);

                List<ReturnClienteDocumentoVO> documentos = await _clienteDocumentoService.ListarDaEquipeAsync(idCliente, cancellationToken);

                _logService.WriteInfo(
                    string.Format(CultureInfo.InvariantCulture, ConstantsMessage.InfoAcessoDocumentosVerificacao, idFuncionario, idCliente, documentos.Count),
                    GetType().ToString());

                return documentos;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarDocumentosVerificacao, GetType().ToString());
                throw;
            }
        }

        /// <summary>Linha que vale do par cliente x locadora do contexto; sem ela, 404 de cliente inexistente (nada mais e lido).</summary>
        private async Task<ClienteLocadora> VinculoDoContextoOuRecusarAsync(int idCliente)
        {
            int idEmpresa = _contextoEmpresa.IdEmpresa;

            ClienteLocadora? vinculo = await _clienteLocadoraRepository.FindVinculoComClienteAsync(idCliente, idEmpresa);

            if (vinculo == null)
            {
                throw new NotFoundException(ConstantsMessage.ErroClienteNaoEncontrada);
            }

            return vinculo;
        }
    }
}
