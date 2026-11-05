using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Options;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using System;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Services
{
    /// <summary>
    /// Cria o administrador inicial a partir da configuracao. Idempotente: se ja existe
    /// funcionario com o CPF configurado, ativo ou desativado, nao faz nada (uma conta
    /// desativada nao e recriada). Sem CPF configurado, nao faz nada. Configuracao invalida
    /// lanca excecao (so o log recebe a mensagem e a subida da API segue); a senha nunca e registrada.
    /// </summary>
    public class AdminInicialService : IAdminInicialService
    {
        private readonly IAutenticacaoRepository _autenticacaoRepository;
        private readonly ISenhaHasher _senhaHasher;
        private readonly AdminInicialOptions _options;
        private readonly ILogService _logService;

        public AdminInicialService(
            IAutenticacaoRepository autenticacaoRepository,
            ISenhaHasher senhaHasher,
            AdminInicialOptions options,
            ILogService logService)
        {
            _autenticacaoRepository = autenticacaoRepository;
            _senhaHasher = senhaHasher;
            _options = options;
            _logService = logService;
        }

        /// <summary>Cria o administrador inicial se ainda nao existir; devolve true somente quando criou.</summary>
        public async Task<bool> GarantirAdminInicialAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_options.Cpf))
                {
                    return false;
                }

                ValidarOpcoes();

                string cpf = StaticMethods.OnlyDigits(_options.Cpf);

                bool existe = await _autenticacaoRepository.ExistsFuncionarioByCpfWithDisabledsAsync(cpf);

                if (existe)
                {
                    return false;
                }

                Funcionario administrador = new Funcionario
                {
                    Nome = _options.Nome.Trim(),
                    Cpf = cpf,
                    Matricula = _options.Matricula.Trim(),
                    Cargo = CargoFuncionario.ADMINISTRATIVO,
                    DataAdmissao = DateOnly.FromDateTime(DateTime.Now),
                    Ativo = true,
                    SenhaHash = _senhaHasher.Hash(_options.Senha)
                };

                await _autenticacaoRepository.InsertFuncionarioAsync(administrador);

                _logService.WriteInfo(ConstantsMessage.InfoAdminInicialCriado, GetType().ToString());

                return true;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroCriarAdminInicial, GetType().ToString());
                throw;
            }
        }

        /// <summary>Confere a configuracao do administrador: CPF valido, nome e matricula informados e dentro do tamanho da coluna, e senha com o minimo de caracteres.</summary>
        private void ValidarOpcoes()
        {
            bool cpfValido = StaticMethods.IsCpfValido(_options.Cpf);
            bool nomeInformado = !string.IsNullOrWhiteSpace(_options.Nome);
            bool nomeCabe = nomeInformado && _options.Nome.Trim().Length <= ConstantsValues.TamanhoMaximoNomeAdminInicial;
            bool matriculaInformada = !string.IsNullOrWhiteSpace(_options.Matricula);
            bool matriculaCabe = matriculaInformada && _options.Matricula.Trim().Length <= ConstantsValues.TamanhoMaximoMatriculaAdminInicial;
            bool senhaValida = !string.IsNullOrEmpty(_options.Senha) && _options.Senha.Length >= ConstantsValues.TamanhoMinimoSenhaAdminInicial;

            if (!cpfValido || !nomeCabe || !matriculaCabe || !senhaValida)
            {
                throw new InvalidOperationException(ConstantsMessage.ErroCriarAdminInicial);
            }
        }
    }
}
