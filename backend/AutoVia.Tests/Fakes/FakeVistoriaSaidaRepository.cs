using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;

namespace AutoVia.Tests.Fakes;
public sealed class FakeVistoriaSaidaRepository : IVistoriaSaidaRepository
    {
        public readonly FakeVistoriaRepository Vistorias = new FakeVistoriaRepository();
        public readonly List<Avaria> Avarias = new List<Avaria>();
        /// <summary>Funcionarios que o Include(Funcionario) da leitura da saida encontra (o INSERT nao preenche a navegacao).</summary>
        public readonly List<Funcionario> Funcionarios = new List<Funcionario>();
        public Task<Vistoria?> FindSaidaByLocacaoAsync(int idLocacao)
        {
            Vistoria? saida = Vistorias.Vistorias.FirstOrDefault(x => x.IdLocacao == idLocacao && x.Tipo == TipoVistoria.SAIDA && x.DisabledAt == null);
            Funcionario? operador = saida == null ? null : Funcionarios.FirstOrDefault(x => x.IdFuncionario == saida.IdFuncionario);
            if (saida != null && operador != null && saida.Funcionario == null)
            {
                saida.Funcionario = operador;
            }
            return Task.FromResult(saida);
        }
        public Task<List<Avaria>> FindAvariasAtivasAsync(int idVistoria)
        {
            return Task.FromResult(Avarias.Where(x => x.IdVistoria == idVistoria && x.DisabledAt == null).ToList());
        }
        public async Task<Vistoria> InsertAsync(Vistoria vistoria)
        {
            Assert.True(Vistorias.SobTrava);
            Vistoria criada = await Vistorias.InsertAsync(vistoria);
            foreach (Avaria avaria in criada.Avarias)
            {
                avaria.IdAvaria = Avarias.Count + 1;
                avaria.IdVistoria = criada.IdVistoria;
                Avarias.Add(avaria);
            }
            return criada;
        }
    }
