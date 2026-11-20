using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Manutencao;
using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.Regras;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using FluentValidation;
using FluentValidation.Results;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Services
{
    /// <summary>
    /// Regras de negocio da manutencao (P8-BE-01, P8-BE-02 e P8-BE-03 e a lista e o resumo da semana 4). Confere, com o
    /// relogio do servidor, a data de entrada e a de saida (nao podem ser futuras), converte o tipo pelo nome, normaliza os
    /// textos e deixa a transacao, a trava e as recusas de data e de km para o repositorio. Calcula os alertas de revisao pela
    /// RegrasRevisao e a capa dos alertas e da lista pelo IFotoCapaVeiculoService. Registra no log uma linha informativa por
    /// ordem aberta e por ordem encerrada (ids e funcionario, sem placa, chassi ou renavam).
    /// </summary>
    public class ManutencaoService : IManutencaoService
    {
        private const char DigitoMinimo = '0';
        private const char DigitoMaximo = '9';

        private readonly IManutencaoRepository _manutencaoRepository;
        private readonly IMapper _mapper;
        private readonly IRelogio _relogio;
        private readonly IContextoEmpresa _contextoEmpresa;
        private readonly ILogService _logService;
        private readonly IFotoCapaVeiculoService _fotoCapaVeiculoService;

        public ManutencaoService(IManutencaoRepository manutencaoRepository, IMapper mapper, IRelogio relogio, IContextoEmpresa contextoEmpresa, ILogService logService, IFotoCapaVeiculoService fotoCapaVeiculoService)
        {
            _manutencaoRepository = manutencaoRepository;
            _mapper = mapper;
            _relogio = relogio;
            _contextoEmpresa = contextoEmpresa;
            _logService = logService;
            _fotoCapaVeiculoService = fotoCapaVeiculoService;
        }

        /// <summary>
        /// Abre a ordem: data de entrada ausente vale hoje e data futura e recusada (400); o tipo vai para o enum pelo nome; o
        /// repositorio faz a trava, a regra de troca para MANUTENCAO e a recusa de ordem aberta (404 e 409).
        /// </summary>
        public async Task<ReturnManutencaoVO> AbrirAsync(AbrirManutencaoVO abrirManutencaoVO)
        {
            try
            {
                DateOnly hoje = DateOnly.FromDateTime(_relogio.Agora);
                DateOnly dataEntrada = abrirManutencaoVO.DataEntrada ?? hoje;

                if (dataEntrada > hoje)
                {
                    throw CriarRecusaDeCampo(nameof(AbrirManutencaoVO.DataEntrada), ConstantsMessage.ErroManutencaoDataEntradaFutura);
                }

                if (!RegrasManutencao.TentarLerTipoPorNome(abrirManutencaoVO.Tipo, out TipoManutencao tipo))
                {
                    throw CriarRecusaDeCampo(nameof(AbrirManutencaoVO.Tipo), ConstantsMessage.ErroManutencaoTipoInvalido);
                }

                Manutencao manutencao = new Manutencao
                {
                    IdVeiculo = abrirManutencaoVO.IdVeiculo,
                    Tipo = tipo,
                    DataEntrada = dataEntrada,
                    Oficina = NormalizarTextoOpcional(abrirManutencaoVO.Oficina),
                    Descricao = NormalizarTextoOpcional(abrirManutencaoVO.Descricao)
                };

                Manutencao gravada = await _manutencaoRepository.AbrirAtomicamenteAsync(manutencao);
                ReturnManutencaoVO retorno = _mapper.Map<ReturnManutencaoVO>(gravada);

                string mensagem = string.Format(ConstantsMessage.InfoManutencaoAberta, gravada.IdManutencao, gravada.IdVeiculo, _contextoEmpresa.IdFuncionario);
                _logService.WriteInfo(mensagem, GetType().ToString());

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
                _logService.Write(ex, ConstantsMessage.ErroAbrirManutencao, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Encerra a ordem: quilometragem obrigatoria; data de saida ausente vale hoje e data futura e recusada (400). A
        /// comparacao com a entrada, o km do veiculo e a ordem ja encerrada ficam no repositorio, na transacao.
        /// </summary>
        public async Task<ReturnManutencaoVO> EncerrarAsync(int idManutencao, EncerrarManutencaoVO encerrarManutencaoVO)
        {
            try
            {
                if (!encerrarManutencaoVO.KmAtual.HasValue)
                {
                    throw CriarRecusaDeCampo(nameof(EncerrarManutencaoVO.KmAtual), ConstantsMessage.ErroManutencaoKmObrigatorio);
                }

                DateOnly hoje = DateOnly.FromDateTime(_relogio.Agora);
                DateOnly dataSaida = encerrarManutencaoVO.DataSaida ?? hoje;

                if (dataSaida > hoje)
                {
                    throw CriarRecusaDeCampo(nameof(EncerrarManutencaoVO.DataSaida), ConstantsMessage.ErroManutencaoDataSaidaFutura);
                }

                Manutencao encerrada = await _manutencaoRepository.EncerrarAtomicamenteAsync(idManutencao, dataSaida, encerrarManutencaoVO.KmAtual.Value, encerrarManutencaoVO.Custo);
                ReturnManutencaoVO retorno = _mapper.Map<ReturnManutencaoVO>(encerrada);

                string mensagem = string.Format(ConstantsMessage.InfoManutencaoEncerrada, encerrada.IdManutencao, encerrada.IdVeiculo, _contextoEmpresa.IdFuncionario);
                _logService.WriteInfo(mensagem, GetType().ToString());

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
                _logService.Write(ex, ConstantsMessage.ErroEncerrarManutencao, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Lista os alertas de revisao: o repositorio devolve os veiculos em VENCIDA ou PROXIMA e o total; aqui cada item
        /// recebe a base, a proxima revisao, quanto falta, o percentual do ciclo e a situacao (RegrasRevisao). O intervalo
        /// padrao vai na raiz e a capa de cada veiculo vai no item (falha do armazenamento vira erroFotoCapa, sem derrubar a lista).
        /// </summary>
        public async Task<ReturnAlertasRevisaoVO> ListarAlertasRevisaoAsync()
        {
            try
            {
                (List<VeiculoRevisaoRegistro> Itens, int Total) consulta = await _manutencaoRepository.ListarAlertasRevisaoAsync();

                ReturnAlertasRevisaoVO retorno = new ReturnAlertasRevisaoVO
                {
                    Total = consulta.Total,
                    IntervaloRevisaoKm = ConstantsValues.IntervaloRevisaoKm
                };

                foreach (VeiculoRevisaoRegistro registro in consulta.Itens)
                {
                    retorno.Itens.Add(MontarAlerta(registro));
                }

                if (retorno.Itens.Count > 0)
                {
                    List<int> idsVeiculos = retorno.Itens.Select(item => item.IdVeiculo).Distinct().ToList();
                    List<Veiculo> veiculos = await _manutencaoRepository.ListarVeiculosDosAlertasAsync(idsVeiculos);
                    await _fotoCapaVeiculoService.PreencherAsync(veiculos, retorno.Itens);
                }

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
                _logService.Write(ex, ConstantsMessage.ErroListarAlertasRevisao, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Lista paginada das ordens da locadora. Normaliza a busca (texto aparado, placa normalizada e codigo da ordem), converte
        /// o tipo e a situacao pelo nome, confere os limites (o validador ja confere antes; aqui fica a mesma regra), pede a
        /// pagina e o total ao repositorio e monta cada item com a capa. Falha do armazenamento vira erroFotoCapa no item (200).
        /// </summary>
        public async Task<PaginaVO<ReturnManutencaoListaVO>> ListarAsync(FiltroManutencaoVO filtro)
        {
            try
            {
                string? termoBusca = NormalizarTextoOpcional(filtro.Busca);

                if (termoBusca != null && termoBusca.Length > ConstantsValues.TamanhoMaximoBuscaManutencao)
                {
                    throw CriarRecusaDeCampo(nameof(FiltroManutencaoVO.Busca), string.Format(ConstantsMessage.ErroManutencaoBuscaTamanho, ConstantsValues.TamanhoMaximoBuscaManutencao));
                }

                if (filtro.IdVeiculo.HasValue && filtro.IdVeiculo.Value <= 0)
                {
                    throw CriarRecusaDeCampo(nameof(FiltroManutencaoVO.IdVeiculo), ConstantsMessage.ErroManutencaoVeiculoFiltroInvalido);
                }

                TipoManutencao? tipo = LerTipoDoFiltro(filtro.Tipo);
                bool? aberta = LerSituacaoDoFiltro(filtro.Situacao);
                string? textoBusca = termoBusca?.ToLowerInvariant();
                string placaBusca = StaticMethods.NormalizePlaca(termoBusca);
                int? idOsBusca = LerCodigoDaOs(termoBusca);

                (List<Manutencao> Itens, int Total) consulta = await _manutencaoRepository.ListarManutencoesAsync(textoBusca, placaBusca, idOsBusca, tipo, aberta, filtro.IdVeiculo, filtro.Deslocamento, filtro.TamanhoPagina);

                List<ReturnManutencaoListaVO> itens = _mapper.Map<List<ReturnManutencaoListaVO>>(consulta.Itens);
                List<Veiculo> veiculos = consulta.Itens.Select(ordem => ordem.Veiculo).ToList();
                await _fotoCapaVeiculoService.PreencherAsync(veiculos, itens);

                return new PaginaVO<ReturnManutencaoListaVO>(itens, consulta.Total, filtro.Pagina, filtro.TamanhoPagina);
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
                _logService.Write(ex, ConstantsMessage.ErroListarManutencoes, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Resumo do mes corrente: as ordens abertas da locadora (sem periodo) e o custo das ordens encerradas com data de saida
        /// no mes do relogio do projeto (IRelogio.Agora). Sem parametros.
        /// </summary>
        public async Task<ReturnResumoManutencaoVO> ResumoAsync()
        {
            try
            {
                PeriodoResumoManutencao periodo = RegrasResumoManutencao.PeriodoPadrao(_relogio.Agora);

                int ordensAbertas = await _manutencaoRepository.ContarOrdensAbertasAsync();
                decimal custoPeriodo = await _manutencaoRepository.SomarCustoEncerradasAsync(periodo.Inicio, periodo.Fim);

                ReturnResumoManutencaoVO retorno = new ReturnResumoManutencaoVO
                {
                    OrdensAbertas = ordensAbertas,
                    PeriodoInicio = periodo.Inicio,
                    PeriodoInicioStr = StaticMethods.FormateDateToPTBR(periodo.Inicio),
                    PeriodoFim = periodo.Fim,
                    PeriodoFimStr = StaticMethods.FormateDateToPTBR(periodo.Fim),
                    CustoPeriodo = custoPeriodo,
                    CustoPeriodoStr = StaticMethods.FormateToMonetary(custoPeriodo)
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
                _logService.Write(ex, ConstantsMessage.ErroResumoManutencao, GetType().ToString());
                throw;
            }
        }

        /// <summary>Monta um item do alerta com a regra pura de revisao. Funcao pura, sem acesso a dados.</summary>
        private static ReturnAlertaRevisaoVO MontarAlerta(VeiculoRevisaoRegistro registro)
        {
            int kmBase = RegrasRevisao.KmBase(registro.KmUltimaPreventiva);
            int kmProximaRevisao = RegrasRevisao.KmProximaRevisao(kmBase);
            int faltamKm = RegrasRevisao.FaltamKm(kmProximaRevisao, registro.KmAtual);

            return new ReturnAlertaRevisaoVO
            {
                IdVeiculo = registro.IdVeiculo,
                Placa = registro.Placa,
                Marca = registro.Marca,
                Modelo = registro.Modelo,
                CategoriaNome = registro.CategoriaNome,
                KmAtual = registro.KmAtual,
                KmUltimaPreventiva = kmBase,
                KmProximaRevisao = kmProximaRevisao,
                FaltamKm = faltamKm,
                PercentualCiclo = RegrasRevisao.PercentualCiclo(registro.KmAtual, kmBase),
                Situacao = RegrasRevisao.Situacao(faltamKm)
            };
        }

        /// <summary>Tipo pedido no filtro, pelo nome exato depois do Trim; vazio ou ausente nao filtra. Outro valor recusa (400).</summary>
        private static TipoManutencao? LerTipoDoFiltro(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return null;
            }

            if (!RegrasManutencao.TentarLerTipoPorNome(texto, out TipoManutencao tipo))
            {
                throw CriarRecusaDeCampo(nameof(FiltroManutencaoVO.Tipo), ConstantsMessage.ErroManutencaoTipoInvalido);
            }

            return tipo;
        }

        /// <summary>Situacao pedida no filtro: ABERTA vira verdadeiro, ENCERRADA falso; vazia ou ausente nao filtra. Outro valor recusa (400).</summary>
        private static bool? LerSituacaoDoFiltro(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return null;
            }

            string situacao = texto.Trim();

            if (string.Equals(situacao, ConstantsValues.SituacaoManutencaoAberta, StringComparison.Ordinal))
            {
                return true;
            }

            if (string.Equals(situacao, ConstantsValues.SituacaoManutencaoEncerrada, StringComparison.Ordinal))
            {
                return false;
            }

            throw CriarRecusaDeCampo(nameof(FiltroManutencaoVO.Situacao), ConstantsMessage.ErroManutencaoSituacaoInvalida);
        }

        /// <summary>
        /// Codigo da ordem pedido na busca ("OS #12", "OS 12", "#12" ou "12"): tira espacos e '#', tira o prefixo OS (sem
        /// diferenciar maiusculas), e so vale com digitos e numero que cabe em int. Sem codigo, nulo.
        /// </summary>
        private static int? LerCodigoDaOs(string? termo)
        {
            if (termo == null)
            {
                return null;
            }

            char[] caracteres = termo
                .Where(caractere => caractere != ConstantsValues.SinalCodigoManutencao && !char.IsWhiteSpace(caractere))
                .ToArray();

            string semSinais = new string(caracteres);

            if (semSinais.StartsWith(ConstantsValues.PrefixoCodigoOsManutencao, StringComparison.OrdinalIgnoreCase))
            {
                semSinais = semSinais.Substring(ConstantsValues.PrefixoCodigoOsManutencao.Length);
            }

            if (semSinais.Length == 0 || !semSinais.All(caractere => caractere >= DigitoMinimo && caractere <= DigitoMaximo))
            {
                return null;
            }

            if (int.TryParse(semSinais, NumberStyles.None, CultureInfo.InvariantCulture, out int codigo))
            {
                return codigo;
            }

            return null;
        }

        /// <summary>Remove os espacos das pontas; nulo ou vazio vira nulo. Funcao pura, sem acesso a dados.</summary>
        private static string? NormalizarTextoOpcional(string? texto)
        {
            if (texto == null)
            {
                return null;
            }

            string textoSemEspacos = texto.Trim();

            if (textoSemEspacos.Length == 0)
            {
                return null;
            }

            return textoSemEspacos;
        }

        /// <summary>Monta a excecao de validacao de um campo (400), para regras que dependem do relogio ou da ordem.</summary>
        private static ValidationException CriarRecusaDeCampo(string campo, string mensagem)
        {
            List<ValidationFailure> falhas = new List<ValidationFailure> { new ValidationFailure(campo, mensagem) };

            return new ValidationException(mensagem, falhas);
        }
    }
}
