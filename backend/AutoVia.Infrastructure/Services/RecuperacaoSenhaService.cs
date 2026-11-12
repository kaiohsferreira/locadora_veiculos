using AutoVia.Communication.Enums;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Autenticacao;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Options;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Services
{
    /// <summary>
    /// Recuperacao de senha por link de uso unico (N1, RF-49). O pedido so enfileira (sem consultar o banco nem
    /// enviar e-mail na requisicao): o processamento roda em segundo plano (RecuperacaoSenhaProcessador). A resposta
    /// e sempre a mesma (status, corpo e cabecalhos), com piso de tempo (TempoMinimoRespostaMs). O token e de 256
    /// bits; so o SHA-256 em hexadecimal vai ao banco. Nunca registra CPF, e-mail, token, hash, link ou senha.
    /// </summary>
    public class RecuperacaoSenhaService : IRecuperacaoSenhaService
    {
        private readonly IRedefinicaoSenhaRepository _redefinicaoSenhaRepository;
        private readonly ISenhaHasher _senhaHasher;
        private readonly ISessaoService _sessaoService;
        private readonly IRecuperacaoSenhaFila _filaPedidos;
        private readonly RecuperacaoSenhaOptions _options;
        private readonly ILogService _logService;

        public RecuperacaoSenhaService(
            IRedefinicaoSenhaRepository redefinicaoSenhaRepository,
            ISenhaHasher senhaHasher,
            ISessaoService sessaoService,
            IRecuperacaoSenhaFila filaPedidos,
            RecuperacaoSenhaOptions options,
            ILogService logService)
        {
            _redefinicaoSenhaRepository = redefinicaoSenhaRepository;
            _senhaHasher = senhaHasher;
            _sessaoService = sessaoService;
            _filaPedidos = filaPedidos;
            _options = options;
            _logService = logService;
        }

        /// <summary>
        /// Pedido de recuperacao: normaliza o CPF, enfileira o pedido e devolve sempre a mesma mensagem. Nao consulta o
        /// banco nem a fila de e-mail na requisicao. Fila cheia tem a mesma resposta e so vai para o log (sem CPF).
        /// Espera o que faltar para TempoMinimoRespostaMs em TODOS os caminhos, inclusive quando sai por excecao
        /// (a espera fica no finally; o erro continua sendo relancado depois dela).
        /// </summary>
        public async Task<ReturnRecuperacaoSolicitadaVO> SolicitarAsync(SolicitarRecuperacaoSenhaVO vo)
        {
            Stopwatch cronometro = Stopwatch.StartNew();

            try
            {
                string cpf = StaticMethods.OnlyDigits(vo.Cpf);

                PedidoRecuperacaoSenha pedido = new PedidoRecuperacaoSenha(vo.Perfil, cpf);

                bool enfileirado = _filaPedidos.Enfileirar(pedido);

                if (!enfileirado)
                {
                    _logService.Write(ConstantsMessage.ErroFilaRecuperacaoCheia, ConstantsMessage.ErroFilaRecuperacaoCheia, GetType().ToString());
                }

                ReturnRecuperacaoSolicitadaVO retorno = new ReturnRecuperacaoSolicitadaVO
                {
                    Mensagem = ConstantsMessage.InfoRecuperacaoSolicitada
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
            catch (UnauthorizedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSolicitarRecuperacaoSenha, GetType().ToString());
                throw;
            }
            finally
            {
                // Piso de tempo em todos os caminhos (sucesso, fila cheia e falha): o erro sai so depois da mesma espera.
                await AguardarTempoMinimoAsync(cronometro);
            }
        }

        /// <summary>
        /// Redefine a senha com o link de uso unico: link expirado, usado, adulterado, inventado ou de titular
        /// inativo recebe a mesma BusinessException (409). Grava o novo hash e derruba as sessoes depois do commit.
        /// Nao abre sessao e nao ativa conta.
        /// </summary>
        public async Task RedefinirAsync(RedefinirSenhaVO vo)
        {
            try
            {
                string tokenHash = CalcularHashToken(vo.Token);

                RedefinicaoSenha? redefinicao = await _redefinicaoSenhaRepository.FindPendenteByTokenHashAsync(tokenHash);

                if (redefinicao == null || redefinicao.ExpiraEm <= DateTime.Now)
                {
                    throw new BusinessException(ConstantsMessage.ErroRedefinicaoLinkInvalido);
                }

                TitularRecuperacao titular = ObterTitularAtivo(redefinicao);

                string novoSenhaHash = _senhaHasher.Hash(vo.NovaSenha);

                await _redefinicaoSenhaRepository.ConcluirRedefinicaoAsync(redefinicao, novoSenhaHash);

                _sessaoService.Invalidar(titular.Perfil, titular.IdUsuario);

                _logService.WriteInfo(ConstantsMessage.InfoSenhaRedefinida, GetType().ToString());
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (UnauthorizedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroRedefinirSenha, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// SHA-256 do token em hexadecimal minusculo (64 caracteres). Usado pelo processador (gravacao) e pela redefinicao
        /// (busca): o mesmo calculo nos dois lados. Interno ao modulo.
        /// </summary>
        internal static string CalcularHashToken(string token)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(token);
            byte[] hash = SHA256.HashData(bytes);

            string hexadecimal = Convert.ToHexString(hash).ToLowerInvariant();

            return hexadecimal;
        }

        /// <summary>Valida o titular do link: cadastro ativo, funcionario com Ativo e senha_hash preenchida. Qualquer falha vira a mesma BusinessException.</summary>
        private static TitularRecuperacao ObterTitularAtivo(RedefinicaoSenha redefinicao)
        {
            if (redefinicao.Cliente != null)
            {
                Cliente cliente = redefinicao.Cliente;

                if (cliente.DisabledAt != null || string.IsNullOrWhiteSpace(cliente.SenhaHash))
                {
                    throw new BusinessException(ConstantsMessage.ErroRedefinicaoLinkInvalido);
                }

                return TitularRecuperacao.DeCliente(cliente);
            }

            if (redefinicao.Funcionario != null)
            {
                Funcionario funcionario = redefinicao.Funcionario;

                if (funcionario.DisabledAt != null || !funcionario.Ativo || string.IsNullOrWhiteSpace(funcionario.SenhaHash))
                {
                    throw new BusinessException(ConstantsMessage.ErroRedefinicaoLinkInvalido);
                }

                return TitularRecuperacao.DeFuncionario(funcionario);
            }

            throw new BusinessException(ConstantsMessage.ErroRedefinicaoLinkInvalido);
        }

        /// <summary>
        /// Espera o que faltar para o piso TempoMinimoRespostaMs, medido desde o inicio do pedido. Repete ate
        /// o cronometro alcancar o piso: Task.Delay pode voltar um pouco antes do tempo pedido no Windows.
        /// </summary>
        private async Task AguardarTempoMinimoAsync(Stopwatch cronometro)
        {
            long faltam = _options.TempoMinimoRespostaMs - cronometro.ElapsedMilliseconds;

            while (faltam > ConstantsValues.TempoMinimoRespostaMsMinimo)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(faltam));

                faltam = _options.TempoMinimoRespostaMs - cronometro.ElapsedMilliseconds;
            }
        }

        /// <summary>Titular do link ja identificado na redefinicao: perfil e id usados para derrubar as sessoes.</summary>
        private sealed class TitularRecuperacao
        {
            private TitularRecuperacao(PerfilUsuario perfil, int idUsuario)
            {
                Perfil = perfil;
                IdUsuario = idUsuario;
            }

            public PerfilUsuario Perfil { get; }

            public int IdUsuario { get; }

            public static TitularRecuperacao DeCliente(Cliente cliente)
            {
                return new TitularRecuperacao(PerfilUsuario.CLIENTE, cliente.IdCliente);
            }

            public static TitularRecuperacao DeFuncionario(Funcionario funcionario)
            {
                return new TitularRecuperacao(PerfilUsuario.FUNCIONARIO, funcionario.IdFuncionario);
            }
        }
    }
}
