using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Validators.CobrancasExtras;
using AutoVia.Communication.ViewObjects.CobrancasExtras;
using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using FluentValidation;

namespace AutoVia.Infrastructure.Services;

public sealed class ConfiguracoesCobrancasExtrasService : IConfiguracoesCobrancasExtrasService
{
    private readonly ICobrancasExtrasRepository _repository;
    private readonly ICategoriaRepository _categorias;
    private readonly IContextoEmpresa _contexto;
    private readonly IRelogio _relogio;
    private readonly IMapper _mapper;
    private readonly ILogService _logService;

    public ConfiguracoesCobrancasExtrasService(ICobrancasExtrasRepository repository, ICategoriaRepository categorias,
        IContextoEmpresa contexto, IRelogio relogio, IMapper mapper, ILogService logService)
    {
        _repository = repository; _categorias = categorias; _contexto = contexto;
        _relogio = relogio; _mapper = mapper; _logService = logService;
    }

    public async Task<PaginaVO<ReturnConfiguracaoCobrancaExtraVO>> ListarAsync(FiltroConfiguracaoCobrancaExtraVO filtro)
    {
        try
        {
            FiltroConfiguracaoCobrancaExtraVOValidator validator = new FiltroConfiguracaoCobrancaExtraVOValidator();
            await validator.ValidateAndThrowAsync(filtro);
            int total = await _repository.ContarConfiguracoesAsync(filtro.IdCategoria);
            List<ConfiguracaoCobrancaExtra> registros = await _repository.ListarConfiguracoesAsync(filtro.IdCategoria, filtro.Deslocamento, filtro.TamanhoPagina);
            List<ReturnConfiguracaoCobrancaExtraVO> itens = _mapper.Map<List<ReturnConfiguracaoCobrancaExtraVO>>(registros);
            return new PaginaVO<ReturnConfiguracaoCobrancaExtraVO>(itens, total, filtro.Pagina, filtro.TamanhoPagina);
        }
        catch (BusinessException) { throw; }
        catch (NotFoundException) { throw; }
        catch (Exception ex) { _logService.Write(ex, ConstantsMessage.ErroConfiguracaoCobrancasExtras, GetType().ToString()); throw; }
    }

    public async Task<ReturnConfiguracaoCobrancaExtraVO> BuscarAsync(int id)
    {
        try
        {
            ConfiguracaoCobrancaExtra configuracao = await ObterAsync(id);
            return _mapper.Map<ReturnConfiguracaoCobrancaExtraVO>(configuracao);
        }
        catch (BusinessException) { throw; }
        catch (NotFoundException) { throw; }
        catch (Exception ex) { _logService.Write(ex, ConstantsMessage.ErroConfiguracaoCobrancasExtras, GetType().ToString()); throw; }
    }

    public async Task<ReturnConfiguracaoCobrancaExtraVO> InserirAsync(ConfiguracaoCobrancaExtraVO model)
    {
        try
        {
            ConfiguracaoCobrancaExtraVOValidator validator = new ConfiguracaoCobrancaExtraVOValidator();
            await validator.ValidateAndThrowAsync(model);
            return await _repository.ComEmpresaBloqueadaAsync(async () =>
            {
                Categoria categoria = await ValidarCategoriaEUnicidadeAsync(model.IdCategoria!.Value, null);
                DateTime agora = _relogio.Agora;
                ConfiguracaoCobrancaExtra configuracao = new ConfiguracaoCobrancaExtra
                {
                    IdEmpresa = _contexto.IdEmpresa, Versao = 1, CreatedAt = agora, UpdatedAt = agora
                };
                Aplicar(configuracao, model);
                ConfiguracaoCobrancaExtra criada = await _repository.InserirConfiguracaoAsync(configuracao);
                criada.Categoria = categoria;
                return _mapper.Map<ReturnConfiguracaoCobrancaExtraVO>(criada);
            });
        }
        catch (BusinessException) { throw; }
        catch (NotFoundException) { throw; }
        catch (Exception ex) { _logService.Write(ex, ConstantsMessage.ErroConfiguracaoCobrancasExtras, GetType().ToString()); throw; }
    }

    public async Task AtualizarAsync(int id, ConfiguracaoCobrancaExtraVO model)
    {
        try
        {
            ConfiguracaoCobrancaExtraVOValidator validator = new ConfiguracaoCobrancaExtraVOValidator();
            await validator.ValidateAndThrowAsync(model);
            if (!model.VersaoEsperada.HasValue || model.VersaoEsperada.Value <= 0)
                throw new ValidationException(ConstantsMessage.ErroCobrancasExtrasParametros);
            await _repository.ComEmpresaBloqueadaAsync(async () =>
            {
                ConfiguracaoCobrancaExtra configuracao = await ObterAsync(id);
                if (configuracao.Versao != model.VersaoEsperada.Value || configuracao.Versao == int.MaxValue)
                    throw new BusinessException(ConstantsMessage.ErroConfiguracaoCobrancasExtrasVersao);
                await ValidarCategoriaEUnicidadeAsync(model.IdCategoria!.Value, id);
                Aplicar(configuracao, model);
                configuracao.Versao++;
                configuracao.UpdatedAt = _relogio.Agora;
                await _repository.SalvarConfiguracaoAsync(configuracao);
                return true;
            });
        }
        catch (BusinessException) { throw; }
        catch (NotFoundException) { throw; }
        catch (Exception ex) { _logService.Write(ex, ConstantsMessage.ErroConfiguracaoCobrancasExtras, GetType().ToString()); throw; }
    }

    public async Task DesativarAsync(int id)
    {
        try
        {
            await _repository.ComEmpresaBloqueadaAsync(async () =>
            {
                ConfiguracaoCobrancaExtra configuracao = await ObterAsync(id);
                DateTime agora = _relogio.Agora;
                configuracao.DisabledAt = agora;
                configuracao.UpdatedAt = agora;
                await _repository.SalvarConfiguracaoAsync(configuracao);
                return true;
            });
        }
        catch (BusinessException) { throw; }
        catch (NotFoundException) { throw; }
        catch (Exception ex) { _logService.Write(ex, ConstantsMessage.ErroConfiguracaoCobrancasExtras, GetType().ToString()); throw; }
    }

    private async Task<ConfiguracaoCobrancaExtra> ObterAsync(int id)
    {
        try { return await _repository.BuscarConfiguracaoAsync(id) ?? throw new NotFoundException(ConstantsMessage.ErroConfiguracaoCobrancasExtrasNaoEncontrada); }
        catch (BusinessException) { throw; }
        catch (NotFoundException) { throw; }
        catch (Exception ex) { _logService.Write(ex, ConstantsMessage.ErroConfiguracaoCobrancasExtras, GetType().ToString()); throw; }
    }

    private async Task<Categoria> ValidarCategoriaEUnicidadeAsync(int idCategoria, int? ignorarId)
    {
        try
        {
            Categoria categoria = await _categorias.FindByIdAsync(idCategoria) ?? throw new NotFoundException(ConstantsMessage.ErroConfiguracaoCobrancasExtrasNaoEncontrada);
            if (await _repository.ExisteConfiguracaoAsync(idCategoria, ignorarId)) throw new BusinessException(ConstantsMessage.ErroConfiguracaoCobrancasExtrasDuplicada);
            return categoria;
        }
        catch (BusinessException) { throw; }
        catch (NotFoundException) { throw; }
        catch (Exception ex) { _logService.Write(ex, ConstantsMessage.ErroConfiguracaoCobrancasExtras, GetType().ToString()); throw; }
    }

    private void Aplicar(ConfiguracaoCobrancaExtra destino, ConfiguracaoCobrancaExtraVO model)
    {
        try
        {
            destino.IdCategoria = model.IdCategoria!.Value;
            destino.BaseFranquia = model.BaseFranquia!;
            destino.FranquiaKm = model.FranquiaKm!.Value;
            destino.ValorKmExcedente = model.ValorKmExcedente!.Value;
            destino.ValorTanqueCompleto = model.ValorTanqueCompleto!.Value;
            destino.ToleranciaAtrasoMinutos = model.ToleranciaAtrasoMinutos!.Value;
            destino.UnidadeAtraso = model.UnidadeAtraso!;
            destino.FracionamentoAtraso = model.FracionamentoAtraso!;
            destino.ValorUnidadeAtraso = model.ValorUnidadeAtraso!.Value;
            destino.BaseAvarias = model.BaseAvarias!;
        }
        catch (Exception ex) { _logService.Write(ex, ConstantsMessage.ErroConfiguracaoCobrancasExtras, GetType().ToString()); throw; }
    }
}
