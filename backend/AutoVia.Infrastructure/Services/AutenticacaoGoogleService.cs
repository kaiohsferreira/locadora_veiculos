using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Autenticacao;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Google;
using AutoVia.Integrations.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Services
{
    /// <summary>
    /// Login de cliente com conta Google (N3, RF-51). A prova vem do IGoogleIdentityGateway (assinatura, emissor,
    /// publico, validade e e-mail verificado). Sem vinculo, o cliente so casa pelo e-mail verificado quando ha
    /// exatamente um cadastro ativo; a conta Google sozinha nunca cria cliente. O vinculo so e gravado depois de
    /// provar CPF e senha da conta AutoVia. Toda recusa do vinculo (senha, CPF, cliente sem senha, e-mail
    /// diferente) sai como o mesmo UnauthorizedException. Nunca registra credencial, sub, e-mail, nome, CPF ou senha.
    /// </summary>
    public class AutenticacaoGoogleService : IAutenticacaoGoogleService
    {
        private readonly IGoogleIdentityGateway _googleIdentityGateway;
        private readonly IGoogleVinculoRepository _googleVinculoRepository;
        private readonly IAutenticacaoRepository _autenticacaoRepository;
        private readonly IAutenticacaoService _autenticacaoService;
        private readonly ISenhaHasher _senhaHasher;
        private readonly ILogService _logService;

        public AutenticacaoGoogleService(
            IGoogleIdentityGateway googleIdentityGateway,
            IGoogleVinculoRepository googleVinculoRepository,
            IAutenticacaoRepository autenticacaoRepository,
            IAutenticacaoService autenticacaoService,
            ISenhaHasher senhaHasher,
            ILogService logService)
        {
            _googleIdentityGateway = googleIdentityGateway;
            _googleVinculoRepository = googleVinculoRepository;
            _autenticacaoRepository = autenticacaoRepository;
            _autenticacaoService = autenticacaoService;
            _senhaHasher = senhaHasher;
            _logService = logService;
        }

        /// <summary>Configuracao publica: o Client ID so vai quando habilitado; modoTeste so quando habilitado.</summary>
        public async Task<ReturnGoogleConfiguracaoVO> ObterConfiguracaoAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                GoogleConfiguracaoPublica configuracao = await _googleIdentityGateway.ObterConfiguracaoPublicaAsync(cancellationToken);

                ReturnGoogleConfiguracaoVO retorno = new ReturnGoogleConfiguracaoVO
                {
                    Habilitado = configuracao.Habilitado,
                    ClientId = configuracao.Habilitado ? configuracao.ClientId : null,
                    ModoTeste = configuracao.Habilitado && configuracao.ModoTeste
                };

                return retorno;
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
            catch (IntegrationException)
            {
                throw;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Cliente desistiu da requisicao: nao e falha do login nem entra no log.
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConfiguracaoGoogle, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Valida a prova e decide a situacao. Conta vinculada a cliente ativo entra direto (AUTENTICADO). Sem vinculo,
        /// conta cadastros ativos com o e-mail verificado: nenhum = CADASTRO_NECESSARIO; um = CONFIRMACAO_NECESSARIA;
        /// mais de um = CONTA_AMBIGUA. Nada e gravado aqui. A sessao do login direto e sempre a curta (campo 'lembrar' ignorado).
        /// </summary>
        public async Task<ReturnGoogleLoginVO> LoginAsync(GoogleLoginVO vo, CancellationToken cancellationToken = default)
        {
            try
            {
                GoogleIdentidade identidade = await _googleIdentityGateway.ValidarCredencialAsync(vo.Credencial, cancellationToken);

                Cliente? vinculado = await _googleVinculoRepository.FindClienteByGoogleSubAsync(identidade.Sub);

                if (vinculado != null)
                {
                    // Decisao do Kaio (C4): no login direto pelo Google a sessao e SEMPRE a curta; 'lembrar' e ignorado aqui.
                    // A prova do Google e reaproveitavel ate o exp; so o vinculo (com CPF e senha provados) pode lembrar.
                    ReturnLoginVO sessao = _autenticacaoService.EmitirSessaoCliente(vinculado, false);

                    _logService.WriteInfo(string.Format(ConstantsMessage.LogGoogleLoginAutenticado, vinculado.IdCliente), GetType().ToString());

                    return MontarRetorno(SituacaoLoginGoogle.AUTENTICADO, sessao, null, null);
                }

                List<Cliente> candidatos = await _googleVinculoRepository.FindClientesByEmailAsync(identidade.Email, ConstantsValues.QuantidadeClientesPorEmailGoogle);

                if (candidatos.Count == ConstantsValues.NenhumClienteGoogle)
                {
                    return MontarRetorno(SituacaoLoginGoogle.CADASTRO_NECESSARIO, null, identidade.Nome, identidade.Email);
                }

                if (candidatos.Count == ConstantsValues.ClienteUnicoGoogle)
                {
                    return MontarRetorno(SituacaoLoginGoogle.CONFIRMACAO_NECESSARIA, null, identidade.Nome, identidade.Email);
                }

                return MontarRetorno(SituacaoLoginGoogle.CONTA_AMBIGUA, null, null, null);
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
            catch (IntegrationException)
            {
                throw;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Cliente desistiu da requisicao: nao e falha do login nem entra no log.
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroLoginGoogle, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Vincula a conta Google ao cliente depois de provar CPF e senha, e abre a sessao. Ordem: prova do Google,
        /// cliente pelo CPF, senha, e-mail igual ao verificado, unicidade do e-mail (409), conta Google livre (409),
        /// cliente sem outra conta (409), gravacao. Mesmo sub ja gravado = idempotente.
        /// </summary>
        public async Task<ReturnLoginVO> VincularAsync(GoogleVinculoVO vo, CancellationToken cancellationToken = default)
        {
            try
            {
                GoogleIdentidade identidade = await _googleIdentityGateway.ValidarCredencialAsync(vo.Credencial, cancellationToken);

                string cpf = StaticMethods.OnlyDigits(vo.Cpf);
                Cliente? cliente = await _autenticacaoRepository.FindClienteByCpfAsync(cpf);

                if (cliente == null)
                {
                    _senhaHasher.SimularVerificacao(vo.Senha);
                    throw new UnauthorizedException(ConstantsMessage.ErroCredenciaisInvalidas);
                }

                string? senhaHash = cliente.SenhaHash;

                if (string.IsNullOrWhiteSpace(senhaHash))
                {
                    _senhaHasher.SimularVerificacao(vo.Senha);
                    throw new UnauthorizedException(ConstantsMessage.ErroCredenciaisInvalidas);
                }

                if (!_senhaHasher.Verify(vo.Senha, senhaHash))
                {
                    throw new UnauthorizedException(ConstantsMessage.ErroCredenciaisInvalidas);
                }

                if (!EmailDoClienteConfere(cliente, identidade.Email))
                {
                    throw new UnauthorizedException(ConstantsMessage.ErroCredenciaisInvalidas);
                }

                List<Cliente> candidatos = await _googleVinculoRepository.FindClientesByEmailAsync(identidade.Email, ConstantsValues.QuantidadeClientesPorEmailGoogle);

                if (candidatos.Count == ConstantsValues.NenhumClienteGoogle)
                {
                    throw new UnauthorizedException(ConstantsMessage.ErroCredenciaisInvalidas);
                }

                if (candidatos.Count > ConstantsValues.ClienteUnicoGoogle)
                {
                    throw new BusinessException(ConstantsMessage.ErroGoogleContaAmbigua);
                }

                bool contaJaVinculadaAOutro = await _googleVinculoRepository.ExistsClienteByGoogleSubAsync(identidade.Sub, cliente.IdCliente);

                if (contaJaVinculadaAOutro)
                {
                    throw new BusinessException(ConstantsMessage.ErroGoogleContaJaVinculada);
                }

                bool mesmoSub = string.Equals(cliente.GoogleSub, identidade.Sub, StringComparison.Ordinal);

                if (!mesmoSub && !string.IsNullOrWhiteSpace(cliente.GoogleSub))
                {
                    throw new BusinessException(ConstantsMessage.ErroGoogleClienteJaVinculado);
                }

                if (!mesmoSub)
                {
                    bool gravado = await _googleVinculoRepository.UpdateGoogleSubAsync(cliente.IdCliente, identidade.Sub);

                    if (!gravado)
                    {
                        throw new BusinessException(ConstantsMessage.ErroVincularGoogle);
                    }

                    _logService.WriteInfo(string.Format(ConstantsMessage.LogGoogleVinculoGravado, cliente.IdCliente), GetType().ToString());
                }

                ReturnLoginVO sessao = _autenticacaoService.EmitirSessaoCliente(cliente, vo.Lembrar);

                return sessao;
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
            catch (IntegrationException)
            {
                throw;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Cliente desistiu da requisicao: nao e falha do login nem entra no log.
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroVincularGoogle, GetType().ToString());
                throw;
            }
        }

        /// <summary>E-mail do cliente (minusculas, sem espacos) igual ao e-mail verificado do Google.</summary>
        private static bool EmailDoClienteConfere(Cliente cliente, string emailGoogle)
        {
            string emailCliente = NormalizarEmail(cliente.Email);
            string emailVerificado = NormalizarEmail(emailGoogle);

            return string.Equals(emailCliente, emailVerificado, StringComparison.Ordinal);
        }

        private static string NormalizarEmail(string email)
        {
            return email.Trim().ToLowerInvariant();
        }

        /// <summary>Monta o retorno da situacao. Sessao so em AUTENTICADO; nome e e-mail so vem do Google.</summary>
        private static ReturnGoogleLoginVO MontarRetorno(SituacaoLoginGoogle situacao, ReturnLoginVO? sessao, string? nome, string? email)
        {
            ReturnGoogleLoginVO retorno = new ReturnGoogleLoginVO
            {
                Situacao = situacao,
                Sessao = sessao,
                Nome = nome,
                Email = email
            };

            return retorno;
        }
    }
}
