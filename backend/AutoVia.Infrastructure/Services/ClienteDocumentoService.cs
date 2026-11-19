using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Clientes;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Options;
using AutoVia.Core.Regras;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Armazenamento;
using AutoVia.Integrations.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Services
{
    /// <summary>
    /// Documentos do cliente (RF-08). Ordem do envio: dono (sessao e rota, 404 sem armazenamento); cliente ativo; teto de envios
    /// em 24 horas (429, nada gravado); tipo pelo nome e tipo do conteudo pelos primeiros bytes; arquivo guardado como veio no
    /// armazenamento; uma gravacao no banco que desativa o ativo do mesmo tipo; arquivos das linhas desativadas removidos
    /// (falha so vai para o log). Falha da gravacao remove o arquivo novo e relanca. A listagem da equipe (P2-BE-05) usa a mesma
    /// montagem da listagem do dono; quem chama confere o vinculo com a locadora antes.
    /// </summary>
    public sealed class ClienteDocumentoService : IClienteDocumentoService
    {
        private readonly IClienteDocumentoRepository _clienteDocumentoRepository;
        private readonly IArmazenamentoGateway _armazenamentoGateway;
        private readonly IRelogio _relogio;
        private readonly ClientesOptions _clientesOptions;
        private readonly ILogService _logService;

        public ClienteDocumentoService(
            IClienteDocumentoRepository clienteDocumentoRepository,
            IArmazenamentoGateway armazenamentoGateway,
            IRelogio relogio,
            ClientesOptions clientesOptions,
            ILogService logService)
        {
            _clienteDocumentoRepository = clienteDocumentoRepository;
            _armazenamentoGateway = armazenamentoGateway;
            _relogio = relogio;
            _clientesOptions = clientesOptions;
            _logService = logService;
        }

        /// <summary>
        /// Envia o documento do cliente da sessao. O link do 201 vem do proprio envio ao armazenamento (ja assinado), sem uma
        /// segunda chamada depois da gravacao.
        /// </summary>
        public async Task<ReturnClienteDocumentoVO> EnviarAsync(int idClienteRota, int idClienteSessao, string? tipo, byte[]? conteudo, string? nomeOriginal, CancellationToken cancellationToken = default)
        {
            try
            {
                ConferirDono(idClienteRota, idClienteSessao);

                bool clienteAtivo = await _clienteDocumentoRepository.ClienteAtivoAsync(idClienteSessao, cancellationToken);
                if (!clienteAtivo)
                {
                    throw new NotFoundException(ConstantsMessage.ErroClienteNaoEncontrada);
                }

                DateTime desde = _relogio.Agora.AddHours(-ConstantsValues.JanelaEnviosDocumentoClienteHoras);
                int enviados = await _clienteDocumentoRepository.CountEnviadosDesdeWithDisabledsAsync(idClienteSessao, desde, cancellationToken);
                if (enviados >= _clientesOptions.LimiteEnviosDocumentoPorDia)
                {
                    throw new LimiteExcedidoException(ConstantsMessage.ErroLimiteEnviosDocumento);
                }

                TipoDocumentoCliente tipoDocumento = LerTipoOuRecusar(tipo);
                string tipoConteudo = LerTipoConteudoOuRecusar(conteudo);
                string nomeLimpo = RegrasClienteDocumento.LimparNomeArquivo(nomeOriginal);
                string chave = RegrasClienteDocumento.MontarChaveObjeto(idClienteSessao, Guid.NewGuid(), tipoConteudo);

                ArquivoArmazenado arquivo = await _armazenamentoGateway.EnviarAsync(chave, conteudo!, tipoConteudo, cancellationToken);

                ClienteDocumento novo = new ClienteDocumento
                {
                    IdCliente = idClienteSessao,
                    Tipo = tipoDocumento,
                    ChaveObjeto = chave,
                    NomeArquivo = nomeLimpo,
                    TipoConteudo = tipoConteudo,
                    TamanhoBytes = conteudo!.Length
                };

                ResultadoSubstituicaoClienteDocumento resultado = await GravarOuRemoverNovoAsync(novo, chave, cancellationToken);

                foreach (DocumentoClienteDesativado desativado in resultado.Desativados)
                {
                    await RemoverArquivoAntigoAsync(desativado);
                }

                return MontarRetorno(resultado.Gravado, arquivo.Url, arquivo.ExpiraEm);
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
            catch (ValidationException)
            {
                throw;
            }
            catch (LimiteExcedidoException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroEnviarClienteDocumento, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Lista os documentos ativos do cliente da sessao: no maximo um por tipo, CNH antes do comprovante. Sem documento
        /// devolve lista vazia sem chamar o armazenamento; item sem link assinado vem com url e expiraEm nulos.
        /// </summary>
        public async Task<List<ReturnClienteDocumentoVO>> ListarAsync(int idClienteRota, int idClienteSessao, CancellationToken cancellationToken = default)
        {
            try
            {
                ConferirDono(idClienteRota, idClienteSessao);

                bool clienteAtivo = await _clienteDocumentoRepository.ClienteAtivoAsync(idClienteSessao, cancellationToken);
                if (!clienteAtivo)
                {
                    throw new NotFoundException(ConstantsMessage.ErroClienteNaoEncontrada);
                }

                return await MontarListaAtivosAsync(idClienteSessao, cancellationToken);
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
                _logService.Write(ex, ConstantsMessage.ErroListarClienteDocumento, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Lista os documentos ativos de um cliente para a equipe da locadora. O acesso (vinculo ativo com a locadora) ja foi
        /// conferido por quem chama; aqui so se monta a lista, com a mesma regra da listagem do dono.
        /// </summary>
        public async Task<List<ReturnClienteDocumentoVO>> ListarDaEquipeAsync(int idCliente, CancellationToken cancellationToken = default)
        {
            try
            {
                return await MontarListaAtivosAsync(idCliente, cancellationToken);
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarClienteDocumento, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Monta a lista dos documentos ativos de um cliente: no maximo um por tipo (CNH antes do comprovante), com URL temporaria
        /// nova. Sem documento nao chama o armazenamento. Item sem link assinado vem com url e expiraEm nulos e so registra no log.
        /// </summary>
        private async Task<List<ReturnClienteDocumentoVO>> MontarListaAtivosAsync(int idCliente, CancellationToken cancellationToken)
        {
            List<ClienteDocumento> ativos = await _clienteDocumentoRepository.FindAtivosDoClienteAsync(idCliente, cancellationToken);
            List<ClienteDocumento> escolhidos = new List<ClienteDocumento>();

            foreach (TipoDocumentoCliente tipo in Enum.GetValues<TipoDocumentoCliente>())
            {
                ClienteDocumento? escolhido = RegrasClienteDocumento.EscolherAtivoPorTipo(ativos, tipo);
                if (escolhido != null)
                {
                    escolhidos.Add(escolhido);
                }
            }

            if (escolhidos.Count == 0)
            {
                return new List<ReturnClienteDocumentoVO>();
            }

            List<string> chaves = escolhidos.Select(documento => documento.ChaveObjeto).ToList();
            IReadOnlyDictionary<string, ArquivoArmazenado> assinados = await _armazenamentoGateway.AssinarUrlsAsync(chaves, cancellationToken);

            List<ReturnClienteDocumentoVO> lista = new List<ReturnClienteDocumentoVO>(escolhidos.Count);
            int semLink = 0;

            foreach (ClienteDocumento documento in escolhidos)
            {
                if (assinados.TryGetValue(documento.ChaveObjeto, out ArquivoArmazenado? arquivo) && arquivo != null)
                {
                    lista.Add(MontarRetorno(documento, arquivo.Url, arquivo.ExpiraEm));
                    continue;
                }

                semLink++;
                lista.Add(MontarRetorno(documento, null, null));
            }

            if (semLink > 0)
            {
                _logService.Write(
                    string.Format(CultureInfo.InvariantCulture, ConstantsMessage.ErroClienteDocumentoSemUrlAssinada, semLink),
                    ConstantsMessage.ErroListarClienteDocumento,
                    GetType().ToString());
            }

            return lista;
        }

        /// <summary>Dono: sessao sem cliente responde 401; id da rota ausente ou diferente do da sessao responde 404 (antes de qualquer armazenamento).</summary>
        private static void ConferirDono(int idClienteRota, int idClienteSessao)
        {
            if (idClienteSessao <= 0)
            {
                throw new UnauthorizedException(ConstantsMessage.ErroNaoAutenticado);
            }

            if (idClienteRota <= 0 || idClienteRota != idClienteSessao)
            {
                throw new NotFoundException(ConstantsMessage.ErroClienteNaoEncontrada);
            }
        }

        /// <summary>Tipo pelo nome exato. A recusa aqui e defesa: o validador HTTP ja recusou antes da action.</summary>
        private static TipoDocumentoCliente LerTipoOuRecusar(string? tipo)
        {
            if (!RegrasClienteDocumento.TentarLerTipoPorNome(tipo, out TipoDocumentoCliente tipoDocumento))
            {
                throw new ValidationException(new List<ValidationFailure>
                {
                    new ValidationFailure(ConstantsValues.CampoTipoDocumentoCliente, ConstantsMessage.ErroClienteDocumentoTipoInvalido)
                });
            }

            return tipoDocumento;
        }

        /// <summary>Tipo do conteudo pelos primeiros bytes (PDF, JPEG ou PNG). Vazio ou fora da tabela e recusado.</summary>
        private static string LerTipoConteudoOuRecusar(byte[]? conteudo)
        {
            if (conteudo == null || conteudo.Length == 0)
            {
                throw new ValidationException(new List<ValidationFailure>
                {
                    new ValidationFailure(ConstantsValues.CampoArquivoDocumentoCliente, ConstantsMessage.ErroClienteDocumentoArquivoObrigatorio)
                });
            }

            int tamanhoLeitura = Math.Min(conteudo.Length, ConstantsValues.AssinaturaPng.Length);
            string? tipoConteudo = RegrasArmazenamentoDisco.TipoPeloConteudo(conteudo.AsSpan(0, tamanhoLeitura));

            if (tipoConteudo == null)
            {
                throw new ValidationException(new List<ValidationFailure>
                {
                    new ValidationFailure(ConstantsValues.CampoArquivoDocumentoCliente, ConstantsMessage.ErroClienteDocumentoFormato)
                });
            }

            return tipoConteudo;
        }

        /// <summary>
        /// Uma gravacao no banco. Falha em qualquer ponto dela remove o arquivo novo (CancellationToken.None, de proposito) e
        /// relanca a excecao original; se a remocao tambem falhar, so registra no log.
        /// </summary>
        private async Task<ResultadoSubstituicaoClienteDocumento> GravarOuRemoverNovoAsync(ClienteDocumento novo, string chave, CancellationToken cancellationToken)
        {
            try
            {
                ResultadoSubstituicaoClienteDocumento resultado = await _clienteDocumentoRepository.SubstituirAsync(novo, cancellationToken);

                return resultado;
            }
            catch (Exception)
            {
                await RemoverArquivoNovoAposFalhaAsync(chave);
                throw;
            }
        }

        /// <summary>Remove o arquivo novo depois de uma gravacao que falhou. Falha da remocao so vai para o log (sem chave).</summary>
        private async Task RemoverArquivoNovoAposFalhaAsync(string chave)
        {
            try
            {
                await _armazenamentoGateway.RemoverAsync(chave, CancellationToken.None);
            }
            catch (Exception exRemocao)
            {
                _logService.Write(exRemocao.GetType().Name, ConstantsMessage.ErroRemoverArquivoNovoClienteDocumento, GetType().ToString());
            }
        }

        /// <summary>
        /// Remove do armazenamento o arquivo de um documento desativado pela substituicao. Falha so vai para o log, com o id do
        /// documento e o nome do tipo da excecao (nunca a chave); o envio continua e responde 201. Sem cancelamento: o banco ja mudou.
        /// </summary>
        private async Task RemoverArquivoAntigoAsync(DocumentoClienteDesativado desativado)
        {
            try
            {
                await _armazenamentoGateway.RemoverAsync(desativado.ChaveObjeto, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logService.Write(
                    ex.GetType().Name,
                    string.Format(CultureInfo.InvariantCulture, ConstantsMessage.ErroRemoverArquivoAntigoClienteDocumento, desativado.IdClienteDocumento),
                    GetType().ToString());
            }
        }

        /// <summary>Monta o VO de retorno. A data usa o formato de data e hora do projeto; o expiraEm usa o formato das fotos.</summary>
        private static ReturnClienteDocumentoVO MontarRetorno(ClienteDocumento documento, string? url, DateTimeOffset? expiraEm)
        {
            ReturnClienteDocumentoVO vo = new ReturnClienteDocumentoVO
            {
                IdClienteDocumento = documento.IdClienteDocumento,
                Tipo = documento.Tipo.ToString(),
                NomeArquivo = documento.NomeArquivo,
                TipoConteudo = documento.TipoConteudo,
                TamanhoBytes = documento.TamanhoBytes,
                DataEnvio = StaticMethods.FormateDateTimeToPTBR(documento.CreatedAt),
                Url = url,
                ExpiraEm = expiraEm.HasValue ? RegrasClienteDocumento.FormatarExpiraEm(expiraEm.Value) : null
            };

            return vo;
        }
    }
}
