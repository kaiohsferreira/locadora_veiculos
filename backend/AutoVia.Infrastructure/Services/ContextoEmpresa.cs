using AutoVia.Communication.Enums;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using System;

namespace AutoVia.Infrastructure.Services
{
    /// <summary>
    /// Contexto de empresa da requisicao (Scoped). Nao depende de ASP.NET: quem o preenche e a
    /// validacao do token (AddAuthenticationStartup), com os valores do banco. Sem Definir, o
    /// contexto e vazio (IdEmpresa = 0) e o filtro "Empresa" nao devolve nenhuma linha escopada.
    /// </summary>
    public class ContextoEmpresa : IContextoEmpresa
    {
        private readonly ILogService _logService;

        public ContextoEmpresa(ILogService logService)
        {
            _logService = logService;
        }

        /// <summary>Id da empresa do funcionario autenticado; 0 quando nao ha contexto.</summary>
        public int IdEmpresa { get; private set; }

        /// <summary>Id do funcionario autenticado; 0 quando nao ha contexto.</summary>
        public int IdFuncionario { get; private set; }

        /// <summary>Id do cliente autenticado (perfil CLIENTE); 0 quando o token nao e de cliente.</summary>
        public int IdCliente { get; private set; }

        /// <summary>Cargo do funcionario autenticado; nulo quando nao ha contexto.</summary>
        public CargoFuncionario? Cargo { get; private set; }

        /// <summary>Verdadeiro quando a empresa do funcionario e a operadora da plataforma.</summary>
        public bool EmpresaOperadora { get; private set; }

        /// <summary>Verdadeiro para ADMINISTRADOR ou FINANCEIRO: unico ponto do corte de valores financeiros.</summary>
        public bool PodeVerValores
        {
            get
            {
                return Cargo == CargoFuncionario.ADMINISTRADOR || Cargo == CargoFuncionario.FINANCEIRO;
            }
        }

        /// <summary>Define o contexto com os valores do banco. Recusa id de empresa ou de funcionario nao positivo.</summary>
        public void Definir(int idEmpresa, int idFuncionario, CargoFuncionario cargo, bool empresaOperadora)
        {
            try
            {
                if (idEmpresa <= 0 || idFuncionario <= 0)
                {
                    throw new InvalidOperationException(ConstantsMessage.ErroDefinirContextoEmpresa);
                }

                if (IdCliente > 0)
                {
                    throw new InvalidOperationException(ConstantsMessage.ErroContextoMisto);
                }

                IdEmpresa = idEmpresa;
                IdFuncionario = idFuncionario;
                Cargo = cargo;
                EmpresaOperadora = empresaOperadora;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroDefinirContextoEmpresa, GetType().ToString());
                throw;
            }
        }

        /// <summary>Define o cliente da requisicao com o id conferido no banco. Recusa id de cliente nao positivo.</summary>
        public void DefinirCliente(int idCliente)
        {
            try
            {
                if (idCliente <= 0)
                {
                    throw new InvalidOperationException(ConstantsMessage.ErroDefinirContextoEmpresa);
                }

                if (IdEmpresa > 0)
                {
                    throw new InvalidOperationException(ConstantsMessage.ErroContextoMisto);
                }

                IdCliente = idCliente;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroDefinirContextoEmpresa, GetType().ToString());
                throw;
            }
        }
    }
}
