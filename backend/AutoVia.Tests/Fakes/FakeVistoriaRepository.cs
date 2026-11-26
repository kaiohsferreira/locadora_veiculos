using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutoVia.Tests.Fakes
{
    public class FakeVistoriaRepository : IVistoriaRepository
    {
        private readonly SemaphoreSlim _trava = new SemaphoreSlim(1, 1);
        public List<Vistoria> Vistorias { get; } = new List<Vistoria>();
        public bool SobTrava { get; private set; }
        public bool LeituraSemTrava { get; private set; }

        /// <summary>Gancho opcional: roda depois de entrar na trava e antes da operacao (simula mudanca feita por outra requisicao).</summary>
        public Action? AoEntrarNaTrava { get; set; }

        /// <summary>Funcionarios que o Include(Funcionario) do repositorio real carrega na leitura da entrada (operadorStr).</summary>
        public List<Funcionario> Funcionarios { get; } = new List<Funcionario>();

        public Task<Vistoria?> FindEntradaByLocacaoAsync(int idLocacao)
        {
            LeituraSemTrava |= !SobTrava;
            Vistoria? vistoria = Vistorias.FirstOrDefault(x => x.IdLocacao == idLocacao && x.Tipo == TipoVistoria.ENTRADA && x.DisabledAt == null);
            // Simula o Include do repositorio real: a leitura traz o funcionario; o objeto que o INSERT devolve nao traz.
            Funcionario? funcionario = vistoria == null ? null : Funcionarios.FirstOrDefault(x => x.IdFuncionario == vistoria.IdFuncionario);
            if (vistoria != null && funcionario != null && vistoria.Funcionario == null)
            {
                vistoria.Funcionario = funcionario;
            }
            return Task.FromResult(vistoria);
        }

        public Task<Vistoria> InsertAsync(Vistoria vistoria)
        {
            Assert.True(SobTrava);
            vistoria.IdVistoria = Vistorias.Count + 1;
            Vistorias.Add(vistoria);
            return Task.FromResult(vistoria);
        }

        public async Task<T> ExecutarComLocacaoBloqueadaAsync<T>(int idLocacao, Func<Task<T>> operacao)
        {
            await _trava.WaitAsync();
            try
            {
                SobTrava = true;
                await Task.Yield();
                AoEntrarNaTrava?.Invoke();
                return await operacao();
            }
            finally
            {
                SobTrava = false;
                _trava.Release();
            }
        }
    }
}
