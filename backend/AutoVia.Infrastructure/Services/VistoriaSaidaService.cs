using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Locacoes;
using AutoVia.Communication.ViewObjects.Vistorias;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Regras;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Communication.Validators.Vistorias;
using FluentValidation;

namespace AutoVia.Infrastructure.Services;

public sealed class VistoriaSaidaService : IVistoriaSaidaService
{
    private readonly IVistoriaSaidaRepository _saidaRepository;
    private readonly IVistoriaRepository _vistoriaRepository;
    private readonly ILocacaoRepository _locacaoRepository;
    private readonly IRelogio _relogio;
    private readonly IMapper _mapper;
    private readonly ILogService _logService;
    private readonly IApuracaoCobrancasExtrasService _apuracao;
    private readonly ICaucaoDevolucaoService _caucao;

    public VistoriaSaidaService(IVistoriaSaidaRepository saidaRepository, IVistoriaRepository vistoriaRepository,
        ILocacaoRepository locacaoRepository, IRelogio relogio, IMapper mapper, ILogService logService,
        IApuracaoCobrancasExtrasService apuracao, ICaucaoDevolucaoService caucao)
    {
        _saidaRepository = saidaRepository;
        _vistoriaRepository = vistoriaRepository;
        _locacaoRepository = locacaoRepository;
        _relogio = relogio;
        _mapper = mapper;
        _logService = logService;
        _apuracao = apuracao;
        _caucao = caucao;
    }

    public async Task<ContextoDevolucaoVO> GetContextoAsync(int idLocacao)
    {
        try
        {
            Locacao locacao = await ObterLocacaoAsync(idLocacao);
            Vistoria? entrada = await _vistoriaRepository.FindEntradaByLocacaoAsync(idLocacao);
            Vistoria? saida = await _saidaRepository.FindSaidaByLocacaoAsync(idLocacao);
            List<Avaria> avariasEntrada = entrada == null ? new List<Avaria>() : await _saidaRepository.FindAvariasAtivasAsync(entrada.IdVistoria);
            ContextoDevolucaoVO contexto = new ContextoDevolucaoVO
            {
                Locacao = _mapper.Map<ReturnLocacaoVO>(locacao),
                Entrada = entrada == null ? null : _mapper.Map<ReturnVistoriaVO>(entrada),
                AvariasEntrada = _mapper.Map<List<AvariaComparacaoVO>>(avariasEntrada),
                Comparacao = saida == null ? null : await MontarComparacaoAsync(entrada, saida)
            };
            await PreencherAditivosDevolucaoAsync(contexto, locacao);
            return contexto;
        }
        catch (BusinessException) { throw; }
        catch (NotFoundException) { throw; }
        catch (Exception ex)
        {
            _logService.Write(ex, ConstantsMessage.ErroBuscarVistoriaSaida, GetType().ToString());
            throw;
        }
    }

    public async Task<ComparacaoSaidaVO> GetSaidaAsync(int idLocacao)
    {
        try
        {
            await ObterLocacaoAsync(idLocacao);
            Vistoria? saida = await _saidaRepository.FindSaidaByLocacaoAsync(idLocacao);
            if (saida == null) throw new NotFoundException(ConstantsMessage.ErroVistoriaSaidaNaoEncontrada);
            Vistoria? entrada = await _vistoriaRepository.FindEntradaByLocacaoAsync(idLocacao);
            return await MontarComparacaoAsync(entrada, saida);
        }
        catch (BusinessException) { throw; }
        catch (NotFoundException) { throw; }
        catch (Exception ex)
        {
            _logService.Write(ex, ConstantsMessage.ErroBuscarVistoriaSaida, GetType().ToString());
            throw;
        }
    }

    public async Task<ComparacaoSaidaVO> RegistrarSaidaAsync(int idLocacao, VistoriaSaidaVO model, int idFuncionario)
    {
        try
        {
            VistoriaSaidaVOValidator validator = new VistoriaSaidaVOValidator();
            await validator.ValidateAndThrowAsync(model);
            await _vistoriaRepository.ExecutarComLocacaoBloqueadaAsync(idLocacao,
                () => RegistrarSobTravaAsync(idLocacao, model, idFuncionario));
            // Relida com o Funcionario (Include do repositorio): o objeto da gravacao nao traz o operador (operadorStr no 201).
            Vistoria saidaGravada = await _saidaRepository.FindSaidaByLocacaoAsync(idLocacao)
                ?? throw new NotFoundException(ConstantsMessage.ErroVistoriaSaidaNaoEncontrada);
            Vistoria? entrada = await _vistoriaRepository.FindEntradaByLocacaoAsync(idLocacao);
            return await MontarComparacaoAsync(entrada, saidaGravada);
        }
        catch (BusinessException) { throw; }
        catch (NotFoundException) { throw; }
        catch (Exception ex)
        {
            _logService.Write(ex, ConstantsMessage.ErroSalvarVistoriaSaida, GetType().ToString());
            throw;
        }
    }

    private async Task<Vistoria> RegistrarSobTravaAsync(int idLocacao, VistoriaSaidaVO model, int idFuncionario)
    {
        try
        {
            Locacao locacao = await ObterLocacaoAsync(idLocacao);
            Funcionario? funcionario = await _locacaoRepository.FindActiveFuncionarioByIdAsync(idFuncionario);
            if (funcionario == null) throw new BusinessException(ConstantsMessage.ErroVistoriaFuncionarioInativo);
            if (!RegrasVistoria.LocacaoAceitaVistoria(locacao.Status))
                throw new BusinessException(ConstantsMessage.ErroVistoriaSaidaStatus);
            Vistoria? existente = await _saidaRepository.FindSaidaByLocacaoAsync(idLocacao);
            if (existente != null) throw new BusinessException(ConstantsMessage.ErroVistoriaSaidaDuplicada);
            Vistoria? entrada = await _vistoriaRepository.FindEntradaByLocacaoAsync(idLocacao);
            if (entrada == null) throw new BusinessException(ConstantsMessage.ErroVistoriaSaidaEntradaObrigatoria);
            if (model.KmRegistrado!.Value < Math.Max(locacao.KmSaida, entrada.KmRegistrado))
                throw new BusinessException(ConstantsMessage.ErroVistoriaSaidaKm);
            DateTime agora = _relogio.Agora;
            if (agora < entrada.DataHora || agora < locacao.DataRetirada)
                throw new BusinessException(ConstantsMessage.ErroVistoriaSaidaData);
            List<Avaria> avariasEntrada = await _saidaRepository.FindAvariasAtivasAsync(entrada.IdVistoria);
            Dictionary<int, Avaria> origens = avariasEntrada.ToDictionary(x => x.IdAvaria);
            Vistoria saida = new Vistoria
            {
                IdLocacao = idLocacao, IdFuncionario = idFuncionario, Tipo = TipoVistoria.SAIDA,
                DataHora = agora, KmRegistrado = model.KmRegistrado.Value, NivelCombustivel = model.NivelCombustivel!.Value,
                Observacoes = string.IsNullOrWhiteSpace(model.Observacoes) ? null : model.Observacoes.Trim(),
                ComparacaoRealizada = true, CreatedAt = agora, UpdatedAt = agora, DisabledAt = null
            };
            foreach (int idOrigem in model.IdsAvariasEntrada!)
            {
                if (!origens.TryGetValue(idOrigem, out Avaria? origem))
                    throw new NotFoundException(ConstantsMessage.ErroAvariaNaoEncontrada);
                saida.Avarias.Add(new Avaria
                {
                    IdAvariaEntrada = origem.IdAvaria, Descricao = origem.Descricao, Gravidade = origem.Gravidade,
                    ValorEstimado = origem.ValorEstimado, CreatedAt = agora, UpdatedAt = agora, DisabledAt = null
                });
            }
            foreach (NovaAvariaSaidaVO nova in model.NovasAvarias!)
            {
                saida.Avarias.Add(new Avaria
                {
                    Descricao = nova.Descricao.Trim(), Gravidade = nova.Gravidade!.Value, ValorEstimado = nova.ValorEstimado,
                    CreatedAt = agora, UpdatedAt = agora, DisabledAt = null
                });
            }
            return await _saidaRepository.InsertAsync(saida);
        }
        catch (BusinessException) { throw; }
        catch (NotFoundException) { throw; }
        catch (Exception ex)
        {
            _logService.Write(ex, ConstantsMessage.ErroSalvarVistoriaSaida, GetType().ToString());
            throw;
        }
    }

    /// <summary>
    /// Campos aditivos do GET devolucao (vistoria-saida v4, O-5): diarias, totais da caixa de confirmacao, politica, placa, marca,
    /// modelo e caucao. Nada grava, nada chama o provedor e nunca traz id_transacao. Totais nulos sem vistoria ou politica.
    /// </summary>
    private async Task PreencherAditivosDevolucaoAsync(ContextoDevolucaoVO contexto, Locacao locacao)
    {
        try
        {
            int diarias = RegrasDevolucao.CalcularDiarias(locacao.DataRetirada, locacao.DataDevolucaoPrevista);
            decimal totalDiarias = RegrasDevolucao.ValorDaDiaria(diarias, locacao.ValorDiaria);
            TotaisContextoApuracaoRegistro totais = await _apuracao.TotaisDoContextoAsync(locacao.IdLocacao);
            List<Pagamento> pagamentos = await _locacaoRepository.ListarPagamentosAtivosDaLocacaoAsync(locacao.IdLocacao);
            Pagamento? linhaCaucao = pagamentos.FirstOrDefault(x => x.Tipo == TipoPagamento.CAUCAO);

            contexto.DiariasPrevistas = diarias;
            contexto.ValorDiaria = locacao.ValorDiaria;
            contexto.ValorDiariaStr = StaticMethods.FormateToMonetary(locacao.ValorDiaria);
            contexto.TotalDiarias = totalDiarias;
            contexto.TotalDiariasStr = StaticMethods.FormateToMonetary(totalDiarias);
            contexto.TotalExtras = totais.TotalExtras;
            contexto.TotalExtrasStr = totais.TotalExtras.HasValue ? StaticMethods.FormateToMonetary(totais.TotalExtras.Value) : null;
            decimal? valorTotalPrevisto = totais.TotalExtras.HasValue
                ? RegrasDevolucao.CalcularValorTotal(diarias, locacao.ValorDiaria, totais.TotalExtras.Value)
                : null;
            contexto.ValorTotalPrevisto = valorTotalPrevisto;
            contexto.ValorTotalPrevistoStr = valorTotalPrevisto.HasValue ? StaticMethods.FormateToMonetary(valorTotalPrevisto.Value) : null;
            contexto.IdConfiguracao = totais.IdConfiguracao;
            contexto.VersaoEsperada = totais.VersaoEsperada;
            contexto.Placa = locacao.Veiculo.Placa;
            contexto.Marca = locacao.Veiculo.Marca;
            contexto.Modelo = locacao.Veiculo.Modelo;
            contexto.Caucao = new ReturnCaucaoContextoVO
            {
                Situacao = linhaCaucao == null ? AcaoCaucaoDevolucao.SEM_CAUCAO.ToString() : linhaCaucao.Status.ToString(),
                Acao = locacao.Status == StatusLocacao.FINALIZADA ? _caucao.AcaoDoBanco(linhaCaucao, locacao.ValorCaucao).ToString() : null
            };
        }
        catch (BusinessException) { throw; }
        catch (NotFoundException) { throw; }
        catch (Exception ex)
        {
            _logService.Write(ex, ConstantsMessage.ErroBuscarVistoriaSaida, GetType().ToString());
            throw;
        }
    }

    private async Task<Locacao> ObterLocacaoAsync(int idLocacao)
    {
        try
        {
            Locacao? locacao = await _locacaoRepository.FindParaEncerramentoAsync(idLocacao);
            if (locacao == null) throw new NotFoundException(ConstantsMessage.ErroLocacaoNaoEncontrada);
            return locacao;
        }
        catch (BusinessException) { throw; }
        catch (NotFoundException) { throw; }
        catch (Exception ex)
        {
            _logService.Write(ex, ConstantsMessage.ErroBuscarVistoriaSaida, GetType().ToString());
            throw;
        }
    }

    private async Task<ComparacaoSaidaVO> MontarComparacaoAsync(Vistoria? entrada, Vistoria saida)
    {
        try
        {
            List<Avaria> snapshots = await _saidaRepository.FindAvariasAtivasAsync(saida.IdVistoria);
            List<AvariaComparacaoVO> avarias = _mapper.Map<List<AvariaComparacaoVO>>(snapshots);
            return new ComparacaoSaidaVO
            {
                Entrada = entrada == null ? null : _mapper.Map<ReturnVistoriaVO>(entrada),
                Saida = _mapper.Map<ReturnVistoriaVO>(saida), ComparacaoRealizada = saida.ComparacaoRealizada,
                AvariasPreexistentes = saida.ComparacaoRealizada ? avarias.Where(x => x.IdAvariaEntrada.HasValue).ToList() : new List<AvariaComparacaoVO>(),
                AvariasNovas = saida.ComparacaoRealizada ? avarias.Where(x => !x.IdAvariaEntrada.HasValue).ToList() : new List<AvariaComparacaoVO>(),
                AvariasIndeterminadas = saida.ComparacaoRealizada ? new List<AvariaComparacaoVO>() : avarias
            };
        }
        catch (BusinessException) { throw; }
        catch (NotFoundException) { throw; }
        catch (Exception ex)
        {
            _logService.Write(ex, ConstantsMessage.ErroBuscarVistoriaSaida, GetType().ToString());
            throw;
        }
    }
}
