using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Empresas;
using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using FluentValidation;
using FluentValidation.Results;
using System;
using System.Collections.Generic;
using System.Linq;
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
        /// Lista as cidades com locadoras ativas. Cidades iguais sem diferenciar maiusculas e espacos nas pontas viram
        /// uma opcao so (mesmo criterio do filtro de veiculos), com a grafia mista entre as variantes (ou a menor em
        /// ordem ordinal, se nenhuma for mista). A lista sai ordenada por cidade, depois por UF.
        /// </summary>
        public async Task<List<ReturnCidadeLocadoraVO>> GetCidadesAsync()
        {
            try
            {
                List<ReturnCidadeLocadoraVO> cidades = await _empresaRepository.FindCidadesAtivasAsync();
                List<ReturnCidadeLocadoraVO> agrupadas = AgruparCidades(cidades);
                List<ReturnCidadeLocadoraVO> ordenadas = OrdenarCidades(agrupadas);
                return ordenadas;
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
                _logService.Write(ex, ConstantsMessage.ErroConsultarLocadora, GetType().ToString());
                throw;
            }
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

        /// <summary>
        /// Agrupa as cidades pelo criterio do filtro de veiculos (cidade aparada e em minusculas, UF em maiusculas).
        /// Cada grupo sai uma vez, com a grafia escolhida por EhGrafiaPreferida. Cidade vazia depois de aparada e
        /// ignorada (defesa: o repositorio ja filtra). A ordem de saida ainda e a da consulta; OrdenarCidades fecha a ordem.
        /// </summary>
        private static List<ReturnCidadeLocadoraVO> AgruparCidades(List<ReturnCidadeLocadoraVO> cidades)
        {
            Dictionary<(string Cidade, string Uf), string> grafiaEscolhida = new Dictionary<(string Cidade, string Uf), string>();
            List<(string Cidade, string Uf)> chavesNaOrdem = new List<(string Cidade, string Uf)>();

            foreach (ReturnCidadeLocadoraVO cidade in cidades)
            {
                if (string.IsNullOrWhiteSpace(cidade.Cidade))
                {
                    continue;
                }

                string grafia = cidade.Cidade.Trim();
                (string Cidade, string Uf) chave = (grafia.ToLowerInvariant(), cidade.Uf.ToUpperInvariant());

                if (!grafiaEscolhida.ContainsKey(chave))
                {
                    grafiaEscolhida[chave] = grafia;
                    chavesNaOrdem.Add(chave);
                }
                else if (EhGrafiaPreferida(grafia, grafiaEscolhida[chave]))
                {
                    grafiaEscolhida[chave] = grafia;
                }
            }

            List<ReturnCidadeLocadoraVO> agrupadas = new List<ReturnCidadeLocadoraVO>();

            foreach ((string Cidade, string Uf) chave in chavesNaOrdem)
            {
                ReturnCidadeLocadoraVO opcao = new ReturnCidadeLocadoraVO
                {
                    Cidade = grafiaEscolhida[chave],
                    Uf = chave.Uf
                };
                agrupadas.Add(opcao);
            }

            return agrupadas;
        }

        /// <summary>
        /// Indica se a grafia candidata deve trocar a atual do grupo: a mista (nem toda maiuscula nem toda minuscula)
        /// vence a que nao e; entre duas do mesmo tipo, vence a menor em ordem ordinal. Assim a escolha nao depende da
        /// ordem em que a consulta devolveu as linhas.
        /// </summary>
        private static bool EhGrafiaPreferida(string candidata, string atual)
        {
            bool candidataEhMista = EhGrafiaMista(candidata);
            bool atualEhMista = EhGrafiaMista(atual);

            if (candidataEhMista != atualEhMista)
            {
                return candidataEhMista;
            }

            return string.CompareOrdinal(candidata, atual) < 0;
        }

        /// <summary>Grafia mista: diferente da versao toda em maiusculas e diferente da versao toda em minusculas.</summary>
        private static bool EhGrafiaMista(string grafia)
        {
            string todaMaiuscula = grafia.ToUpperInvariant();
            string todaMinuscula = grafia.ToLowerInvariant();
            bool mista = grafia != todaMaiuscula && grafia != todaMinuscula;

            return mista;
        }

        /// <summary>
        /// Ordena por cidade sem diferenciar maiusculas e com acento no lugar certo (A acentuado junto dos A), depois por
        /// UF e, so por ultimo, pela grafia em ordem ordinal (desempate de cidades iguais em outra grafia na mesma UF).
        /// Os nomes sao comparados em ordem ordinal sobre uma chave dobrada pela tabela de ConstantsValues, e nao pela
        /// cultura: o projeto nao liga InvariantGlobalization, mas o resultado seria o mesmo se ligasse.
        /// </summary>
        private static List<ReturnCidadeLocadoraVO> OrdenarCidades(List<ReturnCidadeLocadoraVO> cidades)
        {
            List<ReturnCidadeLocadoraVO> ordenadas = cidades
                .OrderBy(cidade => ChaveDeOrdenacaoCidade(cidade.Cidade), StringComparer.Ordinal)
                .ThenBy(cidade => cidade.Uf, StringComparer.Ordinal)
                .ThenBy(cidade => cidade.Cidade, StringComparer.Ordinal)
                .ToList();

            return ordenadas;
        }

        /// <summary>Chave de ordenacao da cidade: maiusculas e letras acentuadas trocadas pela base (Á vira A, Ç vira C).</summary>
        private static string ChaveDeOrdenacaoCidade(string cidade)
        {
            char[] letras = cidade.ToCharArray();

            for (int posicao = 0; posicao < letras.Length; posicao++)
            {
                int indice = ConstantsValues.LetrasComAcentoCidade.IndexOf(letras[posicao]);
                letras[posicao] = indice >= 0
                    ? ConstantsValues.LetrasSemAcentoCidade[indice]
                    : char.ToUpperInvariant(letras[posicao]);
            }

            string chave = new string(letras);

            return chave;
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
