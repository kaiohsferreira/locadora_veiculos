using AutoVia.Communication.ViewObjects.Frota;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AutoVia.Core.ServicesInterface
{
    /// <summary>
    /// Fotos do veiculo: envio, listagem e remocao. Empresa e funcionario vem do IContextoEmpresa da sessao, nunca dos parametros.
    /// Veiculo de outra locadora responde NotFoundException, igual ao inexistente.
    /// </summary>
    public interface IVeiculoFotoService
    {
        /// <summary>Envia a foto (ja lida do formulario), grava a versao recodificada no armazenamento e devolve a foto com a URL temporaria.</summary>
        Task<ReturnVeiculoFotoVO> EnviarAsync(int idVeiculo, byte[]? conteudo, string? contentType, CancellationToken cancellationToken = default);

        /// <summary>Lista as fotos ativas do veiculo em ordem, com URL temporaria assinada em lote.</summary>
        Task<List<ReturnVeiculoFotoVO>> GetAllAsync(int idVeiculo, CancellationToken cancellationToken = default);

        /// <summary>Remove a foto: apaga o objeto no armazenamento e depois desativa a linha, gravando quem removeu.</summary>
        Task RemoverAsync(int idVeiculo, int idVeiculoFoto, CancellationToken cancellationToken = default);
    }
}
