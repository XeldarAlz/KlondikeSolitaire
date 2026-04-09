using System;
using MessagePipe;

namespace KlondikeSolitaire.Tests
{
    public sealed class TestSubscriber<T> : ISubscriber<T>
    {
        private Action<T> _handler;

        public IDisposable Subscribe(IMessageHandler<T> handler, params MessageHandlerFilter<T>[] filters)
        {
            _handler = handler.Handle;
            return new TestDisposable(() => _handler = null);
        }

        public void Trigger(T message)
        {
            _handler?.Invoke(message);
        }
    }

    public sealed class TestDisposable : IDisposable
    {
        private readonly Action _onDispose;

        public TestDisposable(Action onDispose)
        {
            _onDispose = onDispose;
        }

        public void Dispose()
        {
            _onDispose?.Invoke();
        }
    }
}
