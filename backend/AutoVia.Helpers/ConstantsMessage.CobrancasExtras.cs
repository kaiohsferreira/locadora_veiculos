namespace AutoVia.Helpers;

public static partial class ConstantsMessage
{
    public const string AvisoCobrancasExtrasSimulacao = "Simulação provisória com a configuração cadastrada pela locadora; não lança cobranças nem encerra a locação. Valores de avarias são estimados.";
    public const string ErroSimularCobrancasExtras = "Não foi possível simular as cobranças extras.";
    public const string ErroCobrancasExtrasParametros = "Informe todos os parâmetros comerciais válidos; tarifas positivas com até duas casas decimais e máximo de 99999999,99.";
    public const string ErroCobrancasExtrasVistorias = "As vistorias estão incompletas ou inconsistentes para simulação.";
    public const string ErroCobrancasExtrasAvarias = "A comparação de avarias ou os valores estimados estão incompletos ou inconsistentes.";
    public const string ErroCobrancasExtrasLimite = "Uma categoria de cobrança excede o limite de 99999999,99.";
    public const string CobrancasExtrasHora = "HORA";
    public const string CobrancasExtrasDiaria = "DIARIA";
    public const string CobrancasExtrasProporcional = "PROPORCIONAL";
    public const string CobrancasExtrasIniciada = "INICIADA";
    public const string CobrancasExtrasTotal = "TOTAL";
    public const string CobrancasExtrasPorDiaria = "POR_DIARIA";
    public const string CobrancasExtrasEstimativaVistoria = "ESTIMATIVA_VISTORIA";
    public const string ErroConfiguracaoCobrancasExtras = "Não foi possível acessar a configuração de cobranças extras.";
    public const string ErroConfiguracaoCobrancasExtrasNaoEncontrada = "Configuração de cobranças extras não encontrada.";
    public const string ErroConfiguracaoCobrancasExtrasObrigatoria = "Cadastre uma configuração ativa de cobranças extras para a categoria desta locação.";
    public const string ErroConfiguracaoCobrancasExtrasDuplicada = "Já existe configuração ativa para esta categoria na locadora.";
    public const string ErroConfiguracaoCobrancasExtrasVersao = "A configuração foi alterada. Recarregue os dados e faça uma nova simulação.";
    public const string ErroApuracaoCobrancasExtras = "Não foi possível apurar as cobranças extras.";
    public const string ErroApuracaoCobrancasExtrasNaoEncontrada = "Apuração de cobranças extras não encontrada.";
    public const string ErroApuracaoCobrancasExtrasStatus = "A locação precisa estar em andamento ou atrasada para a primeira apuração.";
    public const string ErroApuracaoCobrancasExtrasConflito = "Já existem cobranças destas categorias sem vínculo com a apuração; confira os lançamentos antes de continuar.";
    public const string AvisoCobrancasExtrasApuradas = "Cobranças registradas com a configuração da locadora; valores de avarias baseados nas estimativas da vistoria.";
    public const string DescricaoCobrancaKm = "Quilometragem excedente apurada pela configuração da locadora.";
    public const string DescricaoCobrancaCombustivel = "Reposição de combustível apurada pela configuração da locadora.";
    public const string DescricaoCobrancaAtraso = "Atraso apurado pela configuração da locadora.";
    public const string DescricaoCobrancaAvaria = "Avarias novas apuradas pelas estimativas da vistoria de saída.";
}
