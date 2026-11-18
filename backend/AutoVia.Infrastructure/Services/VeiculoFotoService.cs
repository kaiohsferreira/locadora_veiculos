using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Regras;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Armazenamento;
using AutoVia.Integrations.Interfaces;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Services
{
    /// <summary>
    /// Fotos do veiculo. Veiculo sempre conferido sob o filtro "Empresa" antes de qualquer foto, imagem ou armazenamento.
    /// Envio: armazenamento antes da gravacao; qualquer saida sem linha depois do envio remove o objeto (compensacao).
    /// Remocao: objeto no armazenamento primeiro, linha desativada depois. Autoria (funcionario) sempre do IContextoEmpresa.
    /// </summary>
    public sealed class VeiculoFotoService : IVeiculoFotoService
    {
        private readonly IVeiculoFotoRepository _veiculoFotoRepository;
        private readonly IProcessadorFotoService _processadorFotoService;
        private readonly IArmazenamentoGateway _armazenamentoGateway;
        private readonly IContextoEmpresa _contextoEmpresa;
        private readonly ILogService _logService;

        public VeiculoFotoService(
            IVeiculoFotoRepository veiculoFotoRepository,
            IProcessadorFotoService processadorFotoService,
            IArmazenamentoGateway armazenamentoGateway,
            IContextoEmpresa contextoEmpresa,
            ILogService logService)
        {
            _veiculoFotoRepository = veiculoFotoRepository;
            _processadorFotoService = processadorFotoService;
            _armazenamentoGateway = armazenamentoGateway;
            _contextoEmpresa = contextoEmpresa;
            _logService = logService;
        }

        /// <summary>
        /// Ordem: id do veiculo; sessao; veiculo ativo sob o filtro e contagem numa consulta so (404 sem o veiculo); teto (409);
        /// processamento da imagem; envio ao armazenamento; resumo e gravacao. Depois do envio, qualquer saida sem linha remove o
        /// objeto e relanca.
        /// </summary>
        public async Task<ReturnVeiculoFotoVO> EnviarAsync(int idVeiculo, byte[]? conteudo, string? contentType, CancellationToken cancellationToken = default)
        {
            try
            {
                if (idVeiculo <= 0)
                {
                    throw new NotFoundException(ConstantsMessage.ErroVeiculoNaoEncontrado);
                }

                int idFuncionario = ObterFuncionarioDoContexto();

                int? fotosAtivasAntes = await _veiculoFotoRepository.ContarAtivasDeVeiculoAtivoAsync(idVeiculo, cancellationToken);
                if (!fotosAtivasAntes.HasValue)
                {
                    throw new NotFoundException(ConstantsMessage.ErroVeiculoNaoEncontrado);
                }

                if (!RegrasVeiculoFoto.PodeReceberMaisFotos(fotosAtivasAntes.Value))
                {
                    throw new BusinessException(MontarMensagemLimiteAtingido());
                }

                FotoProcessada processada = await _processadorFotoService.ProcessarAsync(conteudo, contentType, cancellationToken);

                string chave = RegrasVeiculoFoto.MontarChaveObjeto(idVeiculo, System.Guid.NewGuid());
                ArquivoArmazenado arquivo = await _armazenamentoGateway.EnviarAsync(chave, processada.Conteudo, processada.ContentType, cancellationToken);

                return await GravarAposEnvioAsync(idVeiculo, idFuncionario, chave, arquivo, cancellationToken);
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
            catch (FluentValidation.ValidationException)
            {
                throw;
            }
            catch (LimiteExcedidoException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroEnviarFotoVeiculo, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Lista as fotos ativas do veiculo; sem foto devolve lista vazia sem chamar o armazenamento; com foto, uma assinatura em lote.
        /// Foto sem URL assinada (arquivo ausente) vem na mesma posicao com erroFoto e nao derruba a lista; a falha da assinatura inteira sobe.
        /// </summary>
        public async Task<List<ReturnVeiculoFotoVO>> GetAllAsync(int idVeiculo, CancellationToken cancellationToken = default)
        {
            try
            {
                if (idVeiculo <= 0)
                {
                    throw new NotFoundException(ConstantsMessage.ErroVeiculoNaoEncontrado);
                }

                bool veiculoExiste = await _veiculoFotoRepository.VeiculoAtivoExisteAsync(idVeiculo, cancellationToken);
                if (!veiculoExiste)
                {
                    throw new NotFoundException(ConstantsMessage.ErroVeiculoNaoEncontrado);
                }

                List<VeiculoFoto> fotos = await _veiculoFotoRepository.FindAtivasAsync(idVeiculo, cancellationToken);
                if (fotos.Count == 0)
                {
                    return new List<ReturnVeiculoFotoVO>();
                }

                List<string> chaves = fotos.Select(foto => foto.ChaveObjeto).ToList();
                IReadOnlyDictionary<string, ArquivoArmazenado> assinadas = await _armazenamentoGateway.AssinarUrlsAsync(chaves, cancellationToken);

                List<ReturnVeiculoFotoVO> lista = new List<ReturnVeiculoFotoVO>(fotos.Count);
                int semUrlAssinada = 0;
                foreach (VeiculoFoto foto in fotos)
                {
                    if (!assinadas.TryGetValue(foto.ChaveObjeto, out ArquivoArmazenado? arquivo) || arquivo == null)
                    {
                        lista.Add(MapearFotoSemUrl(foto));
                        semUrlAssinada++;
                        continue;
                    }

                    lista.Add(MapearFoto(foto, arquivo));
                }

                if (semUrlAssinada > 0)
                {
                    _logService.Write(
                        string.Format(CultureInfo.InvariantCulture, ConstantsMessage.ErroVeiculoFotoListaSemUrlAssinada, semUrlAssinada),
                        ConstantsMessage.ErroListarFotosVeiculo,
                        GetType().ToString());
                }

                return lista;
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
                _logService.Write(ex, ConstantsMessage.ErroListarFotosVeiculo, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Ordem: id do veiculo e da foto (404 sem banco); sessao; veiculo ativo sob o filtro (404); foto ativa do veiculo (404);
        /// remove o objeto (falha = 502, nada muda); desativa a linha gravando quem removeu (repetir da 404).
        /// </summary>
        public async Task RemoverAsync(int idVeiculo, int idVeiculoFoto, CancellationToken cancellationToken = default)
        {
            try
            {
                if (idVeiculo <= 0)
                {
                    throw new NotFoundException(ConstantsMessage.ErroVeiculoNaoEncontrado);
                }

                if (idVeiculoFoto <= 0)
                {
                    throw new NotFoundException(ConstantsMessage.ErroVeiculoFotoNaoEncontrada);
                }

                int idFuncionario = ObterFuncionarioDoContexto();

                bool veiculoExiste = await _veiculoFotoRepository.VeiculoAtivoExisteAsync(idVeiculo, cancellationToken);
                if (!veiculoExiste)
                {
                    throw new NotFoundException(ConstantsMessage.ErroVeiculoNaoEncontrado);
                }

                VeiculoFoto? foto = await _veiculoFotoRepository.FindAtivaAsync(idVeiculo, idVeiculoFoto, cancellationToken);
                if (foto == null)
                {
                    throw new NotFoundException(ConstantsMessage.ErroVeiculoFotoNaoEncontrada);
                }

                await _armazenamentoGateway.RemoverAsync(foto.ChaveObjeto, cancellationToken);

                bool desativada = await _veiculoFotoRepository.DesativarAsync(idVeiculo, idVeiculoFoto, idFuncionario, cancellationToken);
                if (!desativada)
                {
                    throw new NotFoundException(ConstantsMessage.ErroVeiculoFotoNaoEncontrada);
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
            catch (UnauthorizedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroRemoverFotoVeiculo, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Resumo das fotos ativas (total e maior ordem) numa consulta so: segunda conferencia do teto e gravacao com a proxima
        /// ordem. Qualquer falha depois do envio apaga o objeto recem-enviado (CancellationToken.None, de proposito) e relanca a
        /// excecao original; se a remocao tambem falhar, sobe a falha dela.
        /// </summary>
        private async Task<ReturnVeiculoFotoVO> GravarAposEnvioAsync(int idVeiculo, int idFuncionario, string chave, ArquivoArmazenado arquivo, CancellationToken cancellationToken)
        {
            VeiculoFoto gravada;
            try
            {
                ResumoFotosAtivasVeiculo resumo = await _veiculoFotoRepository.ObterResumoAtivasAsync(idVeiculo, cancellationToken);
                if (!RegrasVeiculoFoto.PodeReceberMaisFotos(resumo.Total))
                {
                    throw new BusinessException(MontarMensagemLimiteAtingido());
                }

                int ordem = RegrasVeiculoFoto.ProximaOrdemDepoisDe(resumo.MaiorOrdem);

                VeiculoFoto nova = new VeiculoFoto
                {
                    IdVeiculo = idVeiculo,
                    Ordem = ordem,
                    ChaveObjeto = chave,
                    ChaveMiniatura = null,
                    IdFuncionarioEnvio = idFuncionario,
                    IdFuncionarioRemocao = null
                };

                gravada = await _veiculoFotoRepository.InsertAsync(nova, cancellationToken);
            }
            catch (BusinessException)
            {
                await _armazenamentoGateway.RemoverAsync(chave, CancellationToken.None);
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroEnviarFotoVeiculo, GetType().ToString());
                await _armazenamentoGateway.RemoverAsync(chave, CancellationToken.None);
                throw;
            }

            return MapearFoto(gravada, arquivo);
        }

        /// <summary>Id do funcionario da sessao; sem empresa ou sem funcionario no contexto, sessao invalida (ramo defensivo).</summary>
        private int ObterFuncionarioDoContexto()
        {
            if (_contextoEmpresa.IdEmpresa <= 0 || _contextoEmpresa.IdFuncionario <= 0)
            {
                throw new UnauthorizedException(ConstantsMessage.ErroSessaoInvalida);
            }

            return _contextoEmpresa.IdFuncionario;
        }

        private static string MontarMensagemLimiteAtingido()
        {
            return string.Format(CultureInfo.InvariantCulture, ConstantsMessage.ErroVeiculoFotoLimiteAtingido, ConstantsValues.MaximoFotosPorVeiculo);
        }

        private static ReturnVeiculoFotoVO MapearFoto(VeiculoFoto foto, ArquivoArmazenado arquivo)
        {
            ReturnVeiculoFotoVO vo = new ReturnVeiculoFotoVO
            {
                IdVeiculoFoto = foto.IdVeiculoFoto,
                Ordem = foto.Ordem,
                Url = arquivo.Url,
                UrlMiniatura = null,
                ExpiraEm = RegrasVeiculoFoto.FormatarExpiraEm(arquivo.ExpiraEm)
            };

            return vo;
        }

        /// <summary>Foto sem URL assinada (arquivo ausente no armazenamento): url, urlMiniatura e expiraEm nulos e erroFoto com o texto do item.</summary>
        private static ReturnVeiculoFotoVO MapearFotoSemUrl(VeiculoFoto foto)
        {
            ReturnVeiculoFotoVO vo = new ReturnVeiculoFotoVO
            {
                IdVeiculoFoto = foto.IdVeiculoFoto,
                Ordem = foto.Ordem,
                Url = null,
                UrlMiniatura = null,
                ExpiraEm = null,
                // ponto unico de atribuicao de erroFoto
                ErroFoto = ConstantsMessage.ErroVeiculoFotoSemArquivo
            };

            return vo;
        }
    }
}
