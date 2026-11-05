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
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Services
{
    /// <summary>
    /// Regras de negocio do cliente (RF-07): normaliza a entrada, impede CPF e CNH
    /// repetidos entre os ativos (409), grava o cadastro PENDENTE com o hash da senha
    /// e devolve so o VO de retorno, sem senha. A senha nunca vai para o log nem para
    /// a excecao. O VO de entrada chega validado pelo ClienteVOValidator antes.
    /// </summary>
    public class ClienteService : IClienteService
    {
        private readonly IClienteRepository _clienteRepository;
        private readonly ISenhaHasher _senhaHasher;
        private readonly IMapper _mapper;
        private readonly ILogService _logService;

        public ClienteService(IClienteRepository clienteRepository, ISenhaHasher senhaHasher, IMapper mapper, ILogService logService)
        {
            _clienteRepository = clienteRepository;
            _senhaHasher = senhaHasher;
            _mapper = mapper;
            _logService = logService;
        }

        /// <summary>Lista os clientes ativos, paginados, com filtro opcional por status, ordenados por data de cadastro.</summary>
        public async Task<PaginaVO<ReturnClienteVO>> GetAllAsync(ClienteFiltroVO filtro)
        {
            try
            {
                int total = await _clienteRepository.CountAsync(filtro.Status);
                List<Cliente> clientes = await _clienteRepository.FindAllAsync(filtro.Status, filtro.Deslocamento, filtro.TamanhoPagina);
                List<ReturnClienteVO> itens = _mapper.Map<List<ReturnClienteVO>>(clientes);
                PaginaVO<ReturnClienteVO> pagina = new PaginaVO<ReturnClienteVO>(itens, total, filtro.Pagina, filtro.TamanhoPagina);

                return pagina;
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
                _logService.Write(ex, ConstantsMessage.ErroListarClientes, GetType().ToString());
                throw;
            }
        }

        /// <summary>Consulta um cliente ativo pelo id; lanca NotFoundException se nao existir ou estiver desativado.</summary>
        public async Task<ReturnClienteVO> GetByIdAsync(int idCliente)
        {
            try
            {
                Cliente? cliente = await _clienteRepository.FindByIdAsync(idCliente);

                if (cliente == null)
                {
                    throw new NotFoundException(ConstantsMessage.ErroClienteNaoEncontrada);
                }

                ReturnClienteVO retorno = _mapper.Map<ReturnClienteVO>(cliente);

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
                _logService.Write(ex, ConstantsMessage.ErroBuscarCliente, GetType().ToString());
                throw;
            }
        }

        /// <summary>Cadastra um cliente PENDENTE; lanca BusinessException se o CPF ou a CNH ja existem entre os ativos.</summary>
        public async Task<ReturnClienteVO> InsertAsync(ClienteVO clienteVO)
        {
            try
            {
                // 1. Normalizacao da entrada (mesma forma que fica gravada).
                clienteVO.Nome = clienteVO.Nome.Trim();
                clienteVO.Cpf = StaticMethods.OnlyDigits(clienteVO.Cpf);
                clienteVO.Telefone = StaticMethods.OnlyDigits(clienteVO.Telefone);
                clienteVO.CnhNumero = StaticMethods.OnlyDigits(clienteVO.CnhNumero);
                clienteVO.Email = clienteVO.Email.Trim().ToLowerInvariant();
                clienteVO.Logradouro = string.IsNullOrWhiteSpace(clienteVO.Logradouro) ? null : clienteVO.Logradouro.Trim();
                clienteVO.Numero = string.IsNullOrWhiteSpace(clienteVO.Numero) ? null : clienteVO.Numero.Trim();
                clienteVO.Complemento = string.IsNullOrWhiteSpace(clienteVO.Complemento) ? null : clienteVO.Complemento.Trim();
                clienteVO.Bairro = string.IsNullOrWhiteSpace(clienteVO.Bairro) ? null : clienteVO.Bairro.Trim();
                clienteVO.Cidade = string.IsNullOrWhiteSpace(clienteVO.Cidade) ? null : clienteVO.Cidade.Trim();
                clienteVO.Uf = string.IsNullOrWhiteSpace(clienteVO.Uf) ? null : clienteVO.Uf.Trim().ToUpperInvariant();
                string cepDigitos = StaticMethods.OnlyDigits(clienteVO.Cep);
                clienteVO.Cep = cepDigitos.Length == 0 ? null : cepDigitos;

                // 2. CPF unico entre os ativos (checado antes da CNH).
                bool cpfDuplicado = await _clienteRepository.ExistsByCpfAsync(clienteVO.Cpf);

                if (cpfDuplicado)
                {
                    throw new BusinessException(ConstantsMessage.ErroClienteCpfDuplicado);
                }

                // 3. CNH unica entre os ativos.
                bool cnhDuplicada = await _clienteRepository.ExistsByCnhNumeroAsync(clienteVO.CnhNumero);

                if (cnhDuplicada)
                {
                    throw new BusinessException(ConstantsMessage.ErroClienteCnhDuplicada);
                }

                // 4. Mapeamento e regras do cadastro: sempre PENDENTE, senha so como hash.
                Cliente cliente = _mapper.Map<Cliente>(clienteVO);

                // A categoria chega validada pelo ClienteVOValidator (presente e definida). Sem valor, nunca grava A por padrao.
                CategoriaCnh? categoriaCnh = clienteVO.CnhCategoria;

                if (!categoriaCnh.HasValue)
                {
                    throw new InvalidOperationException(ConstantsMessage.ErroSalvarCliente);
                }

                cliente.CnhCategoria = categoriaCnh.Value;
                cliente.StatusCadastro = StatusCadastroCliente.PENDENTE;
                cliente.SenhaHash = _senhaHasher.Hash(clienteVO.Senha);

                // 5. Gravacao (o repositorio define DataCadastro, CreatedAt e UpdatedAt).
                Cliente gravado = await _clienteRepository.InsertAsync(cliente);
                ReturnClienteVO retorno = _mapper.Map<ReturnClienteVO>(gravado);

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
                _logService.Write(ex, ConstantsMessage.ErroSalvarCliente, GetType().ToString());
                throw;
            }
        }
    }
}
