using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Validators.Contratos;
using AutoVia.Communication.ViewObjects.Contratos;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Regras;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using FluentValidation;
using FluentValidation.Results;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Services
{
    /// <summary>
    /// Contrato digital da locacao (RF-21, RP-06; contrato-locacao v5). GET: leitura pura (copia guardada ou documento montado).
    /// POST: assinatura sob a trava da locacao (VistoriaRepository.ExecutarComLocacaoBloqueadaAsync), com a ordem de checagens
    /// do contrato: RN-3, RN-1 (RP-06), RN-2 (nao aberta), aceite de combustivel (O-6), e depois a gravacao com o hash.
    /// </summary>
    public class ContratoLocacaoService : IContratoLocacaoService
    {
        private readonly IContratoLocacaoRepository _contratoRepository;
        private readonly IVistoriaRepository _vistoriaRepository;
        private readonly ICobrancasExtrasRepository _cobrancasRepository;
        private readonly IRelogio _relogio;
        private readonly ILogService _logService;

        public ContratoLocacaoService(IContratoLocacaoRepository contratoRepository, IVistoriaRepository vistoriaRepository,
            ICobrancasExtrasRepository cobrancasRepository, IRelogio relogio, ILogService logService)
        {
            _contratoRepository = contratoRepository;
            _vistoriaRepository = vistoriaRepository;
            _cobrancasRepository = cobrancasRepository;
            _relogio = relogio;
            _logService = logService;
        }

        public async Task<ReturnContratoLocacaoVO> ObterAsync(int idLocacao)
        {
            try
            {
                Locacao? locacao = await _contratoRepository.FindLocacaoParaContratoAsync(idLocacao);
                if (locacao == null)
                {
                    throw new NotFoundException(ConstantsMessage.ErroLocacaoNaoEncontrada);
                }

                ContratoLocacao? assinado = await _contratoRepository.FindAtivoByLocacaoAsync(idLocacao);
                if (assinado != null)
                {
                    return MontarRetornoAssinado(assinado);
                }

                ReturnContratoLocacaoVO pendente = await MontarPendenteAsync(locacao);

                return pendente;
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
                _logService.Write(ex, ConstantsMessage.ErroBuscarContratoLocacao, GetType().ToString());
                throw;
            }
        }

        public async Task<ReturnContratoLocacaoVO> AssinarAsync(int idLocacao, int idFuncionario, AssinarContratoLocacaoVO model)
        {
            try
            {
                ValidarEntradaDaAssinatura(model);
                ContratoLocacao gravado = await _vistoriaRepository.ExecutarComLocacaoBloqueadaAsync(idLocacao,
                    () => AssinarSobTravaAsync(idLocacao, idFuncionario, model));

                return MontarRetornoAssinado(gravado);
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
                _logService.Write(ex, ConstantsMessage.ErroAssinarContratoLocacao, GetType().ToString());
                throw;
            }
        }

        private async Task<ContratoLocacao> AssinarSobTravaAsync(int idLocacao, int idFuncionario, AssinarContratoLocacaoVO model)
        {
            try
            {
                Locacao? locacao = await _contratoRepository.FindLocacaoParaContratoAsync(idLocacao);
                if (locacao == null)
                {
                    throw new NotFoundException(ConstantsMessage.ErroLocacaoNaoEncontrada);
                }

                ContratoLocacao? ativo = await _contratoRepository.FindAtivoByLocacaoAsync(idLocacao);
                if (ativo != null)
                {
                    throw new BusinessException(ConstantsMessage.ErroContratoJaAssinado);
                }

                Vistoria? entrada = await _vistoriaRepository.FindEntradaByLocacaoAsync(idLocacao);
                ConferirEmissao(locacao, entrada);

                ConfiguracaoCobrancaExtra? configuracao = await _cobrancasRepository.BuscarConfiguracaoCategoriaAsync(locacao.Veiculo.IdCategoria, false);
                bool exigeCombustivel = RegrasContratoLocacao.ClausulaPoliticaCombustivelConsta(configuracao);
                string? erroAceite = RegrasContratoLocacao.ErroAceites(model.AceiteCondicoesGerais, model.AceitePoliticaCombustivel, exigeCombustivel);
                if (erroAceite != null)
                {
                    string campo = RegrasContratoLocacao.NomeCampoAceiteRecusado(model.AceiteCondicoesGerais);
                    throw new ValidationException(new List<ValidationFailure> { new ValidationFailure(campo, erroAceite) });
                }

                ContratoDocumentoVO documento = RegrasContratoLocacao.MontarDocumento(locacao.Veiculo.Empresa, locacao, locacao.Cliente,
                    locacao.Veiculo, entrada!, configuracao);
                string documentoJson = JsonSerializer.Serialize(documento, ConstantsValues.ContratoOpcoesJson);
                string hash = RegrasContratoLocacao.CalcularHashSha256(documentoJson);

                ContratoLocacao contrato = new ContratoLocacao
                {
                    IdLocacao = idLocacao,
                    IdFuncionario = idFuncionario,
                    DocumentoJson = documentoJson,
                    HashSha256 = hash,
                    NomeAssinante = model.NomeAssinante,
                    AceiteCondicoesGerais = true,
                    AceitePoliticaCombustivel = exigeCombustivel && model.AceitePoliticaCombustivel == true,
                    AssinaturaPngBase64 = model.AssinaturaPngBase64,
                    AssinadoEm = _relogio.Agora
                };

                await _contratoRepository.InsertAsync(contrato);

                // Relida na mesma transacao, com o funcionario do Include: o objeto do INSERT nao traz o nome de quem coletou.
                ContratoLocacao? gravado = await _contratoRepository.FindAtivoByLocacaoAsync(idLocacao);

                return gravado ?? contrato;
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
                _logService.Write(ex, ConstantsMessage.ErroAssinarContratoLocacao, GetType().ToString());
                throw;
            }
        }

        private async Task<ReturnContratoLocacaoVO> MontarPendenteAsync(Locacao locacao)
        {
            try
            {
                Vistoria? entrada = await _vistoriaRepository.FindEntradaByLocacaoAsync(locacao.IdLocacao);
                ConferirEmissao(locacao, entrada);

                ConfiguracaoCobrancaExtra? configuracao = await _cobrancasRepository.BuscarConfiguracaoCategoriaAsync(locacao.Veiculo.IdCategoria, false);
                ContratoDocumentoVO documento = RegrasContratoLocacao.MontarDocumento(locacao.Veiculo.Empresa, locacao, locacao.Cliente,
                    locacao.Veiculo, entrada!, configuracao);

                ReturnContratoLocacaoVO retorno = new ReturnContratoLocacaoVO
                {
                    IdLocacao = locacao.IdLocacao,
                    Situacao = ConstantsValues.ContratoSituacaoPendente,
                    HashSha256 = null,
                    Documento = documento,
                    Assinatura = null
                };

                return retorno;
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarContratoLocacao, GetType().ToString());
                throw;
            }
        }

        /// <summary>RN-1 (RP-06, vistoria de entrada ativa) e depois RN-2 (locacao em aberto), nessa ordem, nas duas rotas (O-6).</summary>
        private void ConferirEmissao(Locacao locacao, Vistoria? entrada)
        {
            try
            {
                if (entrada == null || !RegrasContratoLocacao.TemVistoriaEntradaAtiva(entrada, locacao.IdLocacao))
                {
                    throw new BusinessException(ConstantsMessage.ErroContratoSemVistoriaEntrada);
                }

                if (!RegrasContratoLocacao.LocacaoEmAberto(locacao.Status))
                {
                    throw new BusinessException(ConstantsMessage.ErroContratoLocacaoNaoAberta);
                }
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarContratoLocacao, GetType().ToString());
                throw;
            }
        }

        /// <summary>Confere de novo nome, imagem e aceite antes de gravar (defesa em profundidade; o validador do VO ja fez).</summary>
        private void ValidarEntradaDaAssinatura(AssinarContratoLocacaoVO model)
        {
            try
            {
                string? erroNome = RegrasFormatoAssinaturaContrato.ErroNomeAssinante(model.NomeAssinante);
                if (erroNome != null)
                {
                    throw new ValidationException(new List<ValidationFailure>
                    {
                        new ValidationFailure(nameof(AssinarContratoLocacaoVO.NomeAssinante), erroNome)
                    });
                }

                string? erroImagem = RegrasFormatoAssinaturaContrato.ErroAssinaturaPng(model.AssinaturaPngBase64);
                if (erroImagem != null)
                {
                    throw new ValidationException(new List<ValidationFailure>
                    {
                        new ValidationFailure(nameof(AssinarContratoLocacaoVO.AssinaturaPngBase64), erroImagem)
                    });
                }

                if (!model.AceiteCondicoesGerais)
                {
                    throw new ValidationException(new List<ValidationFailure>
                    {
                        new ValidationFailure(nameof(AssinarContratoLocacaoVO.AceiteCondicoesGerais), ConstantsMessage.ErroContratoAceiteObrigatorio)
                    });
                }
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroAssinarContratoLocacao, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Copia guardada do contrato assinado: o documento e lido do snapshot com as mesmas opcoes da gravacao, e o hash, a imagem
        /// e a assinatura voltam como estao. Nada e recalculado a partir dos dados vivos.
        /// </summary>
        private ReturnContratoLocacaoVO MontarRetornoAssinado(ContratoLocacao contrato)
        {
            try
            {
                ContratoDocumentoVO documento = JsonSerializer.Deserialize<ContratoDocumentoVO>(contrato.DocumentoJson, ConstantsValues.ContratoOpcoesJson)
                    ?? throw new InvalidOperationException(ConstantsMessage.ErroBuscarContratoLocacao);

                bool? aceiteCombustivel = documento.TabelaCobrancas != null ? contrato.AceitePoliticaCombustivel : (bool?)null;
                string nomeColetor = contrato.Funcionario != null ? contrato.Funcionario.Nome : string.Empty;

                ReturnContratoLocacaoVO retorno = new ReturnContratoLocacaoVO
                {
                    IdLocacao = contrato.IdLocacao,
                    Situacao = ConstantsValues.ContratoSituacaoAssinado,
                    HashSha256 = contrato.HashSha256,
                    Documento = documento,
                    Assinatura = new ContratoAssinaturaVO
                    {
                        NomeAssinante = contrato.NomeAssinante,
                        AceiteCondicoesGerais = contrato.AceiteCondicoesGerais,
                        AceitePoliticaCombustivel = aceiteCombustivel,
                        AssinaturaPngBase64 = contrato.AssinaturaPngBase64,
                        AssinadoEmStr = StaticMethods.FormateDateTimeToPTBR(contrato.AssinadoEm),
                        IdFuncionarioColetor = contrato.IdFuncionario,
                        NomeFuncionarioColetor = nomeColetor
                    }
                };

                return retorno;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarContratoLocacao, GetType().ToString());
                throw;
            }
        }
    }
}
