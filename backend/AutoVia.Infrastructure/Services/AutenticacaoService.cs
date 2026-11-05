using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Autenticacao;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using System;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Services
{
    /// <summary>
    /// Fluxo de login (RF-06). Cliente entra em qualquer status de cadastro; funcionario
    /// so entra com Ativo = true. Toda recusa vira UnauthorizedException com a mesma
    /// mensagem. Se o CPF nao existe, a verificacao de senha e simulada para gastar o
    /// mesmo tempo. Senha e hash nunca sao registrados.
    /// </summary>
    public class AutenticacaoService : IAutenticacaoService
    {
        private readonly IAutenticacaoRepository _autenticacaoRepository;
        private readonly ISenhaHasher _senhaHasher;
        private readonly ITokenService _tokenService;
        private readonly ILogService _logService;

        public AutenticacaoService(
            IAutenticacaoRepository autenticacaoRepository,
            ISenhaHasher senhaHasher,
            ITokenService tokenService,
            ILogService logService)
        {
            _autenticacaoRepository = autenticacaoRepository;
            _senhaHasher = senhaHasher;
            _tokenService = tokenService;
            _logService = logService;
        }

        /// <summary>Autentica o CPF e a senha no perfil informado e devolve o token; lanca UnauthorizedException se recusar.</summary>
        public async Task<ReturnLoginVO> LoginAsync(LoginVO loginVO)
        {
            try
            {
                string cpf = StaticMethods.OnlyDigits(loginVO.Cpf);

                if (loginVO.Perfil == PerfilUsuario.CLIENTE)
                {
                    return await LoginClienteAsync(cpf, loginVO.Senha);
                }

                if (loginVO.Perfil == PerfilUsuario.FUNCIONARIO)
                {
                    return await LoginFuncionarioAsync(cpf, loginVO.Senha);
                }

                throw new UnauthorizedException(ConstantsMessage.ErroCredenciaisInvalidas);
            }
            catch (UnauthorizedException)
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
                _logService.Write(ex, ConstantsMessage.ErroAutenticar, GetType().ToString());
                throw;
            }
        }

        private async Task<ReturnLoginVO> LoginClienteAsync(string cpf, string senha)
        {
            Cliente? cliente = await _autenticacaoRepository.FindClienteByCpfAsync(cpf);

            if (cliente == null)
            {
                _senhaHasher.SimularVerificacao(senha);
                throw new UnauthorizedException(ConstantsMessage.ErroCredenciaisInvalidas);
            }

            bool senhaValida = VerificarSenha(senha, cliente.SenhaHash);

            if (!senhaValida)
            {
                throw new UnauthorizedException(ConstantsMessage.ErroCredenciaisInvalidas);
            }

            TokenGeradoVO token = _tokenService.GerarToken(PerfilUsuario.CLIENTE, cliente.IdCliente, cliente.Nome, null);

            ReturnLoginVO retorno = MontarRetorno(token, PerfilUsuario.CLIENTE, cliente.IdCliente, cliente.Nome);
            retorno.StatusCadastro = cliente.StatusCadastro;

            return retorno;
        }

        private async Task<ReturnLoginVO> LoginFuncionarioAsync(string cpf, string senha)
        {
            Funcionario? funcionario = await _autenticacaoRepository.FindFuncionarioByCpfAsync(cpf);

            if (funcionario == null)
            {
                _senhaHasher.SimularVerificacao(senha);
                throw new UnauthorizedException(ConstantsMessage.ErroCredenciaisInvalidas);
            }

            // A senha e conferida mesmo com Ativo = false: o tempo gasto nao revela o estado da conta.
            bool senhaValida = VerificarSenha(senha, funcionario.SenhaHash);

            if (!senhaValida || !funcionario.Ativo)
            {
                throw new UnauthorizedException(ConstantsMessage.ErroCredenciaisInvalidas);
            }

            TokenGeradoVO token = _tokenService.GerarToken(PerfilUsuario.FUNCIONARIO, funcionario.IdFuncionario, funcionario.Nome, funcionario.Cargo);

            ReturnLoginVO retorno = MontarRetorno(token, PerfilUsuario.FUNCIONARIO, funcionario.IdFuncionario, funcionario.Nome);
            retorno.Cargo = funcionario.Cargo;

            return retorno;
        }

        /// <summary>Confere a senha; sem hash cadastrado, simula a verificacao e recusa.</summary>
        private bool VerificarSenha(string senha, string? senhaHash)
        {
            if (string.IsNullOrWhiteSpace(senhaHash))
            {
                _senhaHasher.SimularVerificacao(senha);
                return false;
            }

            return _senhaHasher.Verify(senha, senhaHash);
        }

        /// <summary>Monta o retorno do login a partir do token; a data formatada sai de StaticMethods. ExpiraEm sai sem fuso (Kind Unspecified), como o contrato pede.</summary>
        private static ReturnLoginVO MontarRetorno(TokenGeradoVO token, PerfilUsuario perfil, int idUsuario, string nome)
        {
            ReturnLoginVO retorno = new ReturnLoginVO
            {
                Token = token.Token,
                TipoToken = ConstantsValues.TipoTokenBearer,
                ExpiraEm = DateTime.SpecifyKind(token.ExpiraEm, DateTimeKind.Unspecified),
                ExpiraEmStr = StaticMethods.FormateDateTimeToPTBR(token.ExpiraEm),
                Perfil = perfil,
                IdUsuario = idUsuario,
                Nome = nome
            };

            return retorno;
        }
    }
}
