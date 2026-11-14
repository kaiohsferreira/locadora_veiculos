using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Repositories
{
    /// <summary>
    /// Persistencia da empresa no cadastro publico de locadora (P10-BE-01). Empresa e global: as consultas de
    /// unicidade de CNPJ e e-mail nao usam filtro de locadora. A checagem de CPF do responsavel e na plataforma
    /// inteira, por isso e a unica consulta que desliga o filtro "Empresa" (SemEscopoEmpresa, declarado na lista
    /// fechada da guarda de testes). Leituras sem rastreamento e filtradas por DisabledAt nulo. Nenhum metodo
    /// remove a linha fisicamente.
    /// </summary>
    public class EmpresaRepository : IEmpresaRepository
    {
        private readonly AutoViaDbContext _db;
        private readonly ILogService _logService;

        public EmpresaRepository(AutoViaDbContext db, ILogService logService)
        {
            _db = db;
            _logService = logService;
        }

        /// <summary>Indica se existe empresa ativa (DisabledAt nulo) com o CNPJ informado (so digitos).</summary>
        public async Task<bool> ExistsByCnpjAsync(string cnpj)
        {
            try
            {
                bool existe = await _db.Empresas
                    .AsNoTracking()
                    .AnyAsync(x => x.Cnpj == cnpj && x.DisabledAt == null);

                return existe;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarLocadora, GetType().ToString());
                throw;
            }
        }

        /// <summary>Indica se existe empresa ativa (DisabledAt nulo) com o e-mail informado. O e-mail chega aparado e em minusculas (gravado assim).</summary>
        public async Task<bool> ExistsByEmailAsync(string email)
        {
            try
            {
                bool existe = await _db.Empresas
                    .AsNoTracking()
                    .AnyAsync(x => x.Email == email && x.DisabledAt == null);

                return existe;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarLocadora, GetType().ToString());
                throw;
            }
        }

        /// <summary>Indica se o CNPJ informado pertence a empresa operadora da plataforma (Operadora verdadeiro).</summary>
        public async Task<bool> ExistsOperadoraByCnpjAsync(string cnpj)
        {
            try
            {
                bool existe = await _db.Empresas
                    .AsNoTracking()
                    .AnyAsync(x => x.Operadora && x.Cnpj == cnpj);

                return existe;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarLocadora, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Indica se existe funcionario ativo (DisabledAt nulo, com ou sem Ativo) com o CPF, de QUALQUER empresa.
        /// BYPASS DECLARADO: o cadastro publico nao tem contexto de empresa, e o CPF e unico na plataforma. A equipe
        /// (P10-BE-03) usa este mesmo metodo no cadastro de funcionario, pela mesma regra de CPF unico entre todas as locadoras.
        /// </summary>
        public async Task<bool> ExistsFuncionarioByCpfAsync(string cpf)
        {
            try
            {
                bool existe = await _db.Funcionarios
                    .SemEscopoEmpresa()
                    .AsNoTracking()
                    .AnyAsync(x => x.Cpf == cpf && x.DisabledAt == null);

                return existe;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarLocadora, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Grava a empresa e o responsavel na mesma gravacao: a empresa leva o responsavel na colecao de funcionarios
        /// e um unico SaveChangesAsync grava os dois. Empresa e responsavel recebem o mesmo DateTime.Now (CreatedAt e UpdatedAt).
        /// </summary>
        public async Task<Empresa> InsertComResponsavelAsync(Empresa empresa, Funcionario responsavel)
        {
            try
            {
                DateTime agora = DateTime.Now;

                empresa.IdEmpresa = 0;
                empresa.CreatedAt = agora;
                empresa.UpdatedAt = agora;
                empresa.DisabledAt = null;

                responsavel.IdFuncionario = 0;
                responsavel.CreatedAt = agora;
                responsavel.UpdatedAt = agora;
                responsavel.DisabledAt = null;

                empresa.Funcionarios.Add(responsavel);
                _db.Empresas.Add(empresa);
                await _db.SaveChangesAsync();

                return empresa;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSalvarLocadora, GetType().ToString());
                throw;
            }
        }
    }
}
