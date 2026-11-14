using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Empresas;
using AutoVia.Core.Entities.PgSql;
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
    /// Regras do cadastro publico de locadora (P10-BE-01): normaliza a entrada, recusa o CNPJ da operadora (400),
    /// impede CNPJ, e-mail da empresa e CPF do responsavel repetidos (409, nessa ordem), grava a empresa e o
    /// responsavel ADMINISTRADOR numa unica gravacao (senha so como hash) e devolve so o VO de retorno. A verificacao
    /// publica responde so a existencia. Senha, CNPJ, CPF e e-mail nunca vao para o log nem para a excecao.
    /// </summary>
    public class EmpresaService : IEmpresaService
    {
        private readonly IEmpresaRepository _empresaRepository;
        private readonly ISenhaHasher _senhaHasher;
        private readonly IMapper _mapper;
        private readonly ILogService _logService;

        public EmpresaService(IEmpresaRepository empresaRepository, ISenhaHasher senhaHasher, IMapper mapper, ILogService logService)
        {
            _empresaRepository = empresaRepository;
            _senhaHasher = senhaHasher;
            _mapper = mapper;
            _logService = logService;
        }

        /// <summary>
        /// Cadastra a locadora e o responsavel. Ordem: normalizacao, CNPJ da operadora (400), CNPJ (409), e-mail da
        /// empresa (409), CPF do responsavel (409) e uma unica gravacao. O responsavel nasce ADMINISTRADOR, ativo,
        /// com matricula MAT-0001; a empresa nasce ativa e nao operadora.
        /// </summary>
        public async Task<ReturnEmpresaVO> InsertAsync(CadastroLocadoraVO cadastroVO)
        {
            try
            {
                // 1. Normalizacao da entrada (mesma forma que fica gravada e consultada).
                cadastroVO.RazaoSocial = cadastroVO.RazaoSocial.Trim();
                cadastroVO.NomeFantasia = cadastroVO.NomeFantasia.Trim();
                cadastroVO.Cnpj = StaticMethods.OnlyDigits(cadastroVO.Cnpj);
                cadastroVO.Telefone = StaticMethods.OnlyDigits(cadastroVO.Telefone);
                cadastroVO.Email = NormalizarEmail(cadastroVO.Email);
                cadastroVO.Cep = StaticMethods.OnlyDigits(cadastroVO.Cep);
                cadastroVO.Logradouro = cadastroVO.Logradouro.Trim();
                cadastroVO.Numero = cadastroVO.Numero.Trim();
                cadastroVO.Complemento = string.IsNullOrWhiteSpace(cadastroVO.Complemento) ? null : cadastroVO.Complemento.Trim();
                cadastroVO.Bairro = cadastroVO.Bairro.Trim();
                cadastroVO.Cidade = cadastroVO.Cidade.Trim();
                cadastroVO.Uf = cadastroVO.Uf.Trim().ToUpperInvariant();
                cadastroVO.ResponsavelNome = cadastroVO.ResponsavelNome.Trim();
                cadastroVO.ResponsavelCpf = StaticMethods.OnlyDigits(cadastroVO.ResponsavelCpf);
                cadastroVO.ResponsavelEmail = NormalizarEmail(cadastroVO.ResponsavelEmail);

                // 2. CNPJ da empresa operadora: recusado como invalido (400), antes das checagens de duplicidade.
                bool cnpjDaOperadora = await _empresaRepository.ExistsOperadoraByCnpjAsync(cadastroVO.Cnpj);

                if (cnpjDaOperadora)
                {
                    throw CriarErroDeValidacao(nameof(CadastroLocadoraVO.Cnpj), ConstantsMessage.ErroLocadoraCnpjInvalido);
                }

                // 3. Unicidade no servico, sobre os registros ativos: CNPJ, e-mail da empresa, CPF do responsavel (nessa ordem).
                bool cnpjDuplicado = await _empresaRepository.ExistsByCnpjAsync(cadastroVO.Cnpj);

                if (cnpjDuplicado)
                {
                    throw new BusinessException(ConstantsMessage.ErroLocadoraCnpjDuplicado);
                }

                bool emailDuplicado = await _empresaRepository.ExistsByEmailAsync(cadastroVO.Email);

                if (emailDuplicado)
                {
                    throw new BusinessException(ConstantsMessage.ErroLocadoraEmailDuplicado);
                }

                bool cpfDuplicado = await _empresaRepository.ExistsFuncionarioByCpfAsync(cadastroVO.ResponsavelCpf);

                if (cpfDuplicado)
                {
                    throw new BusinessException(ConstantsMessage.ErroLocadoraResponsavelCpfDuplicado);
                }

                // 4. Montagem: a empresa e o responsavel saem do VO; o servico define as flags, o cargo, a matricula e o hash.
                Empresa empresa = _mapper.Map<Empresa>(cadastroVO);
                empresa.Ativo = true;
                empresa.Operadora = false;

                Funcionario responsavel = _mapper.Map<Funcionario>(cadastroVO);
                responsavel.Cargo = CargoFuncionario.ADMINISTRADOR;
                responsavel.Matricula = ConstantsValues.MatriculaInicialResponsavel;
                responsavel.Ativo = true;
                responsavel.DataAdmissao = DateOnly.FromDateTime(DateTime.Now);
                responsavel.SenhaHash = _senhaHasher.Hash(cadastroVO.Senha);

                // 5. Gravacao unica: empresa e responsavel no mesmo SaveChanges (repositorio).
                Empresa gravada = await _empresaRepository.InsertComResponsavelAsync(empresa, responsavel);
                ReturnEmpresaVO retorno = _mapper.Map<ReturnEmpresaVO>(gravada);

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
                _logService.Write(ex, ConstantsMessage.ErroSalvarLocadora, GetType().ToString());
                throw;
            }
        }

        /// <summary>Informa se existe empresa ativa com o CNPJ (so digitos) ou o e-mail (aparado e em minusculas) informado. Nao grava nada.</summary>
        public async Task<ReturnVerificacaoCadastroEmpresaVO> VerificarCadastroAsync(VerificarCadastroEmpresaVO verificarCadastroVO)
        {
            try
            {
                CampoVerificacaoEmpresa? campo = verificarCadastroVO.Campo;
                bool existe;

                if (campo == CampoVerificacaoEmpresa.CNPJ)
                {
                    string cnpj = StaticMethods.OnlyDigits(verificarCadastroVO.Valor);
                    existe = await _empresaRepository.ExistsByCnpjAsync(cnpj);
                }
                else if (campo == CampoVerificacaoEmpresa.EMAIL)
                {
                    string email = NormalizarEmail(verificarCadastroVO.Valor);
                    existe = await _empresaRepository.ExistsByEmailAsync(email);
                }
                else
                {
                    throw new InvalidOperationException(ConstantsMessage.ErroVerificarCadastroLocadora);
                }

                ReturnVerificacaoCadastroEmpresaVO retorno = new ReturnVerificacaoCadastroEmpresaVO
                {
                    Existe = existe
                };

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
                _logService.Write(ex, ConstantsMessage.ErroVerificarCadastroLocadora, GetType().ToString());
                throw;
            }
        }

        /// <summary>E-mail gravado e consultado aparado e em minusculas.</summary>
        private static string NormalizarEmail(string email)
        {
            string normalizado = email.Trim().ToLowerInvariant();

            return normalizado;
        }

        /// <summary>Monta a excecao de validacao (400 pelo ErrorHandlingMiddleware) com um erro de campo; a mensagem e a constante informada.</summary>
        private static ValidationException CriarErroDeValidacao(string campo, string mensagem)
        {
            List<ValidationFailure> falhas = new List<ValidationFailure>
            {
                new ValidationFailure(campo, mensagem)
            };

            return new ValidationException(falhas);
        }
    }
}
