namespace AutoVia.Tests.Fakes
{
    /// <summary>
    /// TimeProvider de teste: GetUtcNow devolve o instante avancado pelo teste. Os timers usam o relogio do sistema, so para as
    /// esperas curtas de nova tentativa de arquivo (sem Thread.Sleep).
    /// </summary>
    public sealed class FakeTimeProvider : TimeProvider
    {
        private DateTimeOffset _agora;

        public FakeTimeProvider(DateTimeOffset agora)
        {
            _agora = agora;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _agora;
        }

        /// <summary>Avanca o relogio de teste pelo intervalo informado.</summary>
        public void Avancar(TimeSpan intervalo)
        {
            _agora = _agora.Add(intervalo);
        }

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            return TimeProvider.System.CreateTimer(callback, state, dueTime, period);
        }
    }
}
