using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Equipe;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Services
{
    /// <summary>
    /// Regras da equipe da locadora (P10-BE-03). A locadora vem sempre do contexto do ADMINISTRADOR autenticado (nunca do
    /// corpo nem da URL). CPF unico na plataforma inteira (409); matricula unica na locadora (409); o ADMINISTRADOR nao
    /// desativa a si mesmo nem troca o proprio cargo (409); a locadora nao fica sem administrador ativo (409). Toda troca
    /// de cargo ou de situacao invalida a sessao do funcionario afetado. A senha inicial so vai como hash e nunca para o log.
    /// </summary>
    public class EquipeService : IEquipeService
    {
        private readonly IFuncionarioRepository _funcionarioRepository;
        private readonly IEmpresaRepository _empresaRepository;
        private readonly ISenhaHasher _senhaHasher;
        private readonly ISessaoService _sessaoService;
        private readonly IContextoEmpresa _contextoEmpresa;
        private readonly IMapper _mapper;
        private readonly ILogService _logService;

        public EquipeService(
            IFuncionarioRepository funcionarioRepository,
            IEmpresaRepository empresaRepository,
            ISenhaHasher senhaHasher,
            ISessaoService sessaoService,
            IContextoEmpresa contextoEmpresa,
            IMapper mapper,
            ILogService logService)
        {
            _funcionarioRepository = funcionarioRepository;
            _empresaRepository = empresaRepository;
            _senhaHasher = senhaHasher;
            _sessaoService = sessaoService;
            _contextoEmpresa = contextoEmpresa;
            _mapper = mapper;
            _logService = logService;
        }

        /// <summary>
        /// Lista a equipe da locadora: a pagina e o total usam os filtros (busca, cargo, situacao); os quatro totais
        /// (ativos, inativos, administradores ativos e cadastrados) sao da locadora inteira, sem filtro.
        /// </summary>
        public async Task<ReturnEquipeVO> GetAllAsync(FiltroEquipeVO filtro)
        {
            try
            {
                string? busca = NormalizarBusca(filtro.Busca);
                string? digitosCpf = ExtrairDigitosDaBusca(filtro.Busca);

                int total = await _funcionarioRepository.CountAsync(busca, digitosCpf, filtro.Cargo, filtro.Ativo);
                List<Funcionario> funcionarios = await _funcionarioRepository.FindAllAsync(busca, digitosCpf, filtro.Cargo, filtro.Ativo, filtro.Deslocamento, filtro.TamanhoPagina);

                int totalAtivos = await _funcionarioRepository.CountAsync(null, null, null, true);
                int totalInativos = await _funcionarioRepository.CountAsync(null, null, null, false);
                int totalAdministradores = await _funcionarioRepository.CountAdministradoresAtivosAsync(null);

                List<ReturnFuncionarioVO> itens = _mapper.Map<List<ReturnFuncionarioVO>>(funcionarios);

                foreach (ReturnFuncionarioVO item in itens)
                {
                    item.EhVoce = item.IdFuncionario == _contextoEmpresa.IdFuncionario;
                }

                ReturnEquipeVO retorno = new ReturnEquipeVO
                {
                    Itens = itens,
                    Total = total,
                    Pagina = filtro.Pagina,
                    TamanhoPagina = filtro.TamanhoPagina,
                    TotalAtivos = totalAtivos,
                    TotalInativos = totalInativos,
                    TotalAdministradores = totalAdministradores,
                    TotalCadastrados = totalAtivos + totalInativos
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
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarEquipe, GetType().ToString());
                throw;
            }
        }

        /// <summary>Consulta um funcionario da locadora pelo id. Inexistente, desativado ou de outra locadora = 404 com a mesma mensagem.</summary>
        public async Task<ReturnFuncionarioVO> GetByIdAsync(int idFuncionario)
        {
            try
            {
                Funcionario? funcionario = await _funcionarioRepository.FindByIdAsync(idFuncionario);

                if (funcionario == null)
                {
                    throw new NotFoundException(ConstantsMessage.ErroEquipeFuncionarioNaoEncontrado);
                }

                ReturnFuncionarioVO retorno = MapearRetorno(funcionario);

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
                _logService.Write(ex, ConstantsMessage.ErroBuscarFuncionario, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Cadastra o funcionario na locadora do contexto. Ordem: CPF na plataforma (409), matricula na locadora (409) e
        /// gravacao com a senha inicial como hash. Ativo nasce verdadeiro.
        /// </summary>
        public async Task<ReturnFuncionarioVO> InsertAsync(FuncionarioVO funcionarioVO)
        {
            try
            {
                string cpf = StaticMethods.OnlyDigits(funcionarioVO.Cpf);
                string matricula = funcionarioVO.Matricula.Trim();

                bool cpfUsado = await _empresaRepository.ExistsFuncionarioByCpfAsync(cpf);

                if (cpfUsado)
                {
                    throw new BusinessException(ConstantsMessage.ErroEquipeCpfDuplicado);
                }

                bool matriculaUsada = await _funcionarioRepository.ExistsByMatriculaAsync(matricula, null);

                if (matriculaUsada)
                {
                    throw new BusinessException(ConstantsMessage.ErroEquipeMatriculaDuplicada);
                }

                CargoFuncionario? cargo = funcionarioVO.Cargo;

                if (!cargo.HasValue)
                {
                    throw new InvalidOperationException(ConstantsMessage.ErroSalvarFuncionario);
                }

                int idEmpresa = _contextoEmpresa.IdEmpresa;

                if (idEmpresa <= 0)
                {
                    throw new InvalidOperationException(ConstantsMessage.ErroDefinirContextoEmpresa);
                }

                Funcionario novo = new Funcionario
                {
                    Nome = funcionarioVO.Nome.Trim(),
                    Cpf = cpf,
                    Matricula = matricula,
                    Email = NormalizarEmail(funcionarioVO.Email),
                    DataAdmissao = funcionarioVO.DataAdmissao,
                    Cargo = cargo.Value,
                    Ativo = true,
                    IdEmpresa = idEmpresa,
                    SenhaHash = _senhaHasher.Hash(funcionarioVO.SenhaInicial)
                };

                Funcionario gravado = await _funcionarioRepository.InsertAsync(novo);
                ReturnFuncionarioVO retorno = MapearRetorno(gravado);

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
                _logService.Write(ex, ConstantsMessage.ErroSalvarFuncionario, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Edita o funcionario da locadora. Ordem: existe na locadora (404); ADMINISTRADOR nao troca o proprio cargo (409);
        /// matricula de outro funcionario (409); ultimo administrador ativo (409). Se o cargo mudou, invalida a sessao do
        /// funcionario para a proxima requisicao ja valer o cargo novo. O CPF nunca e alterado.
        /// </summary>
        public async Task UpdateAsync(int idFuncionario, FuncionarioEdicaoVO funcionarioVO)
        {
            try
            {
                Funcionario? atual = await _funcionarioRepository.FindByIdAsync(idFuncionario);

                if (atual == null)
                {
                    throw new NotFoundException(ConstantsMessage.ErroEquipeFuncionarioNaoEncontrado);
                }

                CargoFuncionario? cargoInformado = funcionarioVO.Cargo;

                if (!cargoInformado.HasValue)
                {
                    throw new InvalidOperationException(ConstantsMessage.ErroSalvarFuncionario);
                }

                CargoFuncionario cargoNovo = cargoInformado.Value;
                bool mudouCargo = atual.Cargo != cargoNovo;

                if (mudouCargo && EhPropriaConta(idFuncionario))
                {
                    throw new BusinessException(ConstantsMessage.ErroEquipePropriaConta);
                }

                string matricula = funcionarioVO.Matricula.Trim();
                bool matriculaUsada = await _funcionarioRepository.ExistsByMatriculaAsync(matricula, idFuncionario);

                if (matriculaUsada)
                {
                    throw new BusinessException(ConstantsMessage.ErroEquipeMatriculaDuplicada);
                }

                if (mudouCargo && atual.Ativo && atual.Cargo == CargoFuncionario.ADMINISTRADOR)
                {
                    await GarantirOutroAdministradorAtivo(idFuncionario);
                }

                Funcionario alterado = new Funcionario
                {
                    IdFuncionario = idFuncionario,
                    Nome = funcionarioVO.Nome.Trim(),
                    Matricula = matricula,
                    Email = NormalizarEmail(funcionarioVO.Email),
                    DataAdmissao = funcionarioVO.DataAdmissao,
                    Cargo = cargoNovo
                };

                bool atualizado = await _funcionarioRepository.UpdateAsync(alterado);

                if (!atualizado)
                {
                    throw new NotFoundException(ConstantsMessage.ErroEquipeFuncionarioNaoEncontrado);
                }

                if (mudouCargo)
                {
                    _sessaoService.Invalidar(PerfilUsuario.FUNCIONARIO, idFuncionario);
                }
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
                _logService.Write(ex, ConstantsMessage.ErroSalvarFuncionario, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Desativa (ativo = false) ou reativa (ativo = true) o acesso. Ordem: existe na locadora (404); ADMINISTRADOR nao
        /// desativa a si mesmo (409); ultimo administrador ativo nao pode ser desativado (409). Sempre invalida a sessao do
        /// funcionario. Nao e exclusao: DisabledAt continua nulo.
        /// </summary>
        public async Task AlterarSituacaoAsync(int idFuncionario, SituacaoFuncionarioVO situacaoVO)
        {
            try
            {
                bool? ativoInformado = situacaoVO.Ativo;

                if (!ativoInformado.HasValue)
                {
                    throw new InvalidOperationException(ConstantsMessage.ErroAlterarSituacaoFuncionario);
                }

                bool ativo = ativoInformado.Value;
                Funcionario? atual = await _funcionarioRepository.FindByIdAsync(idFuncionario);

                if (atual == null)
                {
                    throw new NotFoundException(ConstantsMessage.ErroEquipeFuncionarioNaoEncontrado);
                }

                if (!ativo && EhPropriaConta(idFuncionario))
                {
                    throw new BusinessException(ConstantsMessage.ErroEquipePropriaConta);
                }

                if (!ativo && atual.Ativo && atual.Cargo == CargoFuncionario.ADMINISTRADOR)
                {
                    await GarantirOutroAdministradorAtivo(idFuncionario);
                }

                bool alterado = await _funcionarioRepository.SetSituacaoAsync(idFuncionario, ativo);

                if (!alterado)
                {
                    throw new NotFoundException(ConstantsMessage.ErroEquipeFuncionarioNaoEncontrado);
                }

                _sessaoService.Invalidar(PerfilUsuario.FUNCIONARIO, idFuncionario);
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
                _logService.Write(ex, ConstantsMessage.ErroAlterarSituacaoFuncionario, GetType().ToString());
                throw;
            }
        }

        /// <summary>Converte o funcionario em VO de saida e marca se e o proprio usuario autenticado.</summary>
        private ReturnFuncionarioVO MapearRetorno(Funcionario funcionario)
        {
            ReturnFuncionarioVO retorno = _mapper.Map<ReturnFuncionarioVO>(funcionario);
            retorno.EhVoce = funcionario.IdFuncionario == _contextoEmpresa.IdFuncionario;

            return retorno;
        }

        /// <summary>Verdadeiro quando o id informado e o do proprio usuario autenticado.</summary>
        private bool EhPropriaConta(int idFuncionario)
        {
            return idFuncionario == _contextoEmpresa.IdFuncionario;
        }

        /// <summary>
        /// Trava do ultimo administrador: se nao sobra nenhum outro ADMINISTRADOR ativo na locadora, bloqueia (409).
        /// Chamada so quando o alvo e um administrador ativo que vai perder o cargo ou o acesso.
        /// </summary>
        private async Task GarantirOutroAdministradorAtivo(int idFuncionario)
        {
            int outrosAdministradores = await _funcionarioRepository.CountAdministradoresAtivosAsync(idFuncionario);

            if (outrosAdministradores == 0)
            {
                throw new BusinessException(ConstantsMessage.ErroEquipeUltimoAdministrador);
            }
        }

        /// <summary>Busca normalizada: aparada e em minusculas; vazia ou so espacos vira null (sem filtro).</summary>
        private static string? NormalizarBusca(string? busca)
        {
            if (string.IsNullOrWhiteSpace(busca))
            {
                return null;
            }

            return busca.Trim().ToLowerInvariant();
        }

        /// <summary>
        /// Digitos para a busca por CPF (guardado so com numeros). So existe quando a busca, sem a mascara (pontos,
        /// traco e espacos), e composta somente de digitos; com letras ou outros caracteres, null (a busca nao procura no CPF).
        /// </summary>
        private static string? ExtrairDigitosDaBusca(string? busca)
        {
            if (string.IsNullOrEmpty(busca))
            {
                return null;
            }

            string semMascara = new string(busca.Where(caractere => !ConstantsValues.CaracteresMascaraCpf.Contains(caractere) && !char.IsWhiteSpace(caractere)).ToArray());

            if (semMascara.Length == 0 || StaticMethods.OnlyDigits(semMascara) != semMascara)
            {
                return null;
            }

            return semMascara;
        }

        /// <summary>E-mail gravado e comparado aparado e em minusculas.</summary>
        private static string NormalizarEmail(string email)
        {
            return email.Trim().ToLowerInvariant();
        }
    }
}
