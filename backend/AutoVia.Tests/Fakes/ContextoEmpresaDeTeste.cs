using AutoVia.Communication.Enums;
using AutoVia.Core.ServicesInterface;
using AutoVia.Infrastructure.Services;

namespace AutoVia.Tests.Fakes
{
    /// <summary>
    /// Monta o ContextoEmpresa REAL (sem fake) ja definido, para os testes que precisam de uma
    /// empresa e um funcionario autenticados. Padrao: empresa 1, funcionario 1, cargo ATENDENTE, nao operadora.
    /// </summary>
    public static class ContextoEmpresaDeTeste
    {
        /// <summary>Cria o contexto real com os valores informados; o log e um FakeLogService.</summary>
        public static IContextoEmpresa Criar(int idEmpresa = 1, CargoFuncionario cargo = CargoFuncionario.ATENDENTE, bool empresaOperadora = false)
        {
            ContextoEmpresa contexto = new ContextoEmpresa(new FakeLogService());
            contexto.Definir(idEmpresa, 1, cargo, empresaOperadora);

            return contexto;
        }

        /// <summary>Cria o contexto real SEM definir nada (fail-closed: IdEmpresa = 0).</summary>
        public static IContextoEmpresa Vazio()
        {
            return new ContextoEmpresa(new FakeLogService());
        }
    }
}
